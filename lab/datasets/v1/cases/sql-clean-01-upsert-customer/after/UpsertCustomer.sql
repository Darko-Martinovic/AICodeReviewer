CREATE OR ALTER PROCEDURE dbo.UpsertCustomer
    @Email NVARCHAR(320),
    @Name NVARCHAR(200),
    @CustomerId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Email IS NULL OR LTRIM(RTRIM(@Email)) = N''
        THROW 50020, N'Email is required.', 1;

    BEGIN TRANSACTION;

    SELECT @CustomerId = CustomerId
    FROM dbo.Customers WITH (UPDLOCK, HOLDLOCK)
    WHERE Email = @Email;

    IF @CustomerId IS NULL
    BEGIN
        INSERT INTO dbo.Customers (Email, Name, CreatedAt)
        VALUES (@Email, @Name, SYSUTCDATETIME());

        SET @CustomerId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.Customers
        SET Name = @Name, UpdatedAt = SYSUTCDATETIME()
        WHERE CustomerId = @CustomerId;
    END;

    COMMIT TRANSACTION;
END;
