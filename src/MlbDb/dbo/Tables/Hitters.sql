CREATE TABLE [mlb].[Hitters](
	[Id] [int] NOT NULL,
	[First] [nvarchar](128) NOT NULL,
	[Last] [nvarchar](128) NOT NULL,
	[TeamId] [int] NOT NULL,
    CONSTRAINT [PK_Hitters] PRIMARY KEY CLUSTERED ([Id] ASC)
)
