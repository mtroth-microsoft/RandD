CREATE TABLE [mlb].[Pitchers](
	[Id] [int] NOT NULL,
	[First] [nvarchar](128) NOT NULL,
	[Last] [nvarchar](128) NOT NULL,
	[TeamId] [int] NOT NULL,
    CONSTRAINT [PK_Pitchers] PRIMARY KEY CLUSTERED ([Id] ASC)
)