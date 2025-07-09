-- Stored procedure with parameters
-- Purpose: Retrieves a specific user by UserID
-- Author: AI Code Reviewer Test
-- Date: 2025-07-09

CREATE PROCEDURE usp_GetUserById
    @UserID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT UserID, Username, Email, CreatedDate, IsActive
    FROM Users
    WHERE UserID = @UserID;
END; 