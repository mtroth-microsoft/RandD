CREATE TABLE [mlb].[Teams](
	[Id] [int] NOT NULL,
	[City] [nvarchar](128) NOT NULL,
	[Name] [nvarchar](128) NOT NULL,
	[Code] [nvarchar](32) NOT NULL,
	[DivisionCode] [nvarchar](10) NULL,
	[LeagueId] [int] NULL,
	[EspnName] [nvarchar](128) NULL,
    CONSTRAINT [PK_Teams] PRIMARY KEY CLUSTERED ([Id] ASC)
)
