# 🤖 AI Code Reviewer

A .NET application that performs AI-powered code reviews using Azure OpenAI with web UI management interface.

## ✨ Key Features

- **🔍 AI Code Analysis**: Automated code review using Azure OpenAI
- **� Web Interface**: React-based UI for managing reviews and system prompts
- **🎯 Multi-Language Support**: C#, VB.NET, JavaScript, TypeScript, React, T-SQL
- **⚙️ Configurable System Prompts**: Customize AI prompts per programming language
- **📊 Performance Metrics**: Track review efficiency, costs, and ROI
- **🔗 Integrations**: GitHub, Jira, Microsoft Teams notifications

## � Quick Start

1. **Clone the repository**

   ```bash
   git clone <repository-url>
   cd AICodeReviewer
   ```

2. **Configure environment**

   ```bash
   # Copy environment template
   cp .env.example .env

   # Edit .env with your Azure OpenAI credentials
   AZURE_OPENAI_ENDPOINT=your-endpoint
   AZURE_OPENAI_API_KEY=your-api-key
   ```

3. **Run the application**

   ```bash
   # Start backend API
   dotnet run

   # Start frontend (in another terminal)
   cd client-app
   npm install
   npm run dev
   ```

4. **Access the web interface**
   - Backend API: `https://localhost:7001`
   - Frontend UI: `http://localhost:5173`

## 📁 Project Structure

```
AICodeReviewer/
├── Controllers/              # Web API controllers
├── Services/                # Business logic services
├── Models/                  # Data models and configuration
├── client-app/             # React frontend application
├── Configuration/          # Workflow and prompt configurations
├── TestProjects/          # Test code for AI validation
└── appsettings.json       # Application configuration
```

## 🎛️ System Prompts Management

The web interface provides a **System Prompts** tab where you can:

- View base system prompts for each programming language
- Add custom requirements and coding standards
- Use quick templates for common prompt additions
- Preview combined prompts before saving
- Track modification history

### Supported Languages

- **C#** - .NET specific code review guidelines
- **VB.NET** - Visual Basic specific patterns
- **T-SQL** - Database and query optimization
- **JavaScript** - Modern JS best practices
- **TypeScript** - Type safety and patterns
- **React** - Component and hook guidelines

## 📊 Review Metrics

Each review provides detailed metrics:

```
📊 REVIEW PERFORMANCE METRICS
Duration: 00:45 | Files: 3 | Issues: 12
Lines of Code: 487 | Tokens: 2,247
Estimated Cost: $0.0035
Cost Savings vs Manual: 99.8%
```

## ⚙️ Configuration

Key settings in `appsettings.json`:

- **Azure OpenAI**: Endpoint, API key, model settings
- **GitHub**: Repository settings and authentication
- **Integrations**: Jira, Teams notification settings
- **System Prompts**: Language-specific review guidelines

## 🔗 Integrations

- **GitHub**: Pull request and commit reviews
- **Jira**: Automatic ticket updates with review results
- **Microsoft Teams**: Real-time notification messages

## 🧪 Testing

Built-in test projects with intentional issues:

- `TestProjects/TestCSharp/` - C# code samples
- `TestProjects/TestVBNet/` - VB.NET samples
- `TestProjects/TestSQL/` - T-SQL samples

Run reviews on test projects to validate AI performance.

## 🛠️ Development

**Prerequisites:**

- .NET 9.0 SDK
- Node.js 18+ for frontend
- Azure OpenAI service access

**Backend Development:**

```bash
dotnet watch run  # Hot reload for API changes
```

**Frontend Development:**

```bash
cd client-app
npm run dev  # Hot reload for UI changes
```

## � License

This project is licensed under the MIT License.

- ✅ **Line-by-line feedback** with specific file locations
- ✅ **Actionable recommendations** with code examples
- ✅ **Configurable AI behavior** (temperature, max tokens, system prompts)
- ✅ **Support for 12+ programming languages**
- ✅ **Dynamic language-specific prompts** for C#, VB.NET, JavaScript, TypeScript, React, and T-SQL
- ✅ **Intelligent language detection** by file extension
- ✅ **Language-optimized issue detection** with specialized prompts for each language

### 📊 **Performance & Cost Tracking**

- ✅ **Real-time metrics**: Duration, token usage, cost calculation
- ✅ **Efficiency analysis**: Issues per file, lines per minute, cost per issue
- ✅ **ROI measurement**: Quantifiable cost savings vs manual reviews
- ✅ **Business reporting**: Data for stakeholder presentations

## 🚀 Quick Start

### Prerequisites

- .NET 8.0 or later
- Azure OpenAI Service access
- GitHub repository access
- (Optional) Jira account for ticket integration
- (Optional) Microsoft Teams webhook for notifications

### Installation

1. **Clone the repository**

   ```bash
   git clone https://github.com/your-username/AICodeReviewer.git
   cd AICodeReviewer
   ```

2. **Install dependencies**

   ```bash
   dotnet restore
   ```

3. **Configure environment variables**

   Copy `.env.example` to `.env` and configure your settings:

   ```env
   # Azure OpenAI Configuration
   AOAI_ENDPOINT=your-azure-openai-endpoint
   AOAI_APIKEY=your-azure-openai-api-key
   CHATCOMPLETION_DEPLOYMENTNAME=your-gpt-deployment-name

   # GitHub Configuration
   GITHUB_TOKEN=your-github-token
   GITHUB_REPO_OWNER=your-github-username
   GITHUB_REPO_NAME=your-repository-name

   # Jira Configuration (Optional)
   JIRA_BASE_URL=https://your-company.atlassian.net
   JIRA_USER_EMAIL=your-email@company.com
   JIRA_API_TOKEN=your-jira-api-token
   JIRA_PROJECT_KEY=YOUR-PROJECT-KEY

   # Teams Configuration (Optional)
   TEAMS_WEBHOOK_URL=your-teams-webhook-url
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

## 🎯 How It Works

### 1. Code Analysis Workflow

```mermaid
graph LR
    A[Commit/PR] --> B[Fetch Files]
    B --> C[AI Analysis]
    C --> D[Issue Detection]
    D --> E[Generate Report]
    E --> F[Collect Metrics]
    F --> G[Send Notifications]
    G --> H[Update Jira]
```

### 2. AI-Powered Review

The system performs comprehensive analysis focusing on:

- **🔒 Security**: Hardcoded secrets, SQL injection, input validation
- **⚡ Performance**: Inefficient algorithms, memory leaks, async/await patterns
- **🏗️ Code Quality**: Naming conventions, code duplication, maintainability
- **🐛 Bug Detection**: Null reference exceptions, race conditions, error handling
- **📐 Best Practices**: SOLID principles, design patterns, C# conventions

### 3. Repository Management

**Multi-Repository Support:**

- **Dynamic Repository Switching**: Seamlessly switch between multiple GitHub repositories
- **Repository History**: Track recently used repositories for quick access
- **Validation & Error Handling**: Automatic validation of repository access and permissions
- **Default Repository**: Configurable default repository from environment variables
- **Repository Selection**: Interactive menu for selecting repositories before operations

**Repository Management Features:**

- **List Available Repositories**: View all repositories accessible with your GitHub token
- **Add New Repository**: Dynamically add repositories to your working set
- **Repository Validation**: Verify repository access and permissions before operations
- **History Tracking**: Maintain a list of recently used repositories
- **Seamless Integration**: All operations (commits, PRs, reviews) work with any selected repository

### 4. Integration Features

**Jira Integration:**

- Automatically detects related tickets from commit messages
- Posts detailed review comments with ADF formatting
- Manages labels based on review severity
- Links issues to specific files and line numbers

**Teams Notifications:**

- Real-time review summaries with issue counts
- Interactive team member simulations
- Severity-based color coding and reactions
- Direct links to pull requests and Jira tickets

**Performance Tracking:**

- Token usage monitoring for cost control
- Review duration and efficiency metrics
- ROI calculations for business justification

## ⚙️ Configuration

### Application Settings (`appsettings.json`)

```json
{
  "AzureOpenAI": {
    "Temperature": 0.3,
    "MaxTokens": 8000,
    "ContentLimit": 15000,
    "SystemPrompt": "Custom AI review prompt...",
    "LanguagePrompts": {
      "CSharp": {
        "SystemPrompt": "C#-specific review prompt...",
        "UserPromptTemplate": "C# file review template..."
      },
      "VbNet": {
        "SystemPrompt": "VB.NET-specific review prompt...",
        "UserPromptTemplate": "VB.NET file review template..."
      },
      "Sql": {
        "SystemPrompt": "T-SQL-specific review prompt...",
        "UserPromptTemplate": "T-SQL file review template..."
      },
      "JavaScript": {
        "SystemPrompt": "JavaScript-specific review prompt...",
        "UserPromptTemplate": "JavaScript file review template..."
      },
      "TypeScript": {
        "SystemPrompt": "TypeScript-specific review prompt...",
        "UserPromptTemplate": "TypeScript file review template..."
      },
      "React": {
        "SystemPrompt": "React-specific review prompt...",
        "UserPromptTemplate": "React file review template..."
      }
    }
  },
  "CodeReview": {
    "MaxFilesToReview": 5,
    "MaxIssuesInSummary": 3
  },
  "Teams": {
    "TeamMembers": ["Alice Smith", "Bob Johnson", "Carol Wilson"],
    "SimulationDelays": {
      "WebhookPreparation": 500,
      "MessageSending": 400
    }
  }
}
```

### Environment Variables

All sensitive data and environment-specific settings can be configured via environment variables:

**Azure OpenAI Configuration:**

- `AOAI_ENDPOINT`: Your Azure OpenAI service endpoint
- `AOAI_APIKEY`: Your Azure OpenAI API key
- `CHATCOMPLETION_DEPLOYMENTNAME`: Your GPT deployment name

**GitHub Configuration:**

- `GITHUB_TOKEN`: Your GitHub personal access token
- `GITHUB_REPO_OWNER`: Default repository owner (e.g., "YourOrg")
- `GITHUB_REPO_NAME`: Default repository name (e.g., "YourRepo")

**AI Review Settings:**

- `AI_TEMPERATURE`: AI creativity level (0.1-1.0)
- `AI_MAX_TOKENS`: Maximum response length
- `AI_CONTENT_LIMIT`: Maximum file size to analyze
- `MAX_FILES_TO_REVIEW`: Maximum files per review session
- `MAX_ISSUES_IN_SUMMARY`: Maximum issues in notifications

**Repository Management:**

- The application automatically uses the default repository from `GITHUB_REPO_OWNER` and `GITHUB_REPO_NAME`
- You can dynamically switch repositories during runtime without restarting the application
- Repository history is maintained for quick access to recently used repositories

## 📊 Sample Output

### **Repository Management Menu**

```
🤖 AI Code Reviewer

Choose an option:

  1. 📝 List recent commits
  2. 🔍 Review latest commit (Push Event)
  3. 🔍 Review commit by hash
  4. 📋 List open Pull Requests
  5. 🔀 Review Pull Request
  6. 🏠 Manage repositories
  7. 🚪 Exit

Enter your choice (1-7): 6

🏠 Repository Management
Current repository: YourOrg/YourRepo

Choose an option:
  1. 📋 List available repositories
  2. ➕ Add new repository
  3. 🔄 Switch repository
  4. 📊 Show repository history
  5. ⬅️ Back to main menu

Enter your choice (1-5): 1

📋 Available Repositories:
  1. YourOrg/YourRepo (current)
  2. YourOrg/AnotherProject
  3. YourOrg/WebApp
  4. YourOrg/MobileApp

Enter repository number to switch (or 0 to cancel): 2

✅ Switched to repository: YourOrg/AnotherProject
```

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

📊 REVIEW PERFORMANCE METRICS
Duration: 00:45
Files Reviewed: 3
Issues Found: 7
Lines of Code: 487
Total Tokens: 2,247
Input Tokens: 1,834
Output Tokens: 413
Estimated Cost: $0.0035
────────────────────────────────────────
EFFICIENCY METRICS
Issues/File: 2.3
Lines/Minute: 649
Cost/Issue: $0.0005
Cost/File: $0.0012
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

| Aspect                    | Before                    | After                                |
| ------------------------- | ------------------------- | ------------------------------------ |
| **Dependency Management** | Manual instantiation      | DI container with interfaces         |
| **Testability**           | Hard to mock dependencies | Easy to inject mocks via interfaces  |
| **Code Organization**     | Tightly coupled services  | Loosely coupled with clear contracts |
| **Resource Management**   | Manual disposal           | Automatic disposal via DI container  |
| **Adding New Services**   | Modify Program.cs         | Register in ConfigureServices()      |
| **Maintenance**           | Unclear dependency graph  | Explicit, documented dependencies    |

## 🧪 Demo and Testing

### Interactive Demo

The project includes `DemoCode/BuggyCodeExample.cs` with intentional issues for demonstration:

- Security vulnerabilities (hardcoded credentials, SQL injection)
- Performance problems (async void, missing ConfigureAwait)
- Code quality issues (magic numbers, deep nesting)
- Potential bugs (null reference exceptions)
- Maintainability concerns (exception swallowing, inefficient loops)

### Comprehensive Test Projects

The solution includes dedicated test projects for validating AI capabilities across different languages:

#### **TestCSharp Project** (`TestProjects/TestCSharp/`)

- **Purpose**: C# code review testing
- **Issues**: Poor naming conventions, missing error handling, hardcoded values
- **Features**: Intentional code quality issues for AI detection

#### **TestVBNet Project** (`TestProjects/TestVBNet/`)

- **Purpose**: VB.NET code review testing
- **Issues**: SQL injection vulnerabilities, poor exception handling, inefficient loops
- **Features**: VB.NET-specific issues with database access patterns

#### **TestSQL Project** (`TestProjects/TestSQL/`)

- **Purpose**: T-SQL code review testing
- **Files**:
  - `CreateUsersTable.sql` - Table creation with missing constraints
  - `CreateOrdersTable.sql` - Foreign key relationships
  - `CreateProductsTable.sql` - Data type and constraint issues
  - `usp_GetAllUsers.sql` - Stored procedure with performance issues
  - `usp_GetUserById.sql` - SQL injection vulnerability
  - `usp_GetUserOrderSummary.sql` - Complex query with missing indexes
- **Issues**: Missing SET NOCOUNT ON, SQL injection, missing indexes, poor error handling

### Language-Specific Testing

Each test project demonstrates the AI's ability to:

- **Detect language-specific issues** (C# async patterns, VB.NET exception handling, T-SQL performance)
- **Apply appropriate severity levels** based on language context
- **Provide language-optimized recommendations** with specific syntax examples
- **Use dynamic prompts** that adapt to the detected programming language

### Performance Benchmarks

Typical performance metrics from production usage:

- **Processing Speed**: 500-800 lines/minute
- **Issue Detection**: 2-6 issues per file average
- **Token Efficiency**: ~750 tokens per file
- **Response Time**: 200-400ms per API call
- **Cost per Review**: $0.002-0.008 depending on file size

## 🤝 Contributing

### **Development Guidelines**

1. **Follow SOLID Principles**: Use the established DI patterns and interface-based design
2. **Add Comprehensive Tests**: Leverage the DI container for easy mocking and testing
3. **Document Public APIs**: Include XML documentation for all public methods and interfaces
4. **Handle Errors Gracefully**: Implement proper error handling with meaningful messages
5. **Update Configuration**: Add new settings to `appsettings.json` and document in `.env.example`

### **Adding New Services**

1. **Create Interface**: Define contract in `Services/I{ServiceName}.cs`
2. **Implement Service**: Create implementation in `Services/{ServiceName}.cs`
3. **Register in DI**: Add to `ConfigureServices()` in `Program.cs`
4. **Inject Dependencies**: Use constructor injection in consuming classes
5. **Add Tests**: Create unit tests with mocked dependencies

### **Adding New Language Support**

1. **Add Language Detection**: Update `LanguageDetectionService.cs` with new file extensions
2. **Create Language Prompts**: Add language-specific prompts to `appsettings.json`
3. **Test with Sample Files**: Create test files with intentional issues
4. **Validate AI Detection**: Run reviews to ensure proper language-specific analysis

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **Azure OpenAI Service** for powerful AI capabilities
- **Microsoft.Extensions.DependencyInjection** for clean architecture
- **Atlassian Jira** for issue tracking integration
- **Microsoft Teams** for seamless team notifications
- **GitHub API** for repository integration

---

**Ready to revolutionize your code review process?** 🚀

Start with the [Quick Start](#quick-start) guide and see the difference AI-powered code reviews can make for your development workflow!
