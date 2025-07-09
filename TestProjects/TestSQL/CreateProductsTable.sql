-- Create Products table
-- Purpose: Defines the Products table structure
-- Author: AI Code Reviewer Test
-- Date: 2025-07-09

CREATE TABLE Products (
    ProductID INT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(100) NOT NULL,
    Price DECIMAL(10,2) NOT NULL,
    Category NVARCHAR(50),
    StockQuantity INT DEFAULT 0
); 