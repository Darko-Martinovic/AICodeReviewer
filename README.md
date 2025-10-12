# 🤖 AI Code Reviewer

AI-powered code review tool using Azure OpenAI with React frontend for managing system prompts.

## ✨ Features

- **🔍 AI Code Analysis**: Automated reviews using Azure OpenAI
- **🌐 Web Interface**: React + TypeScript frontend
- **🎯 Multi-Language Support**: C#, VB.NET, JavaScript, TypeScript, React, T-SQL
- **⚙️ System Prompts Management**: Customize AI behavior per language
- **🤝 Real-Time Collaboration**: Live code review sessions with multiple participants
- **💬 Live Comments**: Real-time commenting and discussions on code lines
- **👥 User Presence**: See who's actively reviewing with live cursor tracking
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

## 🤝 Real-Time Collaboration

The AI Code Reviewer includes powerful real-time collaboration features for team code reviews:

### **Live Review Sessions**

- Create collaborative review sessions for any commit or pull request
- Multiple team members can join the same review session simultaneously
- Real-time synchronization of all review activities

### **Interactive Features**

- **👥 Live User Presence**: See who's currently reviewing with real-time participant lists
- **🖱️ Cursor Tracking**: Track where team members are looking in the code
- **💬 Live Comments**: Add comments on specific code lines that appear instantly for all participants
- **🎨 User Colors**: Each participant gets a unique color for easy identification
- **🔄 Real-Time Updates**: All actions (comments, cursor movements, joins/leaves) sync in real-time

### **Comment System**

- Add comments directly on code lines during live sessions
- Comment types: General, Suggestion, Question, Issue
- Reply to comments with threaded discussions
- Mark comments as resolved when addressed

### **Session Management**

- Sessions are automatically created for commits and pull requests
- Participants can join/leave sessions at any time
- Session cleanup removes inactive sessions automatically
- Session history is maintained for reference

### **Technical Implementation**

- Built with **SignalR** for WebSocket-based real-time communication
- RESTful API endpoints for session management
- In-memory session storage for fast real-time updates
- WebSocket endpoint: `/collaborationHub`

## 🤝 Contributing

1. Fork the repository
2. Create feature branch
3. Make changes and add tests
4. Submit pull request

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.
