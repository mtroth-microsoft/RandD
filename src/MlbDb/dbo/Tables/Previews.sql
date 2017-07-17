CREATE TABLE [dbo].[Previews]
(
    [Id] [uniqueidentifier] NOT NULL,
    [GameId] [nvarchar](128) NOT NULL,
	[Date] [datetimeoffset](7) NOT NULL,
    [TimeOfDay] [nvarchar](10) NOT NULL,
	[GameType] [int] NOT NULL,
    [Address] [nvarchar](128) NOT NULL,
    [HomeTeamId] int NOT NULL ,
    [AwayTeamId] int NOT NULL ,
    CONSTRAINT PK_Previews PRIMARY KEY CLUSTERED (Id)
)
