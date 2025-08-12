#!/usr/bin/env pwsh

# Test GET repositories endpoint  
$url = "https://localhost:7001/api/repositories"

Write-Host "Testing GET URL: $url"

try {
    $response = Invoke-RestMethod -Uri $url -Method GET -UseBasicParsing
    Write-Host "SUCCESS: $($response | ConvertTo-Json)"
} catch {
    Write-Host "ERROR: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        Write-Host "Status: $($_.Exception.Response.StatusCode)"
    }
}

# Test set repository with correct format
$setUrl = "https://localhost:7001/api/repositories/set"
$body = @{
    owner = "Darko-Martinovic"
    repository = "AICodeReviewer"
} | ConvertTo-Json

Write-Host "`nTesting SET URL: $setUrl"
Write-Host "Body: $body"

try {
    $headers = @{
        'Content-Type' = 'application/json'
    }
    
    $response = Invoke-RestMethod -Uri $setUrl -Method POST -Body $body -Headers $headers -UseBasicParsing
    Write-Host "SUCCESS: $($response | ConvertTo-Json)"
} catch {
    Write-Host "ERROR: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        Write-Host "Status: $($_.Exception.Response.StatusCode)"
    }
}
