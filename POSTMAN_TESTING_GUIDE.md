# AI Code Reviewer - Postman API Testing Collection

This comprehensive Postman collection provides complete API testing capabilities for the AI Code Reviewer system. It includes all endpoints across 6 main controllers with organized folders, environment variables, and automated testing scripts.

## 📁 Collection Structure

### 1. Repository Management

- **Get Current Repository** - Retrieve currently configured repository
- **Get All Repositories** - List all available repositories
- **Get Repository History** - View recently accessed repositories
- **Get Available Repositories** - Show repositories for configured user
- **Validate Repository** - Check if repository exists and is accessible
- **Set Current Repository** - Configure the active repository context
- **Set Repository (Alternative)** - Alternative endpoint for repository switching

### 2. Pull Request Operations

- **List All Pull Requests** - Get all PRs for current repository
- **Switch Repository Context** - Change repository context for PR operations
- **Get Open Pull Requests** - List only open pull requests
- **Review Pull Request** - Perform AI-powered PR analysis
- **Get Pull Request Details** - Detailed information about specific PR
- **Get Pull Request Files** - List files changed in a PR

### 3. Commit Operations

- **Get Repository Branches** - List all repository branches
- **Get Recent Commits** - Retrieve recent commits with filtering
- **Review Latest Commit** - AI review of the most recent commit
- **Review Specific Commit** - AI review of commit by SHA

### 4. System Prompts

- **Get All System Prompts** - List prompts for all languages
- **Get Language-Specific Prompt** - Retrieve prompt for specific language
- **Set Custom Language Prompt** - Configure custom AI prompts
- **Preview Language Prompt** - Preview prompt with custom additions
- **Get Prompt Templates** - Available prompt templates

### 5. Test Commits

- **Get Recent Test Commits** - Test data for development
- **Get Test Commit Data** - Sample commit information

### 6. Workflow Engine

- **Get All Workflows** - List available workflow configurations
- **Get Specific Workflow** - Details for specific workflow
- **Execute Pull Request Workflow** - Run automated PR workflow
- **Execute Commit Workflow** - Run automated commit workflow
- **Execute Custom Workflow** - Run custom workflow with context

### 7. Error Testing Scenarios

- **Test Non-Existent Repository** - Validate 404 error handling
- **Test Non-Existent Pull Request** - Test PR validation
- **Test Invalid Commit SHA** - Test commit validation
- **Test Unsupported Language Prompt** - Test language validation

## 🔧 Setup Instructions

### 1. Import Collection and Environment

1. **Import Collection:**

   ```
   File → Import → AICodeReviewer_API_Collection.postman_collection.json
   ```

2. **Import Environment:**

   ```
   File → Import → AICodeReviewer_Environment.postman_environment.json
   ```

3. **Select Environment:**
   - Choose "AI Code Reviewer Environment" from the environment dropdown

### 2. Configure Environment Variables

Update these key variables in your environment:

| Variable       | Description                 | Default Value            |
| -------------- | --------------------------- | ------------------------ |
| `baseUrl`      | API base URL                | `https://localhost:7001` |
| `currentOwner` | Repository owner            | `microsoft`              |
| `currentRepo`  | Repository name             | `vscode`                 |
| `prNumber`     | PR number for testing       | `1`                      |
| `commitSha`    | Commit SHA (auto-populated) | `""`                     |

### 3. Start the Application

Before testing, ensure both backend and frontend are running:

```bash
# Backend (from root directory)
dotnet run --launch-profile https --environment Development

# Frontend (from client-app directory)
npm run dev
```

## 🧪 Testing Workflows

### Basic Testing Flow

1. **Repository Setup:**

   ```
   Repository Management → Set Current Repository
   Repository Management → Validate Repository
   ```

2. **Explore Data:**

   ```
   Commit Operations → Get Recent Commits
   Pull Request Operations → Get Open Pull Requests
   ```

3. **Perform Reviews:**
   ```
   Pull Request Operations → Review Pull Request
   Commit Operations → Review Latest Commit
   ```

### Advanced Testing Scenarios

1. **Repository Switching:**

   ```
   Repository Management → Set Current Repository (microsoft/vscode)
   Pull Request Operations → Switch Repository Context
   Repository Management → Set Current Repository (octocat/Hello-World)
   ```

2. **Error Handling:**

   ```
   Error Testing Scenarios → Test Non-Existent Repository
   Error Testing Scenarios → Test Non-Existent Pull Request
   ```

3. **Workflow Automation:**
   ```
   Workflow Engine → Get All Workflows
   Workflow Engine → Execute Pull Request Workflow
   ```

## 🔍 Environment Variables Reference

### Primary Variables

- **baseUrl**: `https://localhost:7001` - Main API endpoint
- **apiPath**: `/api` - API path prefix
- **currentOwner**: Repository owner (e.g., `microsoft`, `octocat`)
- **currentRepo**: Repository name (e.g., `vscode`, `Hello-World`)

### Dynamic Variables

- **commitSha**: Auto-populated from API responses
- **prNumber**: Pull request number for testing

### Test Variables

- **testOwner**: `octocat` - For testing scenarios
- **testRepo**: `Hello-World` - For testing scenarios

## 📊 Automated Testing Features

### Pre-request Scripts

- Auto-populate `commitSha` from previous responses
- Dynamic variable extraction from API responses

### Post-request Tests

- **Status Code Validation**: Ensures no 500 errors
- **Content Type Validation**: Verifies JSON responses
- **Data Extraction**: Automatically extracts useful data for subsequent requests
- **Repository Context**: Updates environment variables from responses

### Test Assertions

```javascript
pm.test("Status code is not 500", function () {
  pm.expect(pm.response.code).to.not.equal(500);
});

pm.test("Response has JSON content type", function () {
  pm.expect(pm.response.headers.get("Content-Type")).to.include(
    "application/json"
  );
});
```

## 🚀 Common Usage Patterns

### 1. Repository Context Switching

```
1. Repository Management → Set Current Repository (owner/repo)
2. Repository Management → Validate Repository
3. Pull Request Operations → Get Open Pull Requests
```

### 2. Pull Request Analysis

```
1. Pull Request Operations → Get Open Pull Requests
2. Update {{prNumber}} variable with actual PR number
3. Pull Request Operations → Get Pull Request Details
4. Pull Request Operations → Get Pull Request Files
5. Pull Request Operations → Review Pull Request
```

### 3. Commit Review Workflow

```
1. Commit Operations → Get Recent Commits
2. Copy SHA from response to {{commitSha}} variable
3. Commit Operations → Review Specific Commit
```

### 4. System Configuration

```
1. System Prompts → Get All System Prompts
2. System Prompts → Get Language-Specific Prompt (CSharp)
3. System Prompts → Set Custom Language Prompt
4. System Prompts → Preview Language Prompt
```

## 🛠️ Troubleshooting

### Common Issues

1. **SSL Certificate Errors:**

   - Disable SSL verification in Postman settings for localhost testing
   - File → Settings → General → SSL certificate verification: OFF

2. **CORS Issues:**

   - Ensure backend is running with proper CORS configuration
   - Check that `baseUrl` matches the actual backend URL

3. **404 Errors:**

   - Verify repository exists and is accessible
   - Check GitHub token permissions
   - Validate repository owner/name format

4. **Timeout Issues:**
   - AI review operations may take 30-60 seconds
   - Increase Postman timeout in settings if needed

### Debug Tips

1. **Check Environment Variables:**

   - Hover over variables in requests to see current values
   - Use Console tab to see variable resolution

2. **Monitor Network Traffic:**

   - Use Postman Console to see actual requests
   - Check for SSL/certificate issues

3. **Validate Responses:**
   - Use Tests tab to add custom validation
   - Check response status codes and content

## 📝 API Response Examples

### Repository Validation Success

```json
{
  "isValid": true,
  "owner": "microsoft",
  "name": "vscode",
  "message": "Repository is valid and accessible"
}
```

### Pull Request Review Response

```json
{
  "summary": "Code review completed. Found 2 high-priority issues...",
  "issues": [
    {
      "severity": "high",
      "file": "src/component.ts",
      "line": 45,
      "message": "Potential null reference",
      "suggestion": "Add null checking"
    }
  ],
  "suggestions": ["Consider performance optimizations"],
  "complexity": "medium",
  "testCoverage": "partial"
}
```

### Error Response Format

```json
{
  "error": "Repository not found",
  "details": "The specified repository microsoft/nonexistent does not exist or is not accessible"
}
```

This collection provides comprehensive testing capabilities for all aspects of the AI Code Reviewer API, from basic repository operations to complex workflow execution and error handling scenarios.
