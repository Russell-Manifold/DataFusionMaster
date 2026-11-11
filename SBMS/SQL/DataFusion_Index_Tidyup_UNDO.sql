/* UNDO for DataFusion_Index_Tidyup.sql: puts back every index it removed (from dbo.IndexTidyUndo) and removes the
   8 indexes it added. Run on the same database. Safe to run more than once. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO
IF OBJECT_ID('dbo.IndexTidyUndo', 'U') IS NOT NULL
BEGIN
    DECLARE @id int = 0, @tbl sysname, @idx sysname, @sql nvarchar(max);
    WHILE 1 = 1
    BEGIN
        SELECT TOP 1 @id = ID, @tbl = Tbl, @idx = Idx, @sql = CreateSql FROM dbo.IndexTidyUndo WHERE ID > @id ORDER BY ID;
        IF @@ROWCOUNT = 0 BREAK;
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.' + QUOTENAME(@tbl)) AND name = @idx)
        BEGIN
            EXEC (@sql);
            PRINT 'Restored ' + @tbl + '.' + @idx;
        END
    END
END
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.DocLines') AND name = 'IX_DocLines_Open_Selection')
BEGIN DROP INDEX [IX_DocLines_Open_Selection] ON dbo.[DocLines]; PRINT 'Removed IX_DocLines_Open_Selection'; END
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.ItemStoreLinkMaster') AND name = 'IX_ItemStoreLinkMaster_ItemID')
BEGIN DROP INDEX [IX_ItemStoreLinkMaster_ItemID] ON dbo.[ItemStoreLinkMaster]; PRINT 'Removed IX_ItemStoreLinkMaster_ItemID'; END
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.ItemTransaction') AND name = 'IX_ItemTransaction_Company_Lot_Cover')
BEGIN DROP INDEX [IX_ItemTransaction_Company_Lot_Cover] ON dbo.[ItemTransaction]; PRINT 'Removed IX_ItemTransaction_Company_Lot_Cover'; END
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.ItemTransaction') AND name = 'IX_ItemTransaction_Company_Cover')
BEGIN DROP INDEX [IX_ItemTransaction_Company_Cover] ON dbo.[ItemTransaction]; PRINT 'Removed IX_ItemTransaction_Company_Cover'; END
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.DocHeader') AND name = 'IX_DocHeader_Company_DocType_Cover')
BEGIN DROP INDEX [IX_DocHeader_Company_DocType_Cover] ON dbo.[DocHeader]; PRINT 'Removed IX_DocHeader_Company_DocType_Cover'; END
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.DocHeader') AND name = 'IX_DocHeader_Company_LinkedJCID')
BEGIN DROP INDEX [IX_DocHeader_Company_LinkedJCID] ON dbo.[DocHeader]; PRINT 'Removed IX_DocHeader_Company_LinkedJCID'; END
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.ItemsMaster') AND name = 'IX_ItemsMaster_Company_Active_Physical_Cover')
BEGIN DROP INDEX [IX_ItemsMaster_Company_Active_Physical_Cover] ON dbo.[ItemsMaster]; PRINT 'Removed IX_ItemsMaster_Company_Active_Physical_Cover'; END
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.ItemsMaster') AND name = 'IX_ItemsMaster_Company_Code')
BEGIN DROP INDEX [IX_ItemsMaster_Company_Code] ON dbo.[ItemsMaster]; PRINT 'Removed IX_ItemsMaster_Company_Code'; END
GO
-- dbo.IndexTidyUndo is kept as a record; drop it by hand once you are happy:  DROP TABLE dbo.IndexTidyUndo;
