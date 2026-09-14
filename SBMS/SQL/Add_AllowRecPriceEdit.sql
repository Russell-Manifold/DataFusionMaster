/* ============================================================================
   Allow purchase price to be changed on receiving  (Configuration -> Company)
   ---------------------------------------------------------------------------
   Company switch. Off by default. When on, the receiving line shows a Received
   Price box beside the PO's price; the corrected figure drives the receipt, the
   lot cost, the store average and the Sage supplier invoice. The PO in Sage and
   the item's own price are NOT changed.

   *** RUN BEFORE DEPLOYING THE NEW BUILD *** - the column is in the EDMX.
   Safe to run more than once. Old build ignores the column (NOT NULL DEFAULT 0).
   ============================================================================ */
IF COL_LENGTH('dbo.CompanyMaster', 'AllowRecPriceEdit') IS NULL
BEGIN
    ALTER TABLE dbo.CompanyMaster ADD AllowRecPriceEdit BIT NOT NULL
        CONSTRAINT DF_CompanyMaster_AllowRecPriceEdit DEFAULT (0);
    PRINT 'Added CompanyMaster.AllowRecPriceEdit';
END
ELSE PRINT 'CompanyMaster.AllowRecPriceEdit already exists';
GO
