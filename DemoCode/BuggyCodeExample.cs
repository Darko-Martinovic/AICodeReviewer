// Deliberately buggy code for demonstration purposes
// This file contains common issues that AI Code Reviewer should detect
// 


using System;
using System.Threading.Tasks;

namespace DemoCode
{
    public class BuggyCodeExample
    {
        // Security Issue: Hardcoded connection string
        private string connectionString = "Server=localhost;Database=mydb;User Id=admin;Password=123456;";

        // Performance Issue: Async void instead of async Task
        public async void ProcessDataAsync()
        {
            // Security Issue: Simulated SQL Injection vulnerability
            var userId = GetUserInput();
            var query = $"SELECT * FROM Users WHERE Id = {userId}";

            // Simulate database connection (removed actual SQL for demo)
            await Task.Delay(100); // Missing ConfigureAwait(false)

            // Simulate processing without proper disposal
            Console.WriteLine($"Executing: {query}");
        }

        // Code Quality Issue: Magic numbers
        public decimal CalculateDiscount(decimal amount)
        {
            if (amount > 1000)
                return amount * 0.15m; // What does 0.15 represent?
            else if (amount > 500)
                return amount * 0.10m; // Magic number
            else
                return amount * 0.05m; // Magic number
        }

        // Maintainability Issue: Deep nesting
        public string ProcessOrder(Order order)
        {
            if (order != null)
            {
                if (order.Items != null)
                {
                    if (order.Items.Count > 0)
                    {
                        if (order.Customer != null)
                        {
                            if (!string.IsNullOrEmpty(order.Customer.Email))
                            {
                                // Deep nesting makes this hard to read
                                return "Order processed";
                            }
                        }
                    }
                }
            }
            return "Order failed";
        }

        // Bug: Null reference exception potential
        public void UpdateUserProfile(User user)
        {
            user.LastUpdated = DateTime.Now; // What if user is null?
            user.Profile.UpdateCount++; // What if Profile is null?
        }

        // Performance Issue: Inefficient loop
        public List<Product> FilterExpensiveProducts(List<Product> products)
        {
            var result = new List<Product>();

            // Inefficient: Multiple iterations
            for (int i = 0; i < products.Count; i++)
            {
                for (int j = 0; j < products.Count; j++)
                {
                    if (products[i].Price > 100 && products[j].Category == "Electronics")
                    {
                        result.Add(products[i]);
                    }
                }
            }

            return result;
        }

        // Security Issue: Exception swallowing
        public string GetUserInput()
        {
            try
            {
                // Simulate getting user input
                return Console.ReadLine();
            }
            catch
            {
                // Swallowing exceptions hides problems
                return "";
            }
        }
    }

    public class Order
    {
        public List<OrderItem>? Items { get; set; }
        public Customer? Customer { get; set; }
    }

    public class OrderItem
    {
        public string? Name { get; set; }
    }

    public class Customer
    {
        public string? Email { get; set; }
    }

    public class User
    {
        public DateTime LastUpdated { get; set; }
        public UserProfile? Profile { get; set; }
    }

    public class UserProfile
    {
        public int UpdateCount { get; set; }
    }

    public class Product
    {
        public decimal Price { get; set; }
        public string? Category { get; set; }
    }
}
