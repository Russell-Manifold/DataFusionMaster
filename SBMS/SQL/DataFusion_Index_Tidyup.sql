/* Data Fusion - index tidy-up (written 2026-11-11, tested on the local copies of MyDataFusion and MyDataFusionDemo2).
   Run on the Data Fusion database (live: MyDataFusion; demo: MyDataFusionDemo2) in SSMS. Safe to run more than once.

   1. Adds 8 covering indexes for the screens that read the most: barcode scans / item by code, the items list,
      outstanding PO/SO lists, stock enquiry, active lots, job card tracking, stock count list, re-order report.
   2. Removes duplicate indexes - and the old indexes the new ones replace. An index is only removed when its
      replacement exists on THIS database with the same key columns and every column it had, so no query loses
      an index; anything that does not match is skipped and reported. No code or stored procedure names any of them.
   3. Every removed index's definition is saved in dbo.IndexTidyUndo first. DataFusion_Index_Tidyup_UNDO.sql
      puts everything back exactly as it was.

   The messages at the end list what was created, dropped and skipped. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO
IF OBJECT_ID('dbo.IndexTidyUndo', 'U') IS NULL
    CREATE TABLE dbo.IndexTidyUndo (ID int IDENTITY PRIMARY KEY, Tbl sysname NOT NULL, Idx sysname NOT NULL,
                                    CreateSql nvarchar(max) NOT NULL, DroppedAt datetime NOT NULL DEFAULT GETDATE());
GO
-- 1. New indexes (each in its own batch, so one problem cannot stop the others) -------------------------------
-- item by code: barcode scans (GetOneItemFromBarcode) and item look-ups by code all over the app
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.ItemsMaster') AND name = 'IX_ItemsMaster_Company_Code')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ItemsMaster_Company_Code] ON dbo.[ItemsMaster] ([CompanyID], [Code])
        INCLUDE ([Active], [ID], [Description], [Unit]);
    PRINT 'Created IX_ItemsMaster_Company_Code';
END
GO
-- items list (sp_GetItemsForDisplay), re-order report, stock count list, opening balances by store
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.ItemsMaster') AND name = 'IX_ItemsMaster_Company_Active_Physical_Cover')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ItemsMaster_Company_Active_Physical_Cover] ON dbo.[ItemsMaster] ([CompanyID], [Active], [Physical])
        INCLUDE ([ID], [Code], [Description], [CategoryDescript], [Unit], [QuantityOnHand], [QuantityReserved], [ReorderLevel], [MinReOrderQty], [AverageCost], [IsBOMComponent], [IsKitComponent], [IsFinishedGoods], [IsFromBOM], [IsFromKit], [IsLotTracked]);
    PRINT 'Created IX_ItemsMaster_Company_Active_Physical_Cover';
END
GO
-- job card tracking (GetAllActiveJobCards)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.DocHeader') AND name = 'IX_DocHeader_Company_LinkedJCID')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_DocHeader_Company_LinkedJCID] ON dbo.[DocHeader] ([CompanyID], [LinkedJCID])
        INCLUDE ([DocID], [DocumentNumber], [CustSupName], [DueDelDate], [DocGUID]);
    PRINT 'Created IX_DocHeader_Company_LinkedJCID';
END
GO
-- outstanding Purchase / Sales Order lists (GetListOfDocHeadersByType) - the old index plus RecStatus
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.DocHeader') AND name = 'IX_DocHeader_Company_DocType_Cover')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_DocHeader_Company_DocType_Cover] ON dbo.[DocHeader] ([CompanyID], [DocType])
        INCLUDE ([Active], [Complete], [CustSupName], [DelAddress3], [DeliveryBy], [DocGUID], [DocID], [DocumentNumber], [DueDelDate], [LinkedJCID], [LinkedPSID], [Reference], [Started], [Status], [SupplierInvNum], [Total], [RecStatus]);
    PRINT 'Created IX_DocHeader_Company_DocType_Cover';
END
GO
-- stock enquiry (GetAllStockLevels) - the old company index plus ExchRate
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.ItemTransaction') AND name = 'IX_ItemTransaction_Company_Cover')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ItemTransaction_Company_Cover] ON dbo.[ItemTransaction] ([CompanyID])
        INCLUDE ([ItemCode], [ItemDescription], [ItemID], [LotNumber], [PriceExclusive], [Qty], [ToID], [TotalUnitPriceExclInclAdd], [Unit], [ExchRate]);
    PRINT 'Created IX_ItemTransaction_Company_Cover';
END
GO
-- active lots per store (GetActiveLotNumbersLinkedToStores: picking, job cards, works orders)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.ItemTransaction') AND name = 'IX_ItemTransaction_Company_Lot_Cover')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ItemTransaction_Company_Lot_Cover] ON dbo.[ItemTransaction] ([CompanyID], [LotNumber])
        INCLUDE ([ItemID], [Qty], [ToID], [TransactionDate], [TotalUnitPriceExclInclAdd]);
    PRINT 'Created IX_ItemTransaction_Company_Lot_Cover';
END
GO
-- stock count list (GetStockCountList joins on ItemID only)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.ItemStoreLinkMaster') AND name = 'IX_ItemStoreLinkMaster_ItemID')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ItemStoreLinkMaster_ItemID] ON dbo.[ItemStoreLinkMaster] ([ItemID])
        INCLUDE ([StoreID], [CompanyID], [Active]);
    PRINT 'Created IX_ItemStoreLinkMaster_ItemID';
END
GO
-- open order lines per item (re-order report and its detail)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.DocLines') AND name = 'IX_DocLines_Open_Selection')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_DocLines_Open_Selection] ON dbo.[DocLines] ([SelectionId])
        INCLUDE ([DocID], [QtyLeft], [Quantity])
        WHERE ReceiveComplete = 0;
    PRINT 'Created IX_DocLines_Open_Selection';
END
GO
-- 2. Remove duplicates / replaced indexes, only after checking the index that takes over -------------------------
DECLARE @list TABLE (N int IDENTITY, Tbl sysname, DropIdx sysname, KeepIdx sysname);
INSERT @list (Tbl, DropIdx, KeepIdx) VALUES
    ('DocHeader', 'IX_DocHeader_3', '(clustered)'),
    ('DocHeader', 'IX_DocHeader_Company_DocType2', 'IX_DocHeader_Company_DocType'),
    ('DocHeader', 'IX_DocHeader_Fast', 'IX_DocHeader_Company_DocType'),
    ('DocHeader', 'IX_DocHeader_LinkedPSID', 'IX_DocHeader_LinkedPSID_CustSupName'),
    ('ItemTransaction', 'IX_ItemTransaction_CoID_LotNumber_ToID', 'IX_ItemTransaction_LotNumber_QtyYY'),
    ('ItemTransaction', 'IX_ItemTransaction_Co_Item_To_Qty', 'IX_ItemTransaction_CompanyID_ItemID_ToID'),
    ('ItemTransaction', 'IX_ItemTransaction_CompanyID_TrnID', 'IX_ItemTransaction_Company_Item_To'),
    ('ItemTransaction', 'IX_ItemTransaction_QOH_Fast', 'IX_ItemTransaction_CompanyID_ItemID_StoreID_LotNum_TrnID'),
    ('ItemTransaction', 'IX_ItemTrn_CoID_Item_Store_Lot_Trn', 'IX_ItemTransaction_Item_Store_Lot_TrnID2'),
    ('ItemsMaster', 'IX_ItemsMaster_CoID_ID', 'IX_ItemsMaster_CompanyID_IDZZ'),
    ('ItemsMaster', 'IX_ItemsMaster_CompanyID_ID2', 'IX_ItemsMaster_CompanyID_ID'),
    ('ItemsMaster', 'IX_ItemsMaster_CompanyID_IDYY', 'IX_ItemsMaster_CompanyID_ID'),
    ('ItemsMaster', 'IX_ItemsMaster_CompanyID_ItemID', 'IX_ItemsMaster_CompanyID_ID'),
    ('JobCardsMaster', 'IX_JobCardsMaster_CustomerID_JCID', 'IX_JobCardsMaster_CustomerID_JCID_DocHeader'),
    ('JobCardsMaster', 'IX_JobCardsMaster_Customer_JCID', 'IX_JobCardsMaster_CustomerID_JCID_DocHeader'),
    ('PickingSlipMaster', 'IX_PickingSlipMaster_CustomerID_PSID_DocHeader', 'IX_PickingSlipMaster_CustomerID_PSID'),
    ('PickingSlipMaster', 'IX_PickingSlipMaster_Customer_PSID', 'IX_PickingSlipMaster_CustomerID_PSID'),
    ('RolesMaster', 'IX_RolesMaster', 'IX_RolesMaster_CompanyID_RoleID'),
    ('Stores', 'IX_Stores_CoID_StoreID', 'IX_Stores_CompanyID_StoreID'),
    ('Stores', 'IX_Stores_CompanyID_StoreIDZZ', 'IX_Stores_CompanyID_StoreID'),
    ('Stores', 'IX_Stores_StoreID', '(clustered)'),
    ('Stores', 'IX_Stores_StoreID2', '(clustered)'),
    ('Stores', 'IX_Stores_StoreID_StoreCode', '(clustered)'),
    ('ItemTransaction', 'IX_ItemTransaction_LotNumber_QtyYY', 'IX_ItemTransaction_CoID_LotNumber_ToID'),
    ('JobCardsMaster', 'IX_JobCardsMaster_CustomerID_JCID_DocHeader', 'IX_JobCardsMaster_CustomerID_JCID'),
    ('JobCardsMaster', 'IX_JobCardsMaster_Customer_JCID', 'IX_JobCardsMaster_CustomerID_JCID'),
    ('RolesMaster', 'IX_RolesMaster_CompanyID_RoleID', 'IX_RolesMaster_CompanyID_RoleID_JobCard'),
    ('ItemsMaster', 'IX_ItemsMaster_CompanyID_Active_Physical', 'IX_ItemsMaster_Company_Active_Physical_Cover'),
    ('DocHeader', 'IX_DocHeader_Company_DocType', 'IX_DocHeader_Company_DocType_Cover'),
    ('ItemTransaction', 'IX_ItemTransaction_Company_Item_To', 'IX_ItemTransaction_Company_Cover'),
    ('ItemTransaction', 'IX_ItemTransaction_CompanyID_TrnID', 'IX_ItemTransaction_Company_Cover'),
    ('ItemTransaction', 'IX_ItemTrans_CoLot', 'IX_ItemTransaction_Company_Lot_Cover');

DECLARE @n int = 0, @max int = (SELECT MAX(N) FROM @list), @tbl sysname, @drop sysname, @keep sysname,
        @obj int, @did int, @kid int, @dkeys nvarchar(max), @kkeys nvarchar(max), @dfilt nvarchar(max), @kfilt nvarchar(max),
        @missing int, @create nvarchar(max), @dincl nvarchar(max), @sql nvarchar(max),
        @dropped int = 0, @skipped int = 0;
WHILE @n < @max
BEGIN
    SET @n += 1;
    SELECT @tbl = Tbl, @drop = DropIdx, @keep = KeepIdx FROM @list WHERE N = @n;
    SET @obj = OBJECT_ID('dbo.' + QUOTENAME(@tbl));
    SET @did = (SELECT index_id FROM sys.indexes WHERE object_id = @obj AND name = @drop
                AND type = 2 AND is_unique = 0 AND is_primary_key = 0 AND is_unique_constraint = 0);
    IF @did IS NULL CONTINUE;   -- not on this database (or already removed)
    SET @kid = CASE WHEN @keep = '(clustered)' THEN (SELECT index_id FROM sys.indexes WHERE object_id = @obj AND type = 1)
                    ELSE (SELECT index_id FROM sys.indexes WHERE object_id = @obj AND name = @keep AND is_disabled = 0) END;
    IF @kid IS NULL
    BEGIN
        PRINT 'Skipped ' + @tbl + '.' + @drop + ' - ' + @keep + ' is not on this database';
        SET @skipped += 1; CONTINUE;
    END
    SELECT @dkeys = (SELECT ',' + c.name + CASE WHEN ic.is_descending_key = 1 THEN ' DESC' ELSE '' END
                     FROM sys.index_columns ic JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                     WHERE ic.object_id = @obj AND ic.index_id = @did AND ic.is_included_column = 0 ORDER BY ic.key_ordinal FOR XML PATH('')),
           @kkeys = (SELECT ',' + c.name + CASE WHEN ic.is_descending_key = 1 THEN ' DESC' ELSE '' END
                     FROM sys.index_columns ic JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                     WHERE ic.object_id = @obj AND ic.index_id = @kid AND ic.is_included_column = 0 ORDER BY ic.key_ordinal FOR XML PATH('')),
           @dfilt = (SELECT ISNULL(filter_definition, '') FROM sys.indexes WHERE object_id = @obj AND index_id = @did),
           @kfilt = (SELECT ISNULL(filter_definition, '') FROM sys.indexes WHERE object_id = @obj AND index_id = @kid);
    -- every column of the dropped index must be in the one that takes over (or in the clustered key, which every index carries)
    SET @missing = (SELECT COUNT(*) FROM sys.index_columns d
                    WHERE d.object_id = @obj AND d.index_id = @did
                      AND NOT EXISTS (SELECT 1 FROM sys.index_columns k WHERE k.object_id = @obj AND k.index_id IN (@kid, 1) AND k.column_id = d.column_id));
    IF @dkeys <> @kkeys OR @dfilt <> @kfilt OR @missing > 0
    BEGIN
        PRINT 'Skipped ' + @tbl + '.' + @drop + ' - ' + @keep + ' does not cover it here';
        SET @skipped += 1; CONTINUE;
    END
    -- save its definition, then drop it
    SET @dincl = STUFF((SELECT ', ' + QUOTENAME(c.name) FROM sys.index_columns ic JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                        WHERE ic.object_id = @obj AND ic.index_id = @did AND ic.is_included_column = 1 ORDER BY ic.index_column_id FOR XML PATH('')), 1, 2, '');
    SET @create = 'CREATE NONCLUSTERED INDEX ' + QUOTENAME(@drop) + ' ON dbo.' + QUOTENAME(@tbl) + ' ('
        + STUFF((SELECT ', ' + QUOTENAME(c.name) + CASE WHEN ic.is_descending_key = 1 THEN ' DESC' ELSE '' END
                 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                 WHERE ic.object_id = @obj AND ic.index_id = @did AND ic.is_included_column = 0 ORDER BY ic.key_ordinal FOR XML PATH('')), 1, 2, '') + ')'
        + ISNULL(' INCLUDE (' + @dincl + ')', '')
        + CASE WHEN @dfilt <> '' THEN ' WHERE ' + @dfilt ELSE '' END + ';';
    INSERT dbo.IndexTidyUndo (Tbl, Idx, CreateSql) VALUES (@tbl, @drop, @create);
    SET @sql = 'DROP INDEX ' + QUOTENAME(@drop) + ' ON dbo.' + QUOTENAME(@tbl) + ';';
    EXEC (@sql);
    PRINT 'Dropped ' + @tbl + '.' + @drop + ' (covered by ' + @keep + ')';
    SET @dropped += 1;
END
PRINT 'Index tidy-up done on ' + DB_NAME() + ': ' + CAST(@dropped AS varchar(10)) + ' removed, ' + CAST(@skipped AS varchar(10)) + ' skipped.';
GO
