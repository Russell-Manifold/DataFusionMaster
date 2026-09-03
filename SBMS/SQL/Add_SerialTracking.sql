/* ============================================================================
   SERIAL NUMBER TRACKING - schema
   ---------------------------------------------------------------------------
   A serial number is stored as a LOT OF QUANTITY 1. There is no separate serial
   table and no second tracking dimension: every screen, the stock ledger and the
   Sage posting already understand lots, so serials inherit all of it.

   Three columns:
     LotTrackingMaster.ParentLotNumber  the supplier batch a serial belongs to,
                                        so 40 serials still roll up under one lot
                                        and a lot recall catches every unit.
     ItemsMaster.IsSerialTracked        per item. A serial item is ALSO lot
                                        tracked (IsLotTracked = 1) - this flag
                                        only says each unit gets its own lot.
     CompanyMaster.UseSerialNumbers     the module switch.

   *** RUN THIS BEFORE DEPLOYING THE NEW BUILD ***
   The columns are in the EDMX. A build that expects them against a database that
   does not have them fails at login for EVERY user, not just serial ones.

   Safe to run more than once.
   ============================================================================ */

IF COL_LENGTH('dbo.LotTrackingMaster', 'ParentLotNumber') IS NULL
BEGIN
    ALTER TABLE dbo.LotTrackingMaster ADD ParentLotNumber VARCHAR(50) NULL;
    PRINT 'Added LotTrackingMaster.ParentLotNumber';
END
ELSE PRINT 'LotTrackingMaster.ParentLotNumber already exists';
GO

IF COL_LENGTH('dbo.ItemsMaster', 'IsSerialTracked') IS NULL
BEGIN
    ALTER TABLE dbo.ItemsMaster ADD IsSerialTracked BIT NOT NULL
        CONSTRAINT DF_ItemsMaster_IsSerialTracked DEFAULT (0);
    PRINT 'Added ItemsMaster.IsSerialTracked';
END
ELSE PRINT 'ItemsMaster.IsSerialTracked already exists';
GO

IF COL_LENGTH('dbo.CompanyMaster', 'UseSerialNumbers') IS NULL
BEGIN
    ALTER TABLE dbo.CompanyMaster ADD UseSerialNumbers BIT NOT NULL
        CONSTRAINT DF_CompanyMaster_UseSerialNumbers DEFAULT (0);
    PRINT 'Added CompanyMaster.UseSerialNumbers';
END
ELSE PRINT 'CompanyMaster.UseSerialNumbers already exists';
GO

/* Recall and traceability both search by batch, and picking lists serials for one
   item in a store. Without this they table-scan LotTrackingMaster. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LotTracking_ParentLot')
BEGIN
    CREATE INDEX IX_LotTracking_ParentLot
        ON dbo.LotTrackingMaster (CompanyID, ParentLotNumber)
        INCLUDE (LotNumber, ItemCode, UseByDate);
    PRINT 'Created IX_LotTracking_ParentLot';
END
ELSE PRINT 'IX_LotTracking_ParentLot already exists';
GO


/* ---------------------------------------------------------------------------
   PickSlipLineSerials
   ---------------------------------------------------------------------------
   The serials picked against one picking-slip line. A serial line holds up to 20
   units, so one line has many serials and PickSlipLine.LotNumber cannot carry them.

   Kept OUTSIDE the EF model on purpose (raw SQL, exactly like PickSlipLineLPNs):
   no EDMX churn, so no risk of a model/database mismatch locking users out.
   --------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.PickSlipLineSerials', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PickSlipLineSerials
    (
        ID         BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyID  BIGINT       NOT NULL,
        PSID       BIGINT       NOT NULL,   -- picking slip
        LineID     BIGINT       NOT NULL,   -- PickSlipLine.LineID
        Serial     VARCHAR(50)  NOT NULL,   -- = LotTrackingMaster.LotNumber
        CreatedBy  BIGINT       NULL,
        CreatedAt  DATETIME     NOT NULL CONSTRAINT DF_PSLineSerials_CreatedAt DEFAULT (GETDATE())
    );

    CREATE INDEX IX_PSLineSerials_Line ON dbo.PickSlipLineSerials (CompanyID, PSID, LineID);

    /* The same unit must never be picked onto two lines, or onto the same line twice. */
    CREATE UNIQUE INDEX UX_PSLineSerials_Serial ON dbo.PickSlipLineSerials (CompanyID, PSID, Serial);

    PRINT 'Created dbo.PickSlipLineSerials';
END
ELSE PRINT 'dbo.PickSlipLineSerials already exists - nothing to do';
GO


/* ---------------------------------------------------------------------------
   DocLineSerials
   ---------------------------------------------------------------------------
   The serials captured against one RECEIVING line, with each unit's expiry.

   A receiving line stays whole - 10 units is one line, one supplier-invoice line
   of 10 - so the supplier's invoice in Sage matches the paper they sent. The
   stock ledger still gets one movement per unit, because each serial is its own
   lot of quantity 1; this table is what says which units belong to the line.

   Same shape and reasoning as PickSlipLineSerials, and kept OUTSIDE the EF model
   (raw SQL) so nothing here can put the EDMX out of step with the database.
   --------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.DocLineSerials', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DocLineSerials
    (
        ID         BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyID  BIGINT       NOT NULL,
        DocID      BIGINT       NOT NULL,   -- the purchase order / receipt
        SBCALineID BIGINT       NOT NULL,   -- the Sage line id: the ONLY key shared by
                                           -- TempDocLine (capture) and DocLine (posting).
                                           -- TempDocLine.LineID is an autonumber that is
                                           -- rebuilt each time, so it cannot be used here.
        Serial     VARCHAR(50)  NOT NULL,   -- = LotTrackingMaster.LotNumber
        UseByDate  DATETIME     NULL,       -- this unit's own expiry
        CreatedBy  BIGINT       NULL,
        CreatedAt  DATETIME     NOT NULL CONSTRAINT DF_DocLineSerials_CreatedAt DEFAULT (GETDATE())
    );

    CREATE INDEX IX_DocLineSerials_Line ON dbo.DocLineSerials (CompanyID, DocID, SBCALineID);

    /* One physical unit cannot be received twice on the same document. */
    CREATE UNIQUE INDEX UX_DocLineSerials_Serial ON dbo.DocLineSerials (CompanyID, DocID, Serial);

    PRINT 'Created dbo.DocLineSerials';
END
ELSE
BEGIN
    /* Created by an earlier run of this script keyed on the wrong column. It is only ever a
       few test rows at this stage, so re-key it rather than trying to translate the values. */
    IF COL_LENGTH('dbo.DocLineSerials', 'SBCALineID') IS NULL
    BEGIN
        DELETE FROM dbo.DocLineSerials;
        IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocLineSerials_Line')
            DROP INDEX IX_DocLineSerials_Line ON dbo.DocLineSerials;
        ALTER TABLE dbo.DocLineSerials DROP COLUMN LineID;
        ALTER TABLE dbo.DocLineSerials ADD SBCALineID BIGINT NOT NULL CONSTRAINT DF_DocLineSerials_SBCA DEFAULT (0);
        CREATE INDEX IX_DocLineSerials_Line ON dbo.DocLineSerials (CompanyID, DocID, SBCALineID);
        PRINT 'Re-keyed dbo.DocLineSerials onto SBCALineID';
    END
    ELSE PRINT 'dbo.DocLineSerials already exists - nothing to do';
END
GO


/* ---------------------------------------------------------------------------
   Lookup indexes for serial / lot searching
   ---------------------------------------------------------------------------
   Two things hit these constantly and both scan without them:
     - receiving, checking a scanned serial is not already in the company
     - Traceability, finding a unit and then every movement it has made

   Serials are unique per COMPANY, not per company+item: a serial is stored as a
   lot number and is looked up by that number alone, so two products sharing one
   would return both units and mix their history. Different companies sharing a
   serial is fine and always has been - every table here carries CompanyID.
   --------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LotTracking_CoLot')
BEGIN
    CREATE INDEX IX_LotTracking_CoLot
        ON dbo.LotTrackingMaster (CompanyID, LotNumber)
        INCLUDE (ItemId, ItemCode, UseByDate, ParentLotNumber);
    PRINT 'Created IX_LotTracking_CoLot';
END
ELSE PRINT 'IX_LotTracking_CoLot already exists - nothing to do';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ItemTrans_CoLot')
BEGIN
    CREATE INDEX IX_ItemTrans_CoLot
        ON dbo.ItemTransaction (CompanyID, LotNumber)
        INCLUDE (ItemID, ToID, Qty, TransactionDate);
    PRINT 'Created IX_ItemTrans_CoLot';
END
ELSE PRINT 'IX_ItemTrans_CoLot already exists - nothing to do';
GO


/* ---------------------------------------------------------------------------
   Serial implies lot
   ---------------------------------------------------------------------------
   A serial number IS a lot number holding one unit, so an item cannot be serial
   tracked without being lot tracked. Both screens enforce it, but a direct
   UPDATE, an import or a restore could still produce the combination - and an
   item in that state receives with no serials and no lot at all.

   Safe to run more than once.
   --------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_ItemsMaster_SerialNeedsLot')
BEGIN
    /* Fix any row already in that state before the constraint is added. */
    UPDATE dbo.ItemsMaster
       SET IsSerialTracked = 0
     WHERE IsSerialTracked = 1 AND IsLotTracked = 0;

    ALTER TABLE dbo.ItemsMaster WITH CHECK
        ADD CONSTRAINT CK_ItemsMaster_SerialNeedsLot
        CHECK (IsSerialTracked = 0 OR IsLotTracked = 1);

    PRINT 'Created CK_ItemsMaster_SerialNeedsLot';
END
ELSE PRINT 'CK_ItemsMaster_SerialNeedsLot already exists - nothing to do';
GO
