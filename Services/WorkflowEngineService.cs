using Microsoft.SemanticKernel;
using AICodeReviewer.Models.Workflows;
using AICodeReviewer.Plugins;
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
    private readonly string _workflowsPath;

    public WorkflowEngineService(
        Kernel kernel,
        ILogger<WorkflowEngineService> logger,
        IConfiguration configuration)
    {
        _kernel = kernel;
        _logger = logger;
        _workflowsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", "Workflows");
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
            // Build function arguments from step parameters and context data
            var arguments = new KernelArguments();
            
            // Add step parameters
            foreach (var param in step.Parameters)
            {
                arguments[param.Key] = param.Value;
            }

            // Add context data (override step parameters if same key exists)
            foreach (var data in context.Data)
            {
                arguments[data.Key] = data.Value;
            }

            // Get the plugin function
            var function = _kernel.Plugins.GetFunction(step.Plugin, step.Function);
            
            // Invoke the function
            var result = await _kernel.InvokeAsync(function, arguments);
            
            return result.GetValue<object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invoke plugin function {Plugin}.{Function}", step.Plugin, step.Function);
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

    public async Task<WorkflowConfiguration?> GetWorkflowConfigurationAsync(string workflowName)
    {
        var workflows = await GetAvailableWorkflowsAsync();
        return workflows.FirstOrDefault(w => w.WorkflowName.Equals(workflowName, StringComparison.OrdinalIgnoreCase));
    }
}
