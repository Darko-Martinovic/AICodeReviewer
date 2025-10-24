namespace AICodeReviewer.Services.Interfaces;

public interface ICodeValidationService
{
    Task<CodeValidationResult> ValidateAndWrapCodeAsync(string code, string language);
}

public class CodeValidationResult
{
    public bool IsValid { get; set; }
    public string WrappedCode { get; set; } = string.Empty;
    public string OriginalCode { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public bool WasWrapped { get; set; }
    public string Language { get; set; } = string.Empty;
}
