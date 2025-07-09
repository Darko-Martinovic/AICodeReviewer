using System;
using System.Collections.Generic;
using System.Linq;

namespace TestCSharp
{
    /// <summary>
    /// Utility class for testing C# prompt functionality
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Calculates the sum of numbers in a list
        /// </summary>
        /// <param name="numbers">List of numbers to sum</param>
        /// <returns>The sum of all numbers</returns>
        public static int CalculateSum(List<int> numbers)
        {
            if (numbers == null)
                throw new ArgumentNullException(nameof(numbers));
            
            return numbers.Sum();
        }

        /// <summary>
        /// Filters a list based on a predicate
        /// </summary>
        /// <typeparam name="T">Type of items in the list</typeparam>
        /// <param name="items">List to filter</param>
        /// <param name="predicate">Filter condition</param>
        /// <returns>Filtered list</returns>
        public static List<T> Filter<T>(List<T> items, Func<T, bool> predicate)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            
            return items.Where(predicate).ToList();
        }

        /// <summary>
        /// Validates an email address format
        /// </summary>
        /// <param name="email">Email to validate</param>
        /// <returns>True if valid email format</returns>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;
            
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
} 