using AICodeReviewer.Lab;

// dotnet run --project lab/AICodeReviewer.Lab -- list [dataset]
// dotnet run --project lab/AICodeReviewer.Lab -- labels [dataset] [case id]
var command = args.FirstOrDefault() ?? "list";
switch (command)
{
    case "list":
        List(args.Length > 1 ? args[1] : "v1");
        return 0;
    case "labels":
        Labels(args.Length > 1 ? args[1] : "v1", args.Length > 2 ? args[2] : null);
        return 0;
    default:
        Console.Error.WriteLine($"Unknown command '{command}'. Commands: list [dataset], labels [dataset] [case id]");
        return 1;
}

static void Labels(string name, string? caseId)
{
    foreach (var c in Dataset.Load(Dataset.Find(name)).Cases.Where(c => caseId is null || c.Info.Id == caseId))
    {
        var lines = c.ForReview().Content.Split('\n');
        Console.WriteLine($"{c.Info.Id} ({c.Info.File}): {c.Info.Description}");
        foreach (var label in c.Labels())
        {
            Console.WriteLine($"  {label.Line}-{label.EndLine} [{label.Severity}/{label.Category}] {label.Title}");
            for (var n = label.Line; n <= label.EndLine; n++)
            {
                Console.WriteLine($"      {n,4} | {lines[n - 1]}");
            }
        }
        Console.WriteLine();
    }
}

static void List(string name)
{
    var dataset = Dataset.Load(Dataset.Find(name));
    Console.WriteLine($"{"Case",-34} {"Group",-13} {"Kind",-6} {"Labels",6} {"Lines",6}  File");
    foreach (var c in dataset.Cases)
    {
        var lines = c.ForReview().Content.TrimEnd('\n').Split('\n').Length;
        Console.WriteLine($"{c.Info.Id,-34} {c.Info.Group,-13} {c.Info.Kind,-6} {c.Labels().Count,6} {lines,6}  {c.Info.File}");
    }
    var buggy = dataset.Cases.Count(c => c.Info.Kind == "buggy");
    Console.WriteLine($"{dataset.Name}: {dataset.Cases.Count} cases ({buggy} buggy, {dataset.Cases.Count - buggy} clean), " +
        $"{dataset.Cases.Sum(c => c.Labels().Count)} labels");
}
