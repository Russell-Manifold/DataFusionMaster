-- Purchase order supply: ignore DELETED and CANCELLED orders in the three demand procedures.
--
-- A cancelled or deleted purchase order is not going to arrive, but GetItemDemmandsAll counts
-- its open lines as incoming supply - 240 lines carrying 45,149 units of stock that does not
-- exist. Anything planning against that under-orders.
--
-- Evidence for cancelled: 0 of the 102 cancelled PO headers in live data has ever had a GRN
-- posted against it. Deleted is excluded by business rule - a deleted purchase order does not
-- deliver, whether it was marked so by the Sage sync or by Receiving.aspx.cs.
--
-- Run this AFTER Fix_PO_ActiveFilter_In_Demands_Procs.sql. Together they bring all three
-- demand procedures - and the two re-order procedures - to one rule:
--     a purchase order counts as incoming unless its Status is Deleted or Cancelled.
--
-- GetItemDemmandsAll feeds Planning.aspx, ProdPlanning.aspx and ProdPlanningC.aspx.
-- GetFGDemmandsAll feeds the Finished Goods Demands screen.
-- GetProdRMDemands feeds the Raw Materials Demands screen.
--
-- Method: each body is read back with OBJECT_DEFINITION and only the one clause is changed,
-- so every other line stays byte-for-byte as it is. Each block refuses to run unless it finds
-- exactly what it expects. Safe to re-run - a second pass finds nothing to do.

SET NOCOUNT ON;
GO

-------------------------------------------------------------------------------
-- 1. GetItemDemmandsAll - had NO purchase order status filter at all.
-------------------------------------------------------------------------------
DECLARE @sql nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID('dbo.GetItemDemmandsAll'));
DECLARE @done nvarchar(200) = N'NOT IN (''Deleted'', ''Cancelled'')';
DECLARE @narrow nvarchar(200) = N'ISNULL(dh.[Status], '''') <> ''Cancelled''';
DECLARE @find nvarchar(200) = N'WHERE dh.DocType = 1 ';
DECLARE @repl nvarchar(400) = N'WHERE dh.DocType = 1 AND ISNULL(dh.[Status], '''') NOT IN (''Deleted'', ''Cancelled'') ';

IF @sql IS NULL
    RAISERROR('GetItemDemmandsAll not found - nothing changed.', 16, 1);
ELSE IF CHARINDEX(@done, @sql) > 0
    PRINT 'GetItemDemmandsAll: already correct, nothing to do.';
ELSE IF CHARINDEX(@narrow, @sql) > 0
BEGIN
    -- Was narrowed to Cancelled only; widen it back to both.
    SET @sql = STUFF(@sql, CHARINDEX(N'CREATE PROCEDURE', @sql), LEN(N'CREATE PROCEDURE'), N'ALTER PROCEDURE');
    SET @sql = REPLACE(@sql, @narrow, N'ISNULL(dh.[Status], '''') NOT IN (''Deleted'', ''Cancelled'')');
    EXEC sp_executesql @sql;
    PRINT 'GetItemDemmandsAll: widened to exclude Deleted and Cancelled.';
END
ELSE IF (LEN(@sql) - LEN(REPLACE(@sql, @find, ''))) / LEN(@find) <> 1
    RAISERROR('GetItemDemmandsAll: expected exactly one purchase order WHERE clause - procedure has changed, nothing applied.', 16, 1);
ELSE
BEGIN
    SET @sql = STUFF(@sql, CHARINDEX(N'CREATE PROCEDURE', @sql), LEN(N'CREATE PROCEDURE'), N'ALTER PROCEDURE');
    SET @sql = REPLACE(@sql, @find, @repl);
    EXEC sp_executesql @sql;
    PRINT 'GetItemDemmandsAll: deleted and cancelled purchase orders no longer counted as incoming supply.';
END
GO

-------------------------------------------------------------------------------
-- 2. GetFGDemmandsAll
-------------------------------------------------------------------------------
DECLARE @sql2 nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID('dbo.GetFGDemmandsAll'));
DECLARE @narrow2 nvarchar(200) = N'ISNULL(dh.[Status], '''') <> ''Cancelled''';

IF @sql2 IS NULL
    RAISERROR('GetFGDemmandsAll not found - nothing changed.', 16, 1);
ELSE IF CHARINDEX(N'NOT IN (''Deleted'', ''Cancelled'')', @sql2) > 0
    PRINT 'GetFGDemmandsAll: already correct, nothing to do.';
ELSE IF CHARINDEX(@narrow2, @sql2) = 0
    RAISERROR('GetFGDemmandsAll: expected purchase order status filter not found - nothing applied.', 16, 1);
ELSE
BEGIN
    SET @sql2 = STUFF(@sql2, CHARINDEX(N'CREATE PROCEDURE', @sql2), LEN(N'CREATE PROCEDURE'), N'ALTER PROCEDURE');
    SET @sql2 = REPLACE(@sql2, @narrow2, N'ISNULL(dh.[Status], '''') NOT IN (''Deleted'', ''Cancelled'')');
    EXEC sp_executesql @sql2;
    PRINT 'GetFGDemmandsAll: widened to exclude Deleted and Cancelled.';
END
GO

-------------------------------------------------------------------------------
-- 3. GetProdRMDemands
-------------------------------------------------------------------------------
DECLARE @sql3 nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID('dbo.GetProdRMDemands'));
DECLARE @narrow3 nvarchar(200) = N'ISNULL(d.[Status], '''') <> ''Cancelled''';

IF @sql3 IS NULL
    RAISERROR('GetProdRMDemands not found - nothing changed.', 16, 1);
ELSE IF CHARINDEX(N'NOT IN (''Deleted'', ''Cancelled'')', @sql3) > 0
    PRINT 'GetProdRMDemands: already correct, nothing to do.';
ELSE IF CHARINDEX(@narrow3, @sql3) = 0
    RAISERROR('GetProdRMDemands: expected purchase order status filter not found - nothing applied.', 16, 1);
ELSE
BEGIN
    SET @sql3 = STUFF(@sql3, CHARINDEX(N'CREATE PROCEDURE', @sql3), LEN(N'CREATE PROCEDURE'), N'ALTER PROCEDURE');
    SET @sql3 = REPLACE(@sql3, @narrow3, N'ISNULL(d.[Status], '''') NOT IN (''Deleted'', ''Cancelled'')');
    EXEC sp_executesql @sql3;
    PRINT 'GetProdRMDemands: widened to exclude Deleted and Cancelled.';
END
GO
