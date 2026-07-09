-- RejectQty: EF model expects decimal(18,4) NOT NULL with default 0 on DocLines + TempDocLines.
-- Idempotent - safe to run repeatedly on any DB (test/prod). Ends with a verification report.

-- 1) Clear NULLs (no-op where column is already NOT NULL)
UPDATE DocLines     SET RejectQty = 0 WHERE RejectQty IS NULL;
UPDATE TempDocLines SET RejectQty = 0 WHERE RejectQty IS NULL;

-- 2) Make NOT NULL only if currently nullable
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DocLines') AND name = 'RejectQty' AND is_nullable = 1)
    ALTER TABLE dbo.DocLines ALTER COLUMN RejectQty decimal(18,4) NOT NULL;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TempDocLines') AND name = 'RejectQty' AND is_nullable = 1)
    ALTER TABLE dbo.TempDocLines ALTER COLUMN RejectQty decimal(18,4) NOT NULL;

-- 3) Add default 0 only if the column has no default yet
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints
               WHERE parent_object_id = OBJECT_ID('dbo.DocLines')
                 AND COL_NAME(parent_object_id, parent_column_id) = 'RejectQty')
    ALTER TABLE dbo.DocLines ADD CONSTRAINT DF_DocLines_RejectQty DEFAULT 0 FOR RejectQty;

IF NOT EXISTS (SELECT 1 FROM sys.default_constraints
               WHERE parent_object_id = OBJECT_ID('dbo.TempDocLines')
                 AND COL_NAME(parent_object_id, parent_column_id) = 'RejectQty')
    ALTER TABLE dbo.TempDocLines ADD CONSTRAINT DF_TempDocLines_RejectQty DEFAULT 0 FOR RejectQty;

-- 4) Verify: both rows must show IsNullable = 0, HasDefault = 1, NullRows = 0
SELECT t.name                                   AS TableName,
       c.is_nullable                            AS IsNullable,
       CASE WHEN dc.object_id IS NULL THEN 0 ELSE 1 END AS HasDefault,
       CONCAT(TYPE_NAME(c.user_type_id), '(', c.precision, ',', c.scale, ')') AS ColType,
       CASE t.name WHEN 'DocLines'     THEN (SELECT COUNT(*) FROM dbo.DocLines     WHERE RejectQty IS NULL)
                   WHEN 'TempDocLines' THEN (SELECT COUNT(*) FROM dbo.TempDocLines WHERE RejectQty IS NULL)
       END                                      AS NullRows
FROM sys.columns c
JOIN sys.tables t ON t.object_id = c.object_id
LEFT JOIN sys.default_constraints dc
       ON dc.parent_object_id = c.object_id
      AND dc.parent_column_id = c.column_id
WHERE c.name = 'RejectQty'
  AND t.name IN ('DocLines', 'TempDocLines');
