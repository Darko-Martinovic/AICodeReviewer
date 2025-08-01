# AI Code Reviewer - Copilot Instructions

## Project Architecture

This is a **full-stack .NET 9 + React/TypeScript application** for AI-powered code reviews using Azure OpenAI. The system follows a clean architecture with clear separation between backend API and frontend client.

### Backend (.NET 9 Web API)

- **Entry Point**: `Program.cs` - Service registration, middleware configuration, CORS setup
- **Controllers**: REST API endpoints in `/Controllers/` (repositories, commits, pull requests, system prompts)
- **Services**: Business logic in `/Services/` with interface-based dependency injection
- **Models**: DTOs and configuration models in `/Models/`
- **Key Dependencies**: Azure OpenAI SDK, Octokit (GitHub API), DotNetEnv

### Frontend (React + TypeScript + Vite)

- **Entry Point**: `client-app/src/main.tsx` with React.StrictMode
- **Architecture**: Single-page app with tab-based navigation in `App.tsx`
- **Styling**: **CSS Modules pattern** - each component has `.module.css` file for scoped styles
- **API Integration**: Axios-based service layer in `services/api.ts` with separate timeout configs

## Development Workflow

### Starting the Application

```bash
# Backend (from root directory)
dotnet run --launch-profile https --environment Development

# Frontend (from client-app directory)
npm run dev
```

### Key Development Commands

- Backend dev with minimal workflow: `start-dev.cmd`
- Build client: `npm run build` (requires TypeScript compilation first)
- Lint frontend: `npm run lint`

### Port Configuration

- Backend API: `https://localhost:7001`
- Frontend Dev Server: `http://localhost:5174`
- CORS configured for multiple React dev server ports (3000, 5173-5175)

## Project-Specific Conventions

### CSS Modules Pattern (CRITICAL)

**All components use CSS modules** - follow this exact pattern:

```tsx
// Component file: ComponentName.tsx
import styles from "./ComponentName.module.css";

// Usage in JSX
<div className={styles.cardContainer}>
  <button className={styles.primaryButton}>
```

```css
/* ComponentName.module.css */
.cardContainer {
  background-color: white;
  /* NO @apply directives - use standard CSS */
}

/* Dark mode support */
:global(.dark) .cardContainer {
  background-color: rgb(31 41 55);
}
```

### State Management Patterns

- **React State**: useState for local component state, complex state objects in `App.tsx`
- **API State**: Direct API calls with loading/error states, no global state management
- **State Structure**: `state.activeTab`, `state.currentRepository`, `state.commits[]`, etc.

### Service Architecture

- **Interface Segregation**: All services implement interfaces in `Services/Interfaces/`
- **Dependency Injection**: Singleton services for performance (HttpClient, configuration)
- **Azure OpenAI Integration**: Specialized service with prompt management and language detection

## Language-Specific Features

### System Prompts Management

The application has **dynamic language-specific AI prompts**:

- **Languages Supported**: C#, VB.NET, JavaScript, TypeScript, React, T-SQL
- **Prompt Structure**: Base system prompt + customizable additions per language
- **Configuration**: Managed through `SystemPromptsController` and React UI
- **File Pattern**: `Models/Configuration/LanguagePrompts.cs` contains default prompts

### File Extension Detection

Language detection in `LanguageDetectionService.cs`:

```csharp
".cs" => "CSharp"
".tsx" => "React"
".ts" => "TypeScript"
".js" => "JavaScript"
```

## Integration Points

### GitHub API Integration

- **Primary API**: Octokit library for GitHub interactions
- **Authentication**: GITHUB_TOKEN environment variable required
- **Endpoints**: Repositories, commits, pull requests, file contents
- **Rate Limiting**: Handled by Octokit with automatic retries

### Azure OpenAI Service

- **Authentication**: API key + endpoint configuration
- **Model**: Configurable deployment name (typically GPT-4)
- **Request Pattern**: System prompt + user content with structured JSON responses
- **Response Parsing**: Custom JSON deserialization for code review results

### API Communication

- **Base URL**: `https://localhost:7001/api`
- **Timeout Strategy**: 10s for quick operations, 60s for AI reviews
- **Error Handling**: Centralized interceptors in `api.ts`

## Component Communication Patterns

### Parent-Child Data Flow

```tsx
// App.tsx manages global state
const [state, setState] = useState<AppState>({
  activeTab: "repositories",
  currentRepository: null,
  commits: [],
  // ...
});

// Pass down handlers and state
<CommitCard
  commit={commit}
  onReview={handleCommitReview}
  isReviewing={state.reviewingCommit === commit.sha}
/>;
```

### Modal/Dialog Pattern

Components use conditional rendering with proper cleanup:

```tsx
{
  reviewResult && (
    <CodeReviewResult
      review={reviewResult}
      onClose={() => setReviewResult(null)}
    />
  );
}
```

## Development Environment

### Required Environment Variables

```bash
GITHUB_TOKEN=your_github_token
AZURE_OPENAI_ENDPOINT=your_endpoint
AZURE_OPENAI_API_KEY=your_key
AZURE_OPENAI_DEPLOYMENT_NAME=your_deployment
```

### Build Configuration

- **Frontend**: Vite with SWC for fast React compilation
- **Backend**: .NET 9 with AOT-ready configuration
- **Proxy Setup**: Vite proxies `/api` calls to backend automatically

### Code Quality Tools

- **Frontend**: ESLint with TypeScript rules, Tailwind forms plugin
- **Backend**: Standard .NET analyzers, follows .cursorrules conventions
- **Formatting**: .cursorrules specifies PascalCase for public members, camelCase for private

## Common Pitfalls

1. **CSS Modules**: Never use `@apply` directives - they cause lint errors
2. **API Timeouts**: Use `longRunningApi` for review operations, regular `api` for quick calls
3. **State Updates**: Always use functional updates: `setState(prev => ({ ...prev, newData }))`
4. **Component Props**: Review operations pass commit SHA or PR number, not full objects
5. **Dark Mode**: Use `:global(.dark)` prefix in CSS modules for theme support
