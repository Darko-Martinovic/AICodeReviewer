-- Simple stored procedure to get all users
-- Purpose: Retrieves all active users from the Users table
-- Author: AI Code Reviewer Test
-- Date: 2025-07-09

CREATE PROCEDURE usp_GetAllUsers
AS
BEGIN
    -- Missing SET NOCOUNT ON - performance issue
    -- Using SELECT * instead of specific columns - security and performance issue
    
    SELECT * FROM Users
    WHERE IsActive = 1
    ORDER BY CreatedDate DESC;
    
    -- Missing error handling
    -- No transaction management
    -- No parameter validation
END; 