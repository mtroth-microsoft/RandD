CREATE PROCEDURE [mlb].[usp_GetPlayoffs]
    @Year INT = 2021
AS
  SET NOCOUNT ON

  DECLARE @Games TABLE (RowId INT IDENTITY(1,1), Date NVARCHAR(12), GameType INT, 
                        HomeLeagueId INT, HomeTeamId INT, HomeTeam NVARCHAR(30), HomeRecord NVARCHAR(30), HomeWin INT, HomeLoss INT, HomeRuns INT, HomeHits INT, HomeErrors INT,
                        AwayLeagueId INT, AwayTeamId INT, AwayTeam NVARCHAR(30), AwayRecord NVARCHAR(30), AwayWin INT, AwayLoss INT, AwayRuns INT, AwayHits INT, AwayErrors INT,
                        WinningPitcher NVARCHAR(128), LosingPitcher NVARCHAR(128), SavingPitcher NVARCHAR(128), RowNumber INT)

  DECLARE @Matches TABLE (GameType INT, TeamOne NVARCHAR(30), TeamTwo NVARCHAR(30), Record NVARCHAR(30), LeagueId INT)
  DECLARE @Records TABLE (GameType INT, Team NVARCHAR(30), WINS INT, LOSSES INT)

    INSERT @Games
    SELECT
        CAST(CAST(g.Date AS date) AS nvarchar) AS Date,
        CASE g.GameType WHEN 70 THEN 1 WHEN 68 THEN 2 WHEN 76 THEN 3 ELSE 4 END AS GameType,
        ht.LeagueId AS HomeLeagueId,
        ht.Id AS HomeTeamId,
        ht.Name AS HomeTeam,
        CAST(g.HomeRecord_Wins AS nvarchar) + '-' + CAST(g.HomeRecord_Losses AS nvarchar) AS HomeRecord,
        CASE WHEN g.HomeScore_Runs > g.AwayScore_Runs THEN 1 ELSE 0 END AS HomeWin,
        CASE WHEN g.HomeScore_Runs < g.AwayScore_Runs THEN 1 ELSE 0 END AS HomeLoss,
        g.HomeScore_Runs AS HomeRuns,
        g.HomeScore_Hits AS HomeHits,
        g.HomeScore_Errors AS HomeErrors,
        at.LeagueId AS AwayLeagueId,
        at.Id AS AwayTeamId,
        at.Name AS AwayTeam,
        CAST(g.AwayRecord_Wins AS nvarchar) + '-' + CAST(g.AwayRecord_Losses AS nvarchar) AS AwayRecord,
        CASE WHEN g.HomeScore_Runs < g.AwayScore_Runs THEN 1 ELSE 0 END AS AwayWin,
        CASE WHEN g.HomeScore_Runs > g.AwayScore_Runs THEN 1 ELSE 0 END AS AwayLoss,
        g.AwayScore_Runs AS AwayRuns,
        g.AwayScore_Hits AS AwayHits,
        g.AwayScore_Errors AS AwayErrors,
        CASE WHEN WinningPitcherId > 0 THEN wp.First + ' ' + wp.Last ELSE WinningPitcherName END + ' (' + CAST(g.WinningPitcherRecord_Wins AS nvarchar) + '-' + CAST(g.WinningPitcherRecord_Losses AS nvarchar) + ' ' + CAST(CAST(g.WinningPitcherRecord_Era AS decimal(19,2)) AS nvarchar) + ')' AS WinningPitcher,
        CASE WHEN LosingPitcherId > 0 THEN lp.First + ' ' + lp.Last ELSE LosingPitcherName END + ' (' + CAST(g.LosingPitcherRecord_Wins AS nvarchar) + '-' + CAST(g.LosingPitcherRecord_Losses AS nvarchar) + ' ' + CAST(CAST(g.LosingPitcherRecord_Era AS decimal(19,2)) AS nvarchar) + ')' AS LosingPitcher,
        CASE WHEN SavingPitcherId > 0 THEN sp.First + ' ' + sp.Last ELSE SavingPitcherName END + ' (' + CAST(g.SavingPitcherRecord_Saves AS nvarchar) + ')' AS SavingPitcher,
        ROW_NUMBER() OVER(ORDER BY Date DESC) AS RowNumber
    FROM   mlb.Games AS g
    JOIN   mlb.Teams AS at ON g.AwayTeamId=at.Id
    JOIN   mlb.Teams AS ht ON g.HomeTeamId=ht.Id
    JOIN   mlb.Pitchers AS wp ON g.WinningPitcherId=wp.Id
    JOIN   mlb.Pitchers AS lp ON g.LosingPitcherId=lp.Id
    LEFT JOIN mlb.Pitchers AS sp ON g.SavingPitcherId=sp.Id
    WHERE GameType IN (70, 68, 76, 87)
    AND   Year(Date) = @Year
    AND   ht.LeagueId in (103, 104)
    AND   at.LeagueId in (103, 104)
    ORDER BY Date

    INSERT @Records
    SELECT GameType, Team, SUM(Win) AS Wins, SUM(Loss) AS Losses
    FROM (SELECT GameType, HomeTeam AS Team, HomeWin AS Win, HomeLoss AS Loss FROM @Games 
          UNION ALL 
          SELECT GameType, AwayTeam AS Team, AwayWin AS Win, AwayLoss AS Loss FROM @Games) AS Sub
    GROUP BY Team, GameType

    DECLARE @Min INT = 1
    DECLARE @HomeTeam NVARCHAR(30)
    DECLARE @AwayTeam NVARCHAR(30)
    DECLARE @Record NVARCHAR(30)
    DECLARE @GameType INT
    DECLARE @LeagueId INT

    WHILE @Min IS NOT NULL
    BEGIN
      SELECT @HomeTeam=HomeTeam, @AwayTeam=AwayTeam, @GameType=GameType, @Record=HomeRecord, 
      @LeagueId=CASE WHEN HomeLeagueId=AwayLeagueId THEN HomeLeagueId ELSE NULL END 
      FROM @Games WHERE RowNumber = @Min

      IF NOT EXISTS (SELECT 1 FROM @Matches WHERE TeamOne = @HomeTeam AND TeamTwo = @AwayTeam AND GameType = @GameType)
        AND NOT EXISTS (SELECT 1 FROM @Matches WHERE TeamOne = @AwayTeam AND TeamTwo = @HomeTeam AND GameType = @GameType)
        AND @GameType IS NOT NULL
      BEGIN
        
        INSERT @Matches SELECT @GameType, @HomeTeam, @AwayTeam, @Record, @LeagueId
      END

      SELECT @Min=MIN(RowNumber) FROM @Games WHERE RowNumber > @Min
    END

    UPDATE M SET
     Record = CAST(R.Wins AS nvarchar) + '-' + CAST(R.Losses AS nvarchar)
    FROM @Matches AS M
    JOIN @Records AS R ON M.TeamOne = R.Team AND M.GameType = R.GameType

    SELECT * FROM @Matches ORDER BY GameType, LeagueId
    SELECT * FROM @Games ORDER BY RowId --ASC order so that games are in sequence.

/*
exec mlb.usp_GetPlayoffs 2021
70 = Wild Card
68 = Divisional Playoff
76 = League Playoff
87 = World Series
*/