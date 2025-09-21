#!/bin/bash

# Bash script to run the load test
echo "Starting Load Test..."

# Check if the API is running
if curl -f -s "http://localhost:5000/api/sample" > /dev/null; then
    echo "API is responding. Starting load test..."
else
    echo "API is not responding. Please make sure the API is running first."
    echo "Run: docker-compose up -d"
    exit 1
fi

# Navigate to LoadTest directory and run
cd LoadTest
dotnet run

echo "Load test completed!"
