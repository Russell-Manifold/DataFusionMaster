-- =====================================================================
-- MASTER DEPLOYMENT — all mobile/desktop changes (2026-07-19)
--
-- Run this ONCE on EACH tenant database, then deploy the app build.
-- Everything here is idempotent and safe to re-run. All objects are
-- deliberately OUTSIDE the EF model (raw SQL) - do NOT touch the EDMX.
--
-- Sections (run in this order):
--   1. LPN picking            (PickSlipLineLPNs + LPNPickMode)
--   2. SKU-as-barcode procs   (GetOneItemFromBarcode / GetValidateBarcode)
--   3. Manufacture dedup      (ManfPostedAdjustments)
--   4. Pick by bin            (PickSlipLinePicks + PickByBin)
--
-- The individual scripts (Deploy_LPN_Picking.sql, Add_SKU_Barcode_Fallback.sql,
-- Add_ManfPostedAdjustments.sql, Add_PickByBin.sql) remain in this folder;
-- this file is just all four in one run.
-- =====================================================================


-- =====================================================================
-- 1. LPN PICKING
-- =====================================================================
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
        CreatedBy bigint        NULL
    );
END
GO
IF COL_LENGTH('dbo.PickSlipLineLPNs', 'Qty') IS NULL
    ALTER TABLE dbo.PickSlipLineLPNs
        ADD Qty decimal(18,4) NOT NULL CONSTRAINT DF_PickSlipLineLPNs_Qty DEFAULT (1);
GO
UPDATE l
SET    l.Qty = COALESCE(p.PickQty, p.Quantity, 1)
FROM   dbo.PickSlipLineLPNs l
INNER JOIN dbo.PickSlipLines p ON p.LineID = l.LineID AND p.CompanyID = l.CompanyID
WHERE  l.Qty = 1 AND p.PickComplete = 1 AND COALESCE(p.PickQty, p.Quantity, 1) <> 1
  AND  NOT EXISTS (SELECT 1 FROM dbo.PickSlipLineLPNs x
                   WHERE x.CompanyID = l.CompanyID AND x.PSID = l.PSID AND x.LineID = l.LineID AND x.LPNID <> l.LPNID);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PickSlipLineLPNs_Co_PSID' AND object_id = OBJECT_ID('dbo.PickSlipLineLPNs'))
    CREATE NONCLUSTERED INDEX IX_PickSlipLineLPNs_Co_PSID ON dbo.PickSlipLineLPNs (CompanyID, PSID) INCLUDE (LineID, LPN, Qty);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PickSlipLineLPNs_Co_LPN' AND object_id = OBJECT_ID('dbo.PickSlipLineLPNs'))
    CREATE NONCLUSTERED INDEX IX_PickSlipLineLPNs_Co_LPN ON dbo.PickSlipLineLPNs (CompanyID, LPN) INCLUDE (PSID, LineID, Qty);
GO
IF COL_LENGTH('dbo.CompanyMaster', 'LPNPickMode') IS NULL
    ALTER TABLE dbo.CompanyMaster ADD LPNPickMode varchar(10) NOT NULL CONSTRAINT DF_CompanyMaster_LPNPickMode DEFAULT ('off');
GO
UPDATE dbo.CompanyMaster SET LPNPickMode = 'off' WHERE LPNPickMode IS NULL OR LPNPickMode NOT IN ('off','box','unit');
GO


-- =====================================================================
-- 2. SKU-AS-BARCODE FALLBACK (procs; param widened varchar(20) -> varchar(50))
-- =====================================================================
ALTER PROCEDURE [dbo].[GetOneItemFromBarcode]
    @CoID bigint, @bCode varchar(50)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.ItemBarCodeLink bl INNER JOIN dbo.ItemsMaster im ON bl.ItemID = im.ID
               WHERE bl.CompanyID = @CoID AND bl.BarCode = @bCode AND im.Active = 1)
        SELECT im.[Description], im.Code, im.Unit, im.ItmID, im.ID, bl.QtyPerBarcode
        FROM dbo.ItemBarCodeLink bl INNER JOIN dbo.ItemsMaster im ON bl.ItemID = im.ID
        WHERE bl.CompanyID = @CoID AND bl.BarCode = @bCode AND im.Active = 1;
    ELSE
        SELECT im.[Description], im.Code, im.Unit, im.ItmID, im.ID, CAST(1 AS int) AS QtyPerBarcode
        FROM dbo.ItemsMaster im WHERE im.CompanyID = @CoID AND im.Code = @bCode AND im.Active = 1;
END
GO
ALTER PROCEDURE [dbo].[GetValidateBarcode]
    @CoID bigint, @itmCode varchar(50)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.ItemBarCodeLink bl INNER JOIN dbo.ItemsMaster im ON bl.ItemID = im.ID
               WHERE bl.CompanyID = @CoID AND bl.BarCode = @itmCode)
        SELECT bl.QtyPerBarcode, bl.ItemID FROM dbo.ItemBarCodeLink bl INNER JOIN dbo.ItemsMaster im ON bl.ItemID = im.ID
        WHERE bl.CompanyID = @CoID AND bl.BarCode = @itmCode;
    ELSE
        SELECT CAST(1 AS int) AS QtyPerBarcode, im.ID AS ItemID
        FROM dbo.ItemsMaster im WHERE im.CompanyID = @CoID AND im.Code = @itmCode AND im.Active = 1;
END
GO


-- =====================================================================
-- 3. MANUFACTURE ADJUSTMENT DEDUP (durable, replaces in-Session SentKeys)
-- =====================================================================
IF OBJECT_ID('dbo.ManfPostedAdjustments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ManfPostedAdjustments (
        ID        int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ManfPostedAdjustments PRIMARY KEY,
        CompanyID bigint       NOT NULL,
        WOID      bigint       NOT NULL,
        AdjKey    varchar(300) NOT NULL,
        PostedAt  datetime     NOT NULL CONSTRAINT DF_ManfPostedAdjustments_PostedAt DEFAULT (GETDATE()),
        PostedBy  bigint       NULL
    );
    CREATE UNIQUE NONCLUSTERED INDEX UX_ManfPostedAdjustments_Key ON dbo.ManfPostedAdjustments (CompanyID, WOID, AdjKey);
END
GO


-- =====================================================================
-- 4. PICK BY BIN (company flag + per-bin pick rows)
-- =====================================================================
IF COL_LENGTH('dbo.CompanyMaster', 'PickByBin') IS NULL
    ALTER TABLE dbo.CompanyMaster ADD PickByBin bit NOT NULL CONSTRAINT DF_CompanyMaster_PickByBin DEFAULT (0);
GO
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
        ItemTransLineID bigint        NULL,
        CreatedAt       datetime      NOT NULL CONSTRAINT DF_PickSlipLinePicks_CreatedAt DEFAULT (GETDATE()),
        CreatedBy       bigint        NULL
    );
    CREATE NONCLUSTERED INDEX IX_PickSlipLinePicks_Co_PS ON dbo.PickSlipLinePicks (CompanyID, PSID) INCLUDE (LineID, StoreCode, Qty);
END
GO

-- Done. Deploy the app build after this completes.
