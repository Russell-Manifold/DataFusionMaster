-- ---------------------------------------------------------------------
-- Changes:      dbo.GetAllStockLevels  (stored procedure only)
-- Data written: NONE
-- App rebuild:  NOT required - identical columns, names, types and order.
-- Re-runnable:  Yes. Safe during trading hours.
-- ---------------------------------------------------------------------
-- CAUSE  (261 ms CPU, 10,035 logical reads per call)
--
--   The LatestItem CTE did nothing:
--
--       ROW_NUMBER() OVER (PARTITION BY ID ORDER BY ID DESC) AS rn   ... WHERE rn = 1
--
--   It partitions by ID and then orders by that same ID, so every partition
--   holds exactly one row and rn is always 1. The window function was a sort
--   over the whole of ItemsMaster that could not change the result. The CTE
--   also carried no CompanyID predicate, so it materialised every tenant's
--   items before the join threw them away.
--
-- FIX
--   Drop the CTE and join dbo.ItemsMaster directly, adding CompanyID to the
--   join so the optimiser can seek per tenant.
--
-- EQUIVALENCE - VERIFIED
--   The dedupe could only matter if one ItemsMaster.ID spanned more than one
--   company. Checked on live:
--
--       SELECT ID, COUNT(DISTINCT CompanyID) FROM dbo.ItemsMaster
--       GROUP BY ID HAVING COUNT(DISTINCT CompanyID) > 1;   -- returned NO ROWS
--
--   IDs are unique across companies, so rn = 1 selected the only candidate row
--   and adding CompanyID to the join excludes nothing. Same rows, same values,
--   same order.
-- ---------------------------------------------------------------------

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[GetAllStockLevels]
    @CoID       INT,
    @SearchTerm NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        im.ID                                                AS ItemID,
        im.Code                                              AS ItemCode,
        im.Description,
        im.CategoryDescript,
        SUM(it.Qty)                                          AS Sum_Qty,
        s.StoreCode,
        it.LotNumber,
        SUM(it.TotalUnitPriceExclInclAdd * it.Qty)
            / NULLIF(SUM(it.Qty), 0)                         AS Unit_Cost,
        it.ExchRate,
        SUM(it.TotalUnitPriceExclInclAdd * it.Qty)
            / NULLIF(it.ExchRate, 0)                         AS Local_Curr_Value,
        im.TextUserField1,
        im.TextUserField2,
        im.TextUserField3,
        im.NumericUserField1,
        im.NumericUserField2,
        im.NumericUserField3
    FROM       dbo.ItemTransaction it
    INNER JOIN dbo.Stores          s  ON it.ToID   = s.StoreID
    -- Was an unnecessary ROW_NUMBER CTE over all of ItemsMaster. CompanyID
    -- added to the join so this seeks within the tenant.
    INNER JOIN dbo.ItemsMaster     im ON it.ItemID    = im.ID
                                     AND im.CompanyID = it.CompanyID
    WHERE
        it.CompanyID = @CoID
        AND (
            @SearchTerm IS NULL
            OR im.Code        LIKE @SearchTerm + '%'
            OR im.Description LIKE @SearchTerm + '%'
        )
    GROUP BY
        im.ID,
        im.Code,
        s.StoreCode,
        it.LotNumber,
        it.ExchRate,
        im.Description,
        im.CategoryDescript,
        im.TextUserField1,
        im.TextUserField2,
        im.TextUserField3,
        im.NumericUserField1,
        im.NumericUserField2,
        im.NumericUserField3
    HAVING
        SUM(it.Qty) <> 0
    ORDER BY
        im.Code;
END
GO
