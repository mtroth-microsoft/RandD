CREATE PROCEDURE [mlb].[GetGameByGameOutcomes]
  @DivisionCode nvarchar(10)   = 'W'
 ,@LeagueId     int            = 103
 ,@Year         int            = NULL
 ,@Initial      int            = 1
 ,@AsOf         datetimeoffset = NULL
AS
BEGIN

IF @Year IS NULL AND @AsOf IS NULL
BEGIN
  SET @Year = YEAR(GETDATE())
END

IF @AsOf IS NOT NULL AND @Year != YEAR(@AsOf)
BEGIN
  SET @Year = YEAR(@AsOf)
END
ELSE IF @AsOf IS NULL 
BEGIN
  SET @AsOf = DATEADD(hh, 7, CAST(CAST(DATEADD(hh, -7, GETUTCDATE()) AS date) AS datetimeoffset))
END

IF @Year IS NULL
BEGIN
  SET @Year = YEAR(@AsOf)
END

IF @LeagueId IS NULL
BEGIN
  SET @LeagueId = 103
END

IF @Initial IS NULL
BEGIN
  SET @Initial = 1
END

DECLARE @Ids TABLE (Id int, Name nvarchar(128), DivisionCode nvarchar(10))
INSERT @Ids
SELECT Id, Name, DivisionCode FROM mlb.Teams WHERE LeagueId = @LeagueId

SELECT 
  SubSelect.Id, SubSelect.Name, 
  SubSelect.DivisionCode,
  SubSelect.Rank, SubSelect.Date,
  Outcome, Offset,
  --CASE WHEN GameCode = 'Home' THEN CAST(Home AS nvarchar) + '-' + CAST(Away AS nvarchar)
  --     ELSE CAST(Away AS nvarchar) + '-' + CAST(Home AS nvarchar) END AS Score,
  --CASE WHEN GameCode = 'Away' THEN '@' ELSE 'vs ' END + t.Name AS Match,
  CASE WHEN GameCode = 'Home' THEN Home ELSE Away END As Us,
  CASE WHEN GameCode = 'Home' THEN Away ELSE Home END As Them,
  GameCode,
  t.Name AS Opponent,
  Wins,
  Losses,
  WinningPitcherId,
  WinningPitcherName,
  WinningPitcherRecord_Wins,
  WinningPitcherRecord_Losses,
  WinningPitcherRecord_Era,
  LosingPitcherId,
  LosingPitcherName,
  LosingPitcherRecord_Wins,
  LosingPitcherRecord_Losses,
  LosingPitcherRecord_Era,
  SavingPitcherId,
  SavingPitcherName,
  SavingPitcherRecord_Saves,
  SavingPitcherRecord_Era,
  Pct = CAST(Wins / CAST(Wins + Losses AS decimal) AS decimal(19,3)),
  GameId
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
  a.WinningPitcherId,
  a.WinningPitcherName,
  a.WinningPitcherRecord_Wins,
  a.WinningPitcherRecord_Losses,
  a.WinningPitcherRecord_Era,
  a.LosingPitcherId,
  a.LosingPitcherName,
  a.LosingPitcherRecord_Wins,
  a.LosingPitcherRecord_Losses,
  a.LosingPitcherRecord_Era,
  a.SavingPitcherId,
  a.SavingPitcherName,
  a.SavingPitcherRecord_Saves,
  a.SavingPitcherRecord_Era,
  a.GameId,
  Offset = CASE WHEN a.HomeTeamId IN (108, 119, 133, 135, 136, 137, 109) THEN '-04:00'
                    WHEN a.HomeTeamId IN (110, 111, 113, 114, 116, 120, 121, 134, 147, 146, 144, 143, 141, 139) THEN '-07:00'
                    WHEN a.HomeTeamId IN (112, 117, 118, 158, 145, 142, 140, 138) THEN '-06:00'
                    WHEN a.HomeTeamId IN (115) THEN '-05:00'
                    ELSE '?' END,
  RANK() OVER (PARTITION BY b.Id ORDER BY a.Id DESC) AS Rank
  FROM mlb.Games a, @Ids b
  WHERE (HomeTeamId = b.Id OR AwayTeamId = b.Id) AND YEAR(Date) = @Year AND GameType = 82 AND Date < @AsOf) AS SubSelect
JOIN mlb.Teams t ON SubSelect.OpponentId = t.Id
ORDER BY SubSelect.Rank

DECLARE @State TABLE (Id int, Outcome nvarchar(1), 
                      Trend int, W int, L int, E int, WCE int,
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
  --DECLARE @NextOutcome nvarchar(1)

  SELECT @Rank = MIN(Rank) FROM #Data WHERE Id = @Id AND Outcome != @Outcome
  SELECT @Counter = COUNT(*) FROM #Data WHERE Id = @Id AND Rank < @Rank
  IF (SELECT COUNT(*) FROM #Data WHERE Id = @Id AND Rank = @Rank) > 1
  BEGIN
    SET @Counter = @Counter + 1
  END

  --SELECT @Rank = MAX(Rank) FROM #Data WHERE Id = @Id AND Rank = @Rank + 1
  --SELECT @NextOutcome = COALESCE(MAX(Outcome), 'T') FROM #Data WHERE Id = @Id AND Rank = COALESCE(@Rank, 0)
  --WHILE @Outcome = @NextOutcome
  --BEGIN
  --  SET @Counter = @Counter + 1
  --  SELECT @Rank = MAX(Rank) FROM #Data WHERE Id = @Id AND Rank = @Rank + 1
  --  SELECT @NextOutcome = COALESCE(MAX(Outcome), 'T') FROM #Data WHERE Id = @Id AND Rank = COALESCE(@Rank, 0)
  --END

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
	,a.E = 163 - (Losses + @W)
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
offset 2 rows FETCH NEXT 1 rows only

UPDATE a SET
  a.WCGB = (CAST(@W - Wins AS decimal(19,1)) + CAST(Losses - @L AS decimal(19,1))) / 2
 ,a.WCE = 163 - (Losses + @W)
FROM @State a
JOIN #Data b ON a.Id = b.Id
WHERE b.Rank = @Initial
AND   b.Pct < @Pct
AND   b.Id NOT IN (SELECT Id FROM @Teams)

-- Next Game
SELECT TopQuery.Id, TopQuery.Name, TopQuery.Date, TopQuery.TimeOfDay, TopQuery.GameCode, TopQuery.Opponent, TopQuery.HomePitcher, TopQuery.AwayPitcher
INTO #NextQuery
FROM (
    SELECT SubSelect.Id, SubSelect.Name, SubSelect.Date, SubSelect.TimeOfDay, SubSelect.GameCode, team.Name AS Opponent,
	SubSelect.HomePitcher, SubSelect.AwayPitcher,
    RANK() OVER (PARTITION BY SubSelect.Id ORDER BY GameId) AS Rank
    FROM (
        SELECT b.Id, b.Name, a.Date, a.TimeOfDay, a.GameId,
        CASE WHEN a.HomeTeamId = b.Id THEN a.AwayTeamId ELSE a.HomeTeamId END AS OpponentId,
        CASE WHEN a.HomeTeamId = b.Id THEN 'Home' ELSE 'Away' END as GameCode,
		a.HomePitcher, a.AwayPitcher
        FROM  mlb.Previews a, mlb.Teams b
        WHERE (a.HomeTeamId = b.Id OR a.AwayTeamId = b.Id)
		AND   a.Date >= CAST(@AsOf AS DATE)
        UNION ALL 
        SELECT b.Id, b.Name, a.Date, a.TimeOfDay, a.GameId,
        CASE WHEN a.HomeTeamId = b.Id THEN a.AwayTeamId ELSE a.HomeTeamId END AS OpponentId,
        CASE WHEN a.HomeTeamId = b.Id THEN 'Home' ELSE 'Away' END as GameCode,
		NULL, NULL
        FROM  mlb.Games a, mlb.Teams b
        WHERE (a.HomeTeamId = b.Id OR a.AwayTeamId = b.Id)
        AND   a.Date >= CAST(@AsOf AS DATE)) AS SubSelect
    JOIN mlb.Teams AS team
    ON   SubSelect.OpponentId = team.Id) AS TopQuery
WHERE TopQuery.Rank = @Initial

-- Output rows.
SELECT DISTINCT
       a.Id, a.DivisionCode, a.Name, a.Wins, a.Losses, a.Pct, b.GB, b.WCGB, b.E, b.WCE,
       SWITCHOFFSET(a.Date, a.Offset) AS Date, a.Outcome, a.Us, a.Them, a.GameCode, a.Opponent, b.Trend, 
       b.W, b.L, b.WinsAtHome, b.LossesAtHome, b.WinsOnRoad, b.LossesOnRoad,
       c.Opponent AS NextOpponent, c.GameCode AS NextGameCode, SWITCHOFFSET(c.Date, '-07:00') AS NextGame,
	   WinningPitcher = CASE WHEN WinningPitcherId > 0 THEN d.First + ' ' + d.Last ELSE WinningPitcherName END + ' (' + CONVERT(nvarchar(10), a.WinningPitcherRecord_Wins) + '-' + CONVERT(nvarchar(10), a.WinningPitcherRecord_Losses) + ' ' + CONVERT(nvarchar(10), CONVERT(decimal(19,2), a.WinningPitcherRecord_Era)) + ')',
	   LosingPitcher = CASE WHEN LosingPitcherId > 0 THEN e.First + ' ' + e.Last ELSE LosingPitcherName END + ' (' + CONVERT(nvarchar(10), a.LosingPitcherRecord_Wins) + '-' + CONVERT(nvarchar(10), a.LosingPitcherRecord_Losses) + ' ' + CONVERT(nvarchar(10), CONVERT(decimal(19,2), a.LosingPitcherRecord_Era)) + ')',
	   SavingPitcher = CASE WHEN SavingPitcherId > 0 THEN f.First + ' ' + f.Last ELSE SavingPitcherName END + ' (' + CONVERT(nvarchar(10), a.SavingPitcherRecord_Saves) + ' ' + CONVERT(nvarchar(10), CONVERT(decimal(19,2), a.SavingPitcherRecord_Era)) + ')',
	   NextHomePitcher = c.HomePitcher, NextAwayPitcher = c.AwayPitcher
FROM #Data a 
JOIN @State b ON a.Id = b.Id
--JOIN (SELECT HomeTeamId, AwayTeamId, MAX(GameId) AS GameId
--      FROM mlb.Games
--      GROUP BY HomeTeamId, AwayTeamId) AS Agg ON a.GameId=Agg.GameId
LEFT JOIN #NextQuery c ON a.Id = c.Id
LEFT JOIN mlb.Pitchers d WITH (NOLOCK) ON a.WinningPitcherId = d.Id
LEFT JOIN mlb.Pitchers e WITH (NOLOCK) ON a.LosingPitcherId = e.Id
LEFT JOIN mlb.Pitchers f WITH (NOLOCK) ON a.SavingPitcherId = f.Id
WHERE a.Rank = @Initial
AND   a.DivisionCode = ISNULL(@DivisionCode, a.DivisionCode)
ORDER BY a.DivisionCode, a.Pct DESC
END
/*
EXEC mlb.GetGameByGameOutcomes NULL
*/