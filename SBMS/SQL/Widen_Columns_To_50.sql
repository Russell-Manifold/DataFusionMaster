/* ============================================================================
   Widen code / unit / store-code columns to 50 to match the EDMX (Oct 2026).
   Replaces Widen_Unit_Columns.sql, Widen_StoreCode_Columns.sql, Widen_Code_Columns.sql.

   - Unit, BomUnit                         -> varchar(50)
   - StoreCode, StoreCodeFrom              -> 50 (keeps varchar/nvarchar as is)
   - ItemsMaster.BarCode, ItemsMaster.Code -> varchar(50)
   - ItemCode, FGCode, KitCode, BOMCode    -> 50 (verify; widen only if short)

   In-place ALTER COLUMN only: no data changed, no index dropped (widening a
   varchar inside a non-PK index is allowed). Prints one line per column.
   Safe to run more than once. Run on LIVE and DEMO before deploying the DLL.
   ============================================================================ */
SET NOCOUNT ON;
DECLARE @tbl sysname, @col sysname, @len int, @nullable bit, @type sysname, @sql nvarchar(400), @shown int;

DECLARE c CURSOR LOCAL FOR
    SELECT t.name, c.name
    FROM sys.columns c
    JOIN sys.tables t  ON t.object_id = c.object_id
    JOIN sys.types ty  ON ty.user_type_id = c.user_type_id
    WHERE ty.name IN ('varchar','nvarchar')
      AND (   c.name IN ('Unit','BomUnit')
           OR c.name IN ('StoreCode','StoreCodeFrom')
           OR (t.name = 'ItemsMaster' AND c.name IN ('BarCode','Code'))
           OR c.name IN ('ItemCode','FGCode','KitCode','BOMCode','BomCode') )
    ORDER BY t.name, c.name;

OPEN c; FETCH NEXT FROM c INTO @tbl, @col;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @len = c.max_length, @nullable = c.is_nullable, @type = ty.name
    FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID('dbo.' + @tbl) AND c.name = @col;

    SET @shown = CASE WHEN @len = -1 THEN -1 WHEN @type = 'nvarchar' THEN @len / 2 ELSE @len END;

    IF @len = -1 OR @shown >= 50
        PRINT @tbl + '.' + @col + '  ' + @type + '(' + CASE WHEN @len = -1 THEN 'max' ELSE CAST(@shown AS varchar) END + ')  ok';
    ELSE
    BEGIN
        SET @sql = 'ALTER TABLE dbo.' + QUOTENAME(@tbl) + ' ALTER COLUMN ' + QUOTENAME(@col) + ' ' + @type + '(50)'
                 + CASE WHEN @nullable = 1 THEN ' NULL' ELSE ' NOT NULL' END;
        EXEC sp_executesql @sql;
        PRINT @tbl + '.' + @col + '  ' + @type + '(' + CAST(@shown AS varchar) + ')  -> 50';
    END
    FETCH NEXT FROM c INTO @tbl, @col;
END
CLOSE c; DEALLOCATE c;
GO
