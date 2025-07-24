@echo off
echo Starting AI Code Reviewer in Production Mode...
echo Using full workflow configuration
dotnet run --launch-profile https --environment Production
