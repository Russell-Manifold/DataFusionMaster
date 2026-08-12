-- ---------------------------------------------------------------------
-- Changes:      dbo.GetOpeningBalancesAllStores  (stored procedure only)
-- Data written: NONE
-- App rebuild:  NOT required - identical column names, types and order.
-- Re-runnable:  Yes.
-- ---------------------------------------------------------------------
-- CAUSE OF THE 12-SECOND RUNTIME (1,170 rows returned, 8.8s of it CPU)
--
--   1. "IT" was a CTE. SQL Server does not materialise CTEs - it re-runs the
--      definition at every reference. IT was referenced FOUR times, so
--      dbo.ItemTransaction was scanned four times per call.
--
--   2. QOHAtMax self-joined every transaction back to its own group's max:
--          ON it.ItemID = m.ItemID AND it.StoreID = m.StoreID
--         AND it.LotNum = m.LotNum AND it.TrnID  <= m.MaxTrnID
--      m.MaxTrnID is BY DEFINITION the largest TrnID in that exact group, so
--      "it.TrnID <= m.MaxTrnID" is true for every row in the group. The join
--      filtered nothing - it just summed the group, at self-join cost.
--
-- FIX
--   Materialise the company's rows once into #IT, then collapse MaxTrn and
--   QOHAtMax into a single GROUP BY. MAX(TrnID) and SUM(Qty) over the same
--   grouping keys are exactly what the two CTEs computed between them, so the
--   result set is unchanged.
--
-- VERIFY BEFORE TRUSTING IT - see the comparison script at the bottom.
-- ---------------------------------------------------------------------

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[GetOpeningBalancesAllStores]
    @CoID bigint
AS
BEGIN
    SET NOCOUNT ON;

    -------------------------------------------------------------------------
    -- Pull this company's transactions ONCE into a temp table. Was a CTE,
    -- which meant four separate scans of ItemTransaction per execution.
    -------------------------------------------------------------------------
    CREATE TABLE #IT
    (
        TrnID                     bigint        NOT NULL,
        ItemID                    bigint        NULL,
        ItemCode                  varchar(50)   NULL,
        ItemDescription           varchar(100)  NULL,
        PriceExclusive            decimal(18,4) NULL,
        TotalUnitPriceExclInclAdd decimal(18,4) NULL,
        Unit                      varchar(20)   NULL,
        Qty                       decimal(18,4) NULL,
        StoreID                   bigint        NULL,
        LotNum                    varchar(50)   NOT NULL
    );

    INSERT INTO #IT (TrnID, ItemID, ItemCode, ItemDescription, PriceExclusive,
                     TotalUnitPriceExclInclAdd, Unit, Qty, StoreID, LotNum)
    SELECT
        it.TrnID,
        it.ItemID,
        it.ItemCode,
        it.ItemDescription,
        it.PriceExclusive,
        it.TotalUnitPriceExclInclAdd,
        it.Unit,
        it.Qty,
        it.ToID,
        ISNULL(NULLIF(it.LotNumber, 'N/A'), '')
    FROM dbo.ItemTransaction it
    WHERE it.CompanyID = @CoID;

    -- Supports the grouping, the ROW_NUMBER partition and the TrnID lookups below.
    CREATE CLUSTERED INDEX IX_#IT ON #IT (ItemID, StoreID, LotNum, TrnID);

    -------------------------------------------------------------------------
    -- Balance per Item / Store / Lot.
    -- Replaces the old MaxTrn + QOHAtMax pair: MAX(TrnID) is the same TransID
    -- the self-join was keyed on, and SUM(Qty) over the group is exactly what
    -- "TrnID <= MaxTrnID" summed, because that predicate matched the whole group.
    -------------------------------------------------------------------------
    ;WITH QOHAtMax AS
    (
        SELECT
            ItemID,
            StoreID,
            LotNum,
            MAX(TrnID) AS TransID,
            SUM(Qty)   AS QOH
        FROM #IT
        GROUP BY ItemID, StoreID, LotNum
    ),

    -------------------------------------------------------------------------
    -- Latest price per Item / Store / Lot (unchanged, now over the temp table).
    -------------------------------------------------------------------------
    LatestPrices AS
    (
        SELECT
            ItemID,
            StoreID,
            LotNum,
            PriceExclusive,
            TotalUnitPriceExclInclAdd,
            Unit,
            ROW_NUMBER() OVER (
                PARTITION BY ItemID, StoreID, LotNum
                ORDER BY TrnID DESC
            ) AS rn
        FROM #IT
    )

    SELECT
        q.TransID,
        q.ItemID,
        it.ItemCode,
        it.ItemDescription,
        im.CategoryDescript AS Category,
        im.QuantityOnHand   AS TotalOnHand,
        s.StoreCode,
        q.StoreID,
        q.QOH,
        CASE WHEN q.LotNum = '' THEN NULL ELSE q.LotNum END AS LotNumber,
        lp.PriceExclusive,
        lp.TotalUnitPriceExclInclAdd,
        lp.Unit
    FROM QOHAtMax q
    INNER JOIN #IT it
        ON it.ItemID  = q.ItemID
       AND it.StoreID = q.StoreID
       AND it.TrnID   = q.TransID
    INNER JOIN dbo.ItemsMaster im
        ON im.CompanyID = @CoID
       AND im.ID        = q.ItemID
    INNER JOIN dbo.Stores s
        ON s.CompanyID = @CoID
       AND s.StoreID   = q.StoreID
    INNER JOIN LatestPrices lp
        ON lp.ItemID  = q.ItemID
       AND lp.StoreID = q.StoreID
       AND lp.LotNum  = q.LotNum
       AND lp.rn      = 1
    WHERE q.QOH <> 0
    ORDER BY it.ItemCode, q.StoreID, q.LotNum;

    DROP TABLE #IT;
END
GO

-- ---------------------------------------------------------------------
-- Supporting index on the base table. Makes the single remaining scan a
-- seek. Create it ONLINE if your edition allows, so it does not block.
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_ItemTransaction_Company_Item_To'
                 AND object_id = OBJECT_ID('dbo.ItemTransaction'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ItemTransaction_Company_Item_To
        ON dbo.ItemTransaction (CompanyID)
        INCLUDE (TrnID, ItemID, ItemCode, ItemDescription, PriceExclusive,
                 TotalUnitPriceExclInclAdd, Unit, Qty, ToID, LotNumber);
END
GO

-- ---------------------------------------------------------------------
-- VERIFY - run BEFORE trusting this in production.
-- Capture the OLD output first (from a restored backup or before the ALTER),
-- then the NEW, and confirm both directions of the difference are empty.
--
--   SELECT * INTO #old FROM ... -- old proc output
--   SELECT * INTO #new FROM ... -- new proc output
--   SELECT 'in old not new' AS Side, * FROM (SELECT * FROM #old EXCEPT SELECT * FROM #new) a
--   UNION ALL
--   SELECT 'in new not old', * FROM (SELECT * FROM #new EXCEPT SELECT * FROM #old) b;
--
-- Expect ZERO rows. Anything returned means the rewrite is not equivalent -
-- do not deploy it.
-- ---------------------------------------------------------------------
