CREATE OR ALTER PROCEDURE dbo.MonthlyRevenue
    @Year INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Year NOT BETWEEN 2000 AND 2100
        THROW 50030, N'Year is out of range.', 1;

    DECLARE @From DATE = DATEFROMPARTS(@Year, 1, 1);
    DECLARE @To DATE = DATEADD(YEAR, 1, @From);

    SELECT
        DATEFROMPARTS(YEAR(o.OrderDate), MONTH(o.OrderDate), 1) AS MonthStart,
        COUNT(*) AS Orders,
        SUM(o.Total) AS Revenue
    FROM dbo.Orders AS o
    WHERE o.OrderDate >= @From
      AND o.OrderDate < @To
      AND o.Status <> N'Cancelled'
    GROUP BY DATEFROMPARTS(YEAR(o.OrderDate), MONTH(o.OrderDate), 1)
    ORDER BY MonthStart;
END;
