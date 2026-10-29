CREATE OR ALTER PROCEDURE dbo.ImportPriceChanges
    @BatchId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    UPDATE p
    SET p.Price = s.NewPrice,
        p.UpdatedAt = SYSUTCDATETIME()
    FROM dbo.Products AS p
    JOIN staging.PriceChanges AS s ON s.Sku = p.Sku
    WHERE s.BatchId = @BatchId;

    DECLARE @Rows INT = @@ROWCOUNT;

    INSERT INTO dbo.ImportLog (BatchId, RowsUpdated, ImportedAt)
    VALUES (@BatchId, @Rows, SYSUTCDATETIME());

    DECLARE @LogId INT = SCOPE_IDENTITY();

    UPDATE staging.PriceChanges
    SET ImportLogId = @LogId
    WHERE BatchId = @BatchId;

    COMMIT TRANSACTION;
END;
