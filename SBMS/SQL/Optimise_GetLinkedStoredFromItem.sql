-- ---------------------------------------------------------------------
-- Changes:      dbo.GetLinkedStoredFromItem  (stored procedure only)
-- Data written: NONE
-- App rebuild:  NOT required - identical columns, types and order.
-- Re-runnable:  Yes. Safe during trading hours.
-- ---------------------------------------------------------------------
-- CAUSE  (323 executions, 19,295 ms CPU, 25,236 logical reads per call)
--
--   The last-price join used a CORRELATED subquery:
--
--       LEFT JOIN dbo.ItemTransaction AS lp
--              ON lp.TrnID = (SELECT TOP 1 TrnID FROM dbo.ItemTransaction
--                              WHERE CompanyID = @CoID
--                                AND ItemID = l.ItemID
--                                AND ToID   = s.StoreID
--                              ORDER BY TrnID DESC)
--
--   That runs once for EVERY item/store link row, each one its own ordered
--   read of ItemTransaction. With a few thousand links that is a few thousand
--   separate lookups per call - the 25k reads.
--
-- FIX
--   The proc already aggregates ItemTransaction by exactly (CompanyID, ItemID,
--   ToID) to get QOH. "TOP 1 TrnID ORDER BY TrnID DESC" over that same grouping
--   is simply MAX(TrnID), so it comes free out of the aggregate already being
--   computed. One pass replaces N correlated lookups.
--
-- EQUIVALENCE
--   Same grouping keys, same predicate (CompanyID = @CoID), so MAX(TrnID)
--   returns the identical row the subquery selected. Where an item/store has no
--   transactions the aggregate produces no row, tx.LastTrnID is NULL and the
--   LEFT JOIN to lp yields NULL - exactly as the subquery returning NULL did.
--   QOH still comes through ISNULL(...,0) for unmatched links.
-- ---------------------------------------------------------------------

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[GetLinkedStoredFromItem]
    @CoID bigint
AS
BEGIN
    SET NOCOUNT ON;

    -- One aggregate pass over this company's ledger: balance AND the most
    -- recent TrnID per item/store. The MAX replaces the per-row subquery.
    ;WITH tx AS
    (
        SELECT
            CompanyID,
            ItemID,
            ToID       AS StoreID,
            SUM(Qty)   AS QOH,
            MAX(TrnID) AS LastTrnID
        FROM   dbo.ItemTransaction
        WHERE  CompanyID = @CoID
        GROUP BY CompanyID, ItemID, ToID
    )
    SELECT
        s.StoreCode,
        s.StoreID,
        s.StoreDescript,
        l.ItemID,
        s.AllowPicking,
        ISNULL(tx.QOH, 0) AS QOH,
        lp.TotalUnitPriceExclInclAdd
    FROM       dbo.ItemStoreLinkMaster AS l
    INNER JOIN dbo.Stores AS s
            ON l.StoreID   = s.StoreID
           AND l.CompanyID = s.CompanyID
    LEFT JOIN  tx
            ON tx.CompanyID = l.CompanyID
           AND tx.ItemID    = l.ItemID
           AND tx.StoreID   = s.StoreID
    -- Seek on the primary key using the TrnID the aggregate already found.
    LEFT JOIN  dbo.ItemTransaction AS lp
            ON lp.TrnID = tx.LastTrnID
    WHERE  l.CompanyID  = @CoID
      AND  s.StoreActive = 1;
END
GO

-- ---------------------------------------------------------------------
-- Supporting index: makes the aggregate a covered seek instead of touching
-- the base table. Narrower and better ordered than the wide index added by
-- Optimise_GetOpeningBalancesAllStores.sql - if plans stop using that one,
-- consider dropping it to save write overhead on ItemTransaction.
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_ItemTransaction_Co_Item_To_Qty'
                 AND object_id = OBJECT_ID('dbo.ItemTransaction'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ItemTransaction_Co_Item_To_Qty
        ON dbo.ItemTransaction (CompanyID, ItemID, ToID)
        INCLUDE (Qty, TrnID);
END
GO

-- ---------------------------------------------------------------------
-- VERIFY - capture OLD output first, then NEW, and confirm both directions
-- of the difference are empty. Expect ZERO rows.
--
--   SELECT * INTO #old FROM ...   -- old proc output
--   SELECT * INTO #new FROM ...   -- new proc output
--   SELECT 'in old not new' AS Side, * FROM (SELECT * FROM #old EXCEPT SELECT * FROM #new) a
--   UNION ALL
--   SELECT 'in new not old', * FROM (SELECT * FROM #new EXCEPT SELECT * FROM #old) b;
-- ---------------------------------------------------------------------
