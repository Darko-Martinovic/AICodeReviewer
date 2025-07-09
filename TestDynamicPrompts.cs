using System;
using AICodeReviewer.Services;
using AICodeReviewer.Models.Configuration;

namespace AICodeReviewer.Tests
{
    /// <summary>
    /// Simple test to verify dynamic prompt system
    /// </summary>
    public class DynamicPromptTest
    {
        public static void TestLanguageDetection()
        {
            Console.WriteLine("🧪 Testing Dynamic Prompt System");
            Console.WriteLine("=================================");

            // Create test instances
            var languageDetectionService = new LanguageDetectionService();
            var configService = new ConfigurationService();
            var promptManagementService = new PromptManagementService(languageDetectionService, configService);

            // Test files for different languages
            var testFiles = new[]
            {
                "Program.cs",
                "UserService.vb",
                "GetUsers.sql",
                "app.js",
                "types.ts",
                "Component.jsx",
                "Component.tsx",
                "config.py"
            };

            foreach (var fileName in testFiles)
            {
                var detectedLanguage = languageDetectionService.DetectLanguage(fileName);
                var languageKey = languageDetectionService.GetLanguageKey(fileName);
                var isSupported = languageDetectionService.IsLanguageSupported(fileName);
                var hasSpecificPrompts = promptManagementService.HasLanguageSpecificPrompts(fileName);

                Console.WriteLine($"📄 {fileName}");
                Console.WriteLine($"   Detected Language: {detectedLanguage}");
                Console.WriteLine($"   Language Key: {languageKey}");
                Console.WriteLine($"   Is Supported: {(isSupported ? "✅ Yes" : "❌ No")}");
                Console.WriteLine($"   Has Specific Prompts: {(hasSpecificPrompts ? "✅ Yes" : "❌ No")}");
                Console.WriteLine();
            }

            Console.WriteLine("✅ Dynamic prompt system test completed!");
        }
    }
}