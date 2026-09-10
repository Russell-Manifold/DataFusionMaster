/* ============================================================================
   PART MANUFACTURE - stock written to a store that does not exist
   ---------------------------------------------------------------------------
   A part-manufacture split created its BALANCE line without a store. That line's
   pane then opened on "-Store-", the store lookup returned 0, and the finished
   goods were written to ItemTransaction.ToID = 0 - invisible in every store, while
   Sage (which has no stores) still received the adjustment.

   The code fix stops this happening again. It does NOT repair rows already
   written. This script only FINDS them - read-only, safe to run anywhere.

   Deciding which store those units really went into is a business decision, so
   the repair UPDATE is deliberately not here. Send me the results and we will do
   it as a targeted, per-row correction.
   ============================================================================ */

-- 1. Finished-goods movements that landed in "store 0".
--    These are the units that "did not go into stock".
SELECT  t.TrnID, t.CompanyID, t.TransactionDate, t.ItemCode, t.ItemDescription,
        t.Qty, t.LotNumber, t.TransactionReference, t.ToID AS StoreID_ShouldNotBeZero
FROM    dbo.ItemTransaction t
WHERE   t.ToID = 0
  AND   t.TransactionType IN ('MANF', 'DRAW')
ORDER BY t.TransactionDate DESC;

-- 2. Open works-order lines with no store - the next ones that would hit it
--    if manufactured on the OLD build. On the new build they are refused with a
--    message instead; setting the store on the line clears them.
SELECT  l.WOID, h.WONum, l.LineID, l.ItemCode, l.ItemDescription,
        l.Quantity, l.OrderedQty, l.ToStoreID
FROM    dbo.WorksOrderLines l
JOIN    dbo.WorksOrderHeaders h ON h.ID = l.WOID AND h.CompanyID = l.CompanyID
WHERE   l.Active = 1 AND ISNULL(l.Complete, 0) = 0
  AND   l.ToStoreID IS NULL
ORDER BY h.WONum, l.LineID;
