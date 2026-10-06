/* ============================================================================
   Job cards: hide the lines added on the job card from the customer
   (Job Card -> "Print all additional lines")
   ---------------------------------------------------------------------------
   JobCardsMaster.PrintAllLines   1 (default) = every job card line goes onto
       the Sales Order, invoice and delivery note, as it always has.
       0 = lines ADDED on the job card are internal: they stay on the job card,
       are adjusted out of Sage stock and journalled to Cost of Sales, and are
       left off the Sage Sales Order, the invoice and the delivery notes. They
       are a cost of the job only - never billed, whatever their list price.

   CompanyMaster.JobCosAccountID  the Cost of Sales account that journal debits
       (Configuration -> Company). The credit side is the company's existing
       Stock Adjustment Account (Settings -> GL Account Access).

   JobCardSagePostings            what has been posted to Sage for a job, so a
       retry of "Update Sage SO" posts only what is still missing.
       Kind 'A' = stock adjustment out (per item), 'J' = the journal.

   Deliberately OUTSIDE the EF model (raw SQL only) - the EDMX is not touched.
   Safe to run more than once.
   ============================================================================ */
IF COL_LENGTH('dbo.JobCardsMaster', 'PrintAllLines') IS NULL
    ALTER TABLE dbo.JobCardsMaster ADD PrintAllLines BIT NOT NULL
        CONSTRAINT DF_JobCardsMaster_PrintAllLines DEFAULT (1);
GO
IF COL_LENGTH('dbo.CompanyMaster', 'JobCosAccountID') IS NULL
    ALTER TABLE dbo.CompanyMaster ADD JobCosAccountID BIGINT NULL;
GO
IF OBJECT_ID('dbo.JobCardSagePostings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.JobCardSagePostings (
        PostID      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_JobCardSagePostings PRIMARY KEY,
        CompanyID   BIGINT        NOT NULL,
        DocID       BIGINT        NOT NULL,      -- the Sales Order
        JCID        INT           NULL,
        Kind        CHAR(1)       NOT NULL,      -- 'A' adjustment out, 'J' journal
        ItemID      BIGINT        NULL,          -- adjustments only
        Qty         DECIMAL(18,4) NULL,          -- adjustments only (positive = units taken out)
        Amount      DECIMAL(18,4) NOT NULL,      -- value at Sage average cost
        Reference   VARCHAR(100)  NULL,
        Created     DATETIME      NOT NULL CONSTRAINT DF_JobCardSagePostings_Created DEFAULT (GETDATE())
    );
    CREATE INDEX IX_JobCardSagePostings_Co_Doc ON dbo.JobCardSagePostings (CompanyID, DocID);
    PRINT 'Created dbo.JobCardSagePostings';
END
GO
PRINT 'Job card hide-lines columns are in place.';
GO
