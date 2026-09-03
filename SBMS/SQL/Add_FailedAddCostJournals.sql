/* ============================================================================
   FailedAddCostJournals
   ---------------------------------------------------------------------------
   Records every BOM additional-costs journal Sage refused, so an unbalanced GL
   leaves a trace the operator cannot dismiss.

   The table is deliberately OUTSIDE the EF model (written with raw SQL, like
   LPNPickMode and PickByBin). Nothing is added to the EDMX, so there is no
   schema-drift risk at login. If this script has not been run, the code simply
   swallows the insert - manufacturing is unaffected either way.

   Safe to run more than once.

   Follow-up query for support:
       SELECT * FROM dbo.FailedAddCostJournals
       WHERE Resolved = 0
       ORDER BY FailedAt DESC;

   Once the journal has been posted by hand (or retried on the works order):
       UPDATE dbo.FailedAddCostJournals SET Resolved = 1 WHERE ID = <id>;
   ============================================================================ */

IF OBJECT_ID('dbo.FailedAddCostJournals', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FailedAddCostJournals
    (
        ID               BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyID        BIGINT        NOT NULL,
        Reference        NVARCHAR(50)  NULL,      -- e.g. WO83
        DebitAccountID   BIGINT        NULL,      -- company Stock Adjustment Account
        CreditAccountID  BIGINT        NULL,      -- account chosen on the BOM
        Amount           DECIMAL(18,4) NOT NULL,
        Reason           NVARCHAR(500) NULL,      -- what Sage said
        FailedAt         DATETIME      NOT NULL,
        Resolved         BIT           NOT NULL CONSTRAINT DF_FailedAddCostJnl_Resolved DEFAULT (0)
    );

    CREATE INDEX IX_FailedAddCostJnl_Open
        ON dbo.FailedAddCostJournals (CompanyID, Resolved, FailedAt DESC);

    PRINT 'Created dbo.FailedAddCostJournals';
END
ELSE
    PRINT 'dbo.FailedAddCostJournals already exists - nothing to do';
GO


/* ---------------------------------------------------------------------------
   Only ONE GL account per company may be the Stock Adjustment Account.
   That was enforced in the UI only; this makes the database agree, so a second
   tick can never be saved by a concurrent session or a direct update.
   --------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_AccountsMaster_OneAddCostContra')
BEGIN
    /* Clean up first: if any company already has more than one flagged, keep the
       lowest AccountID and clear the rest, otherwise the index cannot be created. */
    ;WITH ranked AS
    (
        SELECT AccountAddCostsContra,
               ROW_NUMBER() OVER (PARTITION BY CompanyID ORDER BY AccountID) AS rn
        FROM dbo.AccountsMaster
        WHERE AccountAddCostsContra = 1
    )
    UPDATE ranked SET AccountAddCostsContra = 0 WHERE rn > 1;

    CREATE UNIQUE INDEX UX_AccountsMaster_OneAddCostContra
        ON dbo.AccountsMaster (CompanyID)
        WHERE AccountAddCostsContra = 1;

    PRINT 'Created UX_AccountsMaster_OneAddCostContra';
END
ELSE
    PRINT 'UX_AccountsMaster_OneAddCostContra already exists - nothing to do';
GO
