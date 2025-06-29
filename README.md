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
│   └── NotificationService.cs   # Teams notification logic
├── Application/         # Application orchestration layer
│   └── CodeReviewApplication.cs # Main application workflows
├── Utils/              # Utility classes and helpers
│   └── FileUtils.cs          # File-related utility methods
└── Program.cs          # Entry point and dependency injection
```

## Features

- ✅ **GitHub Integration**: Connect to GitHub repositories and analyze commits/PRs
- ✅ **AI Code Review**: Leverage Azure OpenAI for intelligent code analysis
- ✅ **Visual Progress**: Real-time progress indicators during review process
- ✅ **Teams Notifications**: Formatted notification output (simulated)
- ✅ **Multiple File Support**: Analyze multiple code files in a single review
- ✅ **File Type Detection**: Supports various programming languages

## Setup

1. **Environment Variables**
   Create a `.env` file in the project root:
   ```env
   GITHUB_TOKEN=your_github_personal_access_token
   GITHUB_REPO_OWNER=repository_owner
   GITHUB_REPO_NAME=repository_name
   AOAI_ENDPOINT=https://your-azure-openai-endpoint.openai.azure.com
   AOAI_APIKEY=your_azure_openai_api_key
   CHATCOMPLETION_DEPLOYMENTNAME=your_chat_completion_deployment_name
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
- Graceful degradation when services are unavailable
- User-friendly error reporting

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
- [ ] Implement Jira ticket creation
- [ ] Add PR comment posting
- [ ] Support for more file types
- [ ] Configuration file support
- [ ] Logging framework integration
- [ ] Unit tests coverage
- [ ] Docker containerization

## Contributing

1. Follow the established architecture patterns
2. Add XML documentation to public methods
3. Include error handling in new services
4. Update this README when adding new features
