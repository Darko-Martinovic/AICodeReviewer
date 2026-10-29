CREATE OR ALTER PROCEDURE dbo.SearchProducts
    @Search NVARCHAR(100),
    @SortColumn SYSNAME = N'Name',
    @SortDescending BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @pattern NVARCHAR(102) = N'%' + @Search + N'%';
    DECLARE @sql NVARCHAR(MAX) = N'
        SELECT ProductId, Name, Price, CreatedAt
        FROM dbo.Products
        WHERE Name LIKE @pattern
        ORDER BY ' + @SortColumn
        + CASE WHEN @SortDescending = 1 THEN N' DESC' ELSE N' ASC' END + N';';

    EXEC sys.sp_executesql @sql, N'@pattern NVARCHAR(102)', @pattern = @pattern;
END;
