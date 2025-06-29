# AI Code Reviewer

A .NET console application that performs AI-powered code reviews on GitHub repositories using Azure OpenAI.

## Architecture Overview

This application follows Clean Architecture principles with separation of concerns:

```
AICodeReviewer/
├── Models/              # Data models and DTOs
│   ├── ChatMessage.cs      # Azure OpenAI API message model
│   ├── ChatRequest.cs      # Azure OpenAI API request model
│   ├── ChatChoice.cs       # Azure OpenAI API choice model
│   ├── ChatResponse.cs     # Azure OpenAI API response model
│   ├── Usage.cs           # Token usage information
│   └── CodeReviewResult.cs # Code review result model
├── Services/            # Business logic and external integrations
│   ├── AzureOpenAIService.cs    # Azure OpenAI API interactions
│   ├── GitHubService.cs         # GitHub API interactions
│   ├── CodeReviewService.cs     # Core code review logic
│   ├── NotificationService.cs   # Teams notification logic
│   └── JiraService.cs          # Jira integration for ticket updates
├── Application/         # Application orchestration layer
│   └── CodeReviewApplication.cs # Main application workflows
├── Utils/              # Utility classes and helpers
│   └── FileUtils.cs          # File-related utility methods
└── Program.cs          # Entry point and dependency injection
```

## Security and Configuration

### 🔒 **Environment Variables Setup**

This application requires several environment variables to function properly. **NEVER commit real API keys to version control.**

1. **Copy the template file:**
   ```bash
   cp .env.example .env
   ```

2. **Edit `.env` with your actual values:**
   - Replace all placeholder values with your real API keys and configuration
   - The `.env` file is already included in `.gitignore` to prevent accidental commits

### 🛡️ **Security Best Practices**

- ✅ **Environment Variables**: All sensitive data is accessed via environment variables
- ✅ **Git Protection**: `.env` file is excluded from version control
- ✅ **No Hardcoded Secrets**: Source code contains no embedded API keys
- ✅ **Graceful Degradation**: Application works with missing optional credentials

### ⚠️ **Before Contributing**

- Never commit real API keys or personal information
- Use the `.env.example` template for documentation
- Test with placeholder values when possible
- Review commits for sensitive data before pushing

## Features

- ✅ **GitHub Integration**: Connect to GitHub repositories and analyze commits/PRs
- ✅ **AI Code Review**: Leverage Azure OpenAI for intelligent code analysis
- ✅ **Jira Integration**: Auto-detect and update Jira tickets based on PR titles (real API + simulation fallback)
- ✅ **PR Comment Posting**: Automatically post code review results as GitHub PR comments
- ✅ **Visual Progress**: Real-time progress indicators during review process
- ✅ **Teams Notifications**: Formatted notification output (simulated)
- ✅ **Multiple File Support**: Analyze multiple code files in a single review
- ✅ **File Type Detection**: Supports various programming languages

## Setup

1. **Environment Variables**
   
   **First, copy the example template:**
   ```bash
   cp .env.example .env
   ```
   
   **Then edit `.env` with your actual values:**

   ```env
   # Required
   GITHUB_TOKEN=your_github_personal_access_token
   GITHUB_REPO_OWNER=repository_owner
   GITHUB_REPO_NAME=repository_name
   AOAI_ENDPOINT=https://your-azure-openai-endpoint.openai.azure.com
   AOAI_APIKEY=your_azure_openai_api_key
   CHATCOMPLETION_DEPLOYMENTNAME=your_chat_completion_deployment_name

   # Optional - Jira Integration
   JIRA_BASE_URL=https://your-company.atlassian.net
   JIRA_USER_EMAIL=your.email@company.com
   JIRA_API_TOKEN=your_jira_api_token
   ```

2. **Dependencies**

   ```bash
   dotnet add package Octokit
   dotnet add package DotNetEnv
   ```

3. **Build and Run**
   ```bash
   dotnet build
   dotnet run
   ```

## Usage

The application provides an interactive menu with the following options:

1. **Review latest commit** - Analyzes the most recent commit on the main branch
2. **Review Pull Request** - Analyzes files in an open pull request
3. **List recent commits** - Shows the 5 most recent commits
4. **List open Pull Requests** - Shows all open pull requests

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

## Architecture Benefits

### 🎯 **Separation of Concerns**

- **Models**: Pure data structures with no business logic
- **Services**: Focused on specific external integrations or business operations
- **Application**: Orchestrates workflows using services
- **Utils**: Shared utility functions

### 🔧 **Maintainability**

- **Single Responsibility**: Each class has one clear purpose
- **Dependency Injection**: Services are injected, making testing easier
- **Modular Design**: Easy to modify or extend individual components

### 🧪 **Testability**

- **Service Isolation**: Each service can be unit tested independently
- **Interface-ready**: Easy to add interfaces for mocking
- **Clear Dependencies**: Dependencies are explicit and injected

### 📈 **Scalability**

- **Layer Separation**: Easy to add new features without affecting existing code
- **Service Abstraction**: Can easily swap implementations (e.g., different AI providers)
- **Configuration Management**: Centralized configuration through environment variables

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
