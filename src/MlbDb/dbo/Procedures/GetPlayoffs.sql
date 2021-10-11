CREATE PROCEDURE dbo.usp_GetPlayoffs
    @Year INT = 2021
AS
  SET NOCOUNT ON

  DECLARE @Games TABLE (Date NVARCHAR(12), GameType INT, 
                        HomeLeagueId INT, HomeTeamId INT, HomeTeam NVARCHAR(30), HomeRecord NVARCHAR(30), HomeRuns INT, HomeHits INT, HomeErrors INT,
                        AwayLeagueId INT, AwayTeamId INT, AwayTeam NVARCHAR(30), AwayRecord NVARCHAR(30), AwayRuns INT, AwayHits INT, AwayErrors INT,
                        WinningPitcher NVARCHAR(128), LosingPitcher NVARCHAR(128), SavingPitcher NVARCHAR(128), RowNumber INT)

  DECLARE @Matches TABLE (GameType INT, TeamOne NVARCHAR(30), TeamTwo NVARCHAR(30), Record NVARCHAR(30), LeagueId INT)

    INSERT @Games
    SELECT
        CAST(CAST(g.Date AS date) AS nvarchar) AS Date,
        CASE g.GameType WHEN 70 THEN 1 WHEN 68 THEN 2 WHEN 76 THEN 3 ELSE 4 END AS GameType,
        ht.LeagueId AS HomeLeagueId,
        ht.Id AS HomeTeamId,
        ht.Name AS HomeTeam,
        CAST(g.HomeRecord_Wins AS nvarchar) + '-' + CAST(g.HomeRecord_Losses AS nvarchar) AS HomeRecord,
        g.HomeScore_Runs AS HomeRuns,
        g.HomeScore_Hits AS HomeHits,
        g.HomeScore_Errors AS HomeErrors,
        at.LeagueId AS AwayLeagueId,
        at.Id AS AwayTeamId,
        at.Name AS AwayTeam,
        CAST(g.AwayRecord_Wins AS nvarchar) + '-' + CAST(g.AwayRecord_Losses AS nvarchar) AS AwayRecord,
        g.AwayScore_Runs AS AwayRuns,
        g.AwayScore_Hits AS AwayHits,
        g.AwayScore_Errors AS AwayErrors,
        wp.First + ' ' + wp.Last + ' (' + CAST(g.WinningPitcherRecord_Wins AS nvarchar) + '-' + CAST(g.WinningPitcherRecord_Losses AS nvarchar) + ' ' + CAST(CAST(g.WinningPitcherRecord_Era AS decimal(19,2)) AS nvarchar) + ')' AS WinningPitcher,
        lp.First + ' ' + lp.Last + ' (' + CAST(g.LosingPitcherRecord_Wins AS nvarchar) + '-' + CAST(g.LosingPitcherRecord_Losses AS nvarchar) + ' ' + CAST(CAST(g.LosingPitcherRecord_Era AS decimal(19,2)) AS nvarchar) + ')' AS LosingPitcher,
        sp.First + ' ' + sp.Last + ' (' + CAST(g.SavingPitcherRecord_Saves AS nvarchar) + ')' AS SavingPitcher,
        ROW_NUMBER() OVER(ORDER BY Date DESC) AS RowNumber
    FROM   dbo.Games AS g
    JOIN   dbo.Teams AS at ON g.AwayTeamId=at.Id
    JOIN   dbo.Teams AS ht ON g.HomeTeamId=ht.Id
    JOIN   dbo.Pitchers AS wp ON g.WinningPitcherId=wp.Id
    JOIN   dbo.Pitchers AS lp ON g.LosingPitcherId=lp.Id
    LEFT JOIN dbo.Pitchers AS sp ON g.SavingPitcherId=sp.Id
    WHERE GameType IN (70, 68, 76, 87)
    AND   Year(Date) = @Year
    AND   ht.LeagueId in (103, 104)
    AND   at.LeagueId in (103, 104)
    ORDER BY Date DESC -- use DESC here to make sure @Record is correct without complex update logic.

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

    SELECT * FROM @Matches ORDER BY GameType, LeagueId
    SELECT * FROM @Games ORDER BY Date, HomeRecord, AwayRecord --ASC order so that games are in sequence.

/*
exec dbo.usp_GetPlayoffs 2021
70 = Wild Card
68 = Divisional Playoff
76 = League Playoff
87 = World Series
*/