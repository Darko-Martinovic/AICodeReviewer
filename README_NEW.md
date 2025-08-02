# 🤖 AI Code Reviewer

AI-powered code review tool using Azure OpenAI with React frontend for managing system prompts.

## ✨ Features

- **🔍 AI Code Analysis**: Automated reviews using Azure OpenAI
- **🌐 Web Interface**: React + TypeScript frontend
- **🎯 Multi-Language Support**: C#, VB.NET, JavaScript, TypeScript, React, T-SQL
- **⚙️ System Prompts Management**: Customize AI behavior per language
- **🔗 Integrations**: GitHub, Jira, Microsoft Teams

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

4. **Access**: Frontend at `http://localhost:5173`, API at `https://localhost:7001`

## ⚙️ Usage

1. Open web interface
2. Connect GitHub repositories in **Repositories** tab
3. Browse commits and pull requests
4. Customize AI prompts in **System Prompts** tab
5. Run automated code reviews

## 🤝 Contributing

1. Fork the repository
2. Create feature branch
3. Make changes and add tests
4. Submit pull request

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.
