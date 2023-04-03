CREATE PROCEDURE [dbo].[usp_GetMonthly]
    @Team nvarchar(128) = 'Mariners'
   ,@AsOfYear INT = 2021
   ,@AsOfMonth INT = 9
AS
  SET NOCOUNT ON

/*
SELECT
SUM(CASE WHEN ht.Name = @Team AND g.HomeScore_Runs > g.AwayScore_Runs THEN 1 WHEN at.Name = @Team AND g.AwayScore_Runs > g.HomeScore_Runs THEN 1 ELSE 0 END) AS Wins,
SUM(CASE WHEN ht.Name = @Team AND g.HomeScore_Runs < g.AwayScore_Runs THEN 1 WHEN at.Name = @Team AND g.AwayScore_Runs < g.HomeScore_Runs THEN 1 ELSE 0 END) AS Losses
FROM   dbo.Games AS g
JOIN   dbo.Teams AS at ON g.AwayTeamId=at.Id
JOIN   dbo.Teams AS ht ON g.HomeTeamId=ht.Id
JOIN   dbo.Pitchers AS wp ON g.WinningPitcherId=wp.Id
JOIN   dbo.Pitchers AS lp ON g.LosingPitcherId=lp.Id
LEFT JOIN dbo.Pitchers AS sp ON g.SavingPitcherId=sp.Id
WHERE  YEAR(g.Date) = @AsOfYear
AND    MONTH(g.Date) = @AsOfMonth
AND    g.GameType = 82
AND   (at.Name = @Team OR ht.Name = @Team)
*/

SELECT
    CAST(CAST(g.Date AS date) AS nvarchar) AS Date,
    CASE WHEN ht.Name = @Team THEN CAST(g.HomeRecord_Wins AS nvarchar) + '-' + CAST(g.HomeRecord_Losses AS nvarchar) ELSE CAST(g.AwayRecord_Wins AS nvarchar) + '-' + CAST(g.AwayRecord_Losses AS nvarchar) END AS TeamRecord,
    CASE WHEN ht.Name = @Team AND g.HomeScore_Runs > g.AwayScore_Runs THEN 'W' WHEN at.Name = @Team AND g.AwayScore_Runs > g.HomeScore_Runs THEN 'W' ELSE 'L' END AS WinOrLoss,
    ht.Name AS HomeTeam,
    g.HomeScore_Runs AS HomeRuns,
    g.HomeScore_Hits AS HomeHits,
    g.HomeScore_Errors AS HomeErrors,
    at.Name AS AwayTeam,
    g.AwayScore_Runs AS AwayRuns,
    g.AwayScore_Hits AS AwayHits,
    g.AwayScore_Errors AS AwayErrors,
    CASE WHEN WinningPitcherId > 0 THEN wp.First + ' ' + wp.Last ELSE WinningPitcherName END + ' (' + CAST(g.WinningPitcherRecord_Wins AS nvarchar) + '-' + CAST(g.WinningPitcherRecord_Losses AS nvarchar) + ' ' + CAST(CAST(g.WinningPitcherRecord_Era AS decimal(19,2)) AS nvarchar) + ')' AS WinningPitcher,
    CASE WHEN LosingPitcherId > 0 THEN lp.First + ' ' + lp.Last ELSE LosingPitcherName END + ' (' + CAST(g.LosingPitcherRecord_Wins AS nvarchar) + '-' + CAST(g.LosingPitcherRecord_Losses AS nvarchar) + ' ' + CAST(CAST(g.LosingPitcherRecord_Era AS decimal(19,2)) AS nvarchar) + ')' AS LosingPitcher,
    CASE WHEN SavingPitcherId > 0 THEN sp.First + ' ' + sp.Last ELSE SavingPitcherName END + ' (' + CAST(g.SavingPitcherRecord_Saves AS nvarchar) + ')' AS SavingPitcher
FROM   dbo.Games AS g
JOIN   dbo.Teams AS at ON g.AwayTeamId=at.Id
JOIN   dbo.Teams AS ht ON g.HomeTeamId=ht.Id
JOIN   dbo.Pitchers AS wp ON g.WinningPitcherId=wp.Id
JOIN   dbo.Pitchers AS lp ON g.LosingPitcherId=lp.Id
LEFT JOIN dbo.Pitchers AS sp ON g.SavingPitcherId=sp.Id
WHERE  YEAR(g.Date) = @AsOfYear
AND    MONTH(g.Date) = @AsOfMonth
AND    g.GameType = 82
AND   (at.Name = @Team OR ht.Name = @Team)
ORDER BY Date
GO

/*
exec usp_GetMonthly @AsOfMonth=4
*/