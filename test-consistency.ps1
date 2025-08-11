# Test script to verify AI review consistency
param([int]$runs = 3)

Write-Host "Testing AI Code Review Consistency..." -ForegroundColor Green
Write-Host "Running $runs consecutive reviews of the same PR to check for consistent results" -ForegroundColor Yellow
Write-Host ""

$results = @()

for ($i = 1; $i -le $runs; $i++) {
    Write-Host "Test Run #$i" -ForegroundColor Cyan
    try {
        $response = Invoke-RestMethod -Uri "https://localhost:7001/api/pullrequests/review/1" -Method POST
        $issueCount = $response.debugInfo.totalIssuesFound
        $filesReviewed = $response.debugInfo.filesReviewed
        
        Write-Host "  Issues Found: $issueCount" -ForegroundColor White
        Write-Host "  Files Reviewed: $filesReviewed" -ForegroundColor White
        
        $results += @{
            Run = $i
            Issues = $issueCount
            Files = $filesReviewed
        }
        
        Write-Host "  ✅ Completed successfully" -ForegroundColor Green
    }
    catch {
        Write-Host "  ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    if ($i -lt $runs) {
        Write-Host "  Waiting 5 seconds before next test..." -ForegroundColor Gray
        Start-Sleep -Seconds 5
    }
    Write-Host ""
}

Write-Host "SUMMARY:" -ForegroundColor Yellow
Write-Host "========" -ForegroundColor Yellow
foreach ($result in $results) {
    Write-Host "Run $($result.Run): $($result.Issues) issues, $($result.Files) files" -ForegroundColor White
}

# Check consistency
$uniqueIssueCounts = $results | Select-Object -ExpandProperty Issues | Sort-Object -Unique
if ($uniqueIssueCounts.Count -eq 1) {
    Write-Host "✅ CONSISTENT: All runs found the same number of issues ($($uniqueIssueCounts[0]))" -ForegroundColor Green
} else {
    Write-Host "❌ INCONSISTENT: Found varying issue counts: $($uniqueIssueCounts -join ', ')" -ForegroundColor Red
}
