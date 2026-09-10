/* Why did a manufacture post nothing? Read-only. Set the two values and run. */
DECLARE @CoID  BIGINT = 869817;
DECLARE @WONum INT    = 9;

DECLARE @WOID BIGINT = (SELECT TOP 1 ID FROM dbo.WorksOrderHeader WHERE CompanyID = @CoID AND WONum = @WONum);
SELECT @WOID AS WOID_Resolved;

-- 1. The lines: what was to be made, and whether the app thinks it was.
SELECT  l.LineID, l.ItemCode, l.Quantity, l.OrderedQty, l.Complete, l.Active, l.ToStoreID, l.LotNumber
FROM    dbo.WorksOrderLines l
WHERE   l.CompanyID = @CoID AND l.WOID = @WOID
ORDER BY l.LineID;

-- 2. "Already posted" claims. An 'H|' row here with NO matching MANF movement in
--    grid 3 = the finished-goods post was skipped silently. That is the smoking gun.
SELECT  m.AdjKey, m.PostedBy, m.*
FROM    dbo.ManfPostedAdjustments m
WHERE   m.CompanyID = @CoID AND m.WOID = @WOID
ORDER BY m.AdjKey;

-- 3. What actually moved. MANF = finished goods in; DRAW = components out.
--    Trailing space in the LIKE stops WO9 matching WO90-WO99.
SELECT  t.TrnID, t.TransactionDate, t.TransactionType, t.ItemCode, t.Qty, t.ToID, t.LotNumber, t.TransactionReference
FROM    dbo.ItemTransaction t
WHERE   t.CompanyID = @CoID
  AND   t.TransactionReference LIKE '%WO' + CAST(@WONum AS VARCHAR(10)) + ' %'
ORDER BY t.TransactionDate;
