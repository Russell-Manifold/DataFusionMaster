/* ============================================================================
   Slim / stand-alone manufacturing: stock lives in Sage only
   (Onboarding + Configuration -> Company)
   ---------------------------------------------------------------------------
   Company switch. Off by default = today's behaviour.
   On = Works Orders, BOMs and Items only. On-hand is read from Sage
   (ItemsMaster.QuantityOnHand), every Sage adjustment is posted at the item's
   own Sage average cost (no cost is ever changed), one store "SAGE", no lots,
   no roles - every user gets everything.

   *** RUN BEFORE DEPLOYING THE NEW BUILD *** - the column is in the EDMX.
   A build without the column fails EVERY login ("Invalid column name").
   After running: Update Model from Database on SBMS.edmx, then build.
   Safe to run more than once. Old build ignores the column (NOT NULL DEFAULT 0).
   ============================================================================ */
IF COL_LENGTH('dbo.CompanyMaster', 'SageStockOnly') IS NULL
BEGIN
    ALTER TABLE dbo.CompanyMaster ADD SageStockOnly BIT NOT NULL
        CONSTRAINT DF_CompanyMaster_SageStockOnly DEFAULT (0);
    PRINT 'Added CompanyMaster.SageStockOnly';
END
ELSE PRINT 'CompanyMaster.SageStockOnly already exists';
GO
