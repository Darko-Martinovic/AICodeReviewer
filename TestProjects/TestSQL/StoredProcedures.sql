-- Sample stored procedures for testing T-SQL prompts
-- This file contains various stored procedures with different complexity levels

-- Simple stored procedure to get all users
CREATE PROCEDURE usp_GetAllUsers
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT UserID, Username, Email, CreatedDate, IsActive
    FROM Users
    WHERE IsActive = 1
    ORDER BY CreatedDate DESC;
END;

-- Stored procedure with parameters
CREATE PROCEDURE usp_GetUserById
    @UserID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT UserID, Username, Email, CreatedDate, IsActive
    FROM Users
    WHERE UserID = @UserID;
END;

-- Complex stored procedure with joins and aggregation
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