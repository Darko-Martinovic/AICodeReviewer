-- Complex stored procedure with joins and aggregation
-- Purpose: Retrieves user order summary with optional filtering
-- Author: AI Code Reviewer Test
-- Date: 2025-07-09

CREATE PROCEDURE usp_GetUserOrderSummary
    @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.UserID,
        u.Username,
        u.Email,
        COUNT(o.OrderID) AS TotalOrders,
        SUM(o.TotalAmount) AS TotalSpent,
        AVG(o.TotalAmount) AS AverageOrderValue
    FROM Users u
    LEFT JOIN Orders o ON u.UserID = o.UserID
    WHERE (@UserID IS NULL OR u.UserID = @UserID)
    GROUP BY u.UserID, u.Username, u.Email
    ORDER BY TotalSpent DESC;
END; 