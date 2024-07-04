  CREATE TABLE mlb.GameTypeMap (
    Id int IDENTITY(1,1) NOT NULL ,
    StartDate datetime NOT NULL ,
    EndDate datetime NOT NULL ,
    GameTypeName nvarchar(128) NOT NULL ,
    GameTypeId int NOT NULL ,
    GameTypeValue char(1) NOT NULL,
    CONSTRAINT PK_GameType PRIMARY KEY CLUSTERED (Id) ,
  )
