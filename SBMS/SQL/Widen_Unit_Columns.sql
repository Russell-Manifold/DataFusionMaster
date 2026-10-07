/* ============================================================================
   Widen unit-of-measure columns to varchar(50) to match the EDMX.
   Live BOM line saves failed with "String or binary data would be truncated"
   (errorlog 2026-10-06) because BOMLines.BomUnit was still varchar(20).
   Safe to run more than once. No data is changed, only the column size.
   ============================================================================ */
DECLARE @t TABLE (tbl sysname, col sysname);
INSERT @t VALUES ('BOMLines','BomUnit'), ('DocLines','Unit'), ('ItemsMaster','Unit'),
                 ('ItemTransaction','Unit'), ('ItemTransferLines','Unit'), ('JobCardLines','Unit'),
                 ('PickSlipLines','Unit'), ('TempDocLines','Unit'), ('WorksOrderRMLines','Unit');
DECLARE @tbl sysname, @col sysname, @len int, @nullable bit, @sql nvarchar(400);
DECLARE c CURSOR LOCAL FOR SELECT tbl, col FROM @t;
OPEN c; FETCH NEXT FROM c INTO @tbl, @col;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @len = c.max_length, @nullable = c.is_nullable
    FROM sys.columns c WHERE c.object_id = OBJECT_ID('dbo.' + @tbl) AND c.name = @col;
    IF @len IS NULL
        PRINT @tbl + '.' + @col + ' not found - skipped';
    ELSE IF @len >= 50 OR @len = -1
        PRINT @tbl + '.' + @col + ' already ' + CAST(@len AS varchar) + ' - ok';
    ELSE
    BEGIN
        SET @sql = 'ALTER TABLE dbo.' + QUOTENAME(@tbl) + ' ALTER COLUMN ' + QUOTENAME(@col) + ' varchar(50)' + CASE WHEN @nullable = 1 THEN ' NULL' ELSE ' NOT NULL' END;
        EXEC sp_executesql @sql;
        PRINT @tbl + '.' + @col + ' widened 20 -> 50';
    END
    SET @len = NULL;
    FETCH NEXT FROM c INTO @tbl, @col;
END
CLOSE c; DEALLOCATE c;
GO
