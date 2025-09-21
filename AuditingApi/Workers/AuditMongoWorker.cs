using Confluent.Kafka;
using AuditingApi.Services;
using AuditingApi.Models;
using System.Text.Json;

namespace AuditingApi.Workers;

public class AuditMongoWorker : BackgroundService
{
    private readonly IMongoDbService _mongoDbService;
    private readonly ILogger<AuditMongoWorker> _logger;
    private readonly IConfiguration _configuration;
    private IConsumer<string, string>? _consumer;

    public AuditMongoWorker(
        IMongoDbService mongoDbService,
        ILogger<AuditMongoWorker> logger,
        IConfiguration configuration)
    {
        _mongoDbService = mongoDbService;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var topicName = _configuration["Kafka:TopicName"] ?? "audit-events";
        
        _logger.LogInformation("Audit MongoDB Worker starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Try to create consumer if not already created
                if (_consumer == null)
                {
                    var config = new ConsumerConfig
                    {
                        BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
                        GroupId = "audit-mongo-consumer",
                        AutoOffsetReset = AutoOffsetReset.Earliest,
                        EnableAutoCommit = true
                    };

                    _consumer = new ConsumerBuilder<string, string>(config).Build();
                    _consumer.Subscribe(topicName);
                    _logger.LogInformation("Audit MongoDB Worker connected to Kafka, subscribed to topic: {TopicName}", topicName);
                }

                var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));
                
                if (consumeResult != null)
                {
                    var auditEntry = JsonSerializer.Deserialize<AuditEntry>(consumeResult.Message.Value);
                    if (auditEntry != null)
                    {
                        await _mongoDbService.InsertAuditEntryAsync(auditEntry);
                        _logger.LogDebug("Processed audit entry {Id} from Kafka to MongoDB", auditEntry.Id);
                    }
                }
                await Task.Yield();
            }
            catch (ConsumeException ex)
            {
                _logger.LogWarning(ex, "Kafka consume error - will retry");
                await Task.Delay(5000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in Audit MongoDB Worker - will retry");
                // Dispose consumer on error to force reconnection
                _consumer?.Close();
                _consumer?.Dispose();
                _consumer = null;
                await Task.Delay(10000, stoppingToken);
            }
        }
    }

    public override void Dispose()
    {
        _consumer?.Close();
        _consumer?.Dispose();
        base.Dispose();
    }
}
