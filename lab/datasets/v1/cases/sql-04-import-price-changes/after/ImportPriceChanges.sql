CREATE OR ALTER PROCEDURE dbo.ImportPriceChanges
    @BatchId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @Sku NVARCHAR(32), @NewPrice DECIMAL(19, 4), @Rows INT = 0;

    DECLARE changes CURSOR FOR
        SELECT Sku, NewPrice FROM staging.PriceChanges WHERE BatchId = @BatchId;
    OPEN changes;
    FETCH NEXT FROM changes INTO @Sku, @NewPrice;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        UPDATE dbo.Products SET Price = @NewPrice, UpdatedAt = SYSUTCDATETIME() WHERE Sku = @Sku;
        SET @Rows += @@ROWCOUNT;
        FETCH NEXT FROM changes INTO @Sku, @NewPrice;
    END;
    CLOSE changes;
    DEALLOCATE changes;

    INSERT INTO dbo.ImportLog (BatchId, RowsUpdated, ImportedAt)
    VALUES (@BatchId, @Rows, SYSUTCDATETIME());

    DECLARE @LogId INT = @@IDENTITY;

    UPDATE staging.PriceChanges
    SET ImportLogId = @LogId
    WHERE BatchId = @BatchId;

    COMMIT TRANSACTION;
END;
