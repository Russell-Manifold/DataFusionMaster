-- Add CountExpression column to StockCountLines table
-- Stores the count calculation string (e.g. "4+3+8") entered via the mobile Stock Counts scanner

ALTER TABLE [dbo].[StockCountLines]
ADD [CountExpression] VARCHAR(200) NULL;
