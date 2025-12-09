using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Asp.Versioning;
using System.Text.Json;

namespace AICodeReviewer.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class SystemPromptsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SystemPromptsController> _logger;

    public SystemPromptsController(IConfiguration configuration, ILogger<SystemPromptsController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetAllSystemPrompts()
    {
        try
        {
            var languagePrompts = _configuration.GetSection("AzureOpenAI:LanguagePrompts");
            var result = new Dictionary<string, SystemPromptData>();

            foreach (var language in new[] { "CSharp", "Java", "VbNet", "Sql", "JavaScript", "TypeScript", "React" })
            {
                var section = languagePrompts.GetSection(language);
                var systemPrompt = section["SystemPrompt"];
                var userPromptTemplate = section["UserPromptTemplate"];

                result[language] = new SystemPromptData
                {
                    Language = language,
                    SystemPrompt = systemPrompt ?? "",
                    UserPromptTemplate = userPromptTemplate ?? "",
                    CustomAdditions = "", // User customizations will be stored separately
                    LastModified = DateTime.UtcNow // This would come from database in real implementation
                };
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system prompts");
            return StatusCode(500, new { error = "Failed to retrieve system prompts" });
        }
    }

    [HttpGet("{language}")]
    public IActionResult GetSystemPrompt(string language)
    {
        try
        {
            var section = _configuration.GetSection($"AzureOpenAI:LanguagePrompts:{language}");

            if (!section.Exists())
            {
                return NotFound(new { error = $"Language '{language}' not found" });
            }

            var result = new SystemPromptData
            {
                Language = language,
                SystemPrompt = section["SystemPrompt"] ?? "",
                UserPromptTemplate = section["UserPromptTemplate"] ?? "",
                CustomAdditions = "", // Load from user preferences/database
                LastModified = DateTime.UtcNow
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system prompt for {Language}", language);
            return StatusCode(500, new { error = $"Failed to retrieve system prompt for {language}" });
        }
    }

    [HttpPost("{language}/custom")]
    public IActionResult UpdateCustomPromptAdditions(string language, [FromBody] UpdateCustomPromptRequest request)
    {
        try
        {
            // In a real implementation, you'd store this in a database or user-specific config
            // For now, we'll simulate storing it in memory or a cache

            _logger.LogInformation("Custom prompt additions updated for {Language}: {CustomAdditions}",
                language, request.CustomAdditions);

            // Here you would:
            // 1. Validate the language exists
            // 2. Store the custom additions in database/user preferences
            // 3. Optionally combine with base prompt for immediate use

            var response = new
            {
                Language = language,
                CustomAdditions = request.CustomAdditions,
                CombinedPrompt = CombinePrompts(language, request.CustomAdditions),
                UpdatedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating custom prompt for {Language}", language);
            return StatusCode(500, new { error = $"Failed to update custom prompt for {language}" });
        }
    }

    [HttpPost("preview/{language}")]
    public IActionResult PreviewCombinedPrompt(string language, [FromBody] PreviewPromptRequest request)
    {
        try
        {
            var combinedPrompt = CombinePrompts(language, request.CustomAdditions);

            return Ok(new
            {
                Language = language,
                BasePrompt = GetBasePrompt(language),
                CustomAdditions = request.CustomAdditions,
                CombinedPrompt = combinedPrompt,
                PreviewGeneratedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing combined prompt for {Language}", language);
            return StatusCode(500, new { error = $"Failed to preview combined prompt for {language}" });
        }
    }

    [HttpGet("templates")]
    public IActionResult GetPromptTemplates()
    {
        var templates = new
        {
            Security = new[]
            {
                "Additional security focus: Check for authentication bypasses and session management issues.",
                "Pay special attention to input validation and output encoding for XSS prevention.",
                "Focus on cryptographic implementations and secure random number generation."
            },
            Performance = new[]
            {
                "Additional performance focus: Look for database query optimization opportunities.",
                "Check for memory usage patterns and potential memory leaks.",
                "Analyze async/await patterns for potential deadlocks or performance issues."
            },
            CompanyStandards = new[]
            {
                "Follow company coding standards: Use PascalCase for public members, camelCase for private fields.",
                "Ensure all public APIs have XML documentation comments.",
                "Validate that dependency injection patterns follow company guidelines."
            },
            ProjectSpecific = new[]
            {
                "Project-specific: This is a microservice, ensure proper error handling and logging.",
                "Check for proper implementation of circuit breaker patterns.",
                "Validate API versioning and backward compatibility considerations."
            }
        };

        return Ok(templates);
    }

    private string GetBasePrompt(string language)
    {
        return _configuration.GetSection($"AzureOpenAI:LanguagePrompts:{language}:SystemPrompt").Value ?? "";
    }

    private string CombinePrompts(string language, string customAdditions)
    {
        var basePrompt = GetBasePrompt(language);

        if (string.IsNullOrWhiteSpace(customAdditions))
            return basePrompt;

        return $"{basePrompt}\n\nADDITIONAL CUSTOM REQUIREMENTS:\n{customAdditions}";
    }
}

public class SystemPromptData
{
    public string Language { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPromptTemplate { get; set; } = string.Empty;
    public string CustomAdditions { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
}

public class UpdateCustomPromptRequest
{
    public string CustomAdditions { get; set; } = string.Empty;
}

public class PreviewPromptRequest
{
    public string CustomAdditions { get; set; } = string.Empty;
}
