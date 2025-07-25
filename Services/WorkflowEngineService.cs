using Microsoft.SemanticKernel;
using AICodeReviewer.Models.Workflows;
using AICodeReviewer.Plugins;
using AICodeReviewer.Services.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AICodeReviewer.Services;

public interface IWorkflowEngineService
{
    Task<WorkflowContext> ExecuteWorkflowAsync(string workflowName, string triggerEvent, Dictionary<string, object> data);
    Task<List<WorkflowConfiguration>> GetAvailableWorkflowsAsync();
    Task<WorkflowConfiguration?> GetWorkflowConfigurationAsync(string workflowName);
}

public class WorkflowEngineService : IWorkflowEngineService
{
    private readonly Kernel _kernel;
    private readonly ILogger<WorkflowEngineService> _logger;
    private readonly IGitHubService _gitHubService;
    private readonly string _workflowsPath;

    public WorkflowEngineService(
        Kernel kernel,
        ILogger<WorkflowEngineService> logger,
        IGitHubService gitHubService,
        IConfiguration configuration,
        JiraPlugin jiraPlugin)
    {
        _kernel = kernel;
        _logger = logger;
        _gitHubService = gitHubService;
        _workflowsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", "Workflows");

        // Register the JiraPlugin with dependency injection
        _kernel.Plugins.AddFromObject(jiraPlugin, "Jira");
        _logger.LogInformation("🔌 JiraPlugin registered with dependency injection");
    }

    public async Task<WorkflowContext> ExecuteWorkflowAsync(string workflowName, string triggerEvent, Dictionary<string, object> data)
    {
        _logger.LogInformation("🚀 Starting workflow execution: {WorkflowName} triggered by {TriggerEvent}", workflowName, triggerEvent);

        var context = new WorkflowContext
        {
            TriggerEvent = triggerEvent,
            Data = data
        };

        try
        {
            var config = await GetWorkflowConfigurationAsync(workflowName);
            if (config == null)
            {
                throw new InvalidOperationException($"Workflow configuration not found: {workflowName}");
            }

            _logger.LogInformation("📋 Loaded workflow configuration: {Description}", config.Description);

            // Execute workflow steps
            foreach (var step in config.Workflow.Steps)
            {
                var stepResult = await ExecuteStepAsync(step, context, config);
                context.Results.Add(stepResult);

                _logger.LogInformation("✅ Step '{StepName}' completed with status: {Status}", step.Name, stepResult.Status);

                // Stop execution if required step failed
                if (step.Required && stepResult.Status == "Failed")
                {
                    _logger.LogError("❌ Required step failed, stopping workflow execution");
                    break;
                }
            }

            _logger.LogInformation("🏁 Workflow execution completed. Steps executed: {StepCount}", context.Results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Workflow execution failed");
            context.Results.Add(new WorkflowStepResult
            {
                StepId = "workflow-error",
                Status = "Failed",
                Error = ex.Message
            });
        }

        return context;
    }

    private async Task<WorkflowStepResult> ExecuteStepAsync(WorkflowStep step, WorkflowContext context, WorkflowConfiguration config)
    {
        var result = new WorkflowStepResult
        {
            StepId = step.Id,
            Status = "Skipped"
        };

        try
        {
            // Check dependencies
            if (step.DependsOn.Any())
            {
                var dependencyResults = context.Results.Where(r => step.DependsOn.Contains(r.StepId)).ToList();
                if (dependencyResults.Any(r => r.Status == "Failed"))
                {
                    _logger.LogWarning("⚠️ Skipping step '{StepName}' due to failed dependencies", step.Name);
                    return result;
                }
            }

            // Check conditions
            if (step.Conditions.Any() && !EvaluateConditions(step.Conditions, context))
            {
                _logger.LogInformation("⏭️ Skipping step '{StepName}' due to unmet conditions", step.Name);
                return result;
            }

            _logger.LogInformation("🔄 Executing step: {StepName} ({Plugin}.{Function})", step.Name, step.Plugin, step.Function);

            // Execute the semantic kernel function
            var functionResult = await InvokePluginFunctionAsync(step, context);

            result.Status = "Success";
            result.Result = functionResult;
            result.Metadata["executionTime"] = DateTime.UtcNow.Subtract(result.ExecutedAt).TotalMilliseconds;

            _logger.LogInformation("✅ Step '{StepName}' executed successfully", step.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Step '{StepName}' execution failed", step.Name);
            result.Status = "Failed";
            result.Error = ex.Message;
        }

        return result;
    }

    private async Task<object?> InvokePluginFunctionAsync(WorkflowStep step, WorkflowContext context)
    {
        try
        {
            _logger.LogInformation("🔍 Invoking plugin function {Plugin}.{Function}", step.Plugin, step.Function);

            // Log available plugins
            _logger.LogInformation("📦 Available plugins: {Plugins}", string.Join(", ", _kernel.Plugins.Select(p => p.Name)));

            // Build function arguments from step parameters and context data
            var arguments = new KernelArguments();

            // Add step parameters with proper type conversion
            foreach (var param in step.Parameters)
            {
                object convertedValue = param.Value;

                // Convert JsonElement to appropriate .NET types
                if (param.Value is JsonElement jsonElement)
                {
                    convertedValue = jsonElement.ValueKind switch
                    {
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.String => jsonElement.GetString() ?? "",
                        JsonValueKind.Number => jsonElement.TryGetInt32(out var intVal) ? intVal : jsonElement.GetDouble(),
                        _ => jsonElement.ToString()
                    };
                }

                arguments[param.Key] = convertedValue;
                _logger.LogInformation("  📝 Step parameter: {Key} = {Value} (Type: {Type})",
                    param.Key, convertedValue, convertedValue?.GetType().Name ?? "null");
            }

            // Add context data (override step parameters if same key exists)
            foreach (var data in context.Data)
            {
                arguments[data.Key] = data.Value;
                _logger.LogInformation("  📊 Context data: {Key} = {Value}", data.Key, data.Value);
            }

            // Add results from dependent steps
            if (step.DependsOn.Any())
            {
                foreach (var dependencyId in step.DependsOn)
                {
                    var dependentResult = context.Results.FirstOrDefault(r => r.StepId == dependencyId);
                    if (dependentResult?.Result != null)
                    {
                        // For the GitHub comment step, if it depends on code_review, pass the review result as content
                        if (step.Id == "github_comment" && dependencyId == "code_review")
                        {
                            arguments["content"] = dependentResult.Result.ToString();
                            arguments["reviewData"] = dependentResult.Result.ToString();
                            _logger.LogInformation("  🔗 Dependency result: {DependencyId} → content/reviewData = {ResultPreview}...",
                                dependencyId, dependentResult.Result.ToString()?.Substring(0, Math.Min(100, dependentResult.Result.ToString()?.Length ?? 0)));
                        }
                        // For the Jira ticket step, format the review data for better description
                        else if (step.Id == "jira_ticket" && dependencyId == "code_review")
                        {
                            var reviewData = dependentResult.Result.ToString();
                            var formattedDescription = FormatReviewDataForJira(reviewData, context.Data);
                            arguments["description"] = formattedDescription;
                            _logger.LogInformation("  🔗 Dependency result: {DependencyId} → formatted Jira description", dependencyId);
                        }
                        else
                        {
                            // Generic approach: add dependency results with prefixed keys
                            arguments[$"dependency_{dependencyId}_result"] = dependentResult.Result;
                            _logger.LogInformation("  🔗 Dependency result: dependency_{DependencyId}_result = {Result}", dependencyId, dependentResult.Result);
                        }
                        }
                    }
                }
            }

            // Check if plugin exists
            if (!_kernel.Plugins.TryGetPlugin(step.Plugin, out var plugin))
            {
                var availablePlugins = string.Join(", ", _kernel.Plugins.Select(p => p.Name));
                throw new InvalidOperationException($"Plugin '{step.Plugin}' not found. Available plugins: {availablePlugins}");
            }

            // Check if function exists
            if (!plugin.TryGetFunction(step.Function, out var function))
            {
                var availableFunctions = string.Join(", ", plugin.Select(f => f.Name));
                throw new InvalidOperationException($"Function '{step.Function}' not found in plugin '{step.Plugin}'. Available functions: {availableFunctions}");
            }

            _logger.LogInformation("✅ Found function {Plugin}.{Function}, invoking with {ArgCount} arguments",
                step.Plugin, step.Function, arguments.Count);

            // Invoke the function
            var result = await _kernel.InvokeAsync(function, arguments);

            var resultValue = result.GetValue<object>();
            _logger.LogInformation("🎯 Function result: {Result}", resultValue?.ToString() ?? "null");

            return resultValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to invoke plugin function {Plugin}.{Function}", step.Plugin, step.Function);
            throw;
        }
    }

    private bool EvaluateConditions(Dictionary<string, object> conditions, WorkflowContext context)
    {
        foreach (var condition in conditions)
        {
            if (!EvaluateCondition(condition.Key, condition.Value, context))
            {
                return false;
            }
        }
        return true;
    }

    private bool EvaluateCondition(string conditionKey, object conditionValue, WorkflowContext context)
    {
        // Simple condition evaluation - can be enhanced with a proper expression engine
        try
        {
            var conditionStr = conditionValue.ToString() ?? "";

            // For demo purposes, implement basic conditions
            switch (conditionKey.ToLower())
            {
                case "highseverityissues":
                    return EvaluateNumericCondition(conditionStr, GetHighSeverityIssueCount(context));
                case "criticalissues":
                    return EvaluateNumericCondition(conditionStr, GetCriticalIssueCount(context));
                case "complexity":
                    return EvaluateStringCondition(conditionStr, GetComplexity(context));
                case "ismainbranch":
                    return bool.Parse(conditionStr) == IsMainBranch(context);
                case "timeofday":
                    return DateTime.Now.ToString("HH:mm") == conditionStr;
                default:
                    _logger.LogWarning("Unknown condition: {ConditionKey}", conditionKey);
                    return true; // Default to true for unknown conditions
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating condition {ConditionKey}={ConditionValue}", conditionKey, conditionValue);
            return false;
        }
    }

    private bool EvaluateNumericCondition(string condition, int actualValue)
    {
        if (condition.StartsWith(">="))
            return actualValue >= int.Parse(condition[2..].Trim());
        if (condition.StartsWith("<="))
            return actualValue <= int.Parse(condition[2..].Trim());
        if (condition.StartsWith(">"))
            return actualValue > int.Parse(condition[1..].Trim());
        if (condition.StartsWith("<"))
            return actualValue < int.Parse(condition[1..].Trim());
        if (condition.StartsWith("=="))
            return actualValue == int.Parse(condition[2..].Trim());

        return actualValue == int.Parse(condition);
    }

    private bool EvaluateStringCondition(string condition, string actualValue)
    {
        var options = condition.Split('|');
        return options.Contains(actualValue, StringComparer.OrdinalIgnoreCase);
    }

    // Helper methods to extract data from context
    private int GetHighSeverityIssueCount(WorkflowContext context)
    {
        // Extract from previous step results or context data
        return 0; // Placeholder
    }

    private int GetCriticalIssueCount(WorkflowContext context)
    {
        return 0; // Placeholder
    }

    private string GetComplexity(WorkflowContext context)
    {
        return context.Data.TryGetValue("complexity", out var complexity) ?
            complexity?.ToString() ?? "Low" : "Low";
    }

    private bool IsMainBranch(WorkflowContext context)
    {
        return context.Data.TryGetValue("branch", out var branch) &&
            (branch?.ToString()?.Equals("main", StringComparison.OrdinalIgnoreCase) == true ||
             branch?.ToString()?.Equals("master", StringComparison.OrdinalIgnoreCase) == true);
    }

    public async Task<List<WorkflowConfiguration>> GetAvailableWorkflowsAsync()
    {
        var workflows = new List<WorkflowConfiguration>();

        if (!Directory.Exists(_workflowsPath))
        {
            _logger.LogWarning("Workflows directory not found: {WorkflowsPath}", _workflowsPath);
            return workflows;
        }

        var workflowFiles = Directory.GetFiles(_workflowsPath, "*.json");

        foreach (var file in workflowFiles)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var config = JsonSerializer.Deserialize<WorkflowConfiguration>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                if (config != null)
                {
                    workflows.Add(config);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load workflow configuration from {File}", file);
            }
        }

        return workflows;
    }

    private string FormatReviewDataForJira(string reviewData, Dictionary<string, object> contextData)
    {
        try
        {
            // Get PR information from context
            var owner = contextData.TryGetValue("owner", out var ownerVal) ? ownerVal.ToString() : "unknown";
            var repository = contextData.TryGetValue("repository", out var repoVal) ? repoVal.ToString() : "unknown";
            var prNumber = contextData.TryGetValue("prNumber", out var prVal) ? prVal.ToString() : "unknown";

            // Parse JSON review data
            var reviewJson = JsonDocument.Parse(reviewData);
            var root = reviewJson.RootElement;

            var summary = root.TryGetProperty("Summary", out var summaryProp) ? summaryProp.GetString() : "Review completed";
            var issues = root.TryGetProperty("Issues", out var issuesProp) ? issuesProp : default;
            var metrics = root.TryGetProperty("Metrics", out var metricsProp) ? metricsProp : default;

            var description = $@"Automated code review found issues that need attention.

*PR Information:*
• Repository: {owner}/{repository}
• Pull Request: #{prNumber}
• GitHub Link: https://github.com/{owner}/{repository}/pull/{prNumber}

*Review Summary:*
{summary}

*Review Details:*";

            // Add metrics if available
            if (metrics.ValueKind == JsonValueKind.Object)
            {
                var filesReviewed = metrics.TryGetProperty("FilesReviewed", out var filesProp) ? filesProp.GetInt32() : 0;
                var issuesFound = metrics.TryGetProperty("IssuesFound", out var issuesFoundProp) ? issuesFoundProp.GetInt32() : 0;
                var duration = metrics.TryGetProperty("Duration", out var durationProp) ? durationProp.GetDouble() : 0;

                description += $@"
• Files Reviewed: {filesReviewed}
• Issues Found: {issuesFound}
• Review Duration: {duration:F1} seconds

";
            }

            // Add issues by priority
            if (issues.ValueKind == JsonValueKind.Array && issues.GetArrayLength() > 0)
            {
                var highIssues = new List<string>();
                var mediumIssues = new List<string>();
                var lowIssues = new List<string>();

                foreach (var issue in issues.EnumerateArray())
                {
                    var severity = issue.TryGetProperty("Severity", out var severityProp) ? severityProp.GetString() : "Unknown";
                    var file = issue.TryGetProperty("File", out var fileProp) ? fileProp.GetString() : "Unknown";
                    var message = issue.TryGetProperty("Message", out var messageProp) ? messageProp.GetString() : "No description";
                    var suggestion = issue.TryGetProperty("Suggestion", out var suggestionProp) ? suggestionProp.GetString() : "";
                    var line = issue.TryGetProperty("Line", out var lineProp) && lineProp.ValueKind != JsonValueKind.Null 
                        ? lineProp.GetInt32().ToString() 
                        : "N/A";

                    var issueText = $"*File:* {file} {(line != "N/A" ? $"(Line {line})" : "")}\n*Issue:* {message}\n*Suggestion:* {suggestion}";

                    switch (severity?.ToLower())
                    {
                        case "high":
                        case "critical":
                            highIssues.Add(issueText);
                            break;
                        case "medium":
                            mediumIssues.Add(issueText);
                            break;
                        case "low":
                            lowIssues.Add(issueText);
                            break;
                    }
                }

                if (highIssues.Any())
                {
                    description += "\n*High Priority Issues:*\n" + string.Join("\n\n", highIssues.Select((issue, i) => $"{i + 1}. {issue}")) + "\n";
                }
                if (mediumIssues.Any())
                {
                    description += "\n*Medium Priority Issues:*\n" + string.Join("\n\n", mediumIssues.Select((issue, i) => $"{i + 1}. {issue}")) + "\n";
                }
                if (lowIssues.Any())
                {
                    description += "\n*Low Priority Issues:*\n" + string.Join("\n\n", lowIssues.Select((issue, i) => $"{i + 1}. {issue}")) + "\n";
                }
            }
            else
            {
                description += "\n✓ No issues found! Code looks great!";
            }

            description += "\n\n*Next Steps:*\nPlease review and address the issues listed above, prioritizing high and medium severity items.";

            return description;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to format review data for Jira");
            var owner = contextData.TryGetValue("owner", out var ownerVal) ? ownerVal.ToString() : "unknown";
            var repository = contextData.TryGetValue("repository", out var repoVal) ? repoVal.ToString() : "unknown";
            var prNumber = contextData.TryGetValue("prNumber", out var prVal) ? prVal.ToString() : "unknown";

            return $@"Automated code review completed for PR #{prNumber}.

Repository: {owner}/{repository}
GitHub Link: https://github.com/{owner}/{repository}/pull/{prNumber}

Please check the GitHub PR for detailed review results.";
        }
    }

    public async Task<WorkflowConfiguration?> GetWorkflowConfigurationAsync(string workflowName)
    {
        var workflows = await GetAvailableWorkflowsAsync();
        return workflows.FirstOrDefault(w => w.WorkflowName.Equals(workflowName, StringComparison.OrdinalIgnoreCase));
    }
}
