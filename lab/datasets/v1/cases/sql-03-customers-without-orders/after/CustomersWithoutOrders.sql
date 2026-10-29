CREATE OR ALTER PROCEDURE dbo.CustomersWithoutOrders
    @Year INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT c.CustomerId, c.Name, c.Email
    FROM dbo.Customers AS c
    WHERE c.CustomerId NOT IN (
        SELECT o.CustomerId
        FROM dbo.Orders AS o
        WHERE YEAR(o.OrderDate) = @Year
    )
    ORDER BY c.Name;
END;
