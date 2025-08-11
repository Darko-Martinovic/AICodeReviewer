$headers = @{
    'Content-Type' = 'application/json'
}

$body = '"This is a test update from the JIRA controller to verify ticket updates are working correctly."'

try {
    Write-Host "Testing JIRA ticket update for OPS-1..." -ForegroundColor Yellow
    
    $response = Invoke-RestMethod -Uri "https://localhost:7001/api/JiraTest/update-ticket/OPS-1" -Method POST -Body $body -Headers $headers
    
    Write-Host "✅ SUCCESS!" -ForegroundColor Green
    Write-Host "Ticket Key: $($response.ticketKey)" -ForegroundColor White
    Write-Host "Message: $($response.message)" -ForegroundColor White
    Write-Host "JIRA URL: $($response.jiraUrl)" -ForegroundColor Cyan
    Write-Host "Timestamp: $($response.timestamp)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ ERROR!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}
