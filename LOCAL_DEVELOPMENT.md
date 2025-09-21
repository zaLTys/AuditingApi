# Local Development Setup

This guide explains how to run the AuditingApi locally with debugger support while using containerized services for Kafka and MongoDB.

## Prerequisites

- .NET 9.0 SDK
- Docker Desktop
- Visual Studio 2022 or VS Code with C# extension

## Quick Start

### Option 1: Using PowerShell Script (Recommended)

1. **Start Infrastructure Services:**
   ```powershell
   .\start-dev-environment.ps1
   ```

2. **Run API Locally:**
   - **Visual Studio:** Open `AuditingApi.sln`, set environment to `Local`, press F5
   - **VS Code:** Open workspace, press F5 (uses the configured launch profile)
   - **Command Line:**
     ```powershell
     cd AuditingApi
     $env:ASPNETCORE_ENVIRONMENT="Local"
     dotnet run
     ```

### Option 2: Manual Setup

1. **Start Infrastructure Services:**
   ```powershell
   docker-compose -f docker-compose.dev.yml up -d
   ```

2. **Create Kafka Topic:**
   ```powershell
   docker exec kafka kafka-topics --bootstrap-server localhost:9092 --create --topic audit-events --partitions 1 --replication-factor 1 --if-not-exists
   ```

3. **Run API with Local Environment:**
   ```powershell
   cd AuditingApi
   $env:ASPNETCORE_ENVIRONMENT="Local"
   dotnet run
   ```

## Available Services

When running locally, you can access:

- **API with Swagger:** http://localhost:5000/swagger
- **Kafka UI:** http://localhost:8080
- **Mongo Express:** http://localhost:8081 (username: `admin`, password: `pass`)
- **MongoDB Direct:** mongodb://admin:password@localhost:27017/AuditingDb

## Configuration

The `appsettings.Local.json` file configures the API to connect to:
- Kafka at `localhost:9092`
- MongoDB at `localhost:27017`

## Debugging Features

### Visual Studio
- Set breakpoints in your code
- Use the debugger to step through API requests
- Watch variables and inspect the call stack
- Hot reload for code changes

### VS Code
- Full IntelliSense support
- Integrated terminal
- Git integration
- Extensions for enhanced debugging

## Testing the Setup

1. **Test API Health:**
   ```powershell
   Invoke-RestMethod -Uri "http://localhost:5000/api/sample" -Method GET
   ```

2. **Test Swagger UI:**
   Open http://localhost:5000/swagger in your browser

3. **Monitor Kafka Messages:**
   - Open Kafka UI at http://localhost:8080
   - Navigate to Topics → audit-events to see messages

4. **View MongoDB Data:**
   - Open Mongo Express at http://localhost:8081
   - Login with admin/pass
   - Navigate to AuditingDb → AuditEntries collection

## Development Workflow

1. Make changes to your C# code
2. Set breakpoints as needed
3. Debug/run the application (F5)
4. Test API endpoints via Swagger or Postman
5. Monitor Kafka and MongoDB through their UIs
6. Stop debugging and repeat

## Stopping Services

```powershell
# Stop infrastructure services
docker-compose -f docker-compose.dev.yml down

# Or stop all containers
docker stop $(docker ps -q)
```

## Troubleshooting

### API Won't Start
- Ensure infrastructure services are running: `docker-compose -f docker-compose.dev.yml ps`
- Check if ports 5000, 8080, 8081, 9092, 27017 are available
- Verify environment is set to "Local"

### Kafka Connection Issues
- Ensure Kafka topic exists: `docker exec kafka kafka-topics --bootstrap-server localhost:9092 --list`
- Check Kafka UI at http://localhost:8080

### MongoDB Connection Issues
- Test connection: `docker exec mongodb mongo --eval "db.adminCommand('ismaster')"`
- Check Mongo Express at http://localhost:8081

### Can't Access Services
- Verify Docker containers are running: `docker ps`
- Check Windows firewall settings
- Ensure no other services are using the same ports

## Environment Variables

For command line execution, set these environment variables:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Local"
$env:ASPNETCORE_URLS="http://localhost:5000"
```

## IDE-Specific Setup

### Visual Studio 2022
1. Open `AuditingApi.sln`
2. Right-click project → Properties → Debug → General
3. Set Environment Variables: `ASPNETCORE_ENVIRONMENT=Local`
4. Set App URL: `http://localhost:5000`

### VS Code
The workspace is pre-configured with:
- Launch configuration for debugging
- Tasks for building and running
- Environment variables automatically set

Just press F5 to start debugging!
