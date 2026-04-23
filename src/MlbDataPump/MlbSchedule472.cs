using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MlbSchedule
{
    public class PitcherRecord
    {
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double ERA { get; set; }

        public PitcherRecord(int wins, int losses, double era)
        {
            Wins = wins;
            Losses = losses;
            ERA = era;
        }

        public override string ToString()
        {
            return $" ({Wins}-{Losses}, ERA: {ERA:F2})";
        }
    }

    public class ScheduledGame
    {
        public DateTimeOffset GameTime { get; set; }
        public string VisitingTeam { get; set; }
        public string HomeTeam { get; set; }
        public string VisitingPitcher { get; set; }
        public string HomePitcher { get; set; }
        public PitcherRecord VisitingPitcherRecord { get; set; }
        public PitcherRecord HomePitcherRecord { get; set; }

        public ScheduledGame(DateTimeOffset gameTime, string visitingTeam, string homeTeam,
            string visitingPitcher, string homePitcher,
            PitcherRecord visitingPitcherRecord = null, PitcherRecord homePitcherRecord = null)
        {
            GameTime = gameTime;
            VisitingTeam = visitingTeam;
            HomeTeam = homeTeam;
            VisitingPitcher = visitingPitcher;
            HomePitcher = homePitcher;
            VisitingPitcherRecord = visitingPitcherRecord;
            HomePitcherRecord = homePitcherRecord;
        }
    }

    public static class TeamNames
    {
        private static readonly Dictionary<string, string> SlugToName =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "ari", "Arizona Diamondbacks" },
                { "atl", "Atlanta Braves" },
                { "bal", "Baltimore Orioles" },
                { "bos", "Boston Red Sox" },
                { "chc", "Chicago Cubs" },
                { "chw", "Chicago White Sox" },
                { "cin", "Cincinnati Reds" },
                { "cle", "Cleveland Guardians" },
                { "col", "Colorado Rockies" },
                { "det", "Detroit Tigers" },
                { "hou", "Houston Astros" },
                { "kc", "Kansas City Royals" },
                { "laa", "Los Angeles Angels" },
                { "lad", "Los Angeles Dodgers" },
                { "mia", "Miami Marlins" },
                { "mil", "Milwaukee Brewers" },
                { "min", "Minnesota Twins" },
                { "nym", "New York Mets" },
                { "nyy", "New York Yankees" },
                { "oak", "Athletics" },
                { "ath", "Athletics" },
                { "phi", "Philadelphia Phillies" },
                { "pit", "Pittsburgh Pirates" },
                { "sd", "San Diego Padres" },
                { "sf", "San Francisco Giants" },
                { "sea", "Seattle Mariners" },
                { "stl", "St. Louis Cardinals" },
                { "tb", "Tampa Bay Rays" },
                { "tex", "Texas Rangers" },
                { "tor", "Toronto Blue Jays" },
                { "wsh", "Washington Nationals" },
            };

        public static string Resolve(string abbr)
        {
            string name;
            return SlugToName.TryGetValue(abbr, out name) ? name : abbr.ToUpper();
        }
    }

    public static class Scraper
    {
        public static async Task<List<ScheduledGame>> ScrapeScheduleAsync(string dateStr)
        {
            if (dateStr == null)
            {
                return new List<ScheduledGame>();
            }

            var url = "https://site.api.espn.com/apis/site/v2/sports/baseball/mlb/scoreboard?dates=" + dateStr;

            using (var client = new HttpClient())
            {
                var json = await client.GetStringAsync(url).ConfigureAwait(false);
                var doc = JObject.Parse(json);
                var games = new List<ScheduledGame>();

                var events = doc["events"] as JArray;
                if (events == null)
                    return games;

                var pacific = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

                foreach (var evt in events)
                {
                    var dateToken = (string)evt["date"];
                    DateTimeOffset gameTime;
                    if (dateToken != null)
                    {
                        // Parse honoring whatever offset ESPN provides (Z, +00:00, etc.)
                        var parsed = DateTimeOffset.Parse(dateToken, CultureInfo.InvariantCulture);
                        gameTime = TimeZoneInfo.ConvertTime(parsed, pacific);
                    }
                    else
                    {
                        gameTime = new DateTimeOffset(DateTime.ParseExact(dateStr, "yyyyMMdd",
                            CultureInfo.InvariantCulture), pacific.BaseUtcOffset);
                    }

                    var competitions = evt["competitions"] as JArray;
                    if (competitions == null)
                        continue;

                    foreach (var comp in competitions)
                    {
                        var competitors = comp["competitors"] as JArray;
                        if (competitors == null)
                            continue;

                        string homeTeam = null, visitingTeam = null;
                        string homePitcher = "Undecided", visitingPitcher = "Undecided";
                        PitcherRecord homeRecord = null, visitingRecord = null;

                        foreach (var competitor in competitors)
                        {
                            var homeAway = (string)competitor["homeAway"] ?? "";

                            var teamName = "";
                            var team = competitor["team"];
                            if (team != null)
                            {
                                var dn = (string)team["displayName"];
                                var abbr = (string)team["abbreviation"];
                                teamName = dn ?? (abbr != null ? TeamNames.Resolve(abbr) : "");
                            }

                            var pitcher = "Undecided";
                            PitcherRecord record = null;
                            var probables = competitor["probables"] as JArray;
                            if (probables != null && probables.Count > 0)
                            {
                                var prob = probables[0];
                                var athlete = prob["athlete"];
                                if (athlete != null)
                                {
                                    var pName = (string)athlete["displayName"];
                                    if (pName != null)
                                        pitcher = pName;
                                }

                                var stats = prob["statistics"] as JArray;
                                if (stats != null)
                                {
                                    record = ParsePitcherRecord(stats);
                                }
                            }

                            if (string.Equals(homeAway, "home", StringComparison.OrdinalIgnoreCase))
                            {
                                homeTeam = teamName;
                                homePitcher = pitcher;
                                homeRecord = record;
                            }
                            else
                            {
                                visitingTeam = teamName;
                                visitingPitcher = pitcher;
                                visitingRecord = record;
                            }
                        }

                        if (homeTeam != null && visitingTeam != null)
                        {
                            games.Add(new ScheduledGame(gameTime, visitingTeam, homeTeam,
                                visitingPitcher, homePitcher, visitingRecord, homeRecord));
                        }
                    }
                }

                return games;
            }
        }

        private static PitcherRecord ParsePitcherRecord(JArray stats)
        {
            int wins = 0, losses = 0;
            double era = 0.0;

            foreach (var stat in stats)
            {
                var name = (string)stat["name"] ?? "";
                var value = (string)stat["displayValue"] ?? "0";

                if (string.Equals(name, "wins", StringComparison.OrdinalIgnoreCase))
                    int.TryParse(value, out wins);
                else if (string.Equals(name, "losses", StringComparison.OrdinalIgnoreCase))
                    int.TryParse(value, out losses);
                else if (string.Equals(name, "ERA", StringComparison.OrdinalIgnoreCase))
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out era);
            }

            return new PitcherRecord(wins, losses, era);
        }
    }
}
