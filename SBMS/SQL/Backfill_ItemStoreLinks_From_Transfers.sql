-- Transfers never linked the item to the RECEIVING store (bug fixed in TransferSlip 2026-10-26), so
-- stock sat in stores the item was not linked to and could not be adjusted or picked there.
-- Creates the missing links for every store that has ever received the item by transfer.
INSERT INTO dbo.ItemStoreLinkMaster (CompanyID, ItemID, StoreID, Active)
SELECT DISTINCT t.CompanyID, t.ItemID, t.ToID, 1
FROM dbo.ItemTransaction t
JOIN dbo.Stores s ON s.StoreID = t.ToID AND s.CompanyID = t.CompanyID
WHERE t.TransactionType = 'TRF' AND t.Qty > 0 AND t.ToID IS NOT NULL AND t.ItemID IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.ItemStoreLinkMaster l
                  WHERE l.CompanyID = t.CompanyID AND l.ItemID = t.ItemID AND l.StoreID = t.ToID);
GO
