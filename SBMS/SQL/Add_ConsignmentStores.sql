-- Consignment stores: a store that physically sits at a customer's premises.
-- When ConsignCustomer is filled in, a transfer INTO that store prints as a
-- "Consignment Delivery Note" (customer name + delivery address, signature lines)
-- instead of the plain Transfer Slip. Empty = ordinary store, nothing changes.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Stores') AND name = 'ConsignCustomerId')
    ALTER TABLE dbo.Stores ADD ConsignCustomerId bigint NULL;   -- Sage customer ID (needed later to invoice usage)
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Stores') AND name = 'ConsignCustomer')
    ALTER TABLE dbo.Stores ADD ConsignCustomer varchar(100) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Stores') AND name = 'ConsignAddress')
    ALTER TABLE dbo.Stores ADD ConsignAddress varchar(400) NULL;
GO
-- Company switch: Consignment Stock Control on/off (Company Config). Off hides the consignment
-- column on Location Master and prints plain transfer slips even for linked stores.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CompanyMaster') AND name = 'UseConsignment')
    ALTER TABLE dbo.CompanyMaster ADD UseConsignment bit NOT NULL CONSTRAINT DF_CompanyMaster_UseConsignment DEFAULT 0;
GO
