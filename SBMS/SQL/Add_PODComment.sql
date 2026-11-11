/* Deliveries: a comment per delivery (picking slip) next to its POD number, and the
   delivery confirmation captured on mobile (Confirm Delivery: received by, when, by whom).
   PODNumber itself comes from SQL\Add_DeliveryNoteLayout.sql. Safe to run more than once. */

IF COL_LENGTH('dbo.PickingSlipMaster', 'PODNumber') IS NULL
    ALTER TABLE dbo.PickingSlipMaster ADD PODNumber VARCHAR(30) NULL;
GO

IF COL_LENGTH('dbo.PickingSlipMaster', 'PODComment') IS NULL
    ALTER TABLE dbo.PickingSlipMaster ADD PODComment NVARCHAR(500) NULL;
GO

IF COL_LENGTH('dbo.PickingSlipMaster', 'PODReceivedBy') IS NULL
    ALTER TABLE dbo.PickingSlipMaster ADD PODReceivedBy NVARCHAR(100) NULL;
GO

IF COL_LENGTH('dbo.PickingSlipMaster', 'PODConfirmedDate') IS NULL
    ALTER TABLE dbo.PickingSlipMaster ADD PODConfirmedDate DATETIME NULL;
GO

IF COL_LENGTH('dbo.PickingSlipMaster', 'PODConfirmedBy') IS NULL
    ALTER TABLE dbo.PickingSlipMaster ADD PODConfirmedBy NVARCHAR(100) NULL;
GO
