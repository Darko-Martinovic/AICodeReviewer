using AICodeReviewer.Models;
using AICodeReviewer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Asp.Versioning;

namespace AICodeReviewer.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[EnableCors("AllowReactApp")]
public class TrainingController : ControllerBase
{
    private readonly ICodeValidationService _validationService;
    private readonly IAzureOpenAIService _openAIService;
    private readonly IPromptManagementService _promptManagementService;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<TrainingController> _logger;

    public TrainingController(
        ICodeValidationService validationService,
        IAzureOpenAIService openAIService,
        IPromptManagementService promptManagementService,
        IConfigurationService configurationService,
        ILogger<TrainingController> logger)
    {
        _validationService = validationService;
        _openAIService = openAIService;
        _promptManagementService = promptManagementService;
        _configurationService = configurationService;
        _logger = logger;
    }

    [HttpPost("validate-code")]
    public async Task<ActionResult<CodeValidationResult>> ValidateCode([FromBody] ValidateCodeRequest request)
    {
        try
        {
            _logger.LogInformation("Validating code for language: {Language}", request.Language);

            var result = await _validationService.ValidateAndWrapCodeAsync(request.Code, request.Language);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating code");
            return StatusCode(500, "Failed to validate code");
        }
    }

    [HttpPost("review-code")]
    public async Task<ActionResult<CodeReviewResult>> ReviewCode([FromBody] TrainingReviewRequest request)
    {
        try
        {
            _logger.LogInformation("Reviewing training code for language: {Language}", request.Language);

            // First validate and wrap the code
            var validation = await _validationService.ValidateAndWrapCodeAsync(request.Code, request.Language);

            if (!validation.IsValid && !request.ReviewAnyway)
            {
                return BadRequest(new
                {
                    message = "Code validation failed. Set 'reviewAnyway' to true to review invalid code.",
                    validation
                });
            }

            // Create a temporary filename with appropriate extension
            var tempFileName = $"Training_{request.Language}_{DateTime.UtcNow.Ticks}{GetFileExtension(request.Language)}";

            // Use AnalyzeCodeAsync which is the actual interface method
            var (issues, detailedIssues, usage) = await _openAIService.AnalyzeCodeAsync(
                tempFileName,
                validation.WrappedCode
            );

            // Convert to CodeReviewResult format
            var metrics = ReviewMetrics.StartReview("Training");
            metrics.AddUsage(usage);
            metrics.FinishReview();
            metrics.FilesReviewed = 1;
            metrics.IssuesFound = detailedIssues.Count;

            var reviewResult = new CodeReviewResult
            {
                ReviewedFiles = new List<string> { tempFileName },
                AllIssues = issues,
                DetailedIssues = detailedIssues,
                Metrics = metrics
            };

            return Ok(new
            {
                validation,
                review = reviewResult
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reviewing training code");
            return StatusCode(500, $"Failed to review code: {ex.Message}");
        }
    }

    [HttpPost("suggest-prompt-improvement")]
    public async Task<ActionResult<PromptSuggestionResult>> SuggestPromptImprovement([FromBody] PromptImprovementRequest request)
    {
        try
        {
            _logger.LogInformation("Suggesting prompt improvement for language: {Language}, feedback: {FeedbackType}",
                request.Language, request.FeedbackType);

            // Get current prompts using the correct method signature (requires filename)
            var tempFileName = $"temp{GetFileExtension(request.Language)}";
            var currentSystemPrompt = _promptManagementService.GetSystemPrompt(tempFileName);
            var currentCustomPrompt = GetCustomPrompt(request.Language);

            // Build a meta-prompt to ask AI for suggestions
            var metaPrompt = BuildMetaPrompt(request, currentSystemPrompt, currentCustomPrompt);

            // Use AnalyzeCodeAsync to get AI suggestions (it's the only available method)
            var (_, detailedIssues, _) = await _openAIService.AnalyzeCodeAsync("meta-prompt.txt", metaPrompt);

            // Extract suggestion from the AI response
            var suggestion = ExtractSuggestion(detailedIssues, request.FeedbackType);

            return Ok(new PromptSuggestionResult
            {
                Language = request.Language,
                FeedbackType = request.FeedbackType,
                SuggestedAddition = suggestion,
                CurrentCustomPrompt = currentCustomPrompt,
                Reasoning = $"Based on '{request.FeedbackType}' feedback about the code review"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating prompt improvement suggestion");
            return StatusCode(500, $"Failed to generate suggestion: {ex.Message}");
        }
    }

    [HttpPost("update-custom-prompt")]
    public async Task<ActionResult> UpdateCustomPrompt([FromBody] TrainingUpdatePromptRequest request)
    {
        try
        {
            _logger.LogInformation("Updating custom prompt for language: {Language}", request.Language);

            var currentPrompt = GetCustomPrompt(request.Language);

            // Append the new addition to the existing custom prompt
            var newPrompt = string.IsNullOrWhiteSpace(currentPrompt)
                ? request.Addition
                : $"{currentPrompt}\n\n{request.Addition}";

            // Update the appropriate language prompt in configuration
            UpdateCustomPrompt(request.Language, newPrompt);

            // Save configuration
            await _configurationService.SaveSettingsAsync();

            return Ok(new { message = "Custom prompt updated successfully", newPrompt });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating custom prompt");
            return StatusCode(500, $"Failed to update custom prompt: {ex.Message}");
        }
    }

    // Helper methods
    private string GetFileExtension(string language)
    {
        return language.ToLower() switch
        {
            "csharp" => ".cs",
            "vbnet" => ".vb",
            "java" => ".java",
            "javascript" => ".js",
            "typescript" => ".ts",
            "react" => ".tsx",
            "sql" or "t-sql" => ".sql",
            _ => ".txt"
        };
    }

    private string GetCustomPrompt(string language)
    {
        var languagePrompts = _configurationService.Settings.AzureOpenAI.LanguagePrompts;

        return language.ToLower() switch
        {
            "csharp" => languagePrompts.CSharp.SystemPrompt,
            "vbnet" => languagePrompts.VbNet.SystemPrompt,
            "java" => languagePrompts.Java.SystemPrompt,
            "javascript" => languagePrompts.JavaScript.SystemPrompt,
            "typescript" => languagePrompts.TypeScript.SystemPrompt,
            "react" => languagePrompts.React.SystemPrompt,
            "sql" or "t-sql" => languagePrompts.Sql.SystemPrompt,
            _ => _configurationService.Settings.AzureOpenAI.SystemPrompt
        };
    }

    private void UpdateCustomPrompt(string language, string newPrompt)
    {
        var languagePrompts = _configurationService.Settings.AzureOpenAI.LanguagePrompts;

        switch (language.ToLower())
        {
            case "csharp":
                languagePrompts.CSharp.SystemPrompt = newPrompt;
                break;
            case "vbnet":
                languagePrompts.VbNet.SystemPrompt = newPrompt;
                break;
            case "java":
                languagePrompts.Java.SystemPrompt = newPrompt;
                break;
            case "javascript":
                languagePrompts.JavaScript.SystemPrompt = newPrompt;
                break;
            case "typescript":
                languagePrompts.TypeScript.SystemPrompt = newPrompt;
                break;
            case "react":
                languagePrompts.React.SystemPrompt = newPrompt;
                break;
            case "sql":
            case "t-sql":
                languagePrompts.Sql.SystemPrompt = newPrompt;
                break;
        }
    }

    private string BuildMetaPrompt(PromptImprovementRequest request, string systemPrompt, string customPrompt)
    {
        var feedbackGuidance = request.FeedbackType.ToLower() switch
        {
            "too strict" => "The AI is being too strict and flagging issues that are not significant. Suggest a prompt addition that makes the AI more lenient and focused only on critical issues.",
            "too lenient" => "The AI is being too lenient and missing important issues. Suggest a prompt addition that makes the AI more thorough and catches more potential problems.",
            "too generous" => "The AI is being too generous in its assessments. Suggest a prompt addition that makes the AI more critical and rigorous in its evaluation.",
            _ => "The AI's behavior needs adjustment. Suggest a prompt addition that improves the review quality."
        };

        return $@"You are a prompt engineering expert helping to improve code review AI behavior.

Current System Prompt:
{systemPrompt}

User Feedback:
{feedbackGuidance}

Code that was reviewed:
```{request.Language}
{request.ReviewedCode}
```

AI's Review Result:
{request.ReviewSummary}

Task: Suggest a SHORT (2-3 sentences max) addition to the system prompt that will address the user's feedback. The addition should be specific and actionable. Return ONLY the suggested text to append, without any explanation or preamble.";
    }

    private string ExtractSuggestion(List<Models.DetailedIssue> detailedIssues, string feedbackType)
    {
        // If AI provided issues as suggestions, combine them into a coherent prompt addition
        if (detailedIssues.Any())
        {
            var suggestions = string.Join(" ", detailedIssues.Select(i => i.Description).Take(3));
            return suggestions.Length > 300 ? suggestions.Substring(0, 300) + "..." : suggestions;
        }

        // Fallback: Generate a basic suggestion based on feedback type
        return feedbackType.ToLower() switch
        {
            "too strict" => "Focus on critical and high-severity issues only. Avoid flagging minor style preferences or low-impact concerns.",
            "too lenient" => "Be more thorough in identifying potential issues. Look for hidden bugs, performance problems, and security vulnerabilities even in seemingly correct code.",
            "too generous" => "Apply stricter evaluation criteria. Be more critical of code quality, maintainability, and adherence to best practices.",
            _ => "Adjust review behavior based on user feedback to improve code review quality."
        };
    }
}

// Request/Response Models
public class ValidateCodeRequest
{
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
}

public class TrainingReviewRequest
{
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public bool ReviewAnyway { get; set; } = false;
}

public class PromptImprovementRequest
{
    public string Language { get; set; } = string.Empty;
    public string FeedbackType { get; set; } = string.Empty; // "too strict", "too lenient", "too generous"
    public string ReviewedCode { get; set; } = string.Empty;
    public string ReviewSummary { get; set; } = string.Empty;
}

public class PromptSuggestionResult
{
    public string Language { get; set; } = string.Empty;
    public string FeedbackType { get; set; } = string.Empty;
    public string SuggestedAddition { get; set; } = string.Empty;
    public string CurrentCustomPrompt { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
}

public class TrainingUpdatePromptRequest
{
    public string Language { get; set; } = string.Empty;
    public string Addition { get; set; } = string.Empty;
}


