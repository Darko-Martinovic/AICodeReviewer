using Microsoft.AspNetCore.Mvc;

namespace Documents.Api;

[ApiController]
[Route("api/documents")]
public class DocumentUploadController(IWebHostEnvironment env, ILogger<DocumentUploadController> logger) : ControllerBase
{
    private const long MaxBytes = 10 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".pdf", ".png", ".jpg"];

    [HttpPost]
    [RequestSizeLimit(MaxBytes)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0 || file.Length > MaxBytes)
        {
            return BadRequest("The file must be between 1 byte and 10 MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest($"Files of type {extension} are not accepted.");
        }

        var folder = Path.Combine(env.ContentRootPath, "uploads");
        Directory.CreateDirectory(folder);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(folder, storedName);

        await using var target = System.IO.File.Create(path);
        await file.CopyToAsync(target, ct);

        logger.LogInformation("Stored upload {StoredName} ({Length} bytes)", storedName, file.Length);
        return Ok(new { id = storedName });
    }

    [HttpGet("{id}")]
    public IActionResult Download(string id)
    {
        if (id.Contains('/') || id.Contains('\\') || id.Contains(".."))
        {
            return BadRequest();
        }

        var path = Path.Combine(env.ContentRootPath, "uploads", id);
        return System.IO.File.Exists(path) ? PhysicalFile(path, "application/octet-stream") : NotFound();
    }
}
