-- Create Users table
-- Purpose: Defines the Users table structure
-- Author: AI Code Reviewer Test
-- Date: 2025-07-09

CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    CreatedDate DATETIME2 DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1
); 