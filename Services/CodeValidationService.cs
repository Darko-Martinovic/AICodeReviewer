using AICodeReviewer.Services.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using System.Reflection;

namespace AICodeReviewer.Services;

public class CodeValidationService : ICodeValidationService
{
    private readonly ILogger<CodeValidationService> _logger;

    public CodeValidationService(ILogger<CodeValidationService> logger)
    {
        _logger = logger;
    }

    public async Task<CodeValidationResult> ValidateAndWrapCodeAsync(string code, string language)
    {
        var result = new CodeValidationResult
        {
            OriginalCode = code,
            Language = language
        };

        try
        {
            switch (language.ToLower())
            {
                case "csharp":
                case "c#":
                    return await ValidateCSharpCodeAsync(code);

                case "vbnet":
                case "vb.net":
                    return await ValidateVBNetCodeAsync(code);

                case "java":
                    return ValidateJavaCode(code);

                case "javascript":
                case "typescript":
                    return ValidateJavaScriptCode(code, language);

                case "react":
                    return ValidateReactCode(code);

                case "tsql":
                case "sql":
                    return ValidateSqlCode(code);

                default:
                    // For unknown languages, just return as-is
                    result.IsValid = true;
                    result.WrappedCode = code;
                    result.WasWrapped = false;
                    return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating code for language {Language}", language);
            result.IsValid = false;
            result.Errors.Add($"Validation error: {ex.Message}");
            result.WrappedCode = code;
            return result;
        }
    }

    private async Task<CodeValidationResult> ValidateCSharpCodeAsync(string code)
    {
        var result = new CodeValidationResult
        {
            OriginalCode = code,
            Language = "CSharp"
        };

        // Check if code needs wrapping
        var needsWrapping = !code.Contains("class ") && !code.Contains("namespace ");

        string codeToValidate = code;
        if (needsWrapping)
        {
            codeToValidate = WrapCSharpCode(code);
            result.WasWrapped = true;
        }

        result.WrappedCode = codeToValidate;

        // Use Roslyn to validate syntax with comprehensive assembly references
        var syntaxTree = CSharpSyntaxTree.ParseText(codeToValidate);

        // Add references to common assemblies
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),                    // System.Private.CoreLib
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),                   // System.Console
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),                // System.Linq
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location), // System.Collections
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),            // System.Runtime
        };

        var compilation = CSharpCompilation.Create("TempAssembly")
            .AddReferences(references)
            .AddSyntaxTrees(syntaxTree);

        var diagnostics = compilation.GetDiagnostics();
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        if (errors.Any())
        {
            result.IsValid = false;
            result.Errors = errors.Select(e => $"Line {e.Location.GetLineSpan().StartLinePosition.Line + 1}: {e.GetMessage()}").ToList();
        }
        else
        {
            result.IsValid = true;
        }

        return await Task.FromResult(result);
    }

    private string WrapCSharpCode(string code)
    {
        var hasUsings = code.Contains("using ");
        var usings = hasUsings ? "" : "using System;\nusing System.Collections.Generic;\nusing System.Linq;\n\n";

        return $@"{usings}public class TestClass
{{
    public static void Main()
    {{
        {IndentCode(code, 8)}
    }}
}}";
    }

    private async Task<CodeValidationResult> ValidateVBNetCodeAsync(string code)
    {
        var result = new CodeValidationResult
        {
            OriginalCode = code,
            Language = "VB.NET"
        };

        var needsWrapping = !code.Contains("Module ") && !code.Contains("Class ");

        string codeToValidate = code;
        if (needsWrapping)
        {
            codeToValidate = WrapVBNetCode(code);
            result.WasWrapped = true;
        }

        result.WrappedCode = codeToValidate;

        // Use Roslyn to validate VB.NET syntax with comprehensive assembly references
        var syntaxTree = VisualBasicSyntaxTree.ParseText(codeToValidate);

        // Add references to common assemblies
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),                    // System.Private.CoreLib
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),                   // System.Console
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),                // System.Linq
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location), // System.Collections
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),            // System.Runtime
        };

        var compilation = VisualBasicCompilation.Create("TempAssembly")
            .AddReferences(references)
            .AddSyntaxTrees(syntaxTree);

        var diagnostics = compilation.GetDiagnostics();
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        if (errors.Any())
        {
            result.IsValid = false;
            result.Errors = errors.Select(e => $"Line {e.Location.GetLineSpan().StartLinePosition.Line + 1}: {e.GetMessage()}").ToList();
        }
        else
        {
            result.IsValid = true;
        }

        return await Task.FromResult(result);
    }

    private string WrapVBNetCode(string code)
    {
        var hasImports = code.Contains("Imports ");
        var imports = hasImports ? "" : "Imports System\nImports System.Collections.Generic\nImports System.Linq\n\n";

        return $@"{imports}Module TestModule
    Sub Main()
        {IndentCode(code, 8)}
    End Sub
End Module";
    }

    private CodeValidationResult ValidateJavaCode(string code)
    {
        var result = new CodeValidationResult
        {
            OriginalCode = code,
            Language = "Java"
        };

        var needsWrapping = !code.Contains("class ") && !code.Contains("public class ") && !code.Contains("interface ");

        if (needsWrapping)
        {
            result.WrappedCode = WrapJavaCode(code);
            result.WasWrapped = true;
        }
        else
        {
            result.WrappedCode = code;
            result.WasWrapped = false;
        }

        // Basic syntax check - ensure braces are balanced
        var openBraces = code.Count(c => c == '{');
        var closeBraces = code.Count(c => c == '}');
        var openParens = code.Count(c => c == '(');
        var closeParens = code.Count(c => c == ')');

        if (openBraces != closeBraces)
        {
            result.IsValid = false;
            result.Errors.Add($"Unbalanced braces: {openBraces} opening, {closeBraces} closing");
        }
        else if (openParens != closeParens)
        {
            result.IsValid = false;
            result.Errors.Add($"Unbalanced parentheses: {openParens} opening, {closeParens} closing");
        }
        else
        {
            result.IsValid = true;
        }

        return result;
    }

    private string WrapJavaCode(string code)
    {
        var hasImport = code.Contains("import ");
        var imports = hasImport ? "" : "import java.util.*;\nimport java.util.stream.*;\n\n";

        return $@"{imports}public class TestClass {{
    public static void main(String[] args) {{
        {IndentCode(code, 8)}
    }}
}}";
    }

    private CodeValidationResult ValidateJavaScriptCode(string code, string language)
    {
        var result = new CodeValidationResult
        {
            OriginalCode = code,
            Language = language
        };

        var needsWrapping = !code.Contains("function ") && !code.Contains("const ") && !code.Contains("let ") && !code.Contains("var ");

        if (needsWrapping)
        {
            result.WrappedCode = WrapJavaScriptCode(code);
            result.WasWrapped = true;
        }
        else
        {
            result.WrappedCode = code;
            result.WasWrapped = false;
        }

        // Basic syntax check - ensure braces are balanced
        var openBraces = code.Count(c => c == '{');
        var closeBraces = code.Count(c => c == '}');
        var openParens = code.Count(c => c == '(');
        var closeParens = code.Count(c => c == ')');

        if (openBraces != closeBraces)
        {
            result.IsValid = false;
            result.Errors.Add($"Unbalanced braces: {openBraces} opening, {closeBraces} closing");
        }
        else if (openParens != closeParens)
        {
            result.IsValid = false;
            result.Errors.Add($"Unbalanced parentheses: {openParens} opening, {closeParens} closing");
        }
        else
        {
            result.IsValid = true;
        }

        return result;
    }

    private string WrapJavaScriptCode(string code)
    {
        return $@"function testFunction() {{
    {IndentCode(code, 4)}
}}

testFunction();";
    }

    private CodeValidationResult ValidateReactCode(string code)
    {
        var result = new CodeValidationResult
        {
            OriginalCode = code,
            Language = "React"
        };

        var needsWrapping = !code.Contains("export ") && !code.Contains("function ") && !code.Contains("const ");

        if (needsWrapping)
        {
            result.WrappedCode = WrapReactCode(code);
            result.WasWrapped = true;
        }
        else
        {
            result.WrappedCode = code;
            result.WasWrapped = false;
        }

        // Basic React syntax checks
        var hasJSX = code.Contains("<") && code.Contains(">");
        var openTags = System.Text.RegularExpressions.Regex.Matches(code, @"<(\w+)").Count;
        var closeTags = System.Text.RegularExpressions.Regex.Matches(code, @"</(\w+)>").Count;
        var selfClosingTags = System.Text.RegularExpressions.Regex.Matches(code, @"/>").Count;

        if (hasJSX && (openTags != closeTags + selfClosingTags))
        {
            result.IsValid = false;
            result.Errors.Add("Potentially unbalanced JSX tags");
        }
        else
        {
            result.IsValid = true;
        }

        return result;
    }

    private string WrapReactCode(string code)
    {
        return $@"import React from 'react';

export default function TestComponent() {{
    return (
        {IndentCode(code, 8)}
    );
}}";
    }

    private CodeValidationResult ValidateSqlCode(string code)
    {
        var result = new CodeValidationResult
        {
            OriginalCode = code,
            Language = "TSQL",
            WrappedCode = code,
            WasWrapped = false
        };

        // Basic SQL validation
        var lowerCode = code.ToLower();
        var hasSqlKeywords = lowerCode.Contains("select ") ||
                           lowerCode.Contains("insert ") ||
                           lowerCode.Contains("update ") ||
                           lowerCode.Contains("delete ") ||
                           lowerCode.Contains("create ") ||
                           lowerCode.Contains("alter ");

        if (!hasSqlKeywords)
        {
            result.IsValid = false;
            result.Errors.Add("No SQL keywords detected. Expected SELECT, INSERT, UPDATE, DELETE, CREATE, or ALTER statements.");
        }
        else
        {
            result.IsValid = true;
        }

        return result;
    }

    private string IndentCode(string code, int spaces)
    {
        var indent = new string(' ', spaces);
        var lines = code.Split('\n');
        return string.Join("\n" + indent, lines);
    }
}
