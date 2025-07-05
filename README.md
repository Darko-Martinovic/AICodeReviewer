# AI Code Reviewer

A .NET console application that performs AI-powered code reviews on GitHub repositories using Azure OpenAI with enterprise-grade dependency injection and configuration management.

## 🏗️ Architecture Overview

This application follows Clean Architecture principles with **Microsoft.Extensions.DependencyInjection** for proper service lifecycle management:

```
AICodeReviewer/
├── Models/                    # Data models and DTOs
│   ├── Configuration/         # Configuration models
│   │   └── AppSettings.cs     # Structured configuration settings
│   ├── ChatMessage.cs         # Azure OpenAI API message model
│   ├── ChatRequest.cs         # Azure OpenAI API request model
│   ├── ChatChoice.cs          # Azure OpenAI API choice model
│   ├── ChatResponse.cs        # Azure OpenAI API response model
│   ├── Usage.cs               # Token usage information
│   └── CodeReviewResult.cs    # Code review result with detailed issues
├── Services/                  # Business logic and external integrations
│   ├── Interfaces/            # Service contracts
│   │   ├── IAzureOpenAIService.cs    # AI service interface
│   │   ├── IGitHubService.cs         # GitHub service interface
│   │   ├── ICodeReviewService.cs     # Code review interface
│   │   ├── INotificationService.cs   # Notification interface
│   │   ├── IJiraService.cs           # Jira service interface
│   │   └── IConfigurationService.cs  # Configuration interface
│   ├── AzureOpenAIService.cs         # Azure OpenAI API interactions
│   ├── GitHubService.cs              # GitHub API interactions
│   ├── CodeReviewService.cs          # Core code review logic
│   ├── NotificationService.cs        # Teams notification logic
│   ├── JiraService.cs               # Jira integration for ticket updates
│   └── ConfigurationService.cs      # Structured configuration management
├── Application/               # Application orchestration layer
│   └── CodeReviewApplication.cs     # Main application workflows
├── Utils/                     # Utility classes and helpers
│   └── FileUtils.cs                 # File-related utility methods
├── Program.cs                 # Entry point with DI container setup
├── appsettings.json          # Structured configuration file
└── .env                      # Environment-specific secrets (gitignored)
```

## ⚡ Key Technical Features

### 🏭 **Enterprise-Grade Dependency Injection**
- ✅ **Microsoft.Extensions.DependencyInjection** implementation
- ✅ Interface-based service contracts for all components
- ✅ Proper service lifetime management (Singleton for console app)
- ✅ Automatic resource disposal (`IDisposable` services)
- ✅ Clean separation of service registration and application logic

### 🎛️ **Advanced Configuration Management**
- ✅ **Hierarchical configuration**: `appsettings.json` → Environment Variables → Defaults
- ✅ **Structured settings** with strongly-typed configuration models
- ✅ **Runtime configuration summary** displayed at startup
- ✅ **Hot-swappable AI parameters** (temperature, tokens, prompts)
- ✅ **Externally configurable** without code changes

### 🔍 **Detailed AI Code Analysis**
- ✅ **Structured issue reporting** with severity, category, recommendations
- ✅ **Line-by-line feedback** with specific file locations
- ✅ **Actionable recommendations** with code examples
- ✅ **Configurable AI behavior** (temperature, max tokens, system prompts)
- ✅ **Support for 12+ programming languages**

## Security and Configuration

### 🔒 **Environment Variables Setup**

This application requires several environment variables to function properly. The security model works as follows:

1. **For Development**: Keep your real API keys in `.env` (protected by `.gitignore`)
2. **For New Contributors**: Copy `.env.example` to `.env` and add their own keys
3. **For Public Repository**: Only `.env.example` is committed (contains safe template values)

#### Setup Steps:

1. **Copy the template file:**

   ```bash
   cp .env.example .env
   ```

2. **Edit `.env` with your actual values:**
   - Replace all placeholder values with your real API keys and configuration
   - The `.env` file is already included in `.gitignore` to prevent accidental commits

### 🛡️ **Security Model**

- ✅ **Your `.env`**: Contains real keys, protected by `.gitignore`, never committed
- ✅ **`.env.example`**: Contains templates, safe to commit, helps new contributors
- ✅ **Source Code**: No hardcoded secrets, only environment variable references
- ✅ **Git Protection**: `.env` excluded from version control since project inception

### 📋 **Key Points**

- **Keep working**: Your `.env` with real keys stays local and functional
- **Stay secure**: `.gitignore` prevents any accidental commits of sensitive data
- **Help others**: `.env.example` provides a clear template for new contributors

## 📋 Features

### 🔍 **Code Analysis & Review**
- ✅ **AI-Powered Analysis**: Leverage Azure OpenAI for intelligent, context-aware code review
- ✅ **Multi-Language Support**: Analyze C#, JavaScript, TypeScript, Python, Java, and more
- ✅ **Detailed Issue Reporting**: Structured feedback with severity, category, description, and recommendations
- ✅ **Line-Level Precision**: Specific file locations and line numbers for each issue
- ✅ **Performance Metrics**: Real-time timing and progress indicators

### 🔗 **GitHub Integration**
- ✅ **Repository Analysis**: Connect to GitHub repositories and analyze commits/PRs
- ✅ **Commit Review**: Analyze latest commits with file change detection
- ✅ **Pull Request Review**: Comprehensive PR analysis with automatic comment posting
- ✅ **File Content Retrieval**: Direct integration with GitHub API for source code access

### 🎫 **Jira Integration**
- ✅ **Auto-Detection**: Extract Jira ticket keys from PR titles (e.g., `OPS-123`, `PROJ-456`)
- ✅ **Rich Comments**: Post detailed, visually formatted comments using Atlassian Document Format (ADF)
- ✅ **Ticket Updates**: Real-time updates with review results and issue summaries
- ✅ **Label Management**: Automatic label assignment based on review severity
- ✅ **Smart Fallback**: Graceful degradation when API is unavailable

### 📢 **Teams Notifications**
- ✅ **Formatted Notifications**: Rich, interactive Teams-style message simulation
- ✅ **Severity-Based Reactions**: Context-aware team member responses
- ✅ **Delivery Tracking**: Simulated webhook delivery and read confirmations
- ✅ **Configurable Behavior**: Customizable team member names and response patterns

### ⚙️ **Configuration Management**
- ✅ **Hierarchical Config**: JSON file → Environment variables → Defaults
- ✅ **Hot-Swappable**: Change AI parameters without code modification
- ✅ **Secure Storage**: Environment variables for sensitive data
- ✅ **Development-Friendly**: `.env` file support with `.env.example` template

## 🚀 Setup & Installation

### 1. **Prerequisites**
- .NET 9.0 or later
- GitHub Personal Access Token
- Azure OpenAI service endpoint and API key
- (Optional) Jira API token for ticket integration

### 2. **Clone and Install Dependencies**

```bash
git clone <repository-url>
cd AICodeReviewer
dotnet restore
```

**Included NuGet Packages:**
- `Microsoft.Extensions.DependencyInjection` (v9.0.0) - Enterprise DI container
- `Microsoft.Extensions.Hosting` (v9.0.0) - Service lifetime management
- `Octokit` (v14.0.0) - GitHub API integration
- `DotNetEnv` (v3.1.1) - Environment variable loading

### 3. **Environment Configuration**

**For new contributors, copy the example template:**

```bash
cp .env.example .env
```

**Then edit `.env` with your actual values:**

```env
# Required - Azure OpenAI Configuration
AOAI_ENDPOINT=https://your-azure-openai-endpoint.openai.azure.com
AOAI_APIKEY=your_azure_openai_api_key
CHATCOMPLETION_DEPLOYMENTNAME=your_chat_completion_deployment_name

# Required - GitHub Configuration
GITHUB_TOKEN=your_github_personal_access_token
GITHUB_REPO_OWNER=repository_owner
GITHUB_REPO_NAME=repository_name

# Optional - Jira Integration
JIRA_BASE_URL=https://your-company.atlassian.net
JIRA_USER_EMAIL=your.email@company.com
JIRA_API_TOKEN=your_jira_api_token
JIRA_PROJECT_KEY=YOUR_PROJECT_KEY

# Optional - AI Configuration (can override appsettings.json)
AI_TEMPERATURE=0.3
AI_MAX_TOKENS=2500
AI_CONTENT_LIMIT=15000
AI_SYSTEM_PROMPT="Your custom system prompt for AI reviews"

# Optional - Teams Configuration
TEAMS_WEBHOOK_URL=your_teams_webhook_url
```

### 4. **Configuration File (Optional)**

Customize `appsettings.json` for structured configuration:

```json
{
  "AzureOpenAI": {
    "ApiVersion": "2024-02-01",
    "Temperature": 0.3,
    "MaxTokens": 2500,
    "ContentLimit": 15000,
    "SystemPrompt": "Custom AI review prompt..."
  },
  "Teams": {
    "TeamMembers": ["Alice Johnson", "Bob Chen", "Carol Smith"],
    "SimulationDelays": {
      "WebhookPreparation": 800,
      "MessageSending": 1200
    }
  },
  "CodeReview": {
    "MaxFilesToReview": 3,
    "SupportedExtensions": [".cs", ".js", ".ts", ".py", ".java"]
  }
}
```

### 5. **Build and Run**

```bash
dotnet build
dotnet run
```

## 📊 Sample Output

### **Latest Commit Review**
```
🔍 Fetching latest commit...
🏠 Repository: YourOrg/YourRepo
🌿 Branch: main
📝 Latest commit: a1b2c3d4 - Fix authentication logic
👤 Author: John Developer
📅 Date: 2025-07-05 15:30

📁 Files changed: 3
  - modified: src/auth/AuthService.cs (+15/-8)
  - modified: src/models/User.cs (+3/-1)
  - added: tests/AuthServiceTests.cs (+45/-0)

🤖 Starting AI Code Review...
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  📄 [1/3] Analyzing src/auth/AuthService.cs...
    🔄 Retrieving file content... ✅ (2,341 characters)
    🤖 Sending to AI for analysis... (1,847ms) ✅ Complete
    🔍 Found 2 issue(s):
      • [High] Hardcoded API key in authentication method
      • [Medium] Missing ConfigureAwait(false) on async calls
  📄 [2/3] Analyzing src/models/User.cs...
    🔄 Retrieving file content... ✅ (567 characters)
    🤖 Sending to AI for analysis... (634ms) ✅ Complete
    ✅ No issues found
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📊 Review Summary:
   Files reviewed: 2
   Total issues: 2
   Severity: Medium
```

### **Detailed Issue Example**
```
CATEGORY: Security
SEVERITY: Critical
TITLE: Hardcoded API key exposed in source code
DESCRIPTION: The API key is hardcoded as a string literal on line 42, making it visible to anyone with source code access and potentially exposing it in version control history.
RECOMMENDATION: Move the API key to environment variables or secure configuration. Use Environment.GetEnvironmentVariable("API_KEY") or inject via IConfiguration.
LINE: 42
```

### **Teams Notification Simulation**
```
💬 Microsoft Teams - Code Review Channel
════════════════════════════════════════════════════════════════════════
🤖 **AI Code Reviewer** _(Bot)_ • Jul 05, 2025 3:30 PM

### 🔍 Code Review Complete: a1b2c3d4
**👤 Author:** John Developer
**📁 Files Reviewed:** 2 code files
**🔍 Issues Found:** 2 (Medium severity)
**📊 Review Status:** ⚠️ Medium severity

**📋 Key Issues Identified:**
• [Critical] Hardcoded API key exposed in source code
• [Medium] Missing ConfigureAwait(false) on async calls

**🎯 Recommended Actions:**
⚠️ **Review recommended** - Security issues need attention

💬 [View Full Report] 🔗 [Open PR] 📋 [View in Jira]
════════════════════════════════════════════════════════════════════════
✅ Message delivered to Teams channel
👥 Team reactions: 😬 ⚠️ 🔧 👀
💬 Alice Johnson: @John Developer Please address the API key issue ASAP 🚨
💬 Bob Chen: Good catch on the security issue! 👍
```

## Jira Integration

The application can automatically detect and update Jira tickets mentioned in pull request titles.

### Features

- **Auto-detection**: Extracts Jira ticket keys (e.g., `OPS-123`, `PROJ-456`) from PR titles
- **Real API Integration**: Posts actual comments to Jira tickets when credentials are configured
- **Review Summary**: Updates tickets with code review results and issue counts
- **Severity Assessment**: Categorizes issues as Clean, Low, Medium, or High severity
- **Graceful Fallback**: Shows simulated updates when Jira API is not available or fails
- **Configurable**: Works with or without Jira API credentials

### Supported Patterns

The following ticket patterns are automatically detected in PR titles:

- `OPS-123` - Standard project keys (2-10 letters, dash, numbers)
- `PROJECT-456` - Longer project names
- `BUG-789` - Any valid Jira ticket format

### Example Output

```
🎫 Detected Jira tickets: OPS-123, PROJ-456
🎫 Jira Ticket Update:
──────────────────────────────────────────────────────────────
📋 Updating ticket: OPS-123
   ✅ Comment successfully posted to Jira ticket OPS-123
   📝 Comment: "AI Code Review completed for PR #42"

💬 GitHub PR Comment:
──────────────────────────────────────────────────────────────
   ✅ Comment successfully posted to PR #42
   📝 Review summary posted with detailed findings
```

_Note: If Jira/GitHub APIs are not configured or fail, the application shows simulated updates instead._

## Integration Status

### ✅ **Fully Implemented**

- **Jira API Integration**: Real ticket updates with comment posting
- **GitHub API Integration**: Real PR comment posting and repository analysis
- **Azure OpenAI Integration**: Real AI-powered code analysis

### 🔄 **Simulation with Real Fallback**

- **Jira Updates**: Attempts real API calls, falls back to simulation if credentials missing or API fails
- **GitHub Comments**: Attempts real API calls, falls back to simulation if credentials missing or API fails

### 🎭 **Simulation Only**

- **Teams Notifications**: Console output simulation (webhook integration planned)

## 🏗️ Architecture Benefits

### 🎯 **Enterprise-Grade Dependency Injection**

**Before (Manual DI):**
```csharp
// Tightly coupled, hard to test
var httpClient = new HttpClient();
var aiService = new AzureOpenAIService(httpClient, endpoint, apiKey, deployment, config);
var githubService = new GitHubService(token, owner, repo);
var app = new CodeReviewApplication(githubService, aiService, /* ... */);
```

**After (Microsoft.Extensions.DI):**
```csharp
// Loosely coupled, easily testable
services.AddSingleton<HttpClient>();
services.AddSingleton<IAzureOpenAIService, AzureOpenAIService>();
services.AddSingleton<IGitHubService, GitHubService>();
services.AddSingleton<CodeReviewApplication>();

// Automatic resolution and disposal
var app = serviceProvider.GetRequiredService<CodeReviewApplication>();
```

### 🔧 **Benefits Achieved**

| Aspect | Before | After |
|--------|--------|-------|
| **Dependency Management** | Manual instantiation | DI container with interfaces |
| **Testability** | Hard to mock dependencies | Easy to inject mocks via interfaces |
| **Code Organization** | Tightly coupled services | Loosely coupled with clear contracts |
| **Resource Management** | Manual disposal | Automatic disposal via DI container |
| **Adding New Services** | Modify Program.cs | Register in ConfigureServices() |
| **Maintenance** | Unclear dependency graph | Explicit, documented dependencies |

### 🧪 **Enhanced Testability**

```csharp
// Easy unit testing with mocked dependencies
[Test]
public async Task ReviewLatestCommit_WithIssues_ReturnsDetailedResults()
{
    // Arrange
    var mockAI = new Mock<IAzureOpenAIService>();
    var mockGitHub = new Mock<IGitHubService>();
    var reviewService = new CodeReviewService(mockAI.Object, mockGitHub.Object, config);
    
    // Act & Assert
    var result = await reviewService.ReviewCommitAsync(files);
    Assert.That(result.IssueCount, Is.GreaterThan(0));
}
```

### 📈 **Scalability & Maintainability**

- ✅ **Single Responsibility**: Each service has one clear purpose
- ✅ **Open/Closed Principle**: Easy to extend without modifying existing code
- ✅ **Interface Segregation**: Small, focused interfaces for each service
- ✅ **Dependency Inversion**: Depend on abstractions, not concretions
- ✅ **Clean Architecture**: Clear separation between layers

## Code Quality Features

### 🛡️ **Error Handling**

- Comprehensive try-catch blocks with meaningful error messages
- Graceful degradation when services are unavailable (API fallback to simulation)
- User-friendly error reporting
- Robust handling of network failures and API rate limits

### 📊 **Progress Tracking**

- Real-time progress indicators during file analysis
- Timing information for AI analysis operations
- Visual separators and status indicators

### 🎨 **User Experience**

- Intuitive menu system
- Rich console output with emojis and formatting
- Clear feedback on all operations

## Supported File Types

- C# (.cs)
- VB.NET (.vb)
- JavaScript (.js)
- TypeScript (.ts)
- Python (.py)
- Java (.java)
- C/C++ (.cpp, .c, .h)
- PHP (.php)
- Ruby (.rb)
- Kotlin (.kt)
- Swift (.swift)
- SQL (.sql)

## Future Enhancements

- [ ] Add actual Teams webhook integration
- [x] ~~Implement real Jira API calls~~ ✅ **Completed**
- [x] ~~Add PR comment posting~~ ✅ **Completed**
- [ ] Support for more file types
- [ ] Configuration file support
- [ ] Logging framework integration
- [ ] Unit tests coverage
- [ ] Docker containerization
- [ ] Webhook endpoints for CI/CD integration
- [ ] Custom review rules and templates

## Contributing

1. Follow the established architecture patterns
2. Add XML documentation to public methods
3. Include error handling in new services
4. Update this README when adding new features
