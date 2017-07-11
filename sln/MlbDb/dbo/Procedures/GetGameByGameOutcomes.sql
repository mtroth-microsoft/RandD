CREATE PROCEDURE [dbo].[GetGameByGameOutcomes]
  @DivisionCode nvarchar(10)   = 'W'
 ,@LeagueId     int            = 103
 ,@Year         int            = 2017
 ,@Initial      int            = 1
 ,@AsOf         datetimeoffset = NULL
AS
BEGIN
IF @AsOf IS NULL SET @AsOf = GETDATE()
DECLARE @Ids TABLE (Id int, Name nvarchar(128), DivisionCode nvarchar(10))
INSERT @Ids
SELECT Id, Name, DivisionCode FROM dbo.Teams WHERE LeagueId = @LeagueId

SELECT 
  SubSelect.Id, SubSelect.Name, 
  SubSelect.DivisionCode,
  SubSelect.Rank, SubSelect.Date,
  Outcome, 
  --CASE WHEN GameCode = 'Home' THEN CAST(Home AS nvarchar) + '-' + CAST(Away AS nvarchar)
  --     ELSE CAST(Away AS nvarchar) + '-' + CAST(Home AS nvarchar) END AS Score,
  --CASE WHEN GameCode = 'Away' THEN '@' ELSE 'vs ' END + t.Name AS Match,
  CASE WHEN GameCode = 'Home' THEN Home ELSE Away END As Us,
  CASE WHEN GameCode = 'Home' THEN Away ELSE Home END As Them,
  GameCode,
  t.Name AS Opponent,
  Wins,
  Losses,
  Pct = CAST(Wins / CAST(Wins + Losses AS decimal) AS decimal(19,3))
INTO #Data
FROM (
  SELECT b.Name, Home = a.HomeScore_Runs, Away = a.AwayScore_Runs, b.Id, a.Date,
  CASE WHEN a.HomeTeamId = b.Id THEN 'Home' ELSE 'Away' END AS GameCode,
  CASE WHEN a.HomeTeamId = b.Id AND a.HomeScore_Runs > a.AwayScore_Runs THEN 'W'
       WHEN a.AwayTeamId = b.Id AND a.AwayScore_Runs > a.HomeScore_Runs THEN 'W'
	   ELSE 'L' END AS Outcome,
  CASE WHEN a.HomeTeamId = b.Id THEN a.AwayTeamId ELSE a.HomeTeamId END AS OpponentId,
  CASE WHEN a.HomeTeamId = b.Id THEN a.HomeRecord_Wins ELSE a.AwayRecord_Wins END AS Wins,
  CASE WHEN a.HomeTeamId = b.Id THEN a.HomeRecord_Losses ELSE a.AwayRecord_Losses END AS Losses,
  b.DivisionCode,
  RANK() OVER (PARTITION BY b.Id ORDER BY Date DESC) AS Rank
  FROM dbo.Games a, @Ids b
  WHERE (HomeTeamId = b.Id OR AwayTeamId = b.Id) AND YEAR(Date) = @Year AND GameType = 82 AND Date < @AsOf) AS SubSelect
JOIN dbo.Teams t ON SubSelect.OpponentId = t.Id
ORDER BY SubSelect.Rank

DECLARE @State TABLE (Id int, Outcome nvarchar(1), 
                      Trend int, W int, L int, 
                      WinsAtHome int, LossesAtHome int, 
                      WinsOnRoad int, LossesOnRoad int, 
                      GB decimal(19,1), WCGB decimal(19,1))
INSERT @State (Id, Outcome)
SELECT Id, Outcome FROM #Data WHERE Rank = @Initial

DECLARE @Id int
DECLARE @Outcome nvarchar(1)
SELECT @Id=MIN(Id) FROM @State
WHILE @Id IS NOT NULL
BEGIN
  SELECT @Outcome = Outcome FROM @State WHERE Id = @Id
  DECLARE @Counter int = 1
  DECLARE @Rank int = @Initial
  DECLARE @NextOutcome nvarchar(1)

  SELECT @NextOutcome = Outcome, @Rank = Rank FROM #Data WHERE Id = @Id AND Rank = @Rank + 1
  WHILE @Outcome = @NextOutcome
  BEGIN
    SET @Counter = @Counter + 1
    SELECT @NextOutcome = Outcome, @Rank = Rank FROM #Data WHERE Id = @Id AND Rank = @Rank + 1
  END

  UPDATE @State SET Trend = @Counter WHERE Id = @Id
  SELECT @Id=MIN(Id) FROM @State  WHERE Id > @Id
END

-- Update record in last 10 games.
UPDATE a SET
 W = WinsInLast10,
 L = LossesInLast10
FROM @State a
JOIN (
SELECT Id, 
SUM(CASE WHEN Outcome = 'W' THEN 1 ELSE 0 END) AS WinsInLast10,
SUM(CASE WHEN Outcome = 'L' THEN 1 ELSE 0 END) AS LossesInLast10
FROM #Data 
WHERE Rank BETWEEN @Initial AND @Initial + 9
GROUP BY Id) AS Sub ON a.Id=Sub.Id

-- Update home record.
UPDATE a SET
 WinsAtHome = Wins,
 LossesAtHome = Losses
FROM @State a
JOIN (
SELECT Id, 
SUM(CASE WHEN Outcome = 'W' THEN 1 ELSE 0 END) AS Wins,
SUM(CASE WHEN Outcome = 'L' THEN 1 ELSE 0 END) AS Losses
FROM #Data 
WHERE GameCode = 'Home'
GROUP BY Id) AS Sub ON a.Id=Sub.Id

-- Update road record.
UPDATE a SET
 WinsOnRoad = Wins,
 LossesOnRoad = Losses
FROM @State a
JOIN (
SELECT Id, 
SUM(CASE WHEN Outcome = 'W' THEN 1 ELSE 0 END) AS Wins,
SUM(CASE WHEN Outcome = 'L' THEN 1 ELSE 0 END) AS Losses
FROM #Data 
WHERE GameCode = 'Away'
GROUP BY Id) AS Sub ON a.Id=Sub.Id

-- Update Games behind first place team.
DECLARE @Teams TABLE (Id int)
DECLARE @Codes TABLE (Code nvarchar(10))
INSERT @Codes SELECT DISTINCT DivisionCode FROM #Data
DECLARE @Code nvarchar(10)
SELECT @Code = MIN(Code) FROM @Codes
WHILE @Code IS NOT NULL
BEGIN
    DECLARE @pct decimal(19,3)
    DECLARE @W int, @L int
    SELECT TOP 1 @pct=Pct, @W = Wins, @L = Losses, @id = Id
    FROM #Data a
    WHERE a.Rank = @Initial AND a.DivisionCode = @Code
    ORDER BY a.Pct DESC

    INSERT @Teams SELECT @id

    UPDATE a SET
     a.GB = (CAST(@W - Wins AS decimal(19,1)) + CAST(Losses - @L AS decimal(19,1))) / 2
    FROM @State a
    JOIN #Data b ON a.Id = b.Id
    WHERE b.Rank = @Initial
    AND   b.Pct != @Pct
    AND   b.DivisionCode = @Code

    SELECT @Code = MIN(Code) FROM @Codes WHERE Code > @Code
END

-- Update Wild Card Games Back.
SELECT @W = a.Wins, @L = a.Losses, @Pct = a.Pct
FROM   #Data a
JOIN @State b ON a.Id = b.Id
WHERE a.Id NOT IN (SELECT Id FROM @Teams)
AND   a.Rank = @Initial
ORDER BY a.Pct DESC
offset 1 rows FETCH NEXT 1 rows only

UPDATE a SET
  a.WCGB = (CAST(@W - Wins AS decimal(19,1)) + CAST(Losses - @L AS decimal(19,1))) / 2
FROM @State a
JOIN #Data b ON a.Id = b.Id
WHERE b.Rank = @Initial
AND   b.Pct < @Pct
AND   b.Id NOT IN (SELECT Id FROM @Teams)

-- Output rows.
SELECT a.Id, a.DivisionCode, a.Name, a.Wins, a.Losses, a.Pct, b.GB, b.WCGB,
       a.Date, a.Outcome, a.Us, a.Them, a.GameCode, a.Opponent, b.Trend, 
       b.W, b.L, b.WinsAtHome, b.LossesAtHome, b.WinsOnRoad, b.LossesOnRoad
FROM #Data a 
JOIN @State b ON a.Id = b.Id
WHERE a.Rank = @Initial
AND   a.DivisionCode = ISNULL(@DivisionCode, a.DivisionCode)
ORDER BY a.DivisionCode, a.Pct DESC
END
/*
EXEC dbo.GetGameByGameOutcomes NULL
*/