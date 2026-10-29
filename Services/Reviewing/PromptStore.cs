using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AICodeReviewer.Services.Reviewing
{
    /// <summary>
    /// Whether the lab kept a prompt version, and the scores it was decided on
    /// </summary>
    public record PromptDecision(
        string Result, string Date, string Reason,
        double RecallBefore, double RecallAfter, double PrecisionBefore, double PrecisionAfter, double F1Before, double F1After,
        IReadOnlyList<string> BaselineRuns, IReadOnlyList<string> CandidateRuns);

    public record PromptVersion(string Language, int Version, int? BasedOn, string Created, string Notes, PromptDecision? Decision);

    /// <summary>
    /// The versions of each language's system prompt, from Configuration/Prompts: one file per version
    /// (&lt;Language&gt;/v&lt;N&gt;.md) and prompts.json, which says which version is active and why
    /// </summary>
    public class PromptStore
    {
        public const string General = "General";

        public static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true
        };

        private record Manifest(Dictionary<string, int> Active, List<PromptVersion> Versions);

        private readonly Manifest _manifest;

        private PromptStore(string folder, Manifest manifest)
        {
            Folder = folder;
            _manifest = manifest;
        }

        public string Folder { get; }
        public IReadOnlyDictionary<string, int> Active => _manifest.Active;
        public IReadOnlyList<PromptVersion> Versions => _manifest.Versions;

        public static PromptStore Load(string folder)
        {
            var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(Path.Combine(folder, "prompts.json")), Json)
                ?? throw new InvalidOperationException($"{folder}/prompts.json is empty");
            return new PromptStore(folder, manifest);
        }

        public static PromptStore LoadDefault() =>
            Load(Path.Combine(AppContext.BaseDirectory, "Configuration", "Prompts"));

        public string Text(string language, int version)
        {
            var path = Path.Combine(Folder, language, $"v{version}.md");
            return File.Exists(path)
                ? File.ReadAllText(path).Replace("\r\n", "\n").Trim()
                : throw new InvalidOperationException($"There is no version {version} of the {language} prompt");
        }

        /// <summary>
        /// The active version for the language, or the active general prompt for a language without prompts of its own
        /// </summary>
        public string ActiveText(string language) =>
            Active.TryGetValue(language, out var version) ? Text(language, version) : Text(General, Active[General]);

        public int ActiveVersion(string language) =>
            Active.TryGetValue(language, out var version) ? version : Active[General];

        /// <summary>
        /// The same versions with some languages switched to other versions, for trying a version without keeping it
        /// </summary>
        public PromptStore WithActive(IReadOnlyDictionary<string, int> overrides)
        {
            var active = new Dictionary<string, int>(_manifest.Active);
            foreach (var (language, version) in overrides)
            {
                Text(language, version);
                active[language] = version;
            }
            return new PromptStore(Folder, _manifest with { Active = active });
        }

        /// <summary>
        /// Copies a version (the active one by default) to a new version with notes, and returns its number
        /// </summary>
        public int NewVersion(string language, string notes, int? from = null, DateOnly? today = null)
        {
            var source = from ?? ActiveVersion(language);
            var text = Text(language, source);
            var version = Versions.Where(v => v.Language == language).Select(v => v.Version).DefaultIfEmpty(0).Max() + 1;
            File.WriteAllText(Path.Combine(Folder, language, $"v{version}.md"), text + "\n");
            _manifest.Versions.Add(new PromptVersion(language, version, source,
                (today ?? DateOnly.FromDateTime(DateTime.UtcNow)).ToString("yyyy-MM-dd"), notes, null));
            Save();
            return version;
        }

        /// <summary>
        /// Records the lab's decision on a version and makes it active when it is kept
        /// </summary>
        public void Decide(string language, int version, PromptDecision decision)
        {
            var index = _manifest.Versions.FindIndex(v => v.Language == language && v.Version == version);
            if (index < 0)
            {
                throw new InvalidOperationException($"There is no version {version} of the {language} prompt");
            }
            _manifest.Versions[index] = _manifest.Versions[index] with { Decision = decision };
            if (decision.Result == "kept")
            {
                _manifest.Active[language] = version;
            }
            Save();
        }

        private void Save() =>
            File.WriteAllText(Path.Combine(Folder, "prompts.json"), JsonSerializer.Serialize(_manifest, Json) + "\n");

        /// <summary>
        /// A short fingerprint of the system prompt a review used, the same on any operating system
        /// </summary>
        public static string Hash(string systemPrompt) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(systemPrompt.Replace("\r\n", "\n").Trim())))[..12].ToLowerInvariant();
    }
}
