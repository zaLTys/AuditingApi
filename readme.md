# Auditing Middleware Proof of Concept

This is a comprehensive auditing application built with .NET 9, featuring request auditing through middleware, Kafka message queuing, and MongoDB storage.

## Architecture

1. **API Layer**: .NET 9 Web API with GET/POST/PUT endpoints
2. **Auditing Middleware**: Captures all HTTP requests/responses and stores them temporarily in memory
3. **Kafka Producer Worker**: Background service that reads from memory and sends audit entries to Kafka
4. **Kafka Consumer Worker**: Background service that reads from Kafka and stores audit entries in MongoDB
5. **Load Testing**: NBomber-based load testing to verify performance under high load

## Components

### API Endpoints
- `GET /api/sample` - Get all sample data
- `GET /api/sample/{id}` - Get sample data by ID
- `POST /api/sample` - Create new sample data
- `PUT /api/sample/{id}` - Update existing sample data
- `DELETE /api/sample/{id}` - Delete sample data

### Infrastructure
- **Kafka**: Message broker for audit events
- **MongoDB**: Persistent storage for audit entries
- **Kafka UI**: Web interface for Kafka management (http://localhost:8080)
- **Mongo Express**: Web interface for MongoDB management (http://localhost:8081)

## Quick Start

### Prerequisites
- Docker and Docker Compose
- .NET 9 SDK (for load testing)

### 1. Start the Infrastructure
```bash
docker-compose up -d
```

This will start:
- Zookeeper (port 2181)
- Kafka (port 9092)
- Kafka UI (port 8080)
- MongoDB (port 27017)
- Mongo Express (port 8081)
- Auditing API (ports 5000/5001)

### 2. Verify the API is Running
```bash
curl http://localhost:5000/api/sample
```

### 3. Run Load Test
**Windows:**
```powershell
.\run-loadtest.ps1
```

**Linux/Mac:**
```bash
./run-loadtest.sh
```

Or manually:
```bash
cd LoadTest
dotnet run
```

## Configuration

### API Configuration (appsettings.json)
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "TopicName": "audit-events"
  },
  "MongoDB": {
    "ConnectionString": "mongodb://admin:password@mongodb:27017/AuditingDb?authSource=admin",
    "DatabaseName": "AuditingDb",
    "CollectionName": "AuditEntries"
  },
  "Workers": {
    "KafkaWorkerIntervalMs": 1000
  }
}
```

### Load Test Configuration (LoadTest/appsettings.json)
```json
{
  "LoadTest": {
    "BaseUrl": "http://localhost:5000",
    "DurationMinutes": 2,
    "RatePerSecond": 100
  }
}
```

## Monitoring

1. **Kafka UI**: http://localhost:8080
   - View topics, messages, and consumer groups
   - Monitor audit-events topic

2. **Mongo Express**: http://localhost:8081
   - Browse AuditingDb database
   - View AuditEntries collection
   - Query audit data

3. **API Swagger**: http://localhost:5000/swagger
   - Test API endpoints
   - View API documentation

## Data Flow

1. HTTP request hits the API
2. Auditing middleware captures request/response details
3. Audit entry stored temporarily in memory (thread-safe concurrent queue)
4. Background worker reads from memory every 1 second (configurable)
5. Worker sends audit entries to Kafka topic "audit-events"
6. Another background worker consumes from Kafka
7. Consumer stores audit entries in MongoDB

## Performance Considerations

- **Memory Store**: Uses `ConcurrentQueue<T>` for thread-safe operations
- **Batch Processing**: Workers process multiple entries at once
- **Configurable Intervals**: Kafka worker interval can be adjusted
- **Connection Pooling**: MongoDB driver handles connection pooling automatically
- **Async Operations**: All I/O operations are asynchronous

## Audit Entry Schema

```json
{
  "Id": "guid",
  "Timestamp": "2023-12-01T10:30:00Z",
  "Method": "POST",
  "Path": "/api/sample",
  "QueryString": "?param=value",
  "RequestBody": "{ \"name\": \"test\" }",
  "ResponseBody": "{ \"id\": 1, \"name\": \"test\" }",
  "StatusCode": 201,
  "ResponseTime": 45,
  "UserAgent": "Mozilla/5.0...",
  "RemoteIpAddress": "192.168.1.100"
}
```

## Load Testing

The load test simulates realistic API usage with:
- GET requests (33% of traffic)
- POST requests (33% of traffic) 
- PUT requests (33% of traffic)
- Mixed random operations scenario
- Configurable duration and request rate
- Detailed performance metrics

Default settings: 100 requests/second for 2 minutes = 12,000 total requests

## Troubleshooting

### Common Issues

1. **Kafka Connection Issues**
   - Ensure Kafka is running: `docker-compose ps`
   - Check Kafka logs: `docker-compose logs kafka`

2. **MongoDB Connection Issues**
   - Verify MongoDB is running: `docker-compose ps`
   - Check connection string in appsettings.json

3. **API Not Responding**
   - Check API logs: `docker-compose logs auditing-api`
   - Verify ports are not in use

### Cleanup
```bash
docker-compose down -v
```

This removes all containers and volumes (including data).

## Development

To run locally without Docker:
1. Start Kafka and MongoDB using Docker Compose (comment out the API service)
2. Update connection strings to use localhost
3. Run the API: `dotnet run --project AuditingApi`