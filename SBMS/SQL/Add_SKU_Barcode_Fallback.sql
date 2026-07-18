-- =====================================================================
-- SKU-as-barcode fallback (2026-07-18)
--
-- Lets users scan/type the item CODE (SKU) where no ItemBarcodeLink row
-- exists, returned as a qty-1 "barcode". Real barcode links always win:
-- the fallback only fires when the scanned value matches no BarCode for
-- the company. No data rows are created and the Sage item import is
-- untouched, so this works for all existing items immediately.
--
-- Also widens the barcode parameter varchar(20) -> varchar(50) to match
-- ItemsMaster.Code (codes up to 50 chars exist; 20 silently truncated).
--
-- IMPORTANT: both procs must return a SINGLE result set - EF function
-- imports only read the first one - hence IF EXISTS / ELSE, not two
-- SELECTs in sequence.
--
-- Run on EVERY tenant database.
-- =====================================================================

ALTER PROCEDURE [dbo].[GetOneItemFromBarcode]
	@CoID bigint, @bCode varchar(50)
AS
BEGIN
	SET NOCOUNT ON;

	IF EXISTS (SELECT 1
	           FROM dbo.ItemBarCodeLink bl
	           INNER JOIN dbo.ItemsMaster im ON bl.ItemID = im.ID
	           WHERE bl.CompanyID = @CoID AND bl.BarCode = @bCode AND im.Active = 1)
	BEGIN
		SELECT im.[Description], im.Code, im.Unit, im.ItmID, im.ID, bl.QtyPerBarcode
		FROM dbo.ItemBarCodeLink bl
		INNER JOIN dbo.ItemsMaster im ON bl.ItemID = im.ID
		WHERE bl.CompanyID = @CoID AND bl.BarCode = @bCode AND im.Active = 1;
	END
	ELSE
	BEGIN
		-- SKU fallback: the scanned value is the item code itself, qty 1.
		SELECT im.[Description], im.Code, im.Unit, im.ItmID, im.ID, CAST(1 AS int) AS QtyPerBarcode
		FROM dbo.ItemsMaster im
		WHERE im.CompanyID = @CoID AND im.Code = @bCode AND im.Active = 1;
	END
END
GO

ALTER PROCEDURE [dbo].[GetValidateBarcode]
	@CoID bigint, @itmCode varchar(50)
AS
BEGIN
	SET NOCOUNT ON;

	IF EXISTS (SELECT 1
	           FROM dbo.ItemBarCodeLink bl
	           INNER JOIN dbo.ItemsMaster im ON bl.ItemID = im.ID
	           WHERE bl.CompanyID = @CoID AND bl.BarCode = @itmCode)
	BEGIN
		SELECT bl.QtyPerBarcode, bl.ItemID
		FROM dbo.ItemBarCodeLink bl
		INNER JOIN dbo.ItemsMaster im ON bl.ItemID = im.ID
		WHERE bl.CompanyID = @CoID AND bl.BarCode = @itmCode;
	END
	ELSE
	BEGIN
		-- SKU fallback: the scanned value is the item code itself, qty 1.
		SELECT CAST(1 AS int) AS QtyPerBarcode, im.ID AS ItemID
		FROM dbo.ItemsMaster im
		WHERE im.CompanyID = @CoID AND im.Code = @itmCode AND im.Active = 1;
	END
END
GO
