using System.Text.Json;
using AICodeReviewer.Models.Review;

namespace AICodeReviewer.Services.Reviewing
{
    /// <summary>
    /// The models the reviewer may use, with their prices, read from Configuration/models.json
    /// </summary>
    public class ModelCatalog
    {
        public static readonly string[] Providers = ["anthropic", "azure-openai", "openai-compatible"];

        public string DefaultReviewer { get; set; } = "";
        public string DefaultJudge { get; set; } = "";
        public List<ModelInfo> Models { get; set; } = new();

        public static ModelCatalog Load(string path)
        {
            var catalog = JsonSerializer.Deserialize<ModelCatalog>(
                File.ReadAllText(path),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException($"{path} is empty");

            var unknown = catalog.Models.FirstOrDefault(m => !Providers.Contains(m.Provider));
            if (unknown is not null)
            {
                throw new InvalidOperationException($"Model '{unknown.Key}' in {path} has unknown provider '{unknown.Provider}'");
            }
            return catalog;
        }

        public static ModelCatalog LoadDefault() =>
            Load(Path.Combine(AppContext.BaseDirectory, "Configuration", "models.json"));

        public ModelInfo Get(string key) =>
            Models.FirstOrDefault(m => m.Key == key)
            ?? throw new InvalidOperationException(
                $"Model '{key}' is not in models.json; known models: {string.Join(", ", Models.Select(m => m.Key))}");
    }
}
