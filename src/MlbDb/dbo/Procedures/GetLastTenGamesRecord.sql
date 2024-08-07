CREATE PROCEDURE [mlb].[GetLastTenGamesRecord]
  @DivisionCode nvarchar(10) = 'W'
 ,@LeagueId     int          = 103
 ,@Year         int          = NULL
AS
BEGIN
DECLARE @Ids TABLE (Id int, Name nvarchar(128))
INSERT @Ids
SELECT Id, Name FROM mlb.Teams WHERE DivisionCode = @DivisionCode AND LeagueId = @LeagueId

IF @Year IS NULL
BEGIN
  SET @Year = Year(GetDate())
END

SELECT TeamId, 
       TeamName, 
	   LastGameCode = CASE WHEN MAX(CASE WHEN LastPlayed = 1 AND Rank = 1 THEN LastPlayed ELSE 0 END) = 1 THEN 'Home' ELSE 'Away' END,
	   LastGameDate = MAX(GameDate),
	   W = MAX(Wins), 
	   L = MAX(Losses), 
	   Pct = CAST(MAX(Wins) / CAST(MAX(Wins) + MAX(Losses) AS decimal) AS decimal(19,3)),
	   Last10 = CAST(SUM(WonGame) AS nvarchar) + '-' + CAST(COUNT(WonGame) - SUM(WonGame) AS nvarchar)
FROM (
  SELECT
    TeamId = b.Id,
	TeamName = b.Name,
	GameDate = a.Date,
    WonGame = CASE WHEN HomeTeamId = b.Id AND HomeScore_Runs > AwayScore_Runs THEN 1
	               WHEN AwayTeamId = b.Id AND AwayScore_Runs > HomeScore_Runs THEN 1
				   ELSE 0 END,
    Wins = CASE WHEN HomeTeamId = b.Id THEN a.HomeRecord_Wins ELSE a.AwayRecord_Wins END,
	Losses = CASE WHEN HomeTeamId = b.Id THEN a.HomeRecord_Losses ELSE a.AwayRecord_Losses END,
	LastPlayed = CASE WHEN HomeTeamId = b.Id THEN 1 ELSE 0 END,
    RANK() OVER (PARTITION BY b.Id ORDER BY Date DESC) AS Rank
  FROM mlb.Games a
  JOIN @Ids b ON a.AwayTeamId = b.Id OR a.HomeTeamId = b.Id
  WHERE (HomeTeamId = b.Id OR AwayTeamId = b.Id) AND YEAR(Date) = @Year AND GameType = 82) AS TopQuery
WHERE TopQuery.Rank <= 10
GROUP BY TeamId, TeamName
ORDER BY CAST(MAX(Wins) / CAST(MAX(Wins) + MAX(Losses) AS decimal) AS decimal(19,3)) DESC

END
/*
exec mlb.GetLastTenGamesRecord
*/

