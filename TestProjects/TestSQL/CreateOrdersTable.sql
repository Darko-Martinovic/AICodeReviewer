-- Create Orders table
-- Purpose: Defines the Orders table structure with foreign key relationship
-- Author: AI Code Reviewer Test
-- Date: 2025-07-09

CREATE TABLE Orders (
    OrderID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    OrderDate DATETIME2 DEFAULT GETDATE(),
    TotalAmount DECIMAL(10,2) NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Pending',
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
); 