-- Create Users table
-- Purpose: Defines the Users table structure
-- Author: AI Code Reviewer Test
-- Date: 2025-07-09

CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL,  -- Missing UNIQUE constraint
    Email NVARCHAR(100) NOT NULL,    -- Missing UNIQUE constraint and validation
    CreatedDate DATETIME2 DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1,
    Password NVARCHAR(100) NOT NULL  -- Storing plain text passwords - security issue
    -- Missing indexes on frequently queried columns
    -- Missing CHECK constraints for data validation
    -- No audit trail columns
); 