
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
            DateTime dt = new DateTime(int.Parse(year.Value), int.Parse(month.Value), int.Parse(day.Value));

            List<Game> games = new List<Game>();
            foreach (XElement child in xml.Elements())
            {
                if (child.Name.LocalName == "game")
                {
                    Game game = TransformGame(child, dt);
                    games.Add(game);
                }
            }

            JsonSerializerSettings settings = new JsonSerializerSettings() { Formatting = Formatting.Indented };
            string json = JsonConvert.SerializeObject(games, settings);
            WriterReader reader = new WriterReader(typeof(Game));
            JArray array = JArray.Parse(json);
            foreach (JObject jobject in array)
            {
                reader.Add(jobject);
            }

            SqlBulkWriter writer = new SqlBulkWriter();
            writer.LoadAndMerge(reader, new BulkWriterSettings() { Store = new MlbType() });
        }

        private static Game TransformGame(XElement child, DateTime date)
        {
            XAttribute id = child.Attribute("id");
            XAttribute gameType = child.Attribute("game_type");
            XAttribute homeCode = child.Attribute("home_code");
            XAttribute awayCode = child.Attribute("away_code");
            XAttribute homeCity = child.Attribute("home_team_city");
            XAttribute awayCity = child.Attribute("away_team_city");
            XAttribute homeName = child.Attribute("home_team_name");
            XAttribute awayName = child.Attribute("away_team_name");
            XAttribute homeId = child.Attribute("home_team_id");
            XAttribute awayId = child.Attribute("away_team_id");

            Game game = new Game();
            game.GameId = id.Value;
            game.Date = date;
            game.GameType = (GameType)char.Parse(gameType.Value);
            game.AwayTeam = new Team() { Id = int.Parse(awayId.Value), Name = awayName.Value, City = awayCity.Value, Code = awayCode.Value };
            game.HomeTeam = new Team() { Id = int.Parse(homeId.Value), Name = homeName.Value, City = homeCity.Value, Code = homeCode.Value };
            game.HomeRecord = new Record();
            game.HomeRecord.Wins = int.Parse(child.Attribute("home_win").Value);
            game.HomeRecord.Losses = int.Parse(child.Attribute("home_loss").Value);
            game.AwayRecord = new Record();
            game.AwayRecord.Wins = int.Parse(child.Attribute("away_win").Value);
            game.AwayRecord.Losses = int.Parse(child.Attribute("away_loss").Value);

            foreach (XElement sub in child.Elements())
            {
                if (sub.Name.LocalName == "status")
                {
                    game.Innings = int.Parse(sub.Attribute("inning").Value);
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

        private static void TransformHomeRuns(Game game, XElement sub)
        {
            game.HomeRuns = new List<HomeRun>();
            foreach (XElement player in sub.Elements())
            {
                string code = player.Attribute("team_code").Value;
                HomeRun hr = new HomeRun();
                hr.GameId = game.GameId;
                hr.Inning = int.Parse(player.Attribute("inning").Value);
                hr.Runners = int.Parse(player.Attribute("runners").Value);
                hr.Team = game.HomeTeam.Code == code ? game.HomeTeam : game.AwayTeam;
                hr.Hitter = new Hitter()
                {
                    Id = int.Parse(player.Attribute("id").Value),
                    First = player.Attribute("first").Value,
                    Last = player.Attribute("last").Value,
                    Team = hr.Team
                };

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
                    Wins = int.Parse(sub.Attribute("wins").Value),
                    Losses = int.Parse(sub.Attribute("losses").Value),
                    Era = decimal.Parse(sub.Attribute("era").Value),
                    Saves = int.Parse(sub.Attribute("saves").Value),
                    Opportunities = int.Parse(sub.Attribute("svo").Value)
                };
            }
        }

        private static void TransformLosingPitcher(Game game, XElement sub)
        {
            game.LosingPitcher = new Pitcher();
            game.LosingPitcher.Id = int.Parse(sub.Attribute("id").Value);
            game.LosingPitcher.Last = sub.Attribute("last").Value;
            game.LosingPitcher.First = sub.Attribute("first").Value;
            game.LosingPitcher.Team = game.HomeScore.Runs > game.AwayScore.Runs ? game.AwayTeam : game.HomeTeam;
            game.LosingPitcherRecord = new Record()
            {
                Wins = int.Parse(sub.Attribute("wins").Value),
                Losses = int.Parse(sub.Attribute("losses").Value),
                Era = decimal.Parse(sub.Attribute("era").Value)
            };
        }

        private static void TransformWinningPitcher(Game game, XElement sub)
        {
            game.WinningPitcher = new Pitcher();
            game.WinningPitcher.Id = int.Parse(sub.Attribute("id").Value);
            game.WinningPitcher.Last = sub.Attribute("last").Value;
            game.WinningPitcher.First = sub.Attribute("first").Value;
            game.WinningPitcher.Team = game.HomeScore.Runs > game.AwayScore.Runs ? game.HomeTeam : game.AwayTeam;
            game.WinningPitcherRecord = new Record()
            {
                Wins = int.Parse(sub.Attribute("wins").Value),
                Losses = int.Parse(sub.Attribute("losses").Value),
                Era = decimal.Parse(sub.Attribute("era").Value)
            };
        }

        private static void TransformLinescore(Game game, XElement sub)
        {
            game.HomeScore = new Score();
            game.AwayScore = new Score();
            foreach (XElement line in sub.Elements())
            {
                if (line.Name.LocalName == "r")
                {
                    game.HomeScore.Runs = int.Parse(line.Attribute("home").Value);
                    game.AwayScore.Runs = int.Parse(line.Attribute("away").Value);
                }
                else if (line.Name.LocalName == "h")
                {
                    game.HomeScore.Hits = int.Parse(line.Attribute("home").Value);
                    game.AwayScore.Hits = int.Parse(line.Attribute("away").Value);
                }
                else if (line.Name.LocalName == "e")
                {
                    game.HomeScore.Errors = int.Parse(line.Attribute("home").Value);
                    game.AwayScore.Errors = int.Parse(line.Attribute("away").Value);
                }
                else if (line.Name.LocalName == "so")
                {
                    game.HomeScore.StrikeOuts = int.Parse(line.Attribute("home").Value);
                    game.AwayScore.StrikeOuts = int.Parse(line.Attribute("away").Value);
                }
                else if (line.Name.LocalName == "sb")
                {
                    game.HomeScore.StolenBases = int.Parse(line.Attribute("home").Value);
                    game.AwayScore.StolenBases = int.Parse(line.Attribute("away").Value);
                }
                else if (line.Name.LocalName == "hr")
                {
                    game.HomeScore.HomeRuns = int.Parse(line.Attribute("home").Value);
                    game.AwayScore.HomeRuns = int.Parse(line.Attribute("away").Value);
                }
            }
        }
    }
}
