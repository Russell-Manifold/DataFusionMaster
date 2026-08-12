-- ---------------------------------------------------------------------
-- Changes:      dbo.Fix_DocLines_ItemCodes  (stored procedure only)
-- Data written: Same end state as before - see EQUIVALENCE below.
-- App rebuild:  NOT required.
-- Re-runnable:  Yes. Safe during trading hours.
-- ---------------------------------------------------------------------
-- CAUSE  (779 executions, 109,720 ms CPU, 66,242 logical reads per call)
--
--   The proc runs at the end of EVERY ApiUrlCall.LoadItems(), which fires on
--   every login plus ItemQOHSync, ConfigMaster, ItemStoresLink, Notifications,
--   Planning, ProductionRMD and ProdPlanningC.
--
--   It rewrote DL.ItemCode for EVERY DocLine belonging to the company, whether
--   or not the value had changed. Writing an identical value is still a write:
--   row locks, transaction-log records and dirty pages, hundreds of times a day.
--
-- FIX
--   Add a predicate so only genuinely mismatched rows are updated. Item codes
--   drift only when someone edits the code in Sage, so in steady state this
--   now updates zero rows.
--
-- EQUIVALENCE
--   Rows where DL.ItemCode already equals IM.Code are skipped - they already
--   hold the value the UPDATE would have written. Rows that differ are still
--   corrected. The state of the table after the statement is identical; only
--   the amount of work differs. NULL is handled explicitly so a NULL ItemCode
--   against a non-NULL Code is still treated as a mismatch and repaired.
-- ---------------------------------------------------------------------

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[Fix_DocLines_ItemCodes]
    @CoID bigint
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE DL
    SET    DL.ItemCode = IM.Code
    FROM   dbo.DocLines   DL
    INNER JOIN dbo.ItemsMaster IM
        ON  DL.SelectionId = IM.ID
       AND  DL.CompanyID   = IM.CompanyID
    WHERE  DL.CompanyID = @CoID
       -- Only rows that have actually drifted. Without this the proc rewrote
       -- every DocLine on the company on every login.
       AND ISNULL(DL.ItemCode, N'') <> ISNULL(IM.Code, N'');
END
GO

-- ---------------------------------------------------------------------
-- Supporting index: lets the mismatch check seek rather than scan DocLines.
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_DocLines_Company_Selection'
                 AND object_id = OBJECT_ID('dbo.DocLines'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DocLines_Company_Selection
        ON dbo.DocLines (CompanyID, SelectionId)
        INCLUDE (ItemCode);
END
GO

-- ---------------------------------------------------------------------
-- VERIFY - run BEFORE and AFTER. Expect ZERO rows both times: no DocLine
-- should be left carrying a stale code.
-- ---------------------------------------------------------------------
-- SELECT DL.CompanyID, DL.LineID, DL.ItemCode AS DocLineCode, IM.Code AS MasterCode
-- FROM   dbo.DocLines DL
-- INNER JOIN dbo.ItemsMaster IM
--     ON DL.SelectionId = IM.ID AND DL.CompanyID = IM.CompanyID
-- WHERE  ISNULL(DL.ItemCode, N'') <> ISNULL(IM.Code, N'');
