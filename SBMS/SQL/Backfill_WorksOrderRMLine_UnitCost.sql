-- Backfills WorksOrderRMLines.UnitCost from ItemsMaster.AverageCost for
-- component lines that were created before their item ever had a costed
-- receiving (UnitCost left at 0/NULL). Companion to the DoItemAdjustment
-- fallback shipped in WorksOrdersManf.aspx.cs - that fix only backfills a
-- line lazily, the next time someone draws against it. This script fixes
-- every currently-stuck line in one pass so nobody has to hit the error
-- first.
--
-- Scope: ALL lines (open and completed) with a zero/null cost where Sage
-- now has a real average cost to backfill from.
--
-- Run after hours. Review the SELECT preview before running the UPDATE.

-- Preview: rows this will change
SELECT
    rm.LineID,
    rm.WOID,
    rm.CompanyID,
    rm.ItemCode,
    rm.Quantity,
    rm.UseQty,
    rm.UnitCost   AS CurrentUnitCost,
    im.AverageCost AS NewUnitCost
FROM dbo.WorksOrderRMLines rm
INNER JOIN dbo.ItemsMaster im
    ON im.CompanyID = rm.CompanyID
   AND im.ID = rm.SelectionId
WHERE (rm.UnitCost IS NULL OR rm.UnitCost <= 0)
  AND ISNULL(im.AverageCost, 0) > 0
ORDER BY rm.CompanyID, rm.WOID, rm.LineID;

-- Apply
UPDATE rm
SET rm.UnitCost = im.AverageCost
FROM dbo.WorksOrderRMLines rm
INNER JOIN dbo.ItemsMaster im
    ON im.CompanyID = rm.CompanyID
   AND im.ID = rm.SelectionId
WHERE (rm.UnitCost IS NULL OR rm.UnitCost <= 0)
  AND ISNULL(im.AverageCost, 0) > 0;
