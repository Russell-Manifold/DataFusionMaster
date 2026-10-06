/* ============================================================================
   Delivery note layout  (Configuration -> Company -> Delivery Note)
   ---------------------------------------------------------------------------
   DNLayout       0 = Standard (the note as it has always printed), 1 = Detailed
                  (customer block with VAT/contact/PO/SO, Order / Inv / Back Order
                  columns, sign-off block, company footer).
   CoRegNo, CoVatNo, DocFooter   printed on the Detailed note.
   PODPrefix + PODNextNumber     one running delivery note number across ALL
                  orders (e.g. TPMDN60284). PODNextNumber 0 = off: the note is
                  numbered from the Sales Order, as before.
   PickingSlipMaster.PODNumber   the number a delivery was given, so a reprint
                  shows the same one.

   Deliberately OUTSIDE the EF model (raw SQL only) - the EDMX is not touched,
   so a build deployed before this script still logs in and prints the
   Standard note. Safe to run more than once.
   ============================================================================ */
IF COL_LENGTH('dbo.CompanyMaster', 'DNLayout') IS NULL
    ALTER TABLE dbo.CompanyMaster ADD DNLayout INT NOT NULL
        CONSTRAINT DF_CompanyMaster_DNLayout DEFAULT (0);
GO
IF COL_LENGTH('dbo.CompanyMaster', 'CoRegNo') IS NULL
    ALTER TABLE dbo.CompanyMaster ADD CoRegNo VARCHAR(50) NULL;
GO
IF COL_LENGTH('dbo.CompanyMaster', 'CoVatNo') IS NULL
    ALTER TABLE dbo.CompanyMaster ADD CoVatNo VARCHAR(50) NULL;
GO
IF COL_LENGTH('dbo.CompanyMaster', 'DocFooter') IS NULL
    ALTER TABLE dbo.CompanyMaster ADD DocFooter VARCHAR(600) NULL;
GO
IF COL_LENGTH('dbo.CompanyMaster', 'PODPrefix') IS NULL
    ALTER TABLE dbo.CompanyMaster ADD PODPrefix VARCHAR(10) NULL;
GO
IF COL_LENGTH('dbo.CompanyMaster', 'PODNextNumber') IS NULL
    ALTER TABLE dbo.CompanyMaster ADD PODNextNumber INT NOT NULL
        CONSTRAINT DF_CompanyMaster_PODNextNumber DEFAULT (0);
GO
IF COL_LENGTH('dbo.PickingSlipMaster', 'PODNumber') IS NULL
    ALTER TABLE dbo.PickingSlipMaster ADD PODNumber VARCHAR(30) NULL;
GO
PRINT 'Delivery note layout columns are in place.';
GO
