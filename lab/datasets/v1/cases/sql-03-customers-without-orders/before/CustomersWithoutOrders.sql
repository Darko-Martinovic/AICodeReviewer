CREATE OR ALTER PROCEDURE dbo.CustomersWithoutOrders
    @Year INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From DATE = DATEFROMPARTS(@Year, 1, 1);
    DECLARE @To DATE = DATEADD(YEAR, 1, @From);

    SELECT c.CustomerId, c.Name, c.Email
    FROM dbo.Customers AS c
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.Orders AS o
        WHERE o.CustomerId = c.CustomerId
          AND o.OrderDate >= @From
          AND o.OrderDate < @To
    )
    ORDER BY c.Name;
END;
