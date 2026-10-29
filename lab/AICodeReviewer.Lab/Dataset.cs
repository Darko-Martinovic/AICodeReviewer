using System.Text.Json;

namespace AICodeReviewer.Lab
{
    /// <summary>
    /// A seeded defect in a case's reviewed file, from labels.json (the answer key)
    /// </summary>
    public record Label(string Id, int Line, int EndLine, string Category, string Severity, string Title, string Description);

    /// <summary>
    /// What case.json says about a case
    /// </summary>
    public record CaseInfo(string Id, string Language, string File, string Group, string Description, string Kind);

    /// <summary>
    /// The file a reviewer is given: its name and content, nothing from the answer key
    /// </summary>
    public record ReviewFile(string FileName, string Content);

    /// <summary>
    /// One case: the reviewed file (after/), optionally the version before the change (before/), and the labels
    /// </summary>
    public class DatasetCase(string folder, CaseInfo info)
    {
        public string Folder { get; } = folder;
        public CaseInfo Info { get; } = info;

        public ReviewFile ForReview() => new(Info.File, ReadText(Path.Combine(Folder, "after", Info.File))!);

        public string? Before() => ReadText(Path.Combine(Folder, "before", Info.File));

        public IReadOnlyList<Label> Labels() =>
            JsonSerializer.Deserialize<List<Label>>(File.ReadAllText(Path.Combine(Folder, "labels.json")), Dataset.Json)
            ?? throw new InvalidOperationException($"{Folder}/labels.json is empty");

        private static string? ReadText(string path) =>
            File.Exists(path) ? File.ReadAllText(path).Replace("\r\n", "\n") : null;
    }

    /// <summary>
    /// A labelled set of review cases in lab/datasets/&lt;name&gt;/cases
    /// </summary>
    public class Dataset(string name, IReadOnlyList<DatasetCase> cases)
    {
        public static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

        public string Name { get; } = name;
        public IReadOnlyList<DatasetCase> Cases { get; } = cases;

        public static Dataset Load(string folder)
        {
            var cases = Directory.GetDirectories(Path.Combine(folder, "cases"))
                .Order(StringComparer.Ordinal)
                .Select(caseFolder => new DatasetCase(
                    caseFolder,
                    JsonSerializer.Deserialize<CaseInfo>(File.ReadAllText(Path.Combine(caseFolder, "case.json")), Json)
                        ?? throw new InvalidOperationException($"{caseFolder}/case.json is empty")))
                .ToList();
            return new Dataset(Path.GetFileName(Path.TrimEndingDirectorySeparator(folder)), cases);
        }

        /// <summary>
        /// lab/datasets/&lt;name&gt;, found by walking up from the given folder to the repository root
        /// </summary>
        public static string Find(string name, string? from = null)
        {
            for (var dir = new DirectoryInfo(from ?? Directory.GetCurrentDirectory()); dir is not null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "lab", "datasets", name);
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }
            throw new DirectoryNotFoundException($"No lab/datasets/{name} above {from ?? Directory.GetCurrentDirectory()}");
        }
    }
}
