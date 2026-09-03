-- Re-Order Report drill-down: the documents behind one item's numbers.
--
-- Every filter here MUST match dbo.GetReOrderReport exactly, or the detail will not add up
-- to the figure the user clicked on and they will stop trusting the report. Specifically:
--   - purchase orders: Status not Deleted/Cancelled, lines not fully received, QtyLeft
--                      (NOT DocHeader.Active - every live PO header has Active = 0)
--   - picking slips:   open slips, lines NOT yet picked (a picked line is already off stock)
--   - job cards:       active cards, physical lines NOT yet picked
--   - works orders:    active orders, incomplete lines, Quantity - UseQty
--   - forecasts:       returned only when @IncludeForecasts = 1, as on the report
--
-- If you change a rule in GetReOrderReport, change it here in the same commit.
--
-- On Hand is deliberately absent: it is a ledger balance, not a document. The existing
-- Stock/Lot Movement screen already shows the transactions behind it.

IF OBJECT_ID('dbo.GetReOrderItemDetail', 'P') IS NOT NULL
    DROP PROCEDURE dbo.GetReOrderItemDetail;
GO

CREATE PROCEDURE [dbo].[GetReOrderItemDetail]
    @CoID             bigint,
    @ItemCode         nvarchar(100),
    @IncludeForecasts bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ItemID bigint =
        (SELECT TOP 1 ID FROM dbo.ItemsMaster WITH (NOLOCK)
         WHERE CompanyID = @CoID AND Code = @ItemCode);

    IF @ItemID IS NULL RETURN;

    -- SortKey groups the sources in a fixed order on screen; Direction drives the +/- shown
    -- against the quantity (In = coming to stock, Out = leaving it).
    SELECT * FROM
    (
        SELECT
            1 AS SortKey,
            'Purchase Order' AS Source,
            'In' AS Direction,
            ISNULL(d.DocumentNumber, '') AS DocNumber,
            ISNULL(d.CustSupName, '') AS Reference,
            ISNULL(d.[Status], '') AS [Status],
            d.DueDelDate AS DueDate,
            ISNULL(l.QtyLeft, 0) AS Qty
        FROM dbo.DocHeader d WITH (NOLOCK)
        INNER JOIN dbo.DocLines l WITH (NOLOCK) ON d.DocID = l.DocID
        WHERE d.DocType = 1
          AND d.CompanyID = @CoID
          AND ISNULL(d.[Status], '') NOT IN ('Deleted', 'Cancelled')
          AND l.ReceiveComplete = 0
          AND l.SelectionId = @ItemID

        UNION ALL

        SELECT
            2 AS SortKey,
            'Picking Slip' AS Source,
            'Out' AS Direction,
            ISNULL(psm.PSIntNumber, '') AS DocNumber,
            ISNULL((SELECT TOP 1 dh.CustSupName FROM dbo.DocHeader dh WITH (NOLOCK)
                    WHERE dh.LinkedPSID = psm.PSID AND dh.DocType = 5 AND dh.CompanyID = @CoID), '') AS Reference,
            'Not picked' AS [Status],
            psm.PSDueDate AS DueDate,
            ISNULL(psl.Quantity, 0) AS Qty
        FROM dbo.PickSlipLines psl WITH (NOLOCK)
        INNER JOIN dbo.PickingSlipMaster psm WITH (NOLOCK) ON psm.PSID = psl.PSID
        WHERE psm.PSActive = 1
          AND ISNULL(psl.PickComplete, 0) = 0
          AND psl.SelectionId = @ItemID
          AND EXISTS (SELECT 1 FROM dbo.DocHeader dh WITH (NOLOCK)
                      WHERE dh.LinkedPSID = psm.PSID AND dh.DocType = 5 AND dh.CompanyID = @CoID)

        UNION ALL

        SELECT
            3 AS SortKey,
            'Job Card' AS Source,
            'Out' AS Direction,
            ISNULL(jcm.JCNumber, '') AS DocNumber,
            ISNULL(jcm.JCIntReference, '') AS Reference,
            ISNULL(jcm.JCStatus, '') AS [Status],
            jcl.LinePickDate AS DueDate,
            ISNULL(jcl.Quantity, 0) AS Qty
        FROM dbo.JobCardLines jcl WITH (NOLOCK)
        INNER JOIN dbo.JobCardsMaster jcm WITH (NOLOCK) ON jcm.JCID = jcl.JCID
        WHERE jcm.CustomerID = @CoID
          AND jcm.JCActive = 1
          AND jcl.Physical = 1
          AND ISNULL(jcl.PickComplete, 0) = 0
          AND jcl.SelectionId = @ItemID

        UNION ALL

        -- Reference names the finished good this material is for, so a buyer can see WHY the
        -- item is committed - and a works order whose components were tailored on the Edit
        -- Components screen shows its real quantity, not the BOM's.
        SELECT
            4 AS SortKey,
            'Works Order' AS Source,
            'Out' AS Direction,
            'WO-' + CAST(h.WONum AS varchar(20)) AS DocNumber,
            CASE WHEN ISNULL(r.LinkedFGCode, '') = '' THEN ISNULL(h.Reference, '')
                 ELSE 'For ' + r.LinkedFGCode END AS Reference,
            ISNULL(h.[Status], '') AS [Status],
            h.DueDate AS DueDate,
            ISNULL(r.Quantity, 0) - ISNULL(r.UseQty, 0) AS Qty
        FROM dbo.WorksOrderRMLines r WITH (NOLOCK)
        INNER JOIN dbo.WorksOrderHeader h WITH (NOLOCK) ON h.ID = r.WOID
        LEFT JOIN dbo.WorksOrderLines wl WITH (NOLOCK) ON wl.LineID = r.LinkedWOLineID
        WHERE r.CompanyID = @CoID
          AND h.Active = 1
          AND ISNULL(wl.Complete, 0) = 0
          AND ISNULL(r.Physical, 1) = 1
          AND r.SelectionId = @ItemID
          AND ISNULL(r.Quantity, 0) - ISNULL(r.UseQty, 0) > 0

        UNION ALL

        SELECT
            5 AS SortKey,
            'Sales Forecast' AS Source,
            'Out' AS Direction,
            ISNULL(fh.CustSupName, '') AS DocNumber,
            ISNULL(fh.Reference, '') AS Reference,
            '' AS [Status],
            fl.DueDelDate AS DueDate,
            ISNULL(fl.Quantity, 0) AS Qty
        FROM dbo.ForCastHeader fh WITH (NOLOCK)
        INNER JOIN dbo.ForCastLines fl WITH (NOLOCK) ON fh.ID = fl.DocID
        WHERE @IncludeForecasts = 1
          AND fh.CompanyID = @CoID
          AND fl.Active = 1
          AND fl.DueDelDate IS NOT NULL
          AND fl.SelectionId = @ItemID
    ) x
    ORDER BY x.SortKey, x.DueDate, x.DocNumber;
END
GO
