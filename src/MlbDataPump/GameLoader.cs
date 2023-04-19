
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

            var xml = XElement.Parse(result.Content);
            var sections = xml.Descendants().Where(p => p.Name == "section" && p.Attribute("class") != null && p.Attribute("class").Value.StartsWith("Scoreboard"));

            List<Game> list = new List<Game>();
            List<object> executes = new List<object>();
            foreach (var match in sections)
            {
                ExecuteQuery query = HandleGame(match, metadata, list);
                if (query != null)
                {
                    executes.Add(query);
                }
            }

            SqlBulkWriter writer = new SqlBulkWriter();
            writer.LoadAndMergeInTransaction(executes, bulkWriteSettings);
        }

        private static ExecuteQuery HandleGame(XElement element, FileMetadata metadata, List<Game> list)
        {
            Game game = TransformGame(element, metadata, list);
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
            List<Model.Preview> previews = new List<Preview>();
            previews.AddRange(ConvertHtml(metadata));

            if (previews.Count > 0)
            {
                QueryHelper.Write(previews);
            }
        }

        private static Game TransformGame(XElement child, FileMetadata metadata, List<Game> list)
        {
            Game game = new Game();
            game.Date = metadata.EventDate.AddMinutes(719);
            game.TimeOfDay = game.Date.TimeOfDay.ToString();
            game.GameType = GetGameType(metadata);

            TransformTeams(child, game, list);

            if (game.HomeScore.Runs == game.AwayScore.Runs)
            {
                game.Innings = 0;
            }

            return game;
        }

        private static GameType GetGameType(FileMetadata metadata)
        {
            string filter = $"StartDate le '{metadata.EventDate.Date.ToString("yyyyMMdd")}' and EndDate ge '{metadata.EventDate.Date.ToString("yyyyMMdd")}'";
            Model.GameTypeMap result = QueryHelper
                .Read<Model.GameTypeMap>(filter)
                .ToList()
                .SingleOrDefault();

            return (GameType)Enum.Parse(typeof(GameType), result.GameTypeName);
        }

        private static void TransformTeams(XElement child, Game game, List<Game> list)
        {
            // Regex regex1 = new Regex("<ul class=\"ScoreboardScoreCell__Competitors\">(.*?)</ul>");
            var games = child.Descendants().Where(p => p.Name == "ul").Single();
            var xgame = games.Descendants().Where(p => p.Name == "div" && (p.Attribute("class")?.Value.StartsWith("ScoreCell__TeamName") ?? false));
            game.AwayTeam = LookupTeamId(xgame.First().Value);
            game.AwayTeamId = game.AwayTeam.Id;
            game.HomeTeam = LookupTeamId(xgame.Last().Value);
            game.HomeTeamId = game.HomeTeam.Id;

            var xrec = games.Descendants().Where(p => p.Name == "div" && (p.Attribute("class")?.Value.StartsWith("ScoreboardScoreCell__RecordContainer") ?? false));
            var xscore = games.Descendants().Where(p => p.Name == "div" && (p.Attribute("class")?.Value.StartsWith("ScoreboardScoreCell_Linescores") ?? false));
            game.AwayRecord = LookupRecord(xrec.First());
            game.HomeRecord = LookupRecord(xrec.Last());
            game.AwayScore = LookupScore(xscore.First());
            game.HomeScore = LookupScore(xscore.Last());

            var xperf = child.Descendants().Where(p => p.Name == "div" && (p.Attribute("class")?.Value.StartsWith("Scoreboard__Performers") ?? false));
            var pitchers = xperf.Descendants().Where(p => p.Name == "div" && (p.Attribute("class")?.Value.StartsWith("ContentList__Item") ?? false));
            XElement[] items = pitchers.ToArray();
            if (items.Length > 0)
            {
                TransformWinningPitcher(game, items[0]);
                TransformLosingPitcher(game, items[1]);
                if (items.Length > 2)
                {
                    TransformSavePitcher(game, items[2]);
                }
            }

            int count = list.Where(p => p.HomeTeam.Name == game.HomeTeam.Name && p.AwayTeam.Name == game.AwayTeam.Name).Count();
            game.GameId = $"{game.Date.Year}/{game.Date.Month}/{game.Date.Day}/{game.AwayTeam.Code}mlb-{game.HomeTeam.Code}mlb-{count + 1}";
            game.Innings = 9; // TODO: locate extra innings indicator?
            list.Add(game);
        }

        private static Score LookupScore(XElement child)
        {
            Score score = new Score();

            XElement[] elements = child.Descendants().ToArray();
            if (elements.Count() > 0)
            {
                score.Runs = int.Parse(elements[0].Value);
                score.Hits = int.Parse(elements[1].Value);
                score.Errors = int.Parse(elements[2].Value);
            }

            return score;
        }

        private static Record LookupRecord(XElement child)
        {
            Record record = new Record();
            string spans = child.ToString();
            Regex regex = new Regex("<span class=\"ScoreboardScoreCell__Record(.*?)</span>");
            Match match = regex.Match(spans);

            string winslosses = XElement.Parse(match.Value).Value;
            string[] items = winslosses.Split('-');
            record.Wins = int.Parse(items[0]);
            record.Losses = int.Parse(items[1]);

            return record;
        }

        private static void TransformSavePitcher(Game game, XElement sub)
        {
            Pitcher pitcher = new Pitcher();
            var name = sub.Descendants().Where(p => p.Name == "span" && (p.Attribute("class")?.Value.StartsWith("Athlete__PlayerName") ?? false));
            var stats = sub.Descendants().Where(p => p.Name == "span" && (p.Attribute("class")?.Value.StartsWith("Athlete__Stats") ?? false));
            if (game.AwayScore.Runs > game.HomeScore.Runs)
            {
                pitcher.Team = game.AwayTeam;
                pitcher.TeamId = game.AwayTeamId;
            }
            else
            {
                pitcher.Team = game.HomeTeam;
                pitcher.TeamId = game.HomeTeamId;
            }

            game.SavingPitcherName = name.Single().Value;
            ParsePitcherName(name.Single().Value, pitcher);
            game.SavingPitcher = pitcher;
            game.SavingPitcherRecord = new Record() { Saves = int.Parse(stats.Single().Value.Replace(")", string.Empty).Replace("(", string.Empty)) };
        }

        private static void TransformLosingPitcher(Game game, XElement sub)
        {
            Pitcher pitcher = new Pitcher();
            var name = sub.Descendants().Where(p => p.Name == "span" && (p.Attribute("class")?.Value.StartsWith("Athlete__PlayerName") ?? false));
            var stats = sub.Descendants().Where(p => p.Name == "span" && (p.Attribute("class")?.Value.StartsWith("Athlete__Stats") ?? false));
            if (game.AwayScore.Runs < game.HomeScore.Runs)
            {
                pitcher.Team = game.AwayTeam;
                pitcher.TeamId = game.AwayTeamId;
            }
            else
            {
                pitcher.Team = game.HomeTeam;
                pitcher.TeamId = game.HomeTeamId;
            }

            game.LosingPitcherName = name.Single().Value;
            ParsePitcherName(name.Single().Value, pitcher);
            game.LosingPitcher = pitcher;
            game.LosingPitcherRecord = ParsePitcherRecord(stats.Single().Value);
        }

        private static void TransformWinningPitcher(Game game, XElement sub)
        {
            Pitcher pitcher = new Pitcher();
            var name = sub.Descendants().Where(p => p.Name == "span" && (p.Attribute("class")?.Value.StartsWith("Athlete__PlayerName") ?? false));
            var stats = sub.Descendants().Where(p => p.Name == "span" && (p.Attribute("class")?.Value.StartsWith("Athlete__Stats") ?? false));
            if (game.AwayScore.Runs > game.HomeScore.Runs)
            {
                pitcher.Team = game.AwayTeam;
                pitcher.TeamId = game.AwayTeamId;
            }
            else
            {
                pitcher.Team = game.HomeTeam;
                pitcher.TeamId = game.HomeTeamId;
            }

            game.WinningPitcherName = name.Single().Value;
            ParsePitcherName(name.Single().Value, pitcher);
            game.WinningPitcher = pitcher;
            game.WinningPitcherRecord = ParsePitcherRecord(stats.Single().Value);
        }

        private static void ParsePitcherName(string value, Pitcher pitcher)
        {
            int endOfFirstName = value.IndexOf(". ");
            pitcher.Last = value.Substring(endOfFirstName + 2);
            pitcher.First = value.Replace(pitcher.Last, string.Empty).Trim();
        }

        private static Record ParsePitcherRecord(string value)
        {
            Record record = new Record();
            string[] items = value.Substring(1, value.Length - 2).Replace(",", string.Empty).Split(' ');

            if (decimal.TryParse(items[1], out decimal era))
            {
                record.Era = era;
            }

            string[] winslosses = items[0].Split('-');
            record.Wins = int.Parse(winslosses[0]);
            record.Losses = int.Parse(winslosses[1]);

            return record;
        }

        private static List<Preview> ConvertHtml(FileMetadata metadata)
        {
            Regex regex = new Regex("<div id=\"espnfitt\">(.*?)\n");
            Regex rx = new Regex("<section class=\"Scoreboard(.*?)</section>");
            var blob = regex.Match(metadata.Blob);
            XElement x = XElement.Parse(blob.Value.Replace("xlink:", string.Empty));
            var matches = rx.Matches(blob.Value);
            List<Preview> previews = new List<Preview>();

            foreach (Match match in matches)
            {
                Preview preview = new Preview();
                previews.Add(preview);
                preview.Date = metadata.EventDate.AddMinutes(719);
                preview.TimeOfDay = preview.Date.TimeOfDay.ToString();
                preview.GameType = GetGameType(metadata);
                preview.Address = metadata.AddressEx;

                Regex regex1 = new Regex("<ul class=\"ScoreboardScoreCell__Competitors\">(.*?)</ul>");
                var games = regex1.Match(match.Value).Value.Replace("xlink:", string.Empty);
                var xgame = XElement.Parse(games).Descendants().Where(p => p.Name == "div" && (p.Attribute("class")?.Value.StartsWith("ScoreCell__TeamName") ?? false));
                preview.AwayTeam = LookupTeamId(xgame.First().Value);
                preview.AwayTeamId = preview.AwayTeam.Id;
                preview.HomeTeam = LookupTeamId(xgame.Last().Value);
                preview.HomeTeamId = preview.HomeTeam.Id;

                int indexer = previews.Where(p => p.AwayTeamId == preview.AwayTeamId && p.HomeTeamId == preview.HomeTeamId).Count();
                TimeSpan offset = TimeSpan.FromHours(-7);
                preview.GameId = $"{preview.Date.ToOffset(offset).Year}/" +
                        $"{Normalize(preview.Date.ToOffset(offset).Month)}/" +
                        $"{Normalize(preview.Date.ToOffset(offset).Day)}/" +
                        $"{preview.AwayTeam.Code}mlb-{preview.HomeTeam.Code}mlb-{indexer}";
                preview.Id = IdUtil.GetGuidFromString(preview.GameId);

                if (indexer == 1)
                {
                    try
                    {
                        var val = match.Value.Replace("xlink:", string.Empty);
                        var xml = XElement.Parse(val);
                        var performers = xml.Descendants().Where(p => p.Name == "div" && p.Attribute("class")?.Value == "ContentList__Item");
                        if (performers.Count() == 2)
                        {
                            preview.AwayPitcher = ScrapePitcher(performers.First());
                            preview.HomePitcher = ScrapePitcher(performers.Last());
                        }
                    }
                    catch (System.Xml.XmlException) { }
                }
            }

            return previews;
        }

        private static string ScrapePitcher(XElement element)
        {
            var nameNode = element.Descendants().Where(p => p.Name == "span" && p.Attribute("class")?.Value == "Athlete__PlayerName").SingleOrDefault();
            var statNode = element.Descendants().Where(p => p.Name == "span" && (p.Attribute("class")?.Value.StartsWith("Athlete__Stats") ?? false)).SingleOrDefault();

            if (nameNode != null)
            {
                return $"{nameNode.Value} {statNode?.Value ?? string.Empty}";
            }
            else
            { 
                return null; 
            }
        }

        private static string Normalize(int value)
        {
            if (value < 10)
            {
                return $"0{value}";
            }

            return value.ToString();
        }

        private static Team LookupTeamId(string value)
        {
            string filter = string.Format("EspnName eq '{0}'", value);
            var results = QueryHelper.Read<Model.Team>(filter).ToList();
            if (results.Count == 1)
            {
                return results.First();
            }

            throw new IndexOutOfRangeException();
        }
    }
}
