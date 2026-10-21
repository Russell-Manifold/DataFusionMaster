-- Sales orders that no longer exist in Sage.
--   DocHeader.MissingInSage   = when the daily check last found the order gone from Sage (NULL = fine).
--   LastCallLog.LastSOReconcile = when that daily check last ran for the company.
--   LastCallLog.LastSOReconcileDocID = where the capped daily check will continue from.
-- The order is NOT deleted: it drops off the open list (filter "Missing in Sage" shows it) and the
-- user deletes it from the Sales Order screen. The normal refresh clears the mark if Sage lists it again.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DocHeader') AND name = 'MissingInSage')
    ALTER TABLE dbo.DocHeader ADD MissingInSage datetime NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LastCallLog') AND name = 'LastSOReconcile')
    ALTER TABLE dbo.LastCallLog ADD LastSOReconcile datetime NULL;
GO
-- Watermark: the daily check does at most 300 orders per company per day (Sage allows 5000 calls a
-- day per company) and carries on from this DocID the next day, wrapping to the start when done.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LastCallLog') AND name = 'LastSOReconcileDocID')
    ALTER TABLE dbo.LastCallLog ADD LastSOReconcileDocID bigint NULL;
GO
