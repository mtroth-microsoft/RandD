
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        private static JsonSerializerSettings settings = new JsonSerializerSettings() { Formatting = Formatting.Indented };

        private static BulkWriterSettings bulkWriteSettings = new BulkWriterSettings() { Store = new MlbType() };

        internal static void Transform(FileMetadata metadata)
        {
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
                    ExecuteQuery query = HandleGame(child, dt);
                    if (query != null)
                    {
                        executes.Add(query);
                    }
                }
            }

            SqlBulkWriter writer = new SqlBulkWriter();
            writer.LoadAndMergeInTransaction(executes, bulkWriteSettings);
        }

        private static ExecuteQuery HandleGame(XElement element, DateTimeOffset dt)
        {
            Game game = TransformGame(element, dt);
            if (game.Innings > 0)
            {
                string json = JsonConvert.SerializeObject(game, settings);
                JObject jobject = JObject.Parse(json);

                WriterReader reader = new WriterReader(typeof(Game));
                reader.Add(jobject);

                SequenceExecutor<Game> sequencer = new SequenceExecutor<Game>(reader, bulkWriteSettings);
                return new ExecuteQuery(sequencer.Execute);
            }

            return null;
        }

        internal static void HandlePreviews(FileMetadata metadata)
        {
            List<object> executes = new List<object>();
            List<Model.Preview> previews = new List<Preview>();
            if (metadata.Converted)
            {
                previews.AddRange(ConvertHtml(metadata));
            }
            else
            {
                XElement xml = XElement.Parse(metadata.Blob);
                XAttribute year = xml.Attribute("year");
                XAttribute month = xml.Attribute("month");
                XAttribute day = xml.Attribute("day");
                DateTimeOffset dt = GetGameTimeBasis(year, month, day);

                foreach (XElement child in xml.Elements())
                {
                    if (child.Name.LocalName == "game")
                    {
                        foreach (XElement sub in child.Elements())
                        {
                            if (sub.Name.LocalName == "status")
                            {
                                if (sub.Attribute("status").Value == "Preview" ||
                                    sub.Attribute("status").Value == "In Progress")
                                {
                                    Preview preview = TransformPreview(metadata, child, dt);
                                    previews.Add(preview);
                                    break;
                                }
                                else if (sub.Attribute("status").Value == "Final")
                                {
                                    ExecuteQuery query = HandleGame(child, dt);
                                    if (query != null)
                                    {
                                        executes.Add(query);
                                    }

                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (previews.Count > 0)
            {
                QueryHelper.Write(previews);
            }

            if (executes.Count > 0)
            {
                SqlBulkWriter writer = new SqlBulkWriter();
                writer.LoadAndMergeInTransaction(executes, bulkWriteSettings);
            }
        }

        private static Preview TransformPreview(FileMetadata metadata, XElement child, DateTimeOffset dt)
        {
            int homeTeamId = ParseInt(child.Attribute("home_team_id"));
            int awayTeamId = ParseInt(child.Attribute("away_team_id"));
            TimeSpan tod = GetTimeOfDay(child.Attribute("time"), child.Attribute("time_zone"));
            XAttribute id = child.Attribute("id");
            XAttribute gameType = child.Attribute("game_type");
            XElement homePitcher = child.Element("home_probable_pitcher");
            XElement awayPitcher = child.Element("away_probable_pitcher");

            Model.Preview preview = new Model.Preview();
            preview.Id = IdUtil.GetGuidFromString(id.Value);
            preview.GameId = id.Value;
            preview.Date = dt + tod;
            preview.TimeOfDay = tod.ToString();
            preview.Address = metadata.Address;
            preview.GameType = (GameType)char.Parse(gameType.Value);
            preview.AwayTeamId = awayTeamId;
            preview.HomeTeamId = homeTeamId;
            preview.HomePitcher = ShredPitcherPreview(homePitcher);
            preview.AwayPitcher = ShredPitcherPreview(awayPitcher);

            return preview;
        }

        private static string ShredPitcherPreview(XElement pitcher)
        {
            string pitcherData = null;
            if (pitcher != null)
            {
                pitcherData = string.Format(
                    "{0} {1} ({2}-{3} {4})",
                    pitcher.Attribute("first_name")?.Value,
                    pitcher.Attribute("last_name")?.Value,
                    pitcher.Attribute("wins")?.Value,
                    pitcher.Attribute("losses")?.Value,
                    pitcher.Attribute("era")?.Value);
            }

            return pitcherData == "  (- )" ? null : pitcherData;
        }

        private static Game TransformGame(XElement child, DateTimeOffset date)
        {
            XAttribute id = child.Attribute("id");
            XAttribute gameType = child.Attribute("game_type");
            TimeSpan tod = GetTimeOfDay(child.Attribute("home_time"), child.Attribute("time_zone"));

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
            game.HomeTeam.EspnName = game.HomeTeam.Name == "D-backs" ? "Diamondbacks" : game.HomeTeam.Name;
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
            game.AwayTeam.EspnName = game.AwayTeam.Name == "D-backs" ? "Diamondbacks" : game.AwayTeam.Name;
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
            DateTime timeOfDay;
            if (DateTime.TryParse(time.Value + " PM", out timeOfDay) == false)
            {
                return TimeSpan.FromMinutes(1439);
            }

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

        private static List<Preview> ConvertHtml(FileMetadata metadata)
        {
            Regex regex = new Regex("<script>(.*?)</script>");
            var matches = regex.Matches(metadata.Blob);
            List<Preview> previews = new List<Preview>();

            foreach (Match match in matches)
            {
                if (match.Value.Contains("window.espn.scoreboardData"))
                {
                    var parsed = match.Value.Replace("<script>", string.Empty).Replace("</script>", string.Empty);
                    parsed = parsed.Substring(parsed.IndexOf("{"));
                    parsed = parsed.Substring(0, parsed.IndexOf("};") + 1);
                    previews.AddRange(TransformPreview(metadata, parsed));
                }
            }

            return previews;
        }

        private static List<Preview> TransformPreview(FileMetadata metadata, string blob)
        {
            Dictionary<Guid, Preview> previews = new Dictionary<Guid, Preview>();
            JObject o = JObject.Parse(blob);
            JProperty evs = (JProperty)o.Children().Where(p => p.Path == "events").Single();
            foreach (var ev in evs.Values())
            {
                var indexer = 0;
                var competitions = ev.Children().Where(p => (p as JProperty)?.Name == "competitions").Single();
                foreach (var competition in competitions.Values())
                {
                    var gamedate = competition.Children().Where(p => (p as JProperty)?.Name == "date").Single() as JProperty;
                    var uid = competition.Children().Where(p => (p as JProperty)?.Name == "uid").Single() as JProperty;

                    Preview preview = new Preview();
                    preview.Date = DateTimeOffset.Parse(gamedate.Value.ToString());
                    preview.TimeOfDay = preview.Date.TimeOfDay.ToString();
                    preview.GameType = GameType.Regular;
                    preview.Address = metadata.AddressEx;
                    indexer++;

                    var competitors = competition.Children().Where(p => (p as JProperty)?.Name == "competitors").Single() as JProperty;
                    foreach (var competitor in competitors.Values())
                    {
                        var homeAway = competitor.Children().Where(p => (p as JProperty)?.Name == "homeAway").Single() as JProperty;
                        var team = competitor.Children().Where(p => (p as JProperty)?.Name == "team").Single() as JProperty;
                        var teamName = team.Values().Where(p => (p as JProperty).Name == "name").Single() as JProperty;
                        var probables = competitor.Children().Where(p => (p as JProperty)?.Name == "probables").SingleOrDefault() as JProperty;

                        if (homeAway.Value.ToString() == "home")
                        {
                            preview.HomeTeam = LookupTeamId(teamName.Value);
                            preview.HomeTeamId = preview.HomeTeam.Id;
                        }
                        else
                        {
                            preview.AwayTeam = LookupTeamId(teamName.Value);
                            preview.AwayTeamId = preview.AwayTeam.Id;
                        }

                        foreach (var probable in probables.EmptyIfNull().Values())
                        {
                            var shortDisplayName = probable.Children().Where(p => (p as JProperty)?.Name == "shortDisplayName").Single() as JProperty;
                            if (shortDisplayName.Value.ToString() == "Starter")
                            {
                                var athlete = probable.Children().Where(p => (p as JProperty)?.Name == "athlete").Single() as JProperty;
                                var displayName = athlete.Values().Where(p => (p as JProperty).Name == "displayName").Single() as JProperty;
                                var statistics = probable.Children().Where(p => (p as JProperty)?.Name == "statistics").Single() as JProperty;
                                int w = 0, l = 0;
                                string era = null;
                                foreach (var statistic in statistics.Values())
                                {
                                    switch ((statistic as JObject)["abbreviation"].ToString())
                                    {
                                        case "W": w = int.Parse((statistic as JObject)["displayValue"].ToString()); break;
                                        case "L": l = int.Parse((statistic as JObject)["displayValue"].ToString()); break;
                                        case "ERA": era = (statistic as JObject)["displayValue"].ToString(); break;
                                        default: break;
                                    }
                                }

                                var sp = $"{displayName.Value} ({w}-{l}, {era})";
                                if (homeAway.Value.ToString() == "home")
                                {
                                    preview.HomePitcher = sp;
                                }
                                else
                                {
                                    preview.AwayPitcher = sp;
                                }
                            }
                        }
                    }

                    preview.GameId = $"{preview.Date.ToLocalTime().Year}/{Normalize(preview.Date.ToLocalTime().Month)}/{Normalize(preview.Date.ToLocalTime().Day)}/{preview.AwayTeam.Code}mlb-{preview.HomeTeam.Code}mlb-{indexer}";
                    preview.Id = IdUtil.GetGuidFromString(preview.GameId);
                    preview.HomeTeam = null;
                    preview.AwayTeam = null;
                    previews[preview.Id] = preview;
                }
            }

            return previews.Values.ToList();
        }

        private static string Normalize(int value)
        {
            if (value < 10)
            {
                return $"0{value}";
            }

            return value.ToString();
        }

        private static Team LookupTeamId(JToken value)
        {
            string filter = string.Format("EspnName eq '{0}'", value.ToString());
            var results = QueryHelper.Read<Model.Team>(filter).ToList();
            if (results.Count == 1)
            {
                return results.First();
            }

            throw new IndexOutOfRangeException();
        }
    }
}
