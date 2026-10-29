using System.Globalization;
using AICodeReviewer.Lab;
using AICodeReviewer.Services;
using AICodeReviewer.Services.Reviewing;
using Microsoft.Extensions.Configuration;

// dotnet run --project lab/AICodeReviewer.Lab -- list [dataset]
// dotnet run --project lab/AICodeReviewer.Lab -- labels [dataset] [case id]
// dotnet run --project lab/AICodeReviewer.Lab -- run [--dataset v1] [--model key] [--judge key] [--cases prefix]
//                                                    [--prompt Language=version]... [--repeat n] [--max-cost dollars] [--estimate]
// dotnet run --project lab/AICodeReviewer.Lab -- scoreboard [--dataset v1]
// dotnet run --project lab/AICodeReviewer.Lab -- rejudge [--dataset v1] [--judge key] [--max-cost dollars] [--estimate]
// dotnet run --project lab/AICodeReviewer.Lab -- prompt list
// dotnet run --project lab/AICodeReviewer.Lab -- prompt new <Language> --notes "what changes" [--from version]
// dotnet run --project lab/AICodeReviewer.Lab -- prompt decide <Language> <version> [--model key] [--judge key] [--margin 0.02]
var command = args.FirstOrDefault() ?? "list";
try
{
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
        case "scoreboard":
            Scoreboard(Options.Parse(args.Skip(1).ToArray()));
            return 0;
        case "rejudge":
            return await Rejudge(Options.Parse(args.Skip(1).ToArray()));
        case "prompt":
            return PromptCommand(args.Skip(1).ToArray());
        default:
            Console.Error.WriteLine($"Unknown command '{command}'. Commands: list, labels, run, scoreboard, rejudge, prompt");
            return 1;
    }
}
catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or DirectoryNotFoundException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static Lab Open(string datasetName, IReadOnlyDictionary<string, int>? promptOverrides = null)
{
    var datasetFolder = Dataset.Find(datasetName);
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

    // The prompts in the repository, not the build output, so new versions and decisions land in source control
    var store = PromptStore.Load(Path.Combine(repoRoot, "Configuration", "Prompts"));
    var runStore = promptOverrides is null ? store : store.WithActive(promptOverrides);
    // Prompts saved in appsettings.json would hide the versions being measured
    var prompts = new PromptManagementService(new LanguageDetectionService(), configService, runStore, useCustomPrompts: false);
    return new Lab(repoRoot, Dataset.Load(datasetFolder), configuration, configService, store, runStore, prompts);
}

static async Task<int> Run(Options options)
{
    var lab = Open(options.Dataset, options.Prompts);
    var catalog = ModelCatalog.LoadDefault();
    var clients = new ModelClientFactory(lab.Configuration, new HttpClient { Timeout = TimeSpan.FromMinutes(5) });
    var runOptions = new RunOptions(
        options.Model ?? catalog.DefaultReviewer, options.Judge ?? catalog.DefaultJudge,
        options.Repeat, options.MaxCost, options.Cases,
        Languages: options.Prompts.Count == 0 ? null : options.Prompts.Keys.ToList());
    var runner = new Runner(
        lab.Dataset,
        new Reviewer(catalog, clients, lab.Prompts, lab.ConfigService, lab.Configuration),
        new Judge(catalog, clients), catalog, lab.Prompts, lab.RunStore);

    var estimate = runner.EstimateCost(runOptions);
    var versions = string.Join(", ", options.Prompts.Select(p => $"{p.Key} v{p.Value}"));
    Console.WriteLine($"{estimate.Cases} reviews of {options.Dataset} with {runOptions.Model}, judged by {runOptions.JudgeModel}" +
        (versions.Length > 0 ? $", trying {versions}" : ""));
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
    var path = Runner.Save(result, Path.Combine(lab.RepoRoot, "lab", "results"));

    Console.WriteLine();
    PrintMetricsHeader("Group");
    foreach (var (name, m) in result.Groups.Append(new KeyValuePair<string, Metrics>("all", result.Summary)))
    {
        PrintMetrics(name, m);
    }
    var s = result.Summary;
    Console.WriteLine($"Strict precision {s.StrictPrecision:P0}, {s.FalseAlarmsPerCleanCase:F2} false alarms per clean file, " +
        $"{s.OutsideWindow} matches outside the line window, {s.Errors} errors");
    Console.WriteLine($"Cost ${s.ReviewerCost:F4} reviewing + ${s.JudgeCost:F4} judging = ${s.ReviewerCost + s.JudgeCost:F4} in {result.Seconds:F0} s" +
        (result.StoppedByBudget ? "; stopped early by the budget" : ""));
    Console.WriteLine($"Saved {Path.GetRelativePath(lab.RepoRoot, path)}");
    RefreshScoreboard(lab);
    return s.Errors == 0 ? 0 : 3;
}

static async Task<int> Rejudge(Options options)
{
    var lab = Open(options.Dataset);
    var catalog = ModelCatalog.LoadDefault();
    var clients = new ModelClientFactory(lab.Configuration, new HttpClient { Timeout = TimeSpan.FromMinutes(5) });
    var judgeModel = options.Judge ?? catalog.DefaultJudge;
    var runner = new Runner(lab.Dataset, new Reviewer(catalog, clients, lab.Prompts, lab.ConfigService, lab.Configuration),
        new Judge(catalog, clients), catalog, lab.Prompts, lab.RunStore);
    var resultsFolder = Path.Combine(lab.RepoRoot, "lab", "results");
    var runs = new PromptLab(resultsFolder, lab.Dataset, lab.Prompts).Runs();

    var estimate = runs.Sum(r => runner.EstimateRejudge(r.Result, judgeModel));
    Console.WriteLine($"Grading the reviews of {runs.Count} saved runs again with {judgeModel}: estimate ${estimate:F2} (budget ${options.MaxCost:F2})");
    if (options.EstimateOnly)
    {
        return 0;
    }
    if (estimate > options.MaxCost)
    {
        Console.Error.WriteLine("The estimate is over the budget; raise --max-cost.");
        return 2;
    }

    decimal spent = 0;
    foreach (var run in runs)
    {
        Console.WriteLine(run.File);
        var result = await runner.RejudgeAsync(run.Result, judgeModel, Console.WriteLine);
        File.WriteAllText(Path.Combine(resultsFolder, lab.Dataset.Name, run.File),
            System.Text.Json.JsonSerializer.Serialize(result, Runner.ResultJson) + "\n");
        var cost = result.Cases.Sum(c => c.JudgeCost);
        spent += cost;
        Console.WriteLine($"  recall {run.Result.Summary.Recall:P0} -> {result.Summary.Recall:P0}, " +
            $"precision {run.Result.Summary.Precision:P0} -> {result.Summary.Precision:P0}, ${cost:F4}");
    }
    Console.WriteLine($"Spent ${spent:F4} judging");
    RefreshScoreboard(lab);
    return 0;
}

static void Scoreboard(Options options)
{
    var lab = Open(options.Dataset);
    var promptLab = new PromptLab(Path.Combine(lab.RepoRoot, "lab", "results"), lab.Dataset, lab.Prompts);
    PrintMetricsHeader("Prompt", extra: $" {"Runs",4}  Reviewer / judge");
    foreach (var score in promptLab.Scoreboard())
    {
        var active = lab.Store.ActiveVersion(score.Language) == score.Version ? "*" : " ";
        PrintMetrics($"{score.Language} v{score.Version}{active}", score.Metrics,
            extra: $" {score.Runs,4}  {score.Model} / {score.JudgeModel}");
    }
    Console.WriteLine("* the active version");
    RefreshScoreboard(lab);
}

static int PromptCommand(string[] args)
{
    var sub = args.FirstOrDefault() ?? "list";
    var options = Options.Parse(args.Skip(sub == "list" ? 1 : sub == "new" ? 2 : 3).ToArray());
    var lab = Open(options.Dataset);
    switch (sub)
    {
        case "list":
            foreach (var v in lab.Store.Versions.OrderBy(v => v.Language, StringComparer.Ordinal).ThenBy(v => v.Version))
            {
                var active = lab.Store.ActiveVersion(v.Language) == v.Version ? " (active)" : "";
                var decision = v.Decision is null ? "" : $" [{v.Decision.Result} {v.Decision.Date}: {v.Decision.Reason}]";
                Console.WriteLine($"{v.Language} v{v.Version}{active}: {v.Notes}{decision}");
            }
            return 0;
        case "new":
        {
            var language = args.Length > 1 ? args[1] : throw new ArgumentException("prompt new needs a language");
            var notes = options.Notes ?? throw new ArgumentException("prompt new needs --notes");
            var version = lab.Store.NewVersion(language, notes, options.From);
            Console.WriteLine($"Created Configuration/Prompts/{language}/v{version}.md from v{options.From ?? lab.Store.ActiveVersion(language)}; " +
                $"edit it, then: run --prompt {language}={version}");
            return 0;
        }
        case "decide":
        {
            var language = args.Length > 1 ? args[1] : throw new ArgumentException("prompt decide needs a language");
            var candidate = args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : throw new ArgumentException("prompt decide needs a version");
            var catalog = ModelCatalog.LoadDefault();
            var baseline = lab.Store.ActiveVersion(language);
            var promptLab = new PromptLab(Path.Combine(lab.RepoRoot, "lab", "results"), lab.Dataset, lab.Prompts);
            var verdict = promptLab.Decide(language, candidate, baseline,
                options.Model ?? catalog.DefaultReviewer, options.Judge ?? catalog.DefaultJudge, options.Margin);

            PrintMetricsHeader("Version");
            PrintMetrics($"{language} v{baseline}", verdict.Baseline);
            PrintMetrics($"{language} v{candidate}", verdict.Candidate);
            lab.Store.Decide(language, candidate, PromptLab.ToDecision(verdict, DateOnly.FromDateTime(DateTime.UtcNow)));
            Console.WriteLine(verdict.Keep
                ? $"Kept: {language} v{candidate} is now active. {verdict.Reason}."
                : $"Rolled back: {language} stays on v{baseline}. {verdict.Reason}.");
            RefreshScoreboard(lab);
            return 0;
        }
        default:
            Console.Error.WriteLine($"Unknown prompt command '{sub}'. Commands: list, new, decide");
            return 1;
    }
}

// The app's Lab page reads lab/results/scoreboard-<dataset>.json
static void RefreshScoreboard(Lab lab) =>
    new PromptLab(Path.Combine(lab.RepoRoot, "lab", "results"), lab.Dataset, lab.Prompts).SaveScoreboard(lab.Store);

static void PrintMetricsHeader(string first, string extra = "") =>
    Console.WriteLine($"{first,-14} {"Cases",5} {"Labels",6} {"Found",5} {"Recall",7} {"Findings",8} {"Real",5} {"False",5} {"Precision",9} {"F1",6} {"FA/clean",8}{extra}");

static void PrintMetrics(string name, Metrics m, string extra = "") =>
    Console.WriteLine($"{name,-14} {m.Cases,5} {m.Labels,6} {m.Found,5} {m.Recall,7:P0} {m.Findings,8} {m.Real,5} {m.FalseAlarms,5} {m.Precision,9:P0} {m.F1,6:F2} {m.FalseAlarmsPerCleanCase,8:F2}{extra}");

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

record Lab(string RepoRoot, Dataset Dataset, IConfiguration Configuration, ConfigurationService ConfigService,
    PromptStore Store, PromptStore RunStore, PromptManagementService Prompts);

record Options(
    string Dataset, string? Model, string? Judge, string? Cases, int Repeat, decimal MaxCost, bool EstimateOnly,
    Dictionary<string, int> Prompts, string? Notes, int? From, double Margin)
{
    public static Options Parse(string[] args)
    {
        var options = new Options("v1", null, null, null, 1, 2.00m, false, new(), null, null, PromptLab.DefaultMargin);
        for (var i = 0; i < args.Length; i++)
        {
            string Value() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value");
            switch (args[i])
            {
                case "--dataset": options = options with { Dataset = Value() }; break;
                case "--model": options = options with { Model = Value() }; break;
                case "--judge": options = options with { Judge = Value() }; break;
                case "--cases": options = options with { Cases = Value() }; break;
                case "--repeat": options = options with { Repeat = int.Parse(Value(), CultureInfo.InvariantCulture) }; break;
                case "--max-cost": options = options with { MaxCost = decimal.Parse(Value(), CultureInfo.InvariantCulture) }; break;
                case "--estimate": options = options with { EstimateOnly = true }; break;
                case "--notes": options = options with { Notes = Value() }; break;
                case "--from": options = options with { From = int.Parse(Value(), CultureInfo.InvariantCulture) }; break;
                case "--margin": options = options with { Margin = double.Parse(Value(), CultureInfo.InvariantCulture) }; break;
                case "--prompt":
                    var parts = Value().Split('=', 2);
                    if (parts.Length != 2 || !int.TryParse(parts[1], out var version))
                    {
                        throw new ArgumentException("--prompt takes Language=version, for example CSharp=2");
                    }
                    options.Prompts[parts[0]] = version;
                    break;
                default:
                    throw new ArgumentException($"Unknown option {args[i]}");
            }
        }
        return options;
    }
}
