/*
    Audit_Description_Column_Lengths.sql
    --------------------------------------
    Lists every column whose name contains "Description" (covers ItemDescription,
    BundDescription, CtDescription, etc.) across all tables in the current database,
    showing the real data type and length straight from the live schema.

    The "Flag" column highlights anything that is NOT varchar/nvarchar(100):
      - "<-- 50"        : the 50-char columns you are hunting for
      - "<-- not 100"   : any other length that differs from 100
      - ""              : exactly 100 (the expected default)

    Run against the SBMS database. Read-only; changes nothing.
*/

SELECT
    c.TABLE_SCHEMA                       AS [Schema],
    c.TABLE_NAME                         AS [Table],
    c.COLUMN_NAME                        AS [Column],
    c.DATA_TYPE                          AS [Type],
    c.CHARACTER_MAXIMUM_LENGTH           AS [MaxLength],   -- -1 means varchar(MAX)
    CASE
        WHEN c.CHARACTER_MAXIMUM_LENGTH = 50  THEN '<-- 50'
        WHEN c.CHARACTER_MAXIMUM_LENGTH = 100 THEN ''
        ELSE '<-- not 100'
    END                                  AS [Flag]
FROM INFORMATION_SCHEMA.COLUMNS c
INNER JOIN INFORMATION_SCHEMA.TABLES t
    ON  t.TABLE_SCHEMA = c.TABLE_SCHEMA
    AND t.TABLE_NAME   = c.TABLE_NAME
    AND t.TABLE_TYPE   = 'BASE TABLE'          -- exclude views
WHERE c.COLUMN_NAME LIKE '%Description%'
  AND c.DATA_TYPE IN ('varchar', 'nvarchar', 'char', 'nchar')
ORDER BY
    CASE WHEN c.CHARACTER_MAXIMUM_LENGTH = 50 THEN 0 ELSE 1 END,  -- 50-char rows first
    c.TABLE_NAME,
    c.COLUMN_NAME;
