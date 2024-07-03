CREATE PROCEDURE [dbo].[PruneMlb]
AS
BEGIN
  DELETE dbo.Previews WHERE Address IN (SELECT Address FROM dbo.FileStaging)

  UPDATE a SET
   a.Content = N''
  FROM dbo.FileStaging a
  WHERE a.Address IN (SELECT Address FROM dbo.FileMetadata WHERE Status = 5)
  AND   a.Content != N''
END