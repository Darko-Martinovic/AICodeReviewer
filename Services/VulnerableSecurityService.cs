using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace AICodeReviewer.Services
{
    /// <summary>
    /// INTENTIONALLY BUGGY SERVICE FOR TESTING CODE REVIEW
    /// This service contains multiple critical security vulnerabilities
    /// </summary>
    public class VulnerableSecurityService
    {
        // BUG: Hardcoded credentials in production code
        private const string ConnectionString = "Server=prod-server;Database=UserData;User=admin;Password=admin123;";
        private const string ApiKey = "sk-1234567890abcdef-PRODUCTION-SECRET";
        private const string EncryptionKey = "MySecretKey123!"; // BUG: Weak hardcoded encryption key
        
        private SqlConnection _connection;

        public VulnerableSecurityService()
        {
            // BUG: Opening connection in constructor without proper disposal
            _connection = new SqlConnection(ConnectionString);
            _connection.Open();
        }

        /// <summary>
        /// BUG: SQL Injection vulnerability - user input directly concatenated
        /// </summary>
        public async Task<List<string>> GetUserDataAsync(string userId, string tableName)
        {
            var results = new List<string>();
            
            // CRITICAL BUG: SQL Injection - never do this!
            var query = $"SELECT * FROM {tableName} WHERE UserId = '{userId}'";
            
            using var command = new SqlCommand(query, _connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                // BUG: Potentially exposing sensitive data in logs
                Console.WriteLine($"Retrieved sensitive data for user: {userId}");
                results.Add(reader["SensitiveData"].ToString());
            }
            
            return results;
        }

        /// <summary>
        /// BUG: Weak encryption implementation
        /// </summary>
        public string EncryptSensitiveData(string data)
        {
            // BUG: Using deprecated/weak encryption
            using var des = new DESCryptoServiceProvider();
            
            // BUG: Hardcoded IV and key
            des.Key = Encoding.ASCII.GetBytes("12345678");
            des.IV = Encoding.ASCII.GetBytes("12345678");
            
            var dataBytes = Encoding.UTF8.GetBytes(data);
            using var encryptor = des.CreateEncryptor();
            
            // BUG: No proper exception handling
            return Convert.ToBase64String(encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length));
        }

        /// <summary>
        /// BUG: Path traversal vulnerability
        /// </summary>
        public async Task<string> ReadUserFileAsync(string fileName)
        {
            // CRITICAL BUG: Path traversal - user can access any file on system
            var filePath = $"C:\\UserFiles\\{fileName}";
            
            // BUG: No validation of file path
            if (File.Exists(filePath))
            {
                return await File.ReadAllTextAsync(filePath);
            }
            
            return null;
        }

        /// <summary>
        /// BUG: Command injection vulnerability
        /// </summary>
        public async Task<string> ProcessUserCommandAsync(string command)
        {
            // CRITICAL BUG: Command injection - never execute user input directly
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/c {command}"; // User input executed directly!
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();
            
            return output;
        }

        /// <summary>
        /// BUG: Cross-Site Scripting (XSS) vulnerability
        /// </summary>
        [HttpPost("comment")]
        public IActionResult SaveUserComment(string comment)
        {
            // BUG: No XSS protection - user input returned directly to browser
            var html = $"<div class='user-comment'>{comment}</div>";
            
            // BUG: Storing unvalidated user input
            var logEntry = $"User comment: {comment} at {DateTime.Now}";
            File.AppendAllText("C:\\logs\\comments.log", logEntry);
            
            return Ok(new { message = html });
        }

        /// <summary>
        /// BUG: Information disclosure through error messages
        /// </summary>
        public async Task<object> AuthenticateUserAsync(string username, string password)
        {
            try
            {
                // BUG: Detailed error messages reveal system information
                var query = "SELECT * FROM Users WHERE Username = @username AND Password = @password";
                
                using var command = new SqlCommand(query, _connection);
                command.Parameters.AddWithValue("@username", username);
                
                // BUG: Storing passwords in plain text (no hashing)
                command.Parameters.AddWithValue("@password", password);
                
                var result = await command.ExecuteScalarAsync();
                
                if (result == null)
                {
                    throw new UnauthorizedAccessException($"Authentication failed for user: {username}. Database connection: {ConnectionString}");
                }
                
                return new { UserId = result, ApiKey = ApiKey }; // BUG: Exposing API key
            }
            catch (SqlException ex)
            {
                // BUG: Exposing database schema and connection details
                throw new Exception($"Database error: {ex.Message}. Connection: {ConnectionString}. Query failed on table Users.");
            }
        }

        /// <summary>
        /// BUG: Race condition and thread safety issues
        /// </summary>
        private static int _counter = 0;
        
        public int GetNextId()
        {
            // BUG: Race condition - not thread safe
            _counter++;
            Thread.Sleep(1); // Simulating some work that increases race condition likelihood
            return _counter;
        }

        /// <summary>
        /// BUG: Memory leak - not disposing resources
        /// </summary>
        public void ProcessLargeDataSet()
        {
            for (int i = 0; i < 1000000; i++)
            {
                // BUG: Creating objects without disposal
                var memoryStream = new MemoryStream();
                var fileStream = new FileStream($"temp_{i}.txt", FileMode.Create);
                var httpClient = new HttpClient();
                
                // Never disposed - memory leak
            }
        }

        /// <summary>
        /// BUG: Insecure random number generation
        /// </summary>
        public string GenerateSecurityToken()
        {
            // BUG: Using weak random number generator for security-critical operations
            var random = new Random(123); // BUG: Fixed seed makes it predictable
            var bytes = new byte[16];
            random.NextBytes(bytes);
            
            return Convert.ToBase64String(bytes);
        }

        // BUG: No disposal pattern implemented
        // Should implement IDisposable but doesn't
    }
}
