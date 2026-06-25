-- Receiving lifecycle status on the receiving header (single source of truth).
--   0 = Started    (receiving in progress)
--   1 = Complete   (all lines picked/received, NOT yet sent to Sage)
--   2 = Submitted  (supplier invoice generated in Sage)
--
-- The legacy Started/Complete bit flags remain for back-compat; RecStatus is the
-- authoritative lifecycle field going forward. After running, do "Update Model from
-- Database" on the EDMX so DocHeader.RecStatus appears on the entity.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.DocHeader') AND name = 'RecStatus')
BEGIN
    ALTER TABLE dbo.DocHeader
        ADD RecStatus int NOT NULL CONSTRAINT DF_DocHeader_RecStatus DEFAULT 0;
END
GO

-- Index for the receiving worklist lookups (WHERE CompanyID = @c AND RecStatus = @s).
-- Composite, CompanyID first, since every query scopes by company.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.DocHeader') AND name = 'IX_DocHeader_Company_RecStatus')
BEGIN
    CREATE NONCLUSTERED INDEX IX_DocHeader_Company_RecStatus
        ON dbo.DocHeader (CompanyID, RecStatus);
END
GO

-- Backfill: existing completed receipts were finalised (and posted) under the old
-- single-action flow, so treat them as Submitted.
UPDATE dbo.DocHeader SET RecStatus = 2 WHERE Complete = 1 AND RecStatus <> 2;
GO
