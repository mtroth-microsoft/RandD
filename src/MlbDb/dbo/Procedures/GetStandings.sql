CREATE PROCEDURE [mlb].[GetStandings]
  @DivisionCode nvarchar(10) = 'W'
 ,@LeagueId     int          = 103
 ,@Year         int          = NULL
AS
BEGIN

DECLARE @top int
SELECT @top = COUNT(*) FROM mlb.Teams WHERE DivisionCode = @DivisionCode AND LeagueId = @LeagueId

IF @Year IS NULL
BEGIN
  SET @Year = YEAR(GETDATE())
END

SELECT TOP (@top)
  TeamId = SubSelect.Id,
  TeamName = SubSelect.Name,
  LastGameCode = SubSelect.Code,
  LastGameDate = SubSelect.Date,
  W = CASE WHEN SubSelect.Code = 'Home' THEN HomeRecord_Wins ELSE AwayRecord_Wins END,
  L = CASE WHEN SubSelect.Code = 'Home' THEN HomeRecord_Losses ELSE AwayRecord_Losses END,
  Pct = CAST(CASE WHEN SubSelect.Code = 'Home' THEN HomeRecord_Wins / CAST(HomeRecord_Wins + HomeRecord_Losses AS decimal)
		     ELSE AwayRecord_Wins / CAST(AwayRecord_Wins + AwayRecord_Losses AS decimal) END AS decimal(19,3))
FROM mlb.Games a WITH (NOLOCK)
JOIN (
	SELECT a.Id, Date = MAX(Date), Name = MAX(a.Name), Code = 'Away'
	FROM mlb.Teams a
	JOIN mlb.Games b ON a.Id = b.AwayTeamId
	WHERE a.DivisionCode = @DivisionCode AND a.LeagueId = @LeagueId
	AND   YEAR(b.Date) = @Year AND GameType = 82
	GROUP BY a.Id
	UNION ALL
	SELECT a.Id, MAX(Date), MAX(a.Name), 'Home'
	FROM mlb.Teams a
	JOIN mlb.Games b ON a.Id = b.HomeTeamId
	WHERE a.DivisionCode = @DivisionCode AND a.LeagueId = @LeagueId
	AND   YEAR(b.Date) = @Year AND GameType = 82
	GROUP BY a.Id) AS SubSelect ON a.Date = SubSelect.Date
WHERE a.HomeTeamId = SubSelect.Id OR a.AwayTeamId = SubSelect.Id
ORDER BY a.Date DESC,
         CASE WHEN SubSelect.Code = 'Home' THEN HomeRecord_Wins / CAST(HomeRecord_Wins + HomeRecord_Losses AS decimal(19,3))
		      ELSE AwayRecord_Wins / CAST(AwayRecord_Wins + AwayRecord_Losses AS decimal(19,3)) END DESC
END
/*
exec mlb.GetStandings
*/