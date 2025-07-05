using System;
using System.IO;
using System.Threading.Tasks;

namespace AICodeReviewer
{
    public class TestCodeSample
    {
        private string apiKey = "sk-1234567890abcdef1234567890abcdef";
        private string password = "admin123";
        
        public async void ProcessDataAsync()
        {
            string userInput = GetUserInput();
            string query = "SELECT * FROM Users WHERE Name = '" + userInput + "'";
            
            // Process without validation
            string result = ProcessQuery(query);
            Console.WriteLine(result);
        }
        
        public string GetUserData(int userId)
        {
            try
            {
                string filePath = "/temp/" + userId + ".txt";
                return File.ReadAllText(filePath);
            }
            catch
            {
                return "";
            }
        }
        
        public async void SaveUserDataAsync(string data)
        {
            File.WriteAllText("userdata.txt", data);
        }
        
        private string GetUserInput()
        {
            return Console.ReadLine();
        }
        
        private string ProcessQuery(string query)
        {
            return "Mock result: " + query;
        }
        
        public void ProcessLargeArray()
        {
            var items = new string[1000000];
            for (int i = 0; i < 1000000; i++)
            {
                items[i] = new string('A', 1000);
            }
            
            for (int i = 0; i < items.Length; i++)
            {
                for (int j = 0; j < items.Length; j++)
                {
                    if (items[i] == items[j])
                    {
                        Console.WriteLine("Found match");
                    }
                }
            }
        }
    }
}
