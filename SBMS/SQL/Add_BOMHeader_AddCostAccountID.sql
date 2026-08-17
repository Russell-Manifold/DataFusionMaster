-- ---------------------------------------------------------------------
-- Adds dbo.BOMHeader.AddCostAccountID
--
-- The GL account that BOM additional costs are credited to when a works
-- order is manufactured. Chosen per BOM on the BOM screen.
--
-- ⚠ RUN THIS BEFORE DEPLOYING THE MATCHING BUILD.
--    The column is in the EDMX storage model, so a build that reaches a
--    database without it fails on the first query against BOMHeader -
--    which includes login. Same trap as LPNPickMode.
--
-- Data written: none (new nullable column, existing rows stay NULL).
-- Re-runnable:  Yes.
-- ---------------------------------------------------------------------

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.BOMHeader')
      AND name = 'AddCostAccountID')
BEGIN
    ALTER TABLE dbo.BOMHeader ADD AddCostAccountID bigint NULL;
    PRINT 'Added dbo.BOMHeader.AddCostAccountID';
END
ELSE
BEGIN
    PRINT 'dbo.BOMHeader.AddCostAccountID already exists - nothing to do';
END
GO

-- ---------------------------------------------------------------------
-- VERIFY - expect one row back.
-- ---------------------------------------------------------------------
SELECT c.name, t.name AS DataType, c.is_nullable
FROM   sys.columns c
JOIN   sys.types   t ON t.user_type_id = c.user_type_id
WHERE  c.object_id = OBJECT_ID('dbo.BOMHeader')
  AND  c.name = 'AddCostAccountID';
GO

-- ---------------------------------------------------------------------
-- The debit side of the journal needs no schema change: it reuses the
-- existing dbo.AccountsMaster.AccountAddCostsContra flag, which was
-- already in the database but never surfaced. Flag ONE account per
-- company (the inventory adjustment account) in Settings > Accounts.
--
-- Check what is currently flagged:
-- ---------------------------------------------------------------------
-- SELECT CompanyID, AccountID, AccountName
-- FROM   dbo.AccountsMaster
-- WHERE  AccountAddCostsContra = 1
-- ORDER BY CompanyID;
