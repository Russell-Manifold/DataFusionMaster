/* ============================================================================
   Returnable items  (Stock Control -> Returnables)
   ---------------------------------------------------------------------------
   Pallets, crates, drums, cylinders - anything that goes out with a delivery
   and is expected back.

   ReturnableItems      which items are returnable, and since when. Deliveries
                        are counted from that date; what customers already held
                        before it is captured as an opening balance.
   ReturnableMovements  returns logged against a customer (MoveType 'R') and
                        opening balances (MoveType 'O').

   What went OUT is not stored here: it is read from the completed picking
   slips, so it can never disagree with what was actually delivered.

   Outstanding = opening balances + delivered since the item's date - returned.

   Deliberately OUTSIDE the EF model (raw SQL only) - the EDMX is not touched.
   Safe to run more than once.
   ============================================================================ */
IF OBJECT_ID('dbo.ReturnableItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReturnableItems (
        CompanyID   BIGINT       NOT NULL,
        ItemID      BIGINT       NOT NULL,
        Since       DATE         NOT NULL,
        CONSTRAINT PK_ReturnableItems PRIMARY KEY (CompanyID, ItemID)
    );
    PRINT 'Created dbo.ReturnableItems';
END
GO
IF OBJECT_ID('dbo.ReturnableMovements', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReturnableMovements (
        RmID            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReturnableMovements PRIMARY KEY,
        CompanyID       BIGINT        NOT NULL,
        CustomerID      BIGINT        NOT NULL,
        CustomerName    VARCHAR(250)  NULL,
        ItemID          BIGINT        NOT NULL,
        ItemCode        VARCHAR(50)   NULL,
        ItemDescription VARCHAR(100)  NULL,
        Qty             DECIMAL(18,4) NOT NULL,
        MoveType        CHAR(1)       NOT NULL,      -- 'R' return, 'O' opening balance
        MoveDate        DATE          NOT NULL,
        StoreID         INT           NULL,          -- where a return was received (returns only)
        Reference       VARCHAR(100)  NULL,
        ByRoleID        INT           NULL,
        Created         DATETIME      NOT NULL CONSTRAINT DF_ReturnableMovements_Created DEFAULT (GETDATE()),
        TrnID           BIGINT        NULL           -- the stock movement a return created
    );
    CREATE INDEX IX_ReturnableMovements_Co_Cust_Item
        ON dbo.ReturnableMovements (CompanyID, CustomerID, ItemID);
    PRINT 'Created dbo.ReturnableMovements';
END
GO
