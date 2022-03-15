-- =============================================
CREATE PROCEDURE [indexer].[GetZoneContentArtDocKSS]
	@ModuleID	tinyint,
	@ID			int
AS
BEGIN
	SET NOCOUNT ON;

DECLARE @myDoc XML
    SET @myDoc = (select ISNULL(XmlContent, Content) from ArtDocKSS where ModuleID = @ModuleID and ID = @ID)

	CREATE TABLE #resultSet (Content text, SortNum INT)
	CREATE TABLE #childLinks (ModuleID tinyint, ID INT, Content TEXT, Title VARCHAR(1024), Num INT)
	CREATE TABLE #grandChildLinks (ModuleID tinyint, ID INT, Content TEXT, Title VARCHAR(1024), Num INT)

	INSERT INTO #childLinks
	SELECT lnk.ModuleFrom, lnk.IDFrom, ISNULL(part.xmlContent, part.Content), ISNULL(part.TopText, part.DocName), lnk.LinkIDFrom
	FROM [dbo].[ArtIntLnk] lnk
	INNER JOIN [dbo].[LinkType] ltype
		 ON ltype.LinkTypeID = lnk.LinkTypeID AND ltype.[isArtPartDoc] = 1
	INNER JOIN dbo.ArtPartDoc part
		ON part.ModuleID = lnk.ModuleTo AND part.ID = lnk.IDTo
	WHERE lnk.ModuleFrom = @ModuleID AND lnk.IDFrom = @ID
		AND lnk.LinkTypeID != 148 -- не индексируем отраслевые блоки
		AND NOT(lnk.LinkTypeID = 150 AND lnk.ModuleTo = 18) -- не индексируем промо-текст
	ORDER BY lnk.LinkIDFrom

	INSERT INTO #grandChildLinks
	SELECT lnk1.ModuleFrom, lnk1.IDFrom, ISNULL(part.xmlContent, part.Content), ISNULL(part.TopText, part.DocName), lnk2.LinkIDFrom FROM [dbo].[ArtIntLnk] lnk1
	INNER JOIN [dbo].[LinkType] ltype1
		ON ltype1.LinkTypeID = lnk1.LinkTypeID AND ltype1.[isArtPartDoc] = 1
	INNER JOIN [dbo].[ArtIntLnk] lnk2
		ON lnk2.ModuleFrom = lnk1.ModuleTo AND lnk2.IDFrom = lnk1.IDTo
		AND lnk2.LinkTypeID != 148 -- не индексируем отраслевые блоки
		AND NOT(lnk2.LinkTypeID = 150 AND lnk2.ModuleTo = 18) -- не индексируем промо-текст
	INNER JOIN [dbo].[LinkType] ltype2
		ON ltype2.LinkTypeID = lnk2.LinkTypeID AND ltype2.[isArtPartDoc] = 1
	INNER JOIN dbo.ArtPartDoc part
		ON part.ModuleID = lnk2.ModuleTo AND part.ID = lnk2.IDTo
	WHERE lnk1.ModuleFrom = @ModuleID AND lnk1.IDFrom = @ID
		AND lnk1.LinkTypeID != 148 -- не индексируем отраслевые блоки
		AND NOT(lnk1.LinkTypeID = 150 AND lnk1.ModuleTo = 18) -- не индексируем промо-текст
	ORDER BY lnk2.LinkIDFrom

	-- блоки документа (дети) + блоки документа (дети): видимый текст
	INSERT INTO #resultSet SELECT CONCAT(REPLACE(lnk.Title, '<toptext', '<toptext><childtoptextremove num="' + CAST(lnk.Num AS VARCHAR) +'"/>'), lnk.Content), lnk.Num
	FROM dbo.ArtDocKSS doc
	INNER JOIN #childLinks lnk
		ON lnk.ModuleID = doc.ModuleID AND lnk.ID = doc.ID

DECLARE @Content NVARCHAR(Max), @Num INT, @ContentXML XML;

DECLARE Nums CURSOR FOR SELECT * FROM #resultSet
OPEN Nums

FETCH NEXT FROM Nums INTO @Content, @Num;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @ContentXML = CAST(@Content AS XML)
    SET @myDoc.modify('insert sql:variable("@ContentXML") after (//artblock[@num eq sql:variable("@Num")])[1]')

FETCH NEXT FROM Nums INTO @Content, @Num;
END
CLOSE Nums
DEALLOCATE Nums

TRUNCATE TABLE #resultSet

-- блоки блоков документа (внуки) + блоки блоков документа (внуки): видимый текст
	INSERT INTO #resultSet SELECT CONCAT(REPLACE(lnk.Title, '<toptext', '<toptext><childtoptextremove num="' + CAST(lnk.Num AS VARCHAR) +'"/>'), lnk.Content), lnk.Num
FROM dbo.ArtDocKSS doc
INNER JOIN #grandChildLinks lnk
    ON lnk.ModuleID = doc.ModuleID AND lnk.ID = doc.ID

DECLARE Nums CURSOR FOR SELECT * FROM #resultSet
OPEN Nums

FETCH NEXT FROM Nums INTO @Content, @Num;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @ContentXML = CAST(@Content AS XML)
    SET @myDoc.modify('insert sql:variable("@ContentXML") after (//artblock[@num eq sql:variable("@Num")])[1]')

FETCH NEXT FROM Nums INTO @Content, @Num;
END

CLOSE Nums
DEALLOCATE Nums

DROP TABLE #resultSet
DROP TABLE #childLinks
DROP TABLE #grandChildLinks

SELECT CAST(@myDoc AS NVARCHAR(MAX));
END
go

