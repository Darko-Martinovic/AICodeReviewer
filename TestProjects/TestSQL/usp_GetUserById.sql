-- Stored procedure with parameters
-- Purpose: Retrieves a specific user by UserID
-- Author: AI Code Reviewer Test
-- Date: 2025-07-09

CREATE PROCEDURE usp_GetUserById
    @UserID NVARCHAR(50)  -- Wrong data type - should be INT
AS
BEGIN
    -- Missing SET NOCOUNT ON
    -- SQL injection vulnerability - dynamic SQL without proper escaping
    
    DECLARE @sql NVARCHAR(MAX)
    SET @sql = 'SELECT * FROM Users WHERE UserID = ' + @UserID
    
    EXEC sp_executesql @sql
    
    -- Missing error handling
    -- No parameter validation
    -- Using dynamic SQL unnecessarily
END; 