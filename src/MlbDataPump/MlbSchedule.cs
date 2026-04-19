using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace MlbSchedule;

public record ScheduledGame(
    DateTime GameTime,
    string VisitingTeam,
    string HomeTeam,
    string VisitingPitcher,
    string HomePitcher
);

public static class TeamNames
{
    private static readonly Dictionary<string, string> SlugToName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ari"] = "Arizona Diamondbacks",
        ["atl"] = "Atlanta Braves",
        ["bal"] = "Baltimore Orioles",
        ["bos"] = "Boston Red Sox",
        ["chc"] = "Chicago Cubs",
        ["chw"] = "Chicago White Sox",
        ["cin"] = "Cincinnati Reds",
        ["cle"] = "Cleveland Guardians",
        ["col"] = "Colorado Rockies",
        ["det"] = "Detroit Tigers",
        ["hou"] = "Houston Astros",
        ["kc"] = "Kansas City Royals",
        ["laa"] = "Los Angeles Angels",
        ["lad"] = "Los Angeles Dodgers",
        ["mia"] = "Miami Marlins",
        ["mil"] = "Milwaukee Brewers",
        ["min"] = "Minnesota Twins",
        ["nym"] = "New York Mets",
        ["nyy"] = "New York Yankees",
        ["oak"] = "Athletics",
        ["ath"] = "Athletics",
        ["phi"] = "Philadelphia Phillies",
        ["pit"] = "Pittsburgh Pirates",
        ["sd"] = "San Diego Padres",
        ["sf"] = "San Francisco Giants",
        ["sea"] = "Seattle Mariners",
        ["stl"] = "St. Louis Cardinals",
        ["tb"] = "Tampa Bay Rays",
        ["tex"] = "Texas Rangers",
        ["tor"] = "Toronto Blue Jays",
        ["wsh"] = "Washington Nationals",
    };

    public static string Resolve(string abbr) =>
        SlugToName.TryGetValue(abbr, out var name) ? name : abbr.ToUpper();
}

public static class Scraper
{
    private static readonly Regex TeamSlugPattern =
        new(@"/mlb/team/_/name/([^/]+)", RegexOptions.Compiled);

    private static readonly Regex PlayerLinkPattern =
        new(@"/mlb/player/", RegexOptions.Compiled);

    public static async Task<List<ScheduledGame>> ScrapeScheduleAsync(string dateStr)
    {
        var url = $"https://www.espn.com/mlb/schedule/_/date/{dateStr}";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36");

        var html = await client.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var currentDate = DateOnly.ParseExact(dateStr, "yyyyMMdd");

        var games = new List<ScheduledGame>();

        // The page shows multiple dates. Each date section is a ResponsiveTable div
        // with an optional .Table__Title containing the date (e.g. "Saturday, April 18, 2026").
        // Tables without a title inherit the previous date.
        var tableDivs = doc.DocumentNode.SelectNodes(
            "//div[contains(@class,'ResponsiveTable')]");

        if (tableDivs == null)
            return games;

        foreach (var tableDiv in tableDivs)
        {
            var titleNode = tableDiv.SelectSingleNode(
                ".//div[contains(@class,'Table__Title')]");
            if (titleNode != null)
            {
                var titleText = titleNode.InnerText.Trim();
                if (DateTime.TryParse(titleText, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsedDate))
                {
                    currentDate = DateOnly.FromDateTime(parsedDate);
                }
            }

            var tbodies = tableDiv.SelectNodes(".//tbody");
            if (tbodies == null) continue;

            foreach (var tbody in tbodies)
            {
                var rows = tbody.SelectNodes("tr");
                if (rows == null) continue;

                foreach (var tr in rows)
                {
                    var tds = tr.SelectNodes("td");
                    if (tds == null || tds.Count < 5)
                        continue;

                    // --- Visiting team (td[0]) ---
                    var visitingLink = FindTeamLink(tds[0]);
                    if (visitingLink == null)
                        continue;
                    var visitingTeam = ResolveTeamName(visitingLink);

                    // --- Home team (td[1]) ---
                    var homeLink = FindTeamLink(tds[1]);
                    if (homeLink == null)
                        continue;
                    var homeTeam = ResolveTeamName(homeLink);

                    // --- Time (td[2]) ---
                    var timeTd = tds[2];
                    var timeLink = timeTd.SelectSingleNode(".//a");
                    var timeText = (timeLink ?? timeTd).InnerText.Trim();
                    var gameTime = TimeOnly.TryParse(timeText, out var parsed)
                        ? currentDate.ToDateTime(parsed)
                        : currentDate.ToDateTime(TimeOnly.MinValue);

                    // --- Pitching matchup (td[4]) ---
                    var pitchingTd = tds[4];
                    var pitchingText = pitchingTd.InnerText.Trim();
                    var pitcherLinks = pitchingTd.SelectNodes(".//a[@href]")?
                        .Where(a => PlayerLinkPattern.IsMatch(a.GetAttributeValue("href", "")))
                        .ToList();

                    string visitingPitcher, homePitcher;

                    if (pitchingText.Contains(" vs "))
                    {
                        var parts = pitchingText.Split(" vs ", 2);
                        visitingPitcher = parts[0].Trim();
                        homePitcher = parts[1].Trim();
                    }
                    else if (pitcherLinks is { Count: 2 })
                    {
                        visitingPitcher = pitcherLinks[0].InnerText.Trim();
                        homePitcher = pitcherLinks[1].InnerText.Trim();
                    }
                    else if (pitcherLinks is { Count: 1 })
                    {
                        var name = pitcherLinks[0].InnerText.Trim();
                        if (pitchingText.StartsWith("Undecided", StringComparison.OrdinalIgnoreCase))
                        {
                            visitingPitcher = "Undecided";
                            homePitcher = name;
                        }
                        else
                        {
                            visitingPitcher = name;
                            homePitcher = "Undecided";
                        }
                    }
                    else
                    {
                        visitingPitcher = "Undecided";
                        homePitcher = "Undecided";
                    }

                    games.Add(new ScheduledGame(gameTime, visitingTeam, homeTeam,
                        visitingPitcher, homePitcher));
                }
            }
        }

        return games;
    }

    private static HtmlNode FindTeamLink(HtmlNode td)
    {
        return td.SelectNodes(".//a[@href]")?
            .FirstOrDefault(a =>
                TeamSlugPattern.IsMatch(a.GetAttributeValue("href", ""))
                && !string.IsNullOrWhiteSpace(a.InnerText));
    }

    private static string ResolveTeamName(HtmlNode linkNode)
    {
        var href = linkNode.GetAttributeValue("href", "");
        var match = TeamSlugPattern.Match(href);
        return match.Success ? TeamNames.Resolve(match.Groups[1].Value) : linkNode.InnerText.Trim();
    }
}
