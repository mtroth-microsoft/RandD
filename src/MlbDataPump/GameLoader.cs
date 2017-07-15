
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Infrastructure.DataAccess;
using MlbDataPump.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MlbDataPump
{
    /// <summary>
    /// game_type: R, S, F, L, and W
    /// </summary>
    internal static class GameLoader
    {
        private const string TimeZoneName = "Eastern Standard Time";

        internal static void Transform(FileMetadata metadata)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings() { Formatting = Formatting.Indented };
            BulkWriterSettings bulkWriteSettings = new BulkWriterSettings() { Store = new MlbType() };

            Model.FileStaging result = QueryHelper
                .Read<Model.FileStaging>(string.Format("Address eq '{0}'", metadata.Address))
                .ToList()
                .SingleOrDefault();
            XElement xml = XElement.Parse(result.Content);
            XAttribute year = xml.Attribute("year");
            XAttribute month = xml.Attribute("month");
            XAttribute day = xml.Attribute("day");
            DateTimeOffset dt = GetGameTimeBasis(year, month, day);

            List<object> executes = new List<object>();
            foreach (XElement child in xml.Elements())
            {
                if (child.Name.LocalName == "game")
                {
                    Game game = TransformGame(child, dt);
                    if (game.Innings > 0)
                    {
                        string json = JsonConvert.SerializeObject(game, settings);
                        JObject jobject = JObject.Parse(json);

                        WriterReader reader = new WriterReader(typeof(Game));
                        reader.Add(jobject);

                        SequenceExecutor<Game> sequencer = new SequenceExecutor<Game>(reader, bulkWriteSettings);
                        executes.Add(new ExecuteQuery(sequencer.Execute));
                    }
                }
            }

            SqlBulkWriter writer = new SqlBulkWriter();
            writer.LoadAndMergeInTransaction(executes, bulkWriteSettings);
        }

        internal static void HandlePreviews(FileMetadata metadata, XElement xml)
        {
            XAttribute year = xml.Attribute("year");
            XAttribute month = xml.Attribute("month");
            XAttribute day = xml.Attribute("day");
            DateTimeOffset dt = GetGameTimeBasis(year, month, day);

            List<Model.Preview> previews = new List<Preview>();
            foreach (XElement child in xml.Elements())
            {
                if (child.Name.LocalName == "game")
                {
                    int homeTeamId = ParseInt(child.Attribute("home_team_id"));
                    int awayTeamId = ParseInt(child.Attribute("away_team_id"));
                    TimeSpan tod = GetTimeOfDay(child.Attribute("time"), child.Attribute("time_zone"));
                    foreach (XElement sub in child.Elements())
                    {
                        if (sub.Name.LocalName == "status")
                        {
                            if (sub.Attribute("status").Value == "Preview" ||
                                sub.Attribute("status").Value == "InProgress")
                            {
                                XAttribute id = child.Attribute("id");
                                XAttribute gameType = child.Attribute("game_type");

                                Model.Preview preview = new Model.Preview();
                                preview.Id = IdUtil.GetGuidFromString(id.Value);
                                preview.GameId = id.Value;
                                preview.Date = dt + tod;
                                preview.TimeOfDay = tod.ToString();
                                preview.Address = metadata.Address;
                                preview.GameType = (Model.GameType)char.Parse(gameType.Value);
                                preview.AwayTeamId = awayTeamId;
                                preview.HomeTeamId = homeTeamId;
                                previews.Add(preview);
                            }
                        }
                    }
                }
            }

            if (previews.Count > 0)
            {
                QueryHelper.Write(previews);
            }
        }

        private static Game TransformGame(XElement child, DateTimeOffset date)
        {
            XAttribute id = child.Attribute("id");
            XAttribute gameType = child.Attribute("game_type");
            TimeSpan tod = GetTimeOfDay(child.Attribute("time"), child.Attribute("time_zone"));

            Game game = new Game();
            game.GameId = id.Value;
            game.Date = date + tod;
            game.TimeOfDay = tod.ToString();
            game.GameType = (GameType)char.Parse(gameType.Value);

            TransformTeams(child, game);
            foreach (XElement sub in child.Elements())
            {
                if (sub.Name.LocalName == "status")
                {
                    if (sub.Attribute("status").Value == "Final")
                    {
                        game.Innings = ParseInt(sub.Attribute("inning"));
                    }
                }
                else if (sub.Name.LocalName == "linescore")
                {
                    TransformLinescore(game, sub);
                }
                else if (sub.Name.LocalName == "winning_pitcher")
                {
                    TransformWinningPitcher(game, sub);
                }
                else if (sub.Name.LocalName == "losing_pitcher")
                {
                    TransformLosingPitcher(game, sub);
                }
                else if (sub.Name.LocalName == "save_pitcher")
                {
                    TransformSavePitcher(game, sub);
                }
                else if (sub.Name.LocalName == "home_runs")
                {
                    TransformHomeRuns(game, sub);
                }
            }

            return game;
        }

        private static void TransformTeams(XElement child, Game game)
        {
            TransformHomeTeam(child, game);
            TransformAwayTeam(child, game);
        }

        private static void TransformHomeTeam(XElement child, Game game)
        {
            game.HomeRecord = new Record();
            game.HomeRecord.Wins = ParseNullableInt(child.Attribute("home_win"));
            game.HomeRecord.Losses = ParseNullableInt(child.Attribute("home_loss"));

            game.HomeTeam = new Team()
            {
                Id = ParseInt(child.Attribute("home_team_id")),
                Name = child.Attribute("home_team_name").Value,
                City = child.Attribute("home_team_city").Value,
                Code = child.Attribute("home_code").Value,
                DivisionCode = child.Attribute("home_division").Value,
                LeagueId = ParseInt(child.Attribute("home_league_id"))
            };
        }

        private static void TransformAwayTeam(XElement child, Game game)
        {
            game.AwayRecord = new Record();
            game.AwayRecord.Wins = ParseNullableInt(child.Attribute("away_win"));
            game.AwayRecord.Losses = ParseNullableInt(child.Attribute("away_loss"));

            game.AwayTeam = new Team()
            {
                Id = ParseInt(child.Attribute("away_team_id")),
                Name = child.Attribute("away_team_name").Value,
                City = child.Attribute("away_team_city").Value,
                Code = child.Attribute("away_code").Value,
                DivisionCode = child.Attribute("away_division").Value,
                LeagueId = ParseInt(child.Attribute("away_league_id"))
            };
        }

        private static void TransformHomeRuns(Game game, XElement sub)
        {
            game.HomeRuns = new List<HomeRun>();
            foreach (XElement player in sub.Elements())
            {
                string id = player.Attribute("id").Value;
                string code = player.Attribute("team_code").Value;
                HomeRun hr = new HomeRun();
                hr.RawGameId = game.GameId;
                hr.Inning = int.Parse(player.Attribute("inning").Value);
                hr.Runners = int.Parse(player.Attribute("runners").Value);
                hr.Team = game.HomeTeam.Code == code ? game.HomeTeam : game.AwayTeam;
                if (string.IsNullOrEmpty(id) == false)
                {
                    hr.Hitter = new Hitter()
                    {
                        Id = int.Parse(player.Attribute("id").Value),
                        First = player.Attribute("first").Value,
                        Last = player.Attribute("last").Value,
                        Team = hr.Team
                    };
                }

                game.HomeRuns.Add(hr);
            }
        }

        private static void TransformSavePitcher(Game game, XElement sub)
        {
            string id = sub.Attribute("id").Value;
            if (string.IsNullOrEmpty(id) == false)
            {
                game.SavingPitcher = new Pitcher();
                game.SavingPitcher.Id = int.Parse(id);
                game.SavingPitcher.Last = sub.Attribute("last").Value;
                game.SavingPitcher.First = sub.Attribute("first").Value;
                game.SavingPitcher.Team = game.HomeScore.Runs > game.AwayScore.Runs ? game.HomeTeam : game.AwayTeam;
                game.SavingPitcherRecord = new Record()
                {
                    Wins = ParseNullableInt(sub.Attribute("wins")),
                    Losses = ParseNullableInt(sub.Attribute("losses")),
                    Era = ParseNullableDecimal(sub.Attribute("era")),
                    Saves = ParseNullableInt(sub.Attribute("saves")),
                    Opportunities = ParseNullableInt(sub.Attribute("svo"))
                };
            }
        }

        private static void TransformLosingPitcher(Game game, XElement sub)
        {
            string id = sub.Attribute("id").Value;
            if (string.IsNullOrEmpty(id) == false)
            {
                game.LosingPitcher = new Pitcher();
                game.LosingPitcher.Id = int.Parse(id);
                game.LosingPitcher.Last = sub.Attribute("last").Value;
                game.LosingPitcher.First = sub.Attribute("first").Value;
                game.LosingPitcher.Team = game.HomeScore.Runs > game.AwayScore.Runs ? game.AwayTeam : game.HomeTeam;
                game.LosingPitcherRecord = new Record()
                {
                    Wins = ParseNullableInt(sub.Attribute("wins")),
                    Losses = ParseNullableInt(sub.Attribute("losses")),
                    Era = ParseNullableDecimal(sub.Attribute("era"))
                };
            }
        }

        private static void TransformWinningPitcher(Game game, XElement sub)
        {
            string id = sub.Attribute("id").Value;
            if (string.IsNullOrEmpty(id) == false)
            {
                game.WinningPitcher = new Pitcher();
                game.WinningPitcher.Id = int.Parse(id);
                game.WinningPitcher.Last = sub.Attribute("last").Value;
                game.WinningPitcher.First = sub.Attribute("first").Value;
                game.WinningPitcher.Team = game.HomeScore.Runs > game.AwayScore.Runs ? game.HomeTeam : game.AwayTeam;
                game.WinningPitcherRecord = new Record()
                {
                    Wins = ParseNullableInt(sub.Attribute("wins")),
                    Losses = ParseNullableInt(sub.Attribute("losses")),
                    Era = ParseNullableDecimal(sub.Attribute("era"))
                };
            }
        }

        private static void TransformLinescore(Game game, XElement sub)
        {
            game.HomeScore = new Score();
            game.AwayScore = new Score();
            foreach (XElement line in sub.Elements())
            {
                if (line.Name.LocalName == "r")
                {
                    game.HomeScore.Runs = ParseInt(line.Attribute("home"));
                    game.AwayScore.Runs = ParseInt(line.Attribute("away"));
                }
                else if (line.Name.LocalName == "h")
                {
                    game.HomeScore.Hits = ParseInt(line.Attribute("home"));
                    game.AwayScore.Hits = ParseInt(line.Attribute("away"));
                }
                else if (line.Name.LocalName == "e")
                {
                    game.HomeScore.Errors = ParseInt(line.Attribute("home"));
                    game.AwayScore.Errors = ParseInt(line.Attribute("away"));
                }
                else if (line.Name.LocalName == "so")
                {
                    game.HomeScore.StrikeOuts = ParseInt(line.Attribute("home"));
                    game.AwayScore.StrikeOuts = ParseInt(line.Attribute("away"));
                }
                else if (line.Name.LocalName == "sb")
                {
                    game.HomeScore.StolenBases = ParseInt(line.Attribute("home"));
                    game.AwayScore.StolenBases = ParseInt(line.Attribute("away"));
                }
                else if (line.Name.LocalName == "hr")
                {
                    game.HomeScore.HomeRuns = ParseInt(line.Attribute("home"));
                    game.AwayScore.HomeRuns = ParseInt(line.Attribute("away"));
                }
            }
        }

        private static int ParseInt(XAttribute att)
        {
            if (att == null || string.IsNullOrEmpty(att.Value) == true)
            {
                return default(int);
            }
            else
            {
                return int.Parse(att.Value);
            }
        }

        private static int? ParseNullableInt(XAttribute att)
        {
            if (att == null || string.IsNullOrEmpty(att.Value) == true)
            {
                return null;
            }
            else
            {
                return int.Parse(att.Value);
            }
        }

        private static decimal? ParseNullableDecimal(XAttribute att)
        {
            decimal value;
            if (att == null || string.IsNullOrEmpty(att.Value) == true)
            {
                return null;
            }
            else if (decimal.TryParse(att.Value, out value) == true)
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        private static DateTimeOffset GetGameTimeBasis(XAttribute year, XAttribute month, XAttribute day)
        {
            DateTimeOffset dt = new DateTimeOffset(
                int.Parse(year.Value),
                int.Parse(month.Value),
                int.Parse(day.Value),
                0,
                0,
                0,
                GetOffset(TimeZoneName));

            return dt;
        }

        private static TimeSpan GetOffset(string tzName)
        {
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(tzName);
            DateTime now = TimeZoneInfo.ConvertTime(DateTime.Now, tz);
            TimeSpan offset = tz.GetUtcOffset(now);

            return offset;
        }

        private static TimeSpan GetTimeOfDay(XAttribute time, XAttribute timeZone)
        {
            DateTime timeOfDay = DateTime.Parse(time.Value + " PM");
            switch (timeZone.Value)
            {
                case "CT":
                    timeOfDay = timeOfDay.AddHours(1);
                    break;
                case "MT":
                    timeOfDay = timeOfDay.AddHours(2);
                    break;
                case "PT":
                    timeOfDay = timeOfDay.AddHours(3);
                    break;
            }

            return timeOfDay - DateTime.Parse("0:00 AM");
        }
    }
}
