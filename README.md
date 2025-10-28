# 🤖 AI Code Reviewer

AI-powered code review tool using Azure OpenAI with React frontend for managing system prompts.

## 🎬 Demo Video

See the AI Code Reviewer in action:

https://github.com/user-attachments/assets/f1a3a6c9-41dd-441f-9e88-4f968f057006

### 🆕 Latest Features Update

See the newest features and improvements:

https://github.com/user-attachments/assets/ccbce9bc-30b1-494a-9efc-146f1473e9d9

**New features showcased:**
1. **🧠 Train AI** - Interactive testing and feedback system for prompt improvement
2. **🤝 Better Collaboration** - Enhanced real-time code review capabilities
3. **⚡ Better Workflow Management** - Streamlined development processes
4. **🤖 Support for GitHub App** - Enterprise-grade authentication and security
5. **🎛️ Repository Filters** - Advanced filtering and repository management

## ✨ Features

- **🔍 AI Code Analysis**: Automated reviews using Azure OpenAI
- **🌐 Web Interface**: React + TypeScript frontend
- **🎯 Multi-Language Support**: C#, Java, VB.NET, JavaScript, TypeScript, React, T-SQL
- **⚙️ System Prompts Management**: Customize AI behavior per language
- **� Train AI**: Interactive testing and feedback system to iteratively improve AI prompts through code validation, review testing, and prompt refinement based on feedback
- **�🤝 Real-Time Collaboration**: Live code review sessions with multiple participants
- **💬 Live Comments**: Real-time commenting and discussions on code lines
- **👥 User Presence**: See who's actively reviewing with live cursor tracking
- **� GitHub App Integration**: Enterprise-grade authentication with GitHub Apps for secure, scoped repository access
- **🎛️ Repository Filtering**: Flexible pattern-based filtering to control visible repositories (application-level, future: user preferences)
- **🔗 Integrations**: GitHub, Jira, Slack

## 🚀 Quick Start

### Prerequisites

- .NET 9.0 SDK
- Node.js 18+
- Azure OpenAI Service access

### Setup

1. **Environment variables** (create `.env` file):

   ```env
   AZURE_OPENAI_ENDPOINT=your-endpoint
   AZURE_OPENAI_API_KEY=your-api-key
   AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4
   GITHUB_TOKEN=your-github-token
   ```

2. **Start backend**:

   ```bash
   dotnet run --launch-profile https --environment Development
   ```

3. **Start frontend**:

   ```bash
   cd client-app
   npm install
   npm run dev
   ```

4. **Access**: Frontend at `http://localhost:5174`, API at `https://localhost:7001`

## ⚙️ Configuration

### Token Metrics Display

Control the visibility of token usage and cost information in code review results by editing `appsettings.json`:

```json
"CodeReview": {
  "MaxFilesToReview": 3,
  "MaxIssuesInSummary": 3,
  "ShowTokenMetrics": true  // Set to false to hide token/cost info
}
```

When enabled, token usage and estimated cost are displayed at the top of review summaries for:

- 🧠 Train AI reviews
- 📝 Commit reviews
- 🔀 Pull request reviews

## ⚙️ Usage

1. Open web interface
2. Connect GitHub repositories in **Repositories** tab
3. Browse commits and pull requests
4. Customize AI prompts in **System Prompts** tab
5. Run automated code reviews
6. View token usage and cost metrics (configurable via `appsettings.json`)

## 🤝 Real-Time Collaboration

Create collaborative review sessions where multiple team members can review code simultaneously:

- **👥 Live Presence**: See who's currently reviewing with real-time participant lists
- **🖱️ Cursor Tracking**: Track where team members are looking in the code
- **💬 Live Comments**: Add comments on specific code lines with instant synchronization
- **🎨 Comment Types**: General, Suggestion, Question, Issue with threaded discussions
- **🔄 WebSocket Communication**: Built with SignalR at `/collaborationHub` endpoint

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.

## 🤝 Contributing

1. Fork the repository
2. Create feature branch
3. Make changes and add tests
4. Submit pull request

## ⚠️ Disclaimer

This project was developed independently on personal equipment and in personal time.  
It is not affiliated with, endorsed by, or derived from the intellectual property of EPAM Systems or any of its clients.  
All examples, configurations, and data are generic and intended solely for demonstration and educational purposes.
