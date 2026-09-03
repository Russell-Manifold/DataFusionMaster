-- Fix: the Finished Goods Demands and Raw Materials Demands reports never show purchase
-- orders, so both screens present demand with nothing incoming to offset it.
--
-- Cause: both procedures filter the purchase order block on DocHeader.Active = 1. No
-- purchase order header in live data has Active = 1 - all 5,666 of them are 0 - so the PO
-- section of each UNION returns nothing, always. Proved on CoID 841099: GetFGDemmandsAll
-- returns Min, OH and PS rows and zero PO rows, for a company with ~5,000 open PO lines.
--
-- The field that carries meaning on a purchase order header is Status: Pending, Overdue and
-- Confirmed are live orders; Deleted and Cancelled are not. GetItemDemmandsAll already gets
-- this right by not filtering on Active at all.
--
-- Method: the procedure body is read back with OBJECT_DEFINITION and only the one filter is
-- substituted, so every other line stays byte-for-byte as it is today. Each block refuses to
-- run if it does not find exactly what it expects, rather than half-applying.
--
-- Only the PO blocks change. Every other "Active = 1" in these procedures is on a different
-- table (ItemsMaster, ForCastLines, JobCardsMaster, PickingSlipMaster, WorksOrderHeader,
-- ProdPlanLines) and those flags ARE properly populated - leave them alone.

SET NOCOUNT ON;
GO

-------------------------------------------------------------------------------
-- 1. GetFGDemmandsAll  (Finished Goods Demands screen)
-------------------------------------------------------------------------------
DECLARE @sql nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID('dbo.GetFGDemmandsAll'));
DECLARE @find nvarchar(200) = N'AND dh.Active = 1 AND dh.CompanyID';
DECLARE @repl nvarchar(400) = N'AND ISNULL(dh.[Status], '''') NOT IN (''Deleted'', ''Cancelled'') AND dh.CompanyID';

IF @sql IS NULL
    RAISERROR('GetFGDemmandsAll not found - nothing changed.', 16, 1);
ELSE IF (LEN(@sql) - LEN(REPLACE(@sql, @find, ''))) / LEN(@find) <> 1
    RAISERROR('GetFGDemmandsAll: expected exactly one PO Active filter - procedure has changed, nothing applied.', 16, 1);
ELSE
BEGIN
    SET @sql = STUFF(@sql, CHARINDEX(N'CREATE PROCEDURE', @sql), LEN(N'CREATE PROCEDURE'), N'ALTER PROCEDURE');
    SET @sql = REPLACE(@sql, @find, @repl);
    EXEC sp_executesql @sql;
    PRINT 'GetFGDemmandsAll: purchase order filter changed from Active to Status.';
END
GO

-------------------------------------------------------------------------------
-- 2. GetProdRMDemands  (Raw Materials Demands screen)
-------------------------------------------------------------------------------
DECLARE @sql2 nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID('dbo.GetProdRMDemands'));
DECLARE @find2 nvarchar(200) = N'And d.Active = 1';
DECLARE @repl2 nvarchar(400) = N'And ISNULL(d.[Status], '''') NOT IN (''Deleted'', ''Cancelled'')';

IF @sql2 IS NULL
    RAISERROR('GetProdRMDemands not found - nothing changed.', 16, 1);
ELSE IF (LEN(@sql2) - LEN(REPLACE(@sql2, @find2, ''))) / LEN(@find2) <> 1
    RAISERROR('GetProdRMDemands: expected exactly one PO Active filter - procedure has changed, nothing applied.', 16, 1);
ELSE
BEGIN
    SET @sql2 = STUFF(@sql2, CHARINDEX(N'CREATE PROCEDURE', @sql2), LEN(N'CREATE PROCEDURE'), N'ALTER PROCEDURE');
    SET @sql2 = REPLACE(@sql2, @find2, @repl2);
    EXEC sp_executesql @sql2;
    PRINT 'GetProdRMDemands: purchase order filter changed from Active to Status.';
END
GO
