@echo off
echo Starting AI Code Reviewer in Development Mode...
echo Using minimal workflow configuration
dotnet run --launch-profile https --environment Development --WorkflowEngine:DefaultCommitWorkflow="MinimalCommitReview"
