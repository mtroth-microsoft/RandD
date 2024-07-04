CREATE PROCEDURE [mlb].[PruneMlb]
AS
BEGIN
  DELETE mlb.Previews WHERE Address IN (SELECT Address FROM mlb.FileStaging)

  UPDATE a SET
   a.Content = N''
  FROM mlb.FileStaging a
  WHERE a.Address IN (SELECT Address FROM mlb.FileMetadata WHERE Status = 5)
  AND   a.Content != N''

  DELETE mlb.FileStaging
  WHERE Content = N''
  AND   UpdatedTime < DATEADD(dd, -7, GETUTCDATE())
END