-- Simple stored procedure to get all users
-- Purpose: Retrieves all active users from the Users table
-- Author: AI Code Reviewer Test
-- Date: 2025-07-09

CREATE PROCEDURE usp_GetAllUsers
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT UserID, Username, Email, CreatedDate, IsActive
    FROM Users
    WHERE IsActive = 1
    ORDER BY CreatedDate DESC;
END; 