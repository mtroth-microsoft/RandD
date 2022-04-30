CREATE TABLE [dbo].[FileMetadata](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[Address] [nvarchar](128) NOT NULL,
	[StartTime] [datetimeoffset](7) NOT NULL,
	[EndTime] [datetimeoffset](7) NULL,
	[Status] [tinyint] NOT NULL,
	[EventDate]  AS (CONVERT([datetime2],replace(replace(replace(replace(replace([address],'http://gd2.mlb.com/components/game/mlb/',''),'/master_scoreboard.xml?store=MlbType',''),'year_',''),'month_',''),'day_',''))),
	[AddressEx]  AS replace(replace(replace(replace(replace([address], 'http://gd2.mlb.com/components/game/mlb/', 'https://www.espn.com/mlb/scoreboard/_/date/'), '/master_scoreboard.xml', ''), 'year_', ''), '/month_', ''), '/day_', ''),
    CONSTRAINT [PK_FileMetadata] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_FileMetadata_Address] UNIQUE NONCLUSTERED ([Address] ASC),
) ON [PRIMARY]
