using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AICodeReviewer.Services.Reviewing;
using Asp.Versioning;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace AICodeReviewer.Controllers;

/// <summary>
/// The lab's results for the Lab page: the prompt scoreboard, the saved runs and one run in full.
/// The lab tool writes them to lab/results; this only reads
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[EnableCors("AllowReactApp")]
public partial class LabController(IWebHostEnvironment environment, IConfiguration configuration, PromptStore promptStore) : ControllerBase
{
    private string ResultsFolder =>
        Path.Combine(environment.ContentRootPath, configuration["Lab:ResultsPath"] ?? Path.Combine("lab", "results"));

    [GeneratedRegex(@"^[A-Za-z0-9_.-]+$")]
    private static partial Regex SafeName();

    /// <summary>
    /// The scoreboard of prompt versions for a dataset, and the active version of each language
    /// </summary>
    [HttpGet("{dataset}/scoreboard")]
    public IActionResult GetScoreboard(string dataset)
    {
        if (!SafeName().IsMatch(dataset))
        {
            return BadRequest(new { error = "Unknown dataset" });
        }
        var path = Path.Combine(ResultsFolder, $"scoreboard-{dataset}.json");
        var scoreboard = System.IO.File.Exists(path) ? JsonNode.Parse(System.IO.File.ReadAllText(path)) : null;
        // JsonObject keeps its keys as written, as the lab wrote them (snake_case, language names as they are)
        var active = new JsonObject();
        foreach (var (language, version) in promptStore.Active)
        {
            active[language] = version;
        }
        return Ok(new JsonObject { ["scoreboard"] = scoreboard, ["active"] = active });
    }

    /// <summary>
    /// The saved runs of a dataset, newest first, without their cases
    /// </summary>
    [HttpGet("{dataset}/runs")]
    public IActionResult GetRuns(string dataset)
    {
        if (!SafeName().IsMatch(dataset))
        {
            return BadRequest(new { error = "Unknown dataset" });
        }
        var folder = Path.Combine(ResultsFolder, dataset);
        if (!Directory.Exists(folder))
        {
            return Ok(Array.Empty<object>());
        }

        var runs = Directory.GetFiles(folder, "*.json")
            .OrderByDescending(f => f, StringComparer.Ordinal)
            .Select(file =>
            {
                var run = JsonNode.Parse(System.IO.File.ReadAllText(file))!.AsObject();
                run.Remove("cases");
                run["file"] = Path.GetFileName(file);
                return run;
            })
            .ToList();
        return Ok(runs);
    }

    /// <summary>
    /// One saved run with every case, finding and verdict
    /// </summary>
    [HttpGet("{dataset}/runs/{file}")]
    public IActionResult GetRun(string dataset, string file)
    {
        if (!SafeName().IsMatch(dataset) || !SafeName().IsMatch(file) || !file.EndsWith(".json", StringComparison.Ordinal))
        {
            return BadRequest(new { error = "Unknown run" });
        }
        var path = Path.Combine(ResultsFolder, dataset, file);
        return System.IO.File.Exists(path)
            ? Content(System.IO.File.ReadAllText(path), "application/json")
            : NotFound(new { error = $"No run {file} in {dataset}" });
    }
}
