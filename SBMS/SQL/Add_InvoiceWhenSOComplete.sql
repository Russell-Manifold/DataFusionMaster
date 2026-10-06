/* ============================================================================
   Short picks: invoice once when the Sales Order is complete
   (Configuration -> Company)
   ---------------------------------------------------------------------------
   Company switch. Off by default = today's behaviour: a short pick puts the
   balance on a NEW Sales Order and each Sales Order is invoiced separately.
   On = the Sales Order stays open across part deliveries and is invoiced ONCE,
   when the whole order has been delivered (or is closed short).

   *** RUN BEFORE DEPLOYING THE NEW BUILD *** - the column is in the EDMX.
   Safe to run more than once. Old build ignores the column (NOT NULL DEFAULT 0).
   ============================================================================ */
IF COL_LENGTH('dbo.CompanyMaster', 'InvoiceWhenSOComplete') IS NULL
BEGIN
    ALTER TABLE dbo.CompanyMaster ADD InvoiceWhenSOComplete BIT NOT NULL
        CONSTRAINT DF_CompanyMaster_InvoiceWhenSOComplete DEFAULT (0);
    PRINT 'Added CompanyMaster.InvoiceWhenSOComplete';
END
ELSE PRINT 'CompanyMaster.InvoiceWhenSOComplete already exists';
GO
