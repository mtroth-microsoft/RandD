﻿CREATE TABLE [dbo].[FileMetadata](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[Address] [nvarchar](128) NOT NULL,
	[StartTime] [datetimeoffset](7) NOT NULL,
	[EndTime] [datetimeoffset](7) NULL,
	[Status] [tinyint] NOT NULL,
	[EventDate]  AS (CONVERT([datetime2],replace(replace(replace(replace(replace([address],'http://gd2.mlb.com/components/game/mlb/',''),'/uber_scoreboard.xml?store=MlbType',''),'year_',''),'month_',''),'day_',''))),
    CONSTRAINT [PK_FileMetadata] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_FileMetadata_Address] UNIQUE NONCLUSTERED ([Address] ASC)
) ON [PRIMARY]