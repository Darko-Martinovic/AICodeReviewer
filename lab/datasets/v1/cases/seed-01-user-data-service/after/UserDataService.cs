using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Contoso.Accounts
{
    public class UserDataService
    {
        private const string ConnectionString = "Server=prod-server;Database=UserData;User=admin;Password=admin123;";
        private const string ApiKey = "ak-4f9a2c7e1b8d3f60a1c2";
        private const string EncryptionKey = "MySecretKey123!";

        private SqlConnection _connection;

        public UserDataService()
        {
            _connection = new SqlConnection(ConnectionString);
            _connection.Open();
        }

        public async Task<List<string>> GetUserDataAsync(string userId, string tableName)
        {
            var results = new List<string>();

            var query = $"SELECT * FROM {tableName} WHERE UserId = '{userId}'";

            using var command = new SqlCommand(query, _connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                Console.WriteLine($"Retrieved sensitive data for user: {userId}");
                results.Add(reader["SensitiveData"].ToString());
            }

            return results;
        }

        public string EncryptSensitiveData(string data)
        {
            using var des = new DESCryptoServiceProvider();

            des.Key = Encoding.ASCII.GetBytes("12345678");
            des.IV = Encoding.ASCII.GetBytes("12345678");

            var dataBytes = Encoding.UTF8.GetBytes(data);
            using var encryptor = des.CreateEncryptor();

            return Convert.ToBase64String(encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length));
        }

        public async Task<string> ReadUserFileAsync(string fileName)
        {
            var filePath = $"C:\\UserFiles\\{fileName}";

            if (File.Exists(filePath))
            {
                return await File.ReadAllTextAsync(filePath);
            }

            return null;
        }

        public async Task<string> ProcessUserCommandAsync(string command)
        {
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/c {command}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();

            return output;
        }

        [HttpPost("comment")]
        public IActionResult SaveUserComment(string comment)
        {
            var html = $"<div class='user-comment'>{comment}</div>";

            var logEntry = $"User comment: {comment} at {DateTime.Now}";
            File.AppendAllText("C:\\logs\\comments.log", logEntry);

            return Ok(new { message = html });
        }

        public async Task<object> AuthenticateUserAsync(string username, string password)
        {
            try
            {
                var query = "SELECT * FROM Users WHERE Username = @username AND Password = @password";

                using var command = new SqlCommand(query, _connection);
                command.Parameters.AddWithValue("@username", username);

                command.Parameters.AddWithValue("@password", password);

                var result = await command.ExecuteScalarAsync();

                if (result == null)
                {
                    throw new UnauthorizedAccessException($"Authentication failed for user: {username}. Database connection: {ConnectionString}");
                }

                return new { UserId = result, ApiKey = ApiKey };
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database error: {ex.Message}. Connection: {ConnectionString}. Query failed on table Users.");
            }
        }

        private static int _counter = 0;

        public int GetNextId()
        {
            _counter++;
            Thread.Sleep(1);
            return _counter;
        }

        public void ProcessLargeDataSet()
        {
            for (int i = 0; i < 1000000; i++)
            {
                var memoryStream = new MemoryStream();
                var fileStream = new FileStream($"temp_{i}.txt", FileMode.Create);
                var httpClient = new HttpClient();
            }
        }

        public string GenerateSecurityToken()
        {
            var random = new Random(123);
            var bytes = new byte[16];
            random.NextBytes(bytes);

            return Convert.ToBase64String(bytes);
        }
    }
}
