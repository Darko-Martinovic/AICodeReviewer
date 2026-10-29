using System.Globalization;
using AICodeReviewer.Lab;
using AICodeReviewer.Services;
using AICodeReviewer.Services.Reviewing;
using Microsoft.Extensions.Configuration;

// dotnet run --project lab/AICodeReviewer.Lab -- list [dataset]
// dotnet run --project lab/AICodeReviewer.Lab -- labels [dataset] [case id]
// dotnet run --project lab/AICodeReviewer.Lab -- run [--dataset v1] [--model key] [--judge key] [--cases prefix]
//                                                    [--repeat n] [--max-cost dollars] [--estimate]
var command = args.FirstOrDefault() ?? "list";
switch (command)
{
    case "list":
        List(args.Length > 1 ? args[1] : "v1");
        return 0;
    case "labels":
        Labels(args.Length > 1 ? args[1] : "v1", args.Length > 2 ? args[2] : null);
        return 0;
    case "run":
        return await Run(Options.Parse(args.Skip(1).ToArray()));
    default:
        Console.Error.WriteLine($"Unknown command '{command}'. Commands: list, labels, run");
        return 1;
}

static async Task<int> Run(Options options)
{
    var datasetFolder = Dataset.Find(options.Dataset);
    var repoRoot = Path.GetFullPath(Path.Combine(datasetFolder, "..", "..", ".."));
    var configuration = new ConfigurationBuilder()
        .AddJsonFile(Path.Combine(repoRoot, "appsettings.json"), optional: true)
        .AddEnvironmentVariables()
        .Build();

    // ConfigurationService prints its progress; the lab's output is the run
    var console = Console.Out;
    Console.SetOut(TextWriter.Null);
    var configService = new ConfigurationService();
    Console.SetOut(console);

    var catalog = ModelCatalog.LoadDefault();
    var prompts = new PromptManagementService(new LanguageDetectionService(), configService);
    var clients = new ModelClientFactory(configuration, new HttpClient { Timeout = TimeSpan.FromMinutes(5) });
    var runOptions = new RunOptions(
        options.Model ?? catalog.DefaultReviewer, options.Judge ?? catalog.DefaultJudge,
        options.Repeat, options.MaxCost, options.Cases);
    var runner = new Runner(
        Dataset.Load(datasetFolder),
        new Reviewer(catalog, clients, prompts, configService, configuration),
        new Judge(catalog, clients), catalog, prompts);

    var estimate = runner.EstimateCost(runOptions);
    Console.WriteLine($"{estimate.Cases} reviews of {options.Dataset} with {runOptions.Model}, judged by {runOptions.JudgeModel}");
    Console.WriteLine($"Estimate: {estimate.InputTokens:N0} input and {estimate.OutputTokens:N0} output tokens, " +
        $"${estimate.ReviewerCost:F2} reviewing + ${estimate.JudgeCost:F2} judging = ${estimate.Total:F2} (budget ${runOptions.MaxCost:F2})");
    if (options.EstimateOnly)
    {
        return 0;
    }
    if (estimate.Total > runOptions.MaxCost)
    {
        Console.Error.WriteLine("The estimate is over the budget; raise --max-cost or narrow --cases.");
        return 2;
    }

    var result = await runner.RunAsync(runOptions, Console.WriteLine);
    var path = Runner.Save(result, Path.Combine(repoRoot, "lab", "results"));

    Console.WriteLine();
    Console.WriteLine($"{"Group",-14} {"Cases",5} {"Labels",6} {"Found",5} {"Recall",7} {"Findings",8} {"Real",5} {"False",5} {"Precision",9} {"F1",6}");
    foreach (var (name, m) in result.Groups.Append(new KeyValuePair<string, Metrics>("all", result.Summary)))
    {
        Console.WriteLine($"{name,-14} {m.Cases,5} {m.Labels,6} {m.Found,5} {m.Recall,7:P0} {m.Findings,8} {m.Real,5} {m.FalseAlarms,5} {m.Precision,9:P0} {m.F1,6:F2}");
    }
    var s = result.Summary;
    Console.WriteLine($"Strict precision {s.StrictPrecision:P0}, {s.FalseAlarmsPerCleanCase:F2} false alarms per clean file, " +
        $"{s.OutsideWindow} matches outside the line window, {s.Errors} errors");
    Console.WriteLine($"Cost ${s.ReviewerCost:F4} reviewing + ${s.JudgeCost:F4} judging = ${s.ReviewerCost + s.JudgeCost:F4} in {result.Seconds:F0} s" +
        (result.StoppedByBudget ? "; stopped early by the budget" : ""));
    Console.WriteLine($"Saved {Path.GetRelativePath(repoRoot, path)}");
    return s.Errors == 0 ? 0 : 3;
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

record Options(string Dataset, string? Model, string? Judge, string? Cases, int Repeat, decimal MaxCost, bool EstimateOnly)
{
    public static Options Parse(string[] args)
    {
        var options = new Options("v1", null, null, null, 1, 2.00m, false);
        for (var i = 0; i < args.Length; i++)
        {
            string Value() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value");
            options = args[i] switch
            {
                "--dataset" => options with { Dataset = Value() },
                "--model" => options with { Model = Value() },
                "--judge" => options with { Judge = Value() },
                "--cases" => options with { Cases = Value() },
                "--repeat" => options with { Repeat = int.Parse(Value(), CultureInfo.InvariantCulture) },
                "--max-cost" => options with { MaxCost = decimal.Parse(Value(), CultureInfo.InvariantCulture) },
                "--estimate" => options with { EstimateOnly = true },
                _ => throw new ArgumentException($"Unknown option {args[i]}")
            };
        }
        return options;
    }
}
