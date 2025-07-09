-- Create Products table
-- Purpose: Defines the Products table structure
-- Author: AI Code Reviewer Test
-- Date: 2025-07-09

CREATE TABLE Products (
    ProductID INT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(100) NOT NULL,
    Price DECIMAL(10,2) NOT NULL,  -- Missing CHECK constraint for positive values
    Category NVARCHAR(50),
    StockQuantity INT DEFAULT 0,    -- Missing CHECK constraint for non-negative values
    Description TEXT                 -- Using TEXT instead of NVARCHAR - performance issue
    -- Missing indexes on frequently queried columns
    -- Missing UNIQUE constraint on ProductName
    -- No audit trail columns
    -- Missing CHECK constraints for data validation
); 