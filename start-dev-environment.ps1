# Start Development Environment Script
# This script starts only the infrastructure services (Kafka, MongoDB, etc.)
# allowing you to run the API locally with debugger support

Write-Host "Starting development infrastructure services..." -ForegroundColor Green

# Start infrastructure services
docker-compose -f docker-compose.dev.yml up -d

# Wait for services to start
Write-Host "Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep 15

# Create Kafka topic if it doesn't exist
Write-Host "Creating Kafka topic..." -ForegroundColor Yellow
docker exec kafka kafka-topics --bootstrap-server localhost:9092 --create --topic audit-events --partitions 1 --replication-factor 1 --if-not-exists

Write-Host "`nDevelopment environment is ready!" -ForegroundColor Green
Write-Host "Services running:" -ForegroundColor Cyan
Write-Host "  - Kafka: localhost:9092" -ForegroundColor White
Write-Host "  - Kafka UI: http://localhost:8080" -ForegroundColor White
Write-Host "  - MongoDB: localhost:27017" -ForegroundColor White
Write-Host "  - Mongo Express: http://localhost:8081 (admin:pass)" -ForegroundColor White
Write-Host "`nNow you can:" -ForegroundColor Cyan
Write-Host "  1. Open AuditingApi project in Visual Studio/VS Code" -ForegroundColor White
Write-Host "  2. Set ASPNETCORE_ENVIRONMENT=Local" -ForegroundColor White
Write-Host "  3. Start debugging (F5)" -ForegroundColor White
Write-Host "`nTo stop services: docker-compose -f docker-compose.dev.yml down" -ForegroundColor Yellow
