# 🧪 AI Code Reviewer: a code review lab

AI Code Reviewer started in 2025 as an AI-powered code review tool for GitHub repositories, built on Azure OpenAI with a React frontend. It was written before GitHub Copilot code review became part of GitHub itself.

Today Copilot reviews pull requests where they live: it can run on every PR, comment on the changed lines and follow the instructions kept in the repository. This project doesn't try to compete with that. It is kept as a **lab for AI code review**: a place to try prompts, models and workflows, and to find out how good an AI reviewer really is.

## 🎬 Demo videos

The tool as it was built:

https://github.com/user-attachments/assets/f1a3a6c9-41dd-441f-9e88-4f968f057006

The later features (Train AI, collaboration, workflows, GitHub App, repository filters):

https://github.com/user-attachments/assets/ccbce9bc-30b1-494a-9efc-146f1473e9d9

## 🔬 What the lab has today

- **AI reviews of commits and pull requests** with Azure OpenAI
- **System prompts per language**: C#, VB.NET, Java, JavaScript, TypeScript, React and T-SQL, editable in the UI
- **Train AI**: try a prompt on a piece of code, give feedback and refine the prompt
- **Workflows**: JSON-defined steps after a review (a PR comment, a Jira ticket, Slack or Teams notifications) run through Semantic Kernel plugins
- **Real-time collaboration**: shared review sessions with live comments and presence over SignalR
- **GitHub App authentication** and **repository filters**
- **Token and cost figures** for each review

## 🧭 Where the lab is going

Copilot answers "can AI review my PR?". The lab is meant to answer "how good is the review?":

1. A labelled set of files with seeded bugs, each bug written down with its file, line and category
2. Structured JSON output from the model in place of free-text parsing
3. A judge that scores each review against the labels: bugs found, bugs missed and false alarms
4. Runs that compare prompts, models and Copilot's own comments on the same changes
5. A scoreboard of prompt versions, keeping a change only when the scores improve

## ⚠️ Known limits

- Reviews read whole files, cut off at `ContentLimit`, and at most `MaxFilesToReview` files per review, not the diff
- A pull request review is posted as one comment, not as comments on the changed lines
- The API has no authentication and allows any origin, so run it locally only
- Collaboration sessions and cached reviews are held in memory and are lost on restart

## 🚀 Quick start

### Prerequisites

- .NET 9.0 SDK
- Node.js 18+
- An Azure OpenAI deployment and a GitHub token

### Setup

1. **Backend settings** go in `appsettings.json` in the repository root (ignored by git, so your keys stay local):

   ```json
   {
     "AzureOpenAI": {
       "Endpoint": "https://your-resource.openai.azure.com",
       "ApiKey": "your-api-key",
       "DeploymentName": "your-deployment"
     },
     "GitHub": {
       "Token": "your-github-token",
       "Owner": "repository-owner",
       "Name": "repository-name"
     }
   }
   ```

   The same keys can come from environment variables instead, such as `AzureOpenAI__ApiKey`.

2. **Start the backend**:

   ```bash
   dotnet run --launch-profile https --environment Development
   ```

   The API listens on `https://localhost:7001`, with Swagger at `/api/docs`.

3. **Point the frontend at it** with `client-app/.env.development`:

   ```env
   VITE_API_BASE_URL=https://localhost:7001/api/v1
   ```

4. **Start the frontend**:

   ```bash
   cd client-app
   npm install
   npm run dev
   ```

   Open `http://localhost:5174`.

### Token metrics

Token usage and estimated cost are shown at the top of each review summary. Turn them off in `appsettings.json`:

```json
"CodeReview": {
  "MaxFilesToReview": 3,
  "MaxIssuesInSummary": 3,
  "ShowTokenMetrics": false
}
```

## 📄 License

MIT License, see the [LICENSE](LICENSE) file.

## ⚠️ Disclaimer

This project was developed independently on personal equipment and in personal time.  
It is not affiliated with, endorsed by, or derived from the intellectual property of EPAM Systems or any of its clients.  
All examples, configurations, and data are generic and intended solely for demonstration and educational purposes.
