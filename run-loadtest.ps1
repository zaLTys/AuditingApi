# PowerShell script to run the load test
Write-Host "Starting Load Test..." -ForegroundColor Green

# Check if the API is running
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/sample" -Method GET -TimeoutSec 5
    Write-Host "API is responding. Starting load test..." -ForegroundColor Green
} catch {
    Write-Host "API is not responding. Please make sure the API is running first." -ForegroundColor Red
    Write-Host "Run: docker-compose up -d" -ForegroundColor Yellow
    exit 1
}

# Navigate to LoadTest directory and run
Set-Location LoadTest
dotnet run

Write-Host "Load test completed!" -ForegroundColor Green
