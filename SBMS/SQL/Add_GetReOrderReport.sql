-- Re-Order Report: one row per item - what is on hand, what is already on order,
-- what is committed out, and how much to buy.
--
-- Deliberate decisions, so a later reader does not "fix" them into bugs:
--
-- 1. ON HAND is the ItemTransaction ledger - ISNULL(SUM(it.Qty), 0) scoped by CompanyID,
--    the same expression GetItemsQtyOnHandMaster and sp_GetItemsForDisplay use. That ledger
--    is where stock totals come from in this application; ItemsMaster.QuantityOnHand is the
--    Sage figure and is NOT used here.
--
-- 2. COMMITTED is only what has NOT yet come off that ledger, because On Hand above has
--    already been reduced by everything that has. So:
--      - picking slips and job cards: outstanding lines only (PickComplete = 0). A completed
--        line has already written its stock transaction, so counting it again would
--        double-count the demand and inflate the recommended order.
--      - works orders: Quantity - UseQty, for the same reason - the drawn part is already out.
--    PickComplete is the flag rather than the picked quantity: PickQty is NULL on 111 of the
--    434 picked lines on open slips in live data, so it cannot be relied on to net down a
--    partly picked line.
--
-- 3. RE-ORDER LEVEL defaults to 1 when the item has no level captured. The @HideDormant
--    filter exists because of that default - see the note on it at the bottom. ReorderLevel is
--    unpopulated in almost every company (4 companies out of 61 have any item with a
--    level set), so without a default this report would be blank for everyone. ABS()
--    because ItemEdit can store the level negative.
--
-- 4. Works order components are read from WorksOrderRMLines, so a works order whose
--    components were tailored on the Edit Components screen reports its REAL quantities,
--    not the BOM's.
--
-- Deploy: run before the build. No EDMX change - the page calls this with
-- Database.SqlQuery<T>, the same way ItemsHeaders calls sp_GetItemsForDisplay.

IF OBJECT_ID('dbo.GetReOrderReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.GetReOrderReport;
GO

CREATE PROCEDURE [dbo].[GetReOrderReport]
    @CoID             bigint,
    @SearchText       nvarchar(200) = NULL,
    @OnlyBelow        bit = 1,
    @IncludeForecasts bit = 0,
    @HideDormant      bit = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF @SearchText = '' SET @SearchText = NULL;

    ;WITH itm AS
    (
        SELECT
            i.ID,
            i.Code,
            i.[Description],
            i.CategoryDescript,
            i.Unit,
            ISNULL(led.QOH, 0) AS QtyOnHand,
            -- No level captured -> treat as 1, otherwise the report is empty for everyone.
            CASE WHEN ABS(ISNULL(i.ReorderLevel, 0)) > 0
                 THEN ABS(i.ReorderLevel) ELSE 1 END AS ReOrderLevel,
            ABS(ISNULL(i.MinReOrderQty, 0)) AS MinReOrderQty
        FROM dbo.ItemsMaster i WITH (NOLOCK)
        LEFT JOIN (
            -- Same ledger total as GetItemsQtyOnHandMaster / sp_GetItemsForDisplay.
            SELECT ItemID, ISNULL(SUM(Qty), 0) AS QOH
            FROM dbo.ItemTransaction WITH (NOLOCK)
            WHERE CompanyID = @CoID
            GROUP BY ItemID
        ) led ON led.ItemID = i.ID
        WHERE i.CompanyID = @CoID
          AND i.Active = 1
          AND i.Physical = 1
          AND (@SearchText IS NULL
               OR i.Code LIKE '%' + @SearchText + '%'
               OR i.[Description] LIKE '%' + @SearchText + '%'
               OR i.CategoryDescript LIKE '%' + @SearchText + '%')
    ),
    -- Outstanding purchase orders: what is already coming in.
        -- NOT filtered on DocHeader.Active: every purchase order header in live data has
        -- Active = 0 (all 5,666 of them), so an Active = 1 filter silently reports nothing on
        -- order for every item in every company. GetProdRMDemands has that fault;
        -- GetItemDemmandsAll does not filter on it at all. Status is the field that means
        -- something - Pending / Overdue / Confirmed are live orders; Deleted and
        -- Cancelled are not and never arrive. QtyLeft is trustworthy: the lines carrying 0 are either already fully
        -- received or non-stock service/GL lines that never match a physical item anyway.
    po AS
    (
        SELECT l.SelectionId AS ItemID, SUM(ISNULL(l.QtyLeft, 0)) AS Qty
        FROM dbo.DocHeader d WITH (NOLOCK)
        INNER JOIN dbo.DocLines l WITH (NOLOCK) ON d.DocID = l.DocID
        WHERE d.DocType = 1
          AND d.CompanyID = @CoID
          AND ISNULL(d.[Status], '') NOT IN ('Deleted', 'Cancelled')
          AND l.ReceiveComplete = 0
        GROUP BY l.SelectionId
    ),
    -- Open picking slips, UNPICKED lines only - a picked line is already out of the ledger.
    -- EXISTS rather than a join to DocHeader: joining fans the line out once per linked
    -- document and silently doubles the committed quantity.
    ps AS
    (
        SELECT psl.SelectionId AS ItemID, SUM(ISNULL(psl.Quantity, 0)) AS Qty
        FROM dbo.PickSlipLines psl WITH (NOLOCK)
        INNER JOIN dbo.PickingSlipMaster psm WITH (NOLOCK) ON psm.PSID = psl.PSID
        WHERE psm.PSActive = 1
          AND ISNULL(psl.PickComplete, 0) = 0
          AND EXISTS (SELECT 1 FROM dbo.DocHeader dh WITH (NOLOCK)
                      WHERE dh.LinkedPSID = psm.PSID AND dh.DocType = 5 AND dh.CompanyID = @CoID)
        GROUP BY psl.SelectionId
    ),
    -- Open job cards, UNPICKED lines only - a completed job card line writes its stock
    -- transaction for the FULL line quantity, so it is already out of the ledger.
    -- JobCardsMaster stores the company on CustomerID.
    jc AS
    (
        SELECT jcl.SelectionId AS ItemID, SUM(ISNULL(jcl.Quantity, 0)) AS Qty
        FROM dbo.JobCardLines jcl WITH (NOLOCK)
        INNER JOIN dbo.JobCardsMaster jcm WITH (NOLOCK) ON jcm.JCID = jcl.JCID
        WHERE jcm.CustomerID = @CoID
          AND jcm.JCActive = 1
          AND jcl.Physical = 1
          AND ISNULL(jcl.PickComplete, 0) = 0
        GROUP BY jcl.SelectionId
    ),
    -- Works order raw materials still to be drawn. Net of UseQty - see note 2 above.
    wo AS
    (
        SELECT r.SelectionId AS ItemID,
               SUM(CASE WHEN ISNULL(r.Quantity, 0) - ISNULL(r.UseQty, 0) > 0
                        THEN ISNULL(r.Quantity, 0) - ISNULL(r.UseQty, 0) ELSE 0 END) AS Qty
        FROM dbo.WorksOrderRMLines r WITH (NOLOCK)
        INNER JOIN dbo.WorksOrderHeader h WITH (NOLOCK) ON h.ID = r.WOID
        LEFT JOIN dbo.WorksOrderLines wl WITH (NOLOCK) ON wl.LineID = r.LinkedWOLineID
        WHERE r.CompanyID = @CoID
          AND h.Active = 1
          AND ISNULL(wl.Complete, 0) = 0
          AND ISNULL(r.Physical, 1) = 1
        GROUP BY r.SelectionId
    ),
    -- Sales forecasts. Off by default: a forecast is not an order, and buying against one
    -- stocks up for sales that may never happen.
    fc AS
    (
        SELECT fl.SelectionId AS ItemID, SUM(ISNULL(fl.Quantity, 0)) AS Qty
        FROM dbo.ForCastHeader fh WITH (NOLOCK)
        INNER JOIN dbo.ForCastLines fl WITH (NOLOCK) ON fh.ID = fl.DocID
        WHERE fh.CompanyID = @CoID
          AND fl.Active = 1
          AND fl.DueDelDate IS NOT NULL
        GROUP BY fl.SelectionId
    ),
    calc AS
    (
        SELECT
            itm.Code                AS ItemCode,
            itm.[Description]       AS [Description],
            ISNULL(itm.CategoryDescript, '') AS CategoryDescript,
            ISNULL(itm.Unit, '')    AS Unit,
            itm.QtyOnHand,
            ISNULL(po.Qty, 0)       AS QtyOnOrder,
            ISNULL(ps.Qty, 0) + ISNULL(jc.Qty, 0) + ISNULL(wo.Qty, 0)
                + CASE WHEN @IncludeForecasts = 1 THEN ISNULL(fc.Qty, 0) ELSE 0 END AS QtyCommitted,
            itm.ReOrderLevel,
            itm.MinReOrderQty
        FROM itm
        LEFT JOIN po ON po.ItemID = itm.ID
        LEFT JOIN ps ON ps.ItemID = itm.ID
        LEFT JOIN jc ON jc.ItemID = itm.ID
        LEFT JOIN wo ON wo.ItemID = itm.ID
        LEFT JOIN fc ON fc.ItemID = itm.ID
    ),
    avail AS
    (
        SELECT c.*, c.QtyOnHand + c.QtyOnOrder - c.QtyCommitted AS QtyAvailable
        FROM calc c
    )
    SELECT
        a.ItemCode,
        a.[Description],
        a.CategoryDescript,
        a.Unit,
        a.QtyOnHand,
        a.QtyOnOrder,
        a.QtyCommitted,
        a.QtyAvailable,
        a.ReOrderLevel,
        -- Top back up to the re-order level, but never order less than the item's minimum
        -- order quantity when one is set.
        CASE WHEN a.QtyAvailable < a.ReOrderLevel
             THEN CASE WHEN a.MinReOrderQty > (a.ReOrderLevel - a.QtyAvailable)
                       THEN a.MinReOrderQty
                       ELSE a.ReOrderLevel - a.QtyAvailable END
             ELSE 0 END AS RecommendedQty
    FROM avail a
    WHERE (@OnlyBelow = 0 OR a.QtyAvailable < a.ReOrderLevel)
      -- Dormant items: nothing on hand, nothing on order, nothing committed - nobody is
      -- buying or selling them. With the default re-order level of 1 every one of these
      -- would be flagged "order 1": on the largest live company that is 11,033 rows of
      -- noise out of 11,276. Hidden by default, and the screen can switch them back on.
      AND (@HideDormant = 0
           OR a.QtyOnHand <> 0 OR a.QtyOnOrder <> 0 OR a.QtyCommitted <> 0)
    ORDER BY a.ItemCode;
END
GO
