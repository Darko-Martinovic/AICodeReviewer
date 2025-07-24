@echo off
echo Starting AI Code Reviewer with Slack Integration...
echo Make sure to set SLACK_WEBHOOK_URL environment variable
dotnet run --launch-profile https --WorkflowEngine:DefaultPullRequestWorkflow="PullRequestReviewWithSlack"
