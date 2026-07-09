-- Diagnose the Partially Received POs screen (OSPurchaseOrdersPartial).
DECLARE @CoID bigint = 1;   -- <<< set your CompanyID

-- 1) Exactly what the screen queries. Rows here = rows on screen.
SELECT h.DocID, h.DocumentNumber AS PONumber, h.DocDate, h.CustSupName AS Supplier,
       l.ItemCode, l.ItemDescription,
       l.Quantity                                AS OrderQty,
       l.Quantity - ISNULL(l.QtyLeft, 0)         AS Received,
       ISNULL(l.QtyLeft, 0)                      AS Balance
FROM dbo.DocHeaders h
JOIN dbo.DocLines   l ON l.DocID = h.DocID
WHERE h.CompanyID = @CoID
  AND h.DocType = 1
  AND ISNULL(h.Complete, 0) <> 1
  AND ISNULL(h.Status, '') <> 'Cancelled'
  AND ISNULL(l.QtyLeft, 0) > 0
  AND ISNULL(l.QtyLeft, 0) < ISNULL(l.Quantity, 0);

-- 2) Why lines are excluded: every PO line with ANY receiving history,
--    with a flag per filter that knocks it off the screen.
SELECT h.DocID, h.DocumentNumber AS PONumber, l.ItemCode,
       l.Quantity AS OrderQty, l.QtyLeft,
       (SELECT SUM(r.RecQty) FROM dbo.ReceivingOutstandings r
         WHERE r.PODocID = h.DocID AND r.Archive = 0
           AND (r.SBCALineID = l.SBCALineID OR (r.SBCALineID IS NULL AND r.ItemCode = l.ItemCode))
       ) AS HistReceived,
       CASE WHEN h.DocType <> 1                            THEN 'X' ELSE '' END AS Not_PO,
       CASE WHEN h.Active <> 1                             THEN 'X' ELSE '' END AS Inactive,
       CASE WHEN ISNULL(h.Complete, 0) = 1                 THEN 'X' ELSE '' END AS HeaderComplete,
       CASE WHEN h.Status = 'Cancelled'                    THEN 'X' ELSE '' END AS Cancelled,
       CASE WHEN l.QtyLeft IS NULL                         THEN 'X' ELSE '' END AS QtyLeft_NULL,
       CASE WHEN ISNULL(l.QtyLeft, 0) <= 0                 THEN 'X' ELSE '' END AS NoBalance,
       CASE WHEN ISNULL(l.QtyLeft, 0) >= ISNULL(l.Quantity, 0) THEN 'X' ELSE '' END AS NothingReceived
FROM dbo.DocHeaders h
JOIN dbo.DocLines   l ON l.DocID = h.DocID
WHERE h.CompanyID = @CoID
  AND EXISTS (SELECT 1 FROM dbo.ReceivingOutstandings r
               WHERE r.PODocID = h.DocID AND r.Archive = 0
                 AND (r.SBCALineID = l.SBCALineID OR (r.SBCALineID IS NULL AND r.ItemCode = l.ItemCode)))
ORDER BY h.DocID, l.LineID;
