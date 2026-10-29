CREATE OR ALTER PROCEDURE dbo.TransferFunds
    @FromAccountId INT,
    @ToAccountId INT,
    @Amount DECIMAL(19, 4)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Amount <= 0
        THROW 50010, N'Amount must be positive.', 1;

    BEGIN TRANSACTION;

    UPDATE dbo.Accounts
    SET Balance = Balance - @Amount
    WHERE AccountId = @FromAccountId
      AND Balance >= @Amount;

    IF @@ROWCOUNT = 0
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50011, N'Insufficient funds or unknown account.', 1;
    END;

    UPDATE dbo.Accounts
    SET Balance = Balance + @Amount
    WHERE AccountId = @ToAccountId;

    IF @@ROWCOUNT = 0
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50012, N'Unknown destination account.', 1;
    END;

    INSERT INTO dbo.Transfers (FromAccountId, ToAccountId, Amount, TransferredAt)
    VALUES (@FromAccountId, @ToAccountId, @Amount, SYSUTCDATETIME());

    COMMIT TRANSACTION;
END;
