-- Sample views for testing T-SQL prompts
-- This file contains various database views

-- View for active users
CREATE VIEW vw_ActiveUsers AS
SELECT UserID, Username, Email, CreatedDate
FROM Users
WHERE IsActive = 1;

-- View for order summary
CREATE VIEW vw_OrderSummary AS
SELECT 
    o.OrderID,
    u.Username,
    o.OrderDate,
    o.TotalAmount,
    o.Status
FROM Orders o
INNER JOIN Users u ON o.UserID = u.UserID;

-- View for product inventory
CREATE VIEW vw_ProductInventory AS
SELECT 
    ProductID,
    ProductName,
    Price,
    Category,
    StockQuantity,
    CASE 
        WHEN StockQuantity = 0 THEN 'Out of Stock'
        WHEN StockQuantity < 10 THEN 'Low Stock'
        ELSE 'In Stock'
    END AS StockStatus
FROM Products; 