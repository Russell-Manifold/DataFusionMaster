-- =====================================================================
-- FIX: Works Order fulfilment shows an inflated unit cost (up to ~2x)
--
-- Changes:      dbo.GetActiveLotNumbersLinkedToStores  (stored procedure only)
-- Data written: NONE
-- App rebuild:  NOT required - same column names and types, EDMX untouched.
-- Re-runnable:  Yes. Safe during trading hours.
--
-- ---------------------------------------------------------------------
-- CAUSE
--   TotalUnitPriceExclInclAdd was in the GROUP BY, so one lot/store returned
--   one row PER DISTINCT HISTORIC UNIT COST instead of a single row. The
--   screens take the cost from the first row but sum the quantity across all
--   of them, so the cost shown is one historic price rather than the blended
--   cost of the stock on hand.
--
-- FIX
--   Aggregate the cost instead of grouping by it. One row per lot/store,
--   carrying the quantity-weighted average cost of the stock on hand.
--   Where no meaningful cost exists the column returns 0 (never NULL, never
--   negative) - EF maps it to a NON-NULLABLE decimal, and the application
--   already refuses to draw stock at zero cost.
--
-- MEASURED ON A RESTORED COPY OF LIVE (all 31 tenants, 14,603 lot/store groups)
--                       OLD        NEW
--   rows returned      14,853     14,603     (250 duplicate rows collapse)
--   negative costs          1          0     (repaired)
--   zero costs          6,966      6,957     (groups with no cost data - unchanged)
--   positive costs          -      7,646     (now correctly blended)
--
--   Duplicated lot/store combinations, before -> after:
--     Biz Afrika 925                65 -> 0
--     TECMO AUTOMATION              60 -> 0
--     CHEMZONE CC                   43 -> 0
--     Torch Tech Supplies            7 -> 0
--     Colin Steel & Fittings         7 -> 0
--     Jacques Germanier              1 -> 0
--
-- CALLERS (all verified compatible)
--   Works Order Manufacture, Works Order Close-Off, Job Card, Picking Slip,
--   mobile Picking Slip. Each filters by item/store then either sums the
--   quantity or takes the first cost. Summing one row gives the same total the
--   duplicates gave; taking the first cost now gives the blended figure.
--   The lot dropdowns also stop listing the same lot several times.
--
-- REVERT
--   Original procedure body is reproduced at the foot of this file.
-- =====================================================================

SET NOCOUNT ON;
GO

PRINT '';
PRINT '=========== BEFORE ===========';
GO

IF OBJECT_ID('tempdb..#before') IS NOT NULL DROP TABLE #before;

;WITH oldRule AS (
    SELECT im.CompanyID, s.StoreCode, it.LotNumber, im.ID AS ItemID
    FROM       dbo.ItemTransaction AS it
    INNER JOIN dbo.Stores          AS s  ON it.ToID   = s.StoreID
    INNER JOIN dbo.ItemsMaster     AS im ON it.ItemID = im.ID
    WHERE  it.LotNumber IS NOT NULL AND it.LotNumber <> ''
    GROUP  BY im.CompanyID, s.StoreCode, it.LotNumber, im.ID, it.TotalUnitPriceExclInclAdd
    HAVING SUM(it.Qty) <> 0
)
SELECT CompanyID, StoreCode, LotNumber, ItemID, COUNT(*) AS CostRows
INTO   #before
FROM   oldRule
GROUP  BY CompanyID, StoreCode, LotNumber, ItemID
HAVING COUNT(*) > 1;

SELECT ISNULL(cm.CompanyName, '(unknown)') AS CompanyName,
       b.CompanyID,
       COUNT(*)        AS DuplicatedLotStoreCombinations,
       SUM(b.CostRows) AS TotalDuplicateRows
FROM   #before b
LEFT   JOIN dbo.CompanyMaster cm ON cm.SBCACoID = b.CompanyID
GROUP  BY cm.CompanyName, b.CompanyID
ORDER  BY COUNT(*) DESC;
GO

-- ---------------------------------------------------------------------
-- APPLY (create-then-alter so it works whether or not the proc exists)
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.GetActiveLotNumbersLinkedToStores', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.GetActiveLotNumbersLinkedToStores @CoID BIGINT AS SET NOCOUNT ON;');
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
        WHERE  im.CompanyID = @CoID
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
        -- result is never NULL and never negative. Zero means "no cost available",
        -- which the application already blocks rather than posting.
        CAST(
            CASE WHEN a.QtyHandToStore > 0 AND a.TotalValue > 0
                 THEN a.TotalValue / a.QtyHandToStore
                 ELSE COALESCE(
                        (SELECT TOP 1
                                CASE WHEN it2.TotalUnitPriceExclInclAdd > 0
                                     THEN it2.TotalUnitPriceExclInclAdd
                                END
                           FROM dbo.ItemTransaction it2
                          WHERE it2.TrnID = a.MostRecentTrnID),
                        0)
            END AS decimal(18,4))  AS TotalUnitPriceExclInclAdd,
        a.MostRecentTrnID,
        a.AllowPicking,
        a.AllowReceiving
    FROM   agg AS a
    ORDER  BY a.MostRecentTrnID DESC;   -- callers rely on first row = most recent
END
GO

-- ---------------------------------------------------------------------
-- AFTER - run the live procedure for EVERY tenant and assert the result.
-- ---------------------------------------------------------------------
PRINT '';
PRINT '=========== AFTER ===========';
GO

IF OBJECT_ID('tempdb..#out')     IS NOT NULL DROP TABLE #out;
IF OBJECT_ID('tempdb..#results') IS NOT NULL DROP TABLE #results;

CREATE TABLE #out (
    LotNumber       varchar(100),
    ItemID          bigint,
    StoreCode       varchar(20),
    QtyHandToStore  decimal(18,4),
    TotalUnitPriceExclInclAdd decimal(18,4),
    MostRecentTrnID bigint,
    AllowPicking    bit,
    AllowReceiving  bit
);
CREATE TABLE #results (
    CompanyID bigint, CompanyName nvarchar(200), RowsReturned int,
    Duplicates int, NullCosts int, NegativeCosts int
);

DECLARE @CoID bigint, @Rows int, @Dupes int, @Nulls int, @Negs int;

DECLARE co CURSOR LOCAL FAST_FORWARD FOR
    SELECT DISTINCT SBCACoID FROM dbo.CompanyMaster WHERE SBCACoID IS NOT NULL
    UNION
    SELECT DISTINCT CompanyID FROM #before;

OPEN co;
FETCH NEXT FROM co INTO @CoID;
WHILE @@FETCH_STATUS = 0
BEGIN
    TRUNCATE TABLE #out;
    INSERT INTO #out EXEC dbo.GetActiveLotNumbersLinkedToStores @CoID;

    SELECT @Rows = COUNT(*) FROM #out;

    SELECT @Dupes = COUNT(*) FROM (
        SELECT LotNumber, ItemID, StoreCode
        FROM   #out
        GROUP  BY LotNumber, ItemID, StoreCode
        HAVING COUNT(*) > 1) d;

    SELECT @Nulls = ISNULL(SUM(CASE WHEN TotalUnitPriceExclInclAdd IS NULL THEN 1 ELSE 0 END), 0),
           @Negs  = ISNULL(SUM(CASE WHEN TotalUnitPriceExclInclAdd <  0    THEN 1 ELSE 0 END), 0)
    FROM   #out;

    IF @Rows > 0
        INSERT INTO #results (CompanyID, CompanyName, RowsReturned, Duplicates, NullCosts, NegativeCosts)
        SELECT @CoID,
               (SELECT TOP 1 CompanyName FROM dbo.CompanyMaster WHERE SBCACoID = @CoID),
               @Rows, @Dupes, @Nulls, @Negs;

    FETCH NEXT FROM co INTO @CoID;
END
CLOSE co; DEALLOCATE co;

SELECT * FROM #results ORDER BY RowsReturned DESC;

DECLARE @BadDupes int = (SELECT ISNULL(SUM(Duplicates),0)    FROM #results);
DECLARE @BadNulls int = (SELECT ISNULL(SUM(NullCosts),0)     FROM #results);
DECLARE @BadNegs  int = (SELECT ISNULL(SUM(NegativeCosts),0) FROM #results);

DROP TABLE #out;
DROP TABLE #results;
DROP TABLE #before;

PRINT '';
IF @BadDupes = 0 AND @BadNulls = 0 AND @BadNegs = 0
    PRINT '*** PASS - one row per lot/store for every tenant, no NULL or negative costs. ***';
ELSE
BEGIN
    PRINT '*** FAIL - see the result grid above. ***';
    RAISERROR('Lot cost fix verification FAILED: %d duplicates, %d NULL costs, %d negative costs.',
              16, 1, @BadDupes, @BadNulls, @BadNegs);
END
GO


-- =====================================================================
-- REVERT - original procedure body, reference only.
-- =====================================================================
/*
ALTER PROCEDURE [dbo].[GetActiveLotNumbersLinkedToStores]
    @CoID BIGINT
AS
BEGIN
SELECT
    it.LotNumber,
    im.ID AS ItemID,
    s.StoreCode,
    SUM(it.Qty) AS QtyHandToStore,
    it.TotalUnitPriceExclInclAdd,
    MAX(it.TrnID) AS MostRecentTrnID,
    s.AllowPicking,
    s.AllowReceiving
FROM dbo.ItemTransaction AS it
INNER JOIN dbo.Stores AS s ON it.ToID = s.StoreID
INNER JOIN dbo.ItemsMaster AS im ON it.ItemID = im.ID
WHERE im.CompanyID = @CoID
    AND it.LotNumber IS NOT NULL
    AND it.LotNumber <> ''
GROUP BY s.StoreCode, it.LotNumber, im.ID, it.TotalUnitPriceExclInclAdd, s.AllowPicking, s.AllowReceiving
HAVING SUM(it.Qty) <> 0
ORDER BY MAX(it.TrnID) DESC;
END
*/
