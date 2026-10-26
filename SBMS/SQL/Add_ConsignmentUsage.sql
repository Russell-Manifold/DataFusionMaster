/* ============================================================================
   Consignment usage  (Stock Control -> Consignment Stock)
   ---------------------------------------------------------------------------
   Month end for a consignment location (a store linked to a Sage customer -
   see Add_ConsignmentStores.sql):
     ConsignmentUsage      one run per location per count: who, when, status,
                           and the Sage sales order it produced.
     ConsignmentUsageLine  one line per item: opening, delivered, other
                           movements, counted, used, price, value.
   Opening/delivered/other are read from the ItemTransaction ledger at the time
   the run starts (ToID = the consignment store); Counted is typed in; Used =
   on hand - counted. "Create Sales Order" raises the Sage SO for the used
   quantities and books them out of the location (ledger type CSU).
   Deliberately OUTSIDE the EF model (raw SQL only). Safe to run more than once.
   ============================================================================ */
IF OBJECT_ID('dbo.ConsignmentUsage', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsignmentUsage (
        UsageID       BIGINT IDENTITY(1,1) NOT NULL,
        CompanyID     BIGINT        NOT NULL,
        StoreID       INT           NOT NULL,
        CustomerID    BIGINT        NOT NULL,
        CustomerName  VARCHAR(100)  NULL,
        CountDate     DATE          NOT NULL,
        PeriodFrom    DATETIME      NULL,          -- previous run's OrderedDate; NULL = first run
        Status        VARCHAR(10)   NOT NULL,      -- Draft / Ordered
        SageSODocID   BIGINT        NULL,
        SageSONumber  VARCHAR(50)   NULL,
        TotalExcl     DECIMAL(18,2) NOT NULL CONSTRAINT DF_ConsignmentUsage_Total DEFAULT 0,
        CreatedBy     INT           NULL,
        CreatedDate   DATETIME      NOT NULL CONSTRAINT DF_ConsignmentUsage_Created DEFAULT GETDATE(),
        OrderedDate   DATETIME      NULL,
        CONSTRAINT PK_ConsignmentUsage PRIMARY KEY (UsageID)
    );
    CREATE INDEX IX_ConsignmentUsage_Store ON dbo.ConsignmentUsage (CompanyID, StoreID, Status);
    PRINT 'Created dbo.ConsignmentUsage';
END
GO
IF OBJECT_ID('dbo.ConsignmentUsageLine', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsignmentUsageLine (
        LineID          BIGINT IDENTITY(1,1) NOT NULL,
        UsageID         BIGINT        NOT NULL,
        CompanyID       BIGINT        NOT NULL,
        ItemID          BIGINT        NOT NULL,
        ItemCode        VARCHAR(50)   NULL,
        ItemDescription VARCHAR(100)  NULL,
        Unit            VARCHAR(50)   NULL,
        Opening         DECIMAL(18,4) NOT NULL CONSTRAINT DF_CUL_Opening   DEFAULT 0,
        Delivered       DECIMAL(18,4) NOT NULL CONSTRAINT DF_CUL_Delivered DEFAULT 0,
        OtherMoves      DECIMAL(18,4) NOT NULL CONSTRAINT DF_CUL_Other     DEFAULT 0,
        Counted         DECIMAL(18,4) NULL,                                   -- NULL = not counted yet
        Used            DECIMAL(18,4) NOT NULL CONSTRAINT DF_CUL_Used      DEFAULT 0,
        UnitPrice       DECIMAL(18,4) NOT NULL CONSTRAINT DF_CUL_Price     DEFAULT 0,
        LineExcl        DECIMAL(18,2) NOT NULL CONSTRAINT DF_CUL_LineExcl  DEFAULT 0,
        TaxTypeId       BIGINT        NULL,
        TaxPerc         DECIMAL(9,4)  NOT NULL CONSTRAINT DF_CUL_TaxPerc   DEFAULT 0,
        CONSTRAINT PK_ConsignmentUsageLine PRIMARY KEY (LineID),
        CONSTRAINT FK_ConsignmentUsageLine_Usage FOREIGN KEY (UsageID) REFERENCES dbo.ConsignmentUsage (UsageID)
    );
    CREATE INDEX IX_ConsignmentUsageLine_Usage ON dbo.ConsignmentUsageLine (UsageID);
    PRINT 'Created dbo.ConsignmentUsageLine';
END
GO
