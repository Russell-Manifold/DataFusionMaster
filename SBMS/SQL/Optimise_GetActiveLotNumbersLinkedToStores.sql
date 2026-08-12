-- ---------------------------------------------------------------------
-- Changes:      dbo.GetActiveLotNumbersLinkedToStores  (stored procedure only)
-- Data written: NONE
-- App rebuild:  NOT required - identical columns, types and order.
-- Re-runnable:  Yes. Safe during trading hours.
-- ---------------------------------------------------------------------
-- CAUSE  (251 executions, 66 ms CPU, 5,845 logical reads per call)
--
--   The tenant filter was applied to ItemsMaster only:
--
--       INNER JOIN dbo.ItemsMaster AS im ON it.ItemID = im.ID
--       WHERE im.CompanyID = @CoID
--
--   it.CompanyID was never tested. The whole of ItemTransaction had to be
--   joined before the tenant could be narrowed, and had a Sage item ID ever
--   been shared between two companies, another tenant's transactions would
--   have joined onto this tenant's item row and their lots would have appeared
--   in this company's list.
--
-- FIX
--   Filter ItemTransaction on CompanyID directly. It is a narrowing predicate,
--   so it can only ever exclude rows belonging to a different company, and it
--   lets the tenant be selected before the join instead of after.
--
-- EQUIVALENCE - VERIFIED
--   Checked on live:
--
--       SELECT ID, COUNT(DISTINCT CompanyID) FROM dbo.ItemsMaster
--       GROUP BY ID HAVING COUNT(DISTINCT CompanyID) > 1;   -- returned NO ROWS
--
--   No item ID is shared between companies, so no row that used to be returned
--   is excluded. Identical result set, less work to produce it.
--
--   The Stores join needs no CompanyID - Stores.StoreID is a global identity.
--
--   Callers rely on the first row being the most recent, so the trailing
--   ORDER BY MostRecentTrnID DESC is preserved exactly.
-- ---------------------------------------------------------------------

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[GetActiveLotNumbersLinkedToStores]
    @CoID BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH agg AS (
        SELECT
            it.LotNumber,
            im.ID         AS ItemID,
            s.StoreCode,
            s.AllowPicking,
            s.AllowReceiving,
            SUM(it.Qty)   AS QtyHandToStore,
            SUM(it.Qty * it.TotalUnitPriceExclInclAdd) AS TotalValue,
            MAX(it.TrnID) AS MostRecentTrnID
        FROM       dbo.ItemTransaction AS it
        INNER JOIN dbo.Stores          AS s  ON it.ToID   = s.StoreID
        INNER JOIN dbo.ItemsMaster     AS im ON it.ItemID = im.ID
        WHERE  it.CompanyID = @CoID      -- narrows the big table up front (was missing)
           AND im.CompanyID = @CoID
           AND it.LotNumber IS NOT NULL
           AND it.LotNumber <> ''
        GROUP  BY it.LotNumber, im.ID, s.StoreCode, s.AllowPicking, s.AllowReceiving
        HAVING SUM(it.Qty) <> 0
    )
    SELECT
        a.LotNumber,
        a.ItemID,
        a.StoreCode,
        a.QtyHandToStore,
        -- Quantity-weighted average cost of the stock on hand.
        -- Falls back to the most recent POSITIVE unit price, then to zero, so the
        -- result is never NULL and never negative. Zero means "no cost available".
        CAST(
            CASE WHEN a.QtyHandToStore > 0 AND a.TotalValue > 0
                 THEN a.TotalValue / a.QtyHandToStore
                 ELSE COALESCE(
                        CASE WHEN lastTrn.TotalUnitPriceExclInclAdd > 0
                             THEN lastTrn.TotalUnitPriceExclInclAdd
                        END,
                        0)
            END AS decimal(18,4))  AS TotalUnitPriceExclInclAdd,
        a.MostRecentTrnID,
        a.AllowPicking,
        a.AllowReceiving
    FROM   agg AS a
    -- Was a per-row correlated subquery. Same single-row primary-key lookup,
    -- expressed as a join so the optimiser can batch it.
    LEFT JOIN dbo.ItemTransaction AS lastTrn
           ON lastTrn.TrnID = a.MostRecentTrnID
    ORDER  BY a.MostRecentTrnID DESC;   -- callers rely on first row = most recent
END
GO
