#!/usr/bin/env pwsh

# Test the repository set endpoint
$url = "https://localhost:7001/api/repositories/current"
$body = @{
    owner = "Darko-Martinovic"
    repository = "AICodeReviewer"
} | ConvertTo-Json

Write-Host "Testing URL: $url"
Write-Host "Body: $body"

try {
    $headers = @{
        'Content-Type' = 'application/json'
    }
    
    # For PowerShell 5.1, use -UseBasicParsing instead of -SkipCertificateCheck
    $response = Invoke-RestMethod -Uri $url -Method POST -Body $body -Headers $headers -UseBasicParsing
    Write-Host "SUCCESS: $response"
} catch {
    Write-Host "ERROR: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        Write-Host "Status: $($_.Exception.Response.StatusCode)"
    }
}
