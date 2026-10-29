namespace AICodeReviewer.Models.Configuration
{
    /// <summary>
    /// Configuration for language-specific prompts
    /// </summary>
    public class LanguagePrompts
    {
        public string Default { get; set; } = string.Empty;
        public CSharpPrompt CSharp { get; set; } = new();
        public JavaPrompt Java { get; set; } = new();
        public VbNetPrompt VbNet { get; set; } = new();
        public SqlPrompt Sql { get; set; } = new();
        public JavaScriptPrompt JavaScript { get; set; } = new();
        public TypeScriptPrompt TypeScript { get; set; } = new();
        public ReactPrompt React { get; set; } = new();
    }

    /// <summary>
    /// C# specific prompt configuration
    /// </summary>
    public class CSharpPrompt
    {
        public string SystemPrompt { get; set; } = "";
        public string UserPromptTemplate { get; set; } = GetDefaultCSharpUserPromptTemplate();

        private static string GetDefaultCSharpUserPromptTemplate()
        {
            return @"Please review this C# file and provide detailed analysis:

File: {fileName}
Content Length: {contentLength} characters
{truncationNotice}

```
{fileContent}
{truncationIndicator}
```

Note: {analysisNote}";
        }
    }

    /// <summary>
    /// VB.NET specific prompt configuration
    /// </summary>
    public class VbNetPrompt
    {
        public string SystemPrompt { get; set; } = "";
        public string UserPromptTemplate { get; set; } = GetDefaultVbNetUserPromptTemplate();

        private static string GetDefaultVbNetUserPromptTemplate()
        {
            return @"Please review this VB.NET file and provide detailed analysis:

File: {fileName}
Content Length: {contentLength} characters
{truncationNotice}

```
{fileContent}
{truncationIndicator}
```

Note: {analysisNote}";
        }
    }

    /// <summary>
    /// T-SQL specific prompt configuration
    /// </summary>
    public class SqlPrompt
    {
        public string SystemPrompt { get; set; } = "";
        public string UserPromptTemplate { get; set; } = GetDefaultSqlUserPromptTemplate();

        private static string GetDefaultSqlUserPromptTemplate()
        {
            return @"Please review this T-SQL file and provide detailed analysis:

File: {fileName}
Content Length: {contentLength} characters
{truncationNotice}

```
{fileContent}
{truncationIndicator}
```

Note: {analysisNote}";
        }
    }

    /// <summary>
    /// JavaScript specific prompt configuration
    /// </summary>
    public class JavaScriptPrompt
    {
        public string SystemPrompt { get; set; } = "";
        public string UserPromptTemplate { get; set; } = GetDefaultJavaScriptUserPromptTemplate();

        private static string GetDefaultJavaScriptUserPromptTemplate()
        {
            return @"Please review this JavaScript file and provide detailed analysis:

File: {fileName}
Content Length: {contentLength} characters
{truncationNotice}

```
{fileContent}
{truncationIndicator}
```

Note: {analysisNote}";
        }
    }

    /// <summary>
    /// TypeScript specific prompt configuration
    /// </summary>
    public class TypeScriptPrompt
    {
        public string SystemPrompt { get; set; } = "";
        public string UserPromptTemplate { get; set; } = GetDefaultTypeScriptUserPromptTemplate();

        private static string GetDefaultTypeScriptUserPromptTemplate()
        {
            return @"Please review this TypeScript file and provide detailed analysis:

File: {fileName}
Content Length: {contentLength} characters
{truncationNotice}

```
{fileContent}
{truncationIndicator}
```

Note: {analysisNote}";
        }
    }

    /// <summary>
    /// React specific prompt configuration
    /// </summary>
    public class ReactPrompt
    {
        public string SystemPrompt { get; set; } = "";
        public string UserPromptTemplate { get; set; } = GetDefaultReactUserPromptTemplate();

        private static string GetDefaultReactUserPromptTemplate()
        {
            return @"Please review this React file and provide detailed analysis:

File: {fileName}
Content Length: {contentLength} characters
{truncationNotice}

```
{fileContent}
{truncationIndicator}
```

Note: {analysisNote}";
        }
    }

    /// <summary>
    /// Java specific prompt configuration
    /// </summary>
    public class JavaPrompt
    {
        public string SystemPrompt { get; set; } = "";
        public string UserPromptTemplate { get; set; } = GetDefaultJavaUserPromptTemplate();

        private static string GetDefaultJavaUserPromptTemplate()
        {
            return @"Please review this Java file and provide detailed analysis:

File: {fileName}
Content Length: {contentLength} characters
{truncationNotice}

```
{fileContent}
{truncationIndicator}
```

Note: {analysisNote}";
        }
    }

}
