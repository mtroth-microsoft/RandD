CREATE TABLE [dbo].[HomeRuns](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[Runners] [int] NOT NULL,
	[Inning] [int] NOT NULL,
	[RawGameId] [nvarchar](128) NOT NULL,
	[GameId] [bigint] NOT NULL,
	[HitterId] [int] NOT NULL,
	[TeamId] [int] NOT NULL,
    CONSTRAINT [PK_HomeRuns] PRIMARY KEY CLUSTERED ([Id] ASC)
)