-- ---------------------------------------------------------------------
-- READ-ONLY check. Changes nothing. Run on the live Data Fusion database.
--
-- Bulk Item Transfer (TransferSlip.aspx) no longer calls
-- GetOpeningBalancesAllStores; it reads one store / one item at a time and
-- leans on the two ItemTransaction indexes below. Quick Item Transfer
-- (Transfer.aspx) and the stock reports still call the proc, so check it too.
--
-- Every row should say OK. For a MISSING row, run the script named in Fix.
-- ---------------------------------------------------------------------
SELECT 'GetOpeningBalancesAllStores optimised' AS [Check],
       CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.GetOpeningBalancesAllStores')) LIKE '%#IT%'
            THEN 'OK' ELSE 'MISSING' END AS Result,
       'Optimise_GetOpeningBalancesAllStores.sql' AS Fix
UNION ALL
-- Checked by key columns, not name: DataFusion_Index_Tidyup.sql drops
-- IX_ItemTransaction_Co_Item_To_Qty in favour of IX_ItemTransaction_CompanyID_ItemID_ToID.
SELECT 'Index keyed CompanyID, ItemID, ToID (item stock per store)',
       CASE WHEN EXISTS (
            SELECT 1 FROM sys.indexes i
            WHERE i.object_id = OBJECT_ID('dbo.ItemTransaction')
              AND (SELECT COUNT(*) FROM sys.index_columns ic
                   JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                   WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
                     AND c.name = CASE ic.key_ordinal WHEN 1 THEN 'CompanyID' WHEN 2 THEN 'ItemID' WHEN 3 THEN 'ToID' END) = 3)
            THEN 'OK' ELSE 'MISSING' END,
       'CREATE INDEX block at the end of Optimise_GetLinkedStoredFromItem.sql'
UNION ALL
SELECT 'IX_ItemTransaction_Company_Cover (item list per store)',
       CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.ItemTransaction')
                         AND name = 'IX_ItemTransaction_Company_Cover') THEN 'OK' ELSE 'MISSING' END,
       'DataFusion_Index_Tidyup.sql';
-- (IX_ItemTransaction_Company_Item_To is not checked: the tidy-up replaced it with
--  IX_ItemTransaction_Company_Cover, which covers the same columns.)
