-- =====================================================================
-- LPN picking - full deployment script (2026-07-19)
--
-- One file, idempotent, safe to rerun. Run on EVERY tenant database.
-- Supersedes Add_PickSlipLineLPNs.sql and Add_CompanyLPNPickMode.sql.
-- (The SKU-as-barcode procs are separate: Add_SKU_Barcode_Fallback.sql)
--
-- Contents:
--   1. dbo.PickSlipLineLPNs table (one row per label/box stamped on a
--      picked PickSlipLine) + Qty column upgrade for early installs.
--   2. Qty backfill for rows created before the Qty column existed
--      (box-mode rows carry the line's pick qty; unit-mode rows stay 1).
--   3. Indexes: per-slip lookup (page binds/counts) and per-label lookup
--      (duplicate checks now, "which box is this?" later).
--   4. dbo.CompanyMaster.LPNPickMode setting ('off'|'box'|'unit') +
--      normalisation backfill. Companies default to 'off' = no LPN UI.
--
-- All of this is deliberately OUTSIDE the EF model (raw SQL access only)
-- - do not add the table or column to the EDMX.
-- =====================================================================

------------------------------------------------------------------------
-- 1. PickSlipLineLPNs
------------------------------------------------------------------------
IF OBJECT_ID('dbo.PickSlipLineLPNs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PickSlipLineLPNs (
        LPNID     int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PickSlipLineLPNs PRIMARY KEY,
        CompanyID bigint        NOT NULL,
        PSID      int           NOT NULL,
        LineID    int           NOT NULL,
        LPN       varchar(50)   NOT NULL,
        Qty       decimal(18,4) NOT NULL CONSTRAINT DF_PickSlipLineLPNs_Qty DEFAULT (1),
        CreatedAt datetime      NOT NULL CONSTRAINT DF_PickSlipLineLPNs_CreatedAt DEFAULT (GETDATE()),
        CreatedBy bigint        NULL  -- RoleID of the picker
    );
END
GO

-- Upgrade path: tables created before the Qty column existed.
IF COL_LENGTH('dbo.PickSlipLineLPNs', 'Qty') IS NULL
BEGIN
    ALTER TABLE dbo.PickSlipLineLPNs
        ADD Qty decimal(18,4) NOT NULL CONSTRAINT DF_PickSlipLineLPNs_Qty DEFAULT (1);
END
GO

------------------------------------------------------------------------
-- 2. Qty backfill for pre-Qty rows.
-- Box-mode rows are the only kind that predate the Qty column, and they
-- are identifiable as the SINGLE label on a COMPLETED line: stamp them
-- with the line's pick qty. Unit-mode rows (several qty-1 labels per
-- line) and in-flight lines are left untouched.
------------------------------------------------------------------------
UPDATE l
SET    l.Qty = COALESCE(p.PickQty, p.Quantity, 1)
FROM   dbo.PickSlipLineLPNs l
INNER JOIN dbo.PickSlipLines p
        ON p.LineID = l.LineID AND p.CompanyID = l.CompanyID
WHERE  l.Qty = 1
  AND  p.PickComplete = 1
  AND  COALESCE(p.PickQty, p.Quantity, 1) <> 1
  AND  NOT EXISTS (SELECT 1
                   FROM dbo.PickSlipLineLPNs x
                   WHERE x.CompanyID = l.CompanyID
                     AND x.PSID     = l.PSID
                     AND x.LineID   = l.LineID
                     AND x.LPNID   <> l.LPNID);
GO

------------------------------------------------------------------------
-- 3. Indexes
------------------------------------------------------------------------
-- Per-slip: the picking page loads/counts labels by (CompanyID, PSID).
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_PickSlipLineLPNs_Co_PSID'
                 AND object_id = OBJECT_ID('dbo.PickSlipLineLPNs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PickSlipLineLPNs_Co_PSID
        ON dbo.PickSlipLineLPNs (CompanyID, PSID)
        INCLUDE (LineID, LPN, Qty);
END
GO

-- Per-label: duplicate-sticker checks now; "which box/slip holds label X"
-- lookups (packing lists, dispatch queries) later.
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_PickSlipLineLPNs_Co_LPN'
                 AND object_id = OBJECT_ID('dbo.PickSlipLineLPNs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PickSlipLineLPNs_Co_LPN
        ON dbo.PickSlipLineLPNs (CompanyID, LPN)
        INCLUDE (PSID, LineID, Qty);
END
GO

------------------------------------------------------------------------
-- 4. Company-level picking mode: 'off' (default) | 'box' | 'unit'.
-- Set on ConfigCompany ("LPN Boxing in Mobile Picking") or manually:
--   UPDATE dbo.CompanyMaster SET LPNPickMode = 'unit' WHERE CoID = <CoID>;
------------------------------------------------------------------------
IF COL_LENGTH('dbo.CompanyMaster', 'LPNPickMode') IS NULL
BEGIN
    ALTER TABLE dbo.CompanyMaster
        ADD LPNPickMode varchar(10) NOT NULL
            CONSTRAINT DF_CompanyMaster_LPNPickMode DEFAULT ('off');
END
GO

-- Backfill / normalise: anything unset or invalid becomes 'off'
-- (covers manually-added nullable columns or typo'd values).
UPDATE dbo.CompanyMaster
SET    LPNPickMode = 'off'
WHERE  LPNPickMode IS NULL
   OR  LPNPickMode NOT IN ('off', 'box', 'unit');
GO
