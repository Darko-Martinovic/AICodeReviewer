# 🤖 AI Code Reviewer

A .NET application that performs AI-powered code reviews using Azure OpenAI with a modern React frontend for system prompt management.

## ✨ Features

- **🔍 AI Code Analysis**: Automated code review using Azure OpenAI
- **🌐 Web Interface**: React + TypeScript frontend with Tailwind CSS
- **🎯 Multi-Language Support**: C#, VB.NET, JavaScript, TypeScript, React, T-SQL
- **⚙️ System Prompts Management**: Visual interface to customize AI behavior per language
- ** Integrations**: GitHub, Jira, Microsoft Teams notifications

## 🚀 Quick Start

### Prerequisites

- .NET 9.0 SDK
- Node.js 18+
- Azure OpenAI Service access
- GitHub account (optional)

### Setup

1. **Clone and configure**

   ```bash
   git clone <repository-url>
   cd AICodeReviewer
   ```

2. **Environment variables** (create `.env` file)

   ```env
   AZURE_OPENAI_ENDPOINT=your-endpoint
   AZURE_OPENAI_API_KEY=your-api-key
   AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4
   GITHUB_TOKEN=your-github-token (optional)
   ```

3. **Start backend**

   ```bash
   dotnet run --launch-profile https --environment Development
   ```

4. **Start frontend**

   ```bash
   cd client-app
   npm install
   npm run dev
   ```

5. **Access application**
   - Frontend: `http://localhost:5173`
   - Backend API: `https://localhost:7001`
   - Swagger: `https://localhost:7001/swagger`

## 📁 Project Structure

```
AICodeReviewer/
├── Controllers/              # API controllers
├── Services/                 # Business logic
├── Models/                   # Data models
├── client-app/              # React frontend
│   ├── src/
│   │   ├── components/      # React components
│   │   ├── services/        # API client
│   │   └── styles/          # CSS modules
│   └── package.json
├── appsettings.json         # Configuration
└── Program.cs               # Application entry point
```

## ⚙️ Configuration

The application uses `appsettings.json` for configuration. Key sections:

- **AzureOpenAI**: API settings and language-specific prompts
- **GitHub**: Repository integration settings
- **Teams/Jira**: Integration webhooks and settings

System prompts can be customized through the web interface under **System Prompts** tab.

## 🔧 Usage

### Web Interface

1. Navigate to the frontend URL
2. Use **Repositories** tab to connect GitHub repos
3. Browse **Commits** and **Pull Requests**
4. Use **System Prompts** tab to customize AI behavior
5. Run code reviews and view results

### API Endpoints

- `GET /api/systemprompts` - Get all system prompts
- `PUT /api/systemprompts/{language}` - Update language prompt
- `POST /api/repositories/{owner}/{repo}/commits/{sha}/review` - Review commit

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.

---

**Transform your code review process with AI-powered analysis!** 🚀
│ ├── AzureOpenAIService.cs # AI-powered code analysis engine
│ ├── GitHubService.cs # Repository and commit management
│ └── ... # Integration services (Jira, Teams)
├── Models/ # Data models and configuration classes
│ ├── Configuration/ # System prompt configurations per language
│ └── ... # Request/response models
├── client-app/ # React + TypeScript frontend application
│ ├── src/
│ │ ├── components/ # React components
│ │ │ ├── SystemPromptsManager.tsx # Main prompt management UI
│ │ │ ├── RepositoryCard.tsx # Repository selection interface
│ │ │ └── ... # Additional UI components
│ │ ├── App.tsx # Main React application
│ │ └── main.tsx # Application entry point
│ ├── package.json # Frontend dependencies
│ └── tailwind.config.js # Styling configuration
├── Configuration/ # Workflow and application configurations
│ └── Workflows/ # Review workflow definitions
├── TestProjects/ # Sample code for AI validation
│ ├── TestCSharp/ # C# test files with intentional issues
│ ├── TestVBNet/ # VB.NET test samples
│ └── TestSQL/ # T-SQL test scripts
├── appsettings.json # Application configuration and AI prompts
├── Program.cs # Main application entry point
└── start-backend.bat # Quick start script for development

```

## 🎛️ System Prompts Management

The **System Prompts Manager** provides a comprehensive web interface for customizing AI behavior across different programming languages. Access this powerful feature through the "System Prompts" tab in the web application.

### 🌟 Key Features

**Language-Specific Customization:**

- **Base System Prompts**: View read-only foundational prompts optimized for each language
- **Custom Additions**: Add your own coding standards, company-specific requirements, and focus areas
- **Live Preview**: See exactly how the combined prompt will appear to the AI before saving
- **Template Library**: Quick-insert common prompt templates for various scenarios

**Interactive Web Interface:**

- **Tabbed Navigation**: Switch seamlessly between programming languages
- **Real-time Editing**: Immediate feedback and validation as you type
- **Save & Preview**: Test your changes before applying them to active reviews
- **History Tracking**: See when prompts were last modified

### Supported Languages & Specializations

| Language       | Icon | Specialization Focus                                      |
| -------------- | ---- | --------------------------------------------------------- |
| **C#**         | 🟢   | .NET best practices, async patterns, SOLID principles     |
| **VB.NET**     | 🔵   | Legacy code modernization, error handling patterns        |
| **T-SQL**      | 🗄️   | Query optimization, security, indexing strategies         |
| **JavaScript** | 🟨   | Modern ES6+, performance, browser compatibility           |
| **TypeScript** | 🔷   | Type safety, interface design, strict mode compliance     |
| **React**      | ⚛️   | Component patterns, hooks usage, performance optimization |

### 📝 Usage Examples

**Adding Company Standards:**

```

- Follow our naming convention: PascalCase for classes, camelCase for methods
- All public APIs must include XML documentation
- Use dependency injection for all service dependencies
- Implement proper logging for all error scenarios

```

**Performance Focus:**

```

- Pay special attention to database query efficiency
- Flag any N+1 query patterns
- Ensure proper async/await usage in data access layers
- Check for memory leaks in disposable resources

```

**Security Requirements:**

```

- Validate all user inputs against injection attacks
- Ensure sensitive data is not logged or exposed
- Check for proper authentication and authorization
- Flag any hardcoded secrets or credentials

```

## 🌐 Web Interface Features

The React-based frontend provides an intuitive and powerful interface for managing all aspects of the AI Code Reviewer system.

### 📊 Main Dashboard

**Repository Management:**

- **Multi-Repository Support**: Switch between different GitHub repositories
- **Repository Browser**: View commits, pull requests, and file changes
- **Quick Actions**: Perform code reviews directly from the web interface
- **History Tracking**: Access previously reviewed commits and results

**Navigation Tabs:**

- **🏠 Repositories**: Browse and select repositories for review
- **📝 Commits**: View recent commits and trigger reviews
- **🔀 Pull Requests**: Manage PR reviews and approvals
- **⚙️ System Prompts**: Customize AI behavior and prompts

### 🎨 System Prompts Interface

**Language Tabs with Visual Indicators:**

- **🟢 C#**: .NET ecosystem optimization
- **🔵 VB.NET**: Legacy code improvement
- **🗄️ T-SQL**: Database performance focus
- **🟨 JavaScript**: Modern web standards
- **🔷 TypeScript**: Type safety enforcement
- **⚛️ React**: Component best practices

**Editing Experience:**

- **Split-Pane Layout**: Base prompts on left, custom additions on right
- **Template Library**: Pre-built snippets for common requirements
- **Live Preview**: Real-time combined prompt generation
- **Auto-Save**: Persistent storage of your customizations

### 🔧 Technical Implementation

**Frontend Stack:**

- **React 19** with TypeScript for type safety
- **Tailwind CSS** for responsive, modern styling
- **Vite** for fast development and optimized builds
- **Lucide React** for consistent iconography

**API Integration:**

- **RESTful Backend**: Clean separation between frontend and backend
- **Real-time Updates**: Instant feedback on prompt changes
- **Error Handling**: Comprehensive error states and user feedback
- **Loading States**: Smooth user experience during operations

## 📊 Review Metrics

Each review provides detailed metrics:

```

📊 REVIEW PERFORMANCE METRICS
Duration: 00:45 | Files: 3 | Issues: 12
Lines of Code: 487 | Tokens: 2,247
Estimated Cost: $0.0035
Cost Savings vs Manual: 99.8%

````

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

### **Development Environment Setup**

**Prerequisites:**

- **.NET 9.0 SDK** - Latest LTS version
- **Node.js 18+** with npm for frontend development
- **Azure OpenAI service** access with API keys
- **Git** for version control
- **VS Code** or **Visual Studio** recommended

### **Development Workflow**

**Backend Development (.NET API):**

```bash
# Start with hot reload for immediate feedback
dotnet watch run

# Run tests
dotnet test

# Build for production
dotnet build --configuration Release
````

**Frontend Development (React + TypeScript):**

```bash
# Navigate to frontend directory
cd client-app

# Install dependencies (first time only)
npm install

# Start development server with hot reload
npm run dev

# Run linting
npm run lint

# Build for production
npm run build
```

**Full Stack Development:**

```bash
# Terminal 1: Start backend API
dotnet watch run

# Terminal 2: Start frontend application
cd client-app && npm run dev

# Access at:
# Backend: https://localhost:7001
# Frontend: http://localhost:5173
```

### **Key Development Features**

- **Hot Reload**: Both backend and frontend support live reloading
- **TypeScript**: Full type safety across the React application
- **Tailwind CSS**: Utility-first styling with responsive design
- **API Integration**: RESTful API with Swagger documentation
- **Component Library**: Reusable React components with consistent styling

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

## 📊 Web Interface Examples

### **Repository Management Interface**

The modern web interface provides intuitive repository management through a clean, responsive design:

**Main Dashboard Navigation:**

- **🏠 Repositories Tab**: Browse and select GitHub repositories
- **📝 Commits Tab**: View recent commits with review status
- **� Pull Requests Tab**: Manage PR reviews and approvals
- **⚙️ System Prompts Tab**: Customize AI behavior per language

**Repository Selection:**

```
┌─────────────────────────────────────────────────┐
│ 🏠 Repository Management                        │
├─────────────────────────────────────────────────┤
│ Current: YourOrg/YourRepo                       │
│                                                 │
│ Available Repositories:                         │
│ ✓ YourOrg/YourRepo (current)                    │
│   YourOrg/AnotherProject                        │
│   YourOrg/WebApp                                │
│   YourOrg/MobileApp                             │
│                                                 │
│ [+ Add Repository]  [🔄 Refresh]                │
└─────────────────────────────────────────────────┘
```

### **Code Review Results Display**

**Repository Information:**

- **Repository**: YourOrg/YourRepo
- **Branch**: main
- **Latest Commit**: a1b2c3d4 - Fix authentication logic
- **Author**: John Developer
- **Date**: 2025-07-05 15:30

**Files Changed:**

- ✏️ `src/auth/AuthService.cs` (+15/-8 lines)
- ✏️ `src/models/User.cs` (+3/-1 lines)
- ➕ `tests/AuthServiceTests.cs` (+45/-0 lines)

**AI Analysis Results:**

```
🤖 AI Code Review Complete

📊 REVIEW PERFORMANCE METRICS
Duration: 00:45 | Files: 3 | Issues: 12
Lines of Code: 487 | Tokens: 2,247
Estimated Cost: $0.0035
Cost Savings vs Manual: 99.8%

🔍 ISSUES FOUND BY CATEGORY
🔒 Security: 2 issues (1 Critical, 1 High)
⚡ Performance: 3 issues (2 Medium, 1 Low)
🏗️ Code Quality: 4 issues (3 Medium, 1 Low)
🐛 Potential Bugs: 3 issues (2 High, 1 Medium)
```

### **Detailed Issue Report**

**Web Interface Issue Card:**

```
┌─────────────────────────────────────────────────────────────────┐
│ 🔒 SECURITY - CRITICAL                                         │
├─────────────────────────────────────────────────────────────────┤
│ Hardcoded API key exposed in source code                       │
│                                                                 │
│ 📄 File: src/auth/AuthService.cs                               │
│ 📍 Line: 42                                                    │
│                                                                 │
│ 📝 Description:                                                │
│ The API key is hardcoded as a string literal, making it        │
│ visible to anyone with source code access and potentially      │
│ exposing it in version control history.                        │
│                                                                 │
│ 💡 Recommendation:                                             │
│ Move the API key to environment variables or secure            │
│ configuration. Use Environment.GetEnvironmentVariable()        │
│ or inject via IConfiguration.                                  │
│                                                                 │
│ 🔧 Example Fix:                                                │
│ // Instead of: var key = "sk-abc123...";                       │
│ // Use: var key = _config["AzureOpenAI:ApiKey"];               │
└─────────────────────────────────────────────────────────────────┘
```

### **System Prompts Management Interface**

**Language Selection Tabs:**

```
┌─────────────────────────────────────────────────────────────────┐
│ [🟢 C#] [🔵 VB.NET] [🗄️ T-SQL] [🟨 JS] [🔷 TS] [⚛️ React]     │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ 📝 Base System Prompt (Read-only)                              │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ You are a C# code reviewer focused on .NET best practices, │ │
│ │ security, performance, and maintainability. Look for:      │ │
│ │ - SOLID principle violations                                │ │
│ │ - Async/await pattern issues                               │ │
│ │ - Memory management problems...                            │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ ✏️ Custom Additions (Editable)                                 │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ - Follow our company naming conventions                     │ │
│ │ - Ensure all public APIs have XML documentation           │ │
│ │ - Focus on dependency injection patterns                   │ │
│ │ - Check for proper logging in error scenarios             │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ [💾 Save] [👁️ Preview] [🔄 Reset] [📋 Templates]              │
└─────────────────────────────────────────────────────────────────┘
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

The AI Code Reviewer combines the power of Azure OpenAI with an intuitive web interface to deliver:

✅ **Enterprise-grade code analysis** with language-specific intelligence  
✅ **Visual prompt management** through the React-based interface  
✅ **Customizable AI behavior** tailored to your coding standards  
✅ **Real-time performance metrics** and cost tracking  
✅ **Seamless integration** with GitHub, Jira, and Teams

### 🎯 Get Started in Minutes

1. **Clone the repository** and configure your Azure OpenAI credentials
2. **Start the backend** with `dotnet run`
3. **Launch the frontend** with `npm run dev` in the `client-app` directory
4. **Open your browser** to `http://localhost:5173` and begin customizing prompts
5. **Connect your repositories** and start automated AI-powered reviews

Transform your development workflow with intelligent, customizable code reviews that scale with your team's needs!
