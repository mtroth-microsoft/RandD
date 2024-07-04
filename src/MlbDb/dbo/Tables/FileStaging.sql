CREATE TABLE [mlb].[FileStaging](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[Content] [nvarchar](max) NOT NULL,
	[InsertedTime] [datetimeoffset](7) NOT NULL,
	[UpdatedTime] [datetimeoffset](7) NOT NULL,
	[Address] [nvarchar](128) NULL,
    CONSTRAINT [PK_FileStaging] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_FileStaging_Address] UNIQUE NONCLUSTERED ([Address] ASC)
)