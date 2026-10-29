# AI Code Reviewer: a code review lab

AI Code Reviewer started in 2025 as an AI code review tool for GitHub repositories, built on Azure OpenAI with a React frontend. I wrote it before GitHub Copilot code review became part of GitHub itself.

Today Copilot reviews pull requests where they live: it can run on every PR, comment on the changed lines and follow the instructions kept in the repository. This project doesn't try to compete with that. I keep it as a lab for AI code review, a place to try prompts, models and workflows, and to find out how good an AI reviewer really is.

## Demo videos

The tool as it was first built:

https://github.com/user-attachments/assets/f1a3a6c9-41dd-441f-9e88-4f968f057006

The later features (Train AI, collaboration, workflows, GitHub App, repository filters):

https://github.com/user-attachments/assets/ccbce9bc-30b1-494a-9efc-146f1473e9d9

## What the lab has today

- AI reviews of commits and pull requests, with Claude Haiku 5.5 by default; any model in `Configuration/models.json` (Anthropic, Azure OpenAI or an OpenAI-compatible API such as Ollama) can do the review
- Findings come back as structured JSON (line, end line, category, severity, title, explanation, suggestion), with the cost of each review worked out from the prices in `models.json`
- A system prompt per language (C#, VB.NET, Java, JavaScript, TypeScript, React and T-SQL), editable in the UI
- Train AI: try a prompt on a piece of code, give feedback and refine the prompt
- Workflows defined in JSON that run after a review (a PR comment, a Jira ticket, Slack or Teams notifications) through Semantic Kernel plugins
- Shared review sessions with live comments and presence over SignalR
- GitHub App authentication and repository filters
- Token usage and estimated cost for each review

## Where the lab is going

Copilot answers "can AI review my PR?". The lab is meant to answer "how good is the review?":

1. A labelled set of files with seeded bugs, each bug written down with its file, line and category (done: [`lab/datasets/v1`](lab/datasets/v1/README.md), 28 files and 71 labels)
2. Structured JSON output from the model instead of parsing free text (done)
3. A judge that scores each review against the labels: bugs found, bugs missed and false alarms (done: `dotnet run --project lab/AICodeReviewer.Lab -- run`)
4. Runs that compare prompts, models and Copilot's own comments on the same changes
5. A scoreboard of prompt versions, keeping a change only when the scores improve

How to run the lab and how a review is scored: [`lab/README.md`](lab/README.md).

## Known limits

- Reviews read whole files, cut off at `ContentLimit`, and at most `MaxFilesToReview` files per review, rather than the diff
- A pull request review is posted as one comment, not as comments on the changed lines
- The API has no authentication and allows any origin, so run it locally only
- Collaboration sessions and cached reviews are kept in memory and are lost on restart

## Quick start

### Prerequisites

- .NET 10 SDK
- Node.js 18+
- An Anthropic API key (or an Azure OpenAI deployment, or a local model in Ollama) and a GitHub token

### Setup

1. Put the backend settings in `appsettings.json` in the repository root. Git ignores the file, so your keys stay local.

   ```json
   {
     "Anthropic": {
       "ApiKey": "your-anthropic-key"
     },
     "Review": {
       "Model": "claude-haiku-5-5"
     },
     "GitHub": {
       "Token": "your-github-token",
       "Owner": "repository-owner",
       "Name": "repository-name"
     }
   }
   ```

   The same keys can come from environment variables instead, such as `Anthropic__ApiKey` (or `ANTHROPIC_API_KEY`). `Review:Model` is a key from `Configuration/models.json` and defaults to `claude-haiku-5-5`. To review with Azure OpenAI, set `Review:Model` to `azure-openai` and add an `AzureOpenAI` section with `Endpoint`, `ApiKey` and `DeploymentName`.

2. Start the backend:

   ```bash
   dotnet run --launch-profile https --environment Development
   ```

   The API listens on `https://localhost:7001`, with Swagger at `/api/docs`.

3. Point the frontend at it in `client-app/.env.development`:

   ```env
   VITE_API_BASE_URL=https://localhost:7001/api/v1
   ```

4. Start the frontend and open `http://localhost:5174`:

   ```bash
   cd client-app
   npm install
   npm run dev
   ```

### Token metrics

Token usage and estimated cost are shown at the top of each review summary. To hide them, set `ShowTokenMetrics` to `false` in `appsettings.json`:

```json
"CodeReview": {
  "MaxFilesToReview": 3,
  "MaxIssuesInSummary": 3,
  "ShowTokenMetrics": false
}
```

## Checks

The backend tests start the API in memory with placeholder settings, so they need no keys:

```bash
dotnet test
cd client-app && npm run lint && npm run build
```

GitHub Actions runs the same checks on every pull request.

## License

MIT, see [LICENSE](LICENSE).

## Disclaimer

This project was developed independently on personal equipment and in personal time.  
It is not affiliated with, endorsed by, or derived from the intellectual property of EPAM Systems or any of its clients.  
All examples, configurations, and data are generic and intended solely for demonstration and educational purposes.
