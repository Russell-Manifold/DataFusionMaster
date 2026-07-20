-- =====================================================================
-- "Pick by bin" mobile picking (2026-07-19)
--
-- For warehouses that use Stores AS bins (one warehouse, thousands of
-- locations). Instead of choosing one picking store for the whole slip,
-- the mobile picker takes each line from the bin(s) that actually hold
-- stock - splitting a line across bins when needed. Company-flagged:
-- off = today's behaviour (the "Select Picking Store" modal) unchanged.
--
-- Deliberately OUTSIDE the EF model (raw SQL only) - do NOT touch the EDMX.
-- Idempotent. Run on EVERY tenant database. MOBILE ONLY - web unchanged.
-- =====================================================================

-- Company flag
IF COL_LENGTH('dbo.CompanyMaster', 'PickByBin') IS NULL
BEGIN
    ALTER TABLE dbo.CompanyMaster
        ADD PickByBin bit NOT NULL CONSTRAINT DF_CompanyMaster_PickByBin DEFAULT (0);
END
GO

-- One row per bin-pick of a picking-slip line (a line may span several bins).
-- The line is complete when its rows sum to the ordered quantity. Reset
-- reverses each row's stock movement and clears the rows.
IF OBJECT_ID('dbo.PickSlipLinePicks', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PickSlipLinePicks (
        PickID          int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PickSlipLinePicks PRIMARY KEY,
        CompanyID       bigint        NOT NULL,
        PSID            int           NOT NULL,
        LineID          int           NOT NULL,
        StoreCode       varchar(50)   NOT NULL,
        LotNumber       varchar(50)   NULL,
        Qty             decimal(18,4) NOT NULL,
        ItemTransLineID bigint        NULL,   -- the outbound ItemTransaction, for reversal on reset
        CreatedAt       datetime      NOT NULL CONSTRAINT DF_PickSlipLinePicks_CreatedAt DEFAULT (GETDATE()),
        CreatedBy       bigint        NULL
    );

    CREATE NONCLUSTERED INDEX IX_PickSlipLinePicks_Co_PS
        ON dbo.PickSlipLinePicks (CompanyID, PSID) INCLUDE (LineID, StoreCode, Qty);
END
GO
