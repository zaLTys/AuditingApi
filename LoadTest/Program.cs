using NBomber.CSharp;
using NBomber.Http.CSharp;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var baseUrl = configuration["LoadTest:BaseUrl"] ?? "http://localhost:5000";
var durationMinutes = configuration.GetValue<int>("LoadTest:DurationMinutes", 2);
var ratePerSecond = configuration.GetValue<int>("LoadTest:RatePerSecond", 50);
var maxConcurrentConnections = configuration.GetValue<int>("LoadTest:MaxConcurrentConnections", 10);

// Configure HttpClient with connection pooling
var httpClientHandler = new HttpClientHandler()
{
    MaxConnectionsPerServer = maxConcurrentConnections
};

var httpClient = new HttpClient(httpClientHandler);

// Sample data for POST/PUT operations
var sampleData = new
{
    Name = "Load Test Item",
    Description = "This is a test item created during load testing"
};

var jsonContent = JsonSerializer.Serialize(sampleData);

// GET scenario
var getScenario = Scenario.Create("get_all_items", async context =>
{
    var request = Http.CreateRequest("GET", $"{baseUrl}/api/sample");
    var response = await Http.Send(httpClient, request);
    
    return response;
})
.WithLoadSimulations(
    Simulation.KeepConstant(copies: ratePerSecond / 3, during: TimeSpan.FromMinutes(durationMinutes))
);

// POST scenario
var postScenario = Scenario.Create("create_item", async context =>
{
    var request = Http.CreateRequest("POST", $"{baseUrl}/api/sample")
                     .WithJsonBody(jsonContent);
    
    var response = await Http.Send(httpClient, request);
    
    return response;
})
.WithLoadSimulations(
    Simulation.KeepConstant(copies: ratePerSecond / 3, during: TimeSpan.FromMinutes(durationMinutes))
);

// PUT scenario
var putScenario = Scenario.Create("update_item", async context =>
{
    var itemId = Random.Shared.Next(1, 100); // Assume items 1-100 exist
    var request = Http.CreateRequest("PUT", $"{baseUrl}/api/sample/{itemId}")
                     .WithJsonBody(jsonContent);
    
    var response = await Http.Send(httpClient, request);
    
    return response;
})
.WithLoadSimulations(
    Simulation.KeepConstant(copies: ratePerSecond / 3, during: TimeSpan.FromMinutes(durationMinutes))
);

// Mixed scenario with random operations
var mixedScenario = Scenario.Create("mixed_operations", async context =>
{
    var operation = Random.Shared.Next(1, 4);
    
    var request = operation switch
    {
        1 => Http.CreateRequest("GET", $"{baseUrl}/api/sample"),
        2 => Http.CreateRequest("POST", $"{baseUrl}/api/sample").WithJsonBody(jsonContent),
        3 => Http.CreateRequest("PUT", $"{baseUrl}/api/sample/{Random.Shared.Next(1, 100)}").WithJsonBody(jsonContent),
        _ => Http.CreateRequest("GET", $"{baseUrl}/api/sample")
    };
    
    var response = await Http.Send(httpClient, request);
    
    return response;
})
.WithLoadSimulations(
    Simulation.KeepConstant(copies: ratePerSecond, during: TimeSpan.FromMinutes(durationMinutes))
);

Console.WriteLine($"Starting load test against: {baseUrl}");
Console.WriteLine($"Duration: {durationMinutes} minutes");
Console.WriteLine($"Rate: {ratePerSecond} requests per second");
Console.WriteLine("Press any key to start...");
Console.ReadKey();

NBomberRunner
    .RegisterScenarios(getScenario, postScenario, putScenario, mixedScenario)
    .Run();

Console.WriteLine("Load test completed. Check the results above.");
Console.WriteLine("Press any key to exit...");
Console.ReadKey();

// Clean up resources
httpClient.Dispose();
httpClientHandler.Dispose();
