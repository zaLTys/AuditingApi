using Confluent.Kafka;
using AuditingApi.Models;
using System.Text.Json;

namespace AuditingApi.Services;

public interface IKafkaProducerService
{
    Task ProduceAsync(AuditEntry auditEntry);
    Task ProduceBatchAsync(IEnumerable<AuditEntry> auditEntries);
}

public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private IProducer<string, string>? _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _topicName;

    public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        _configuration = configuration;
        _topicName = configuration["Kafka:TopicName"] ?? "audit-events";
    }

    public async Task ProduceAsync(AuditEntry auditEntry)
    {
        try
        {
            // Create producer if not already created
            if (_producer == null)
            {
                var config = new ProducerConfig
                {
                    BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
                    ClientId = "auditing-api-producer"
                };

                _producer = new ProducerBuilder<string, string>(config).Build();
                _logger.LogInformation("Kafka producer created and connected");
            }

            var message = new Message<string, string>
            {
                Key = auditEntry.Id,
                Value = JsonSerializer.Serialize(auditEntry)
            };

            var result = await _producer.ProduceAsync(_topicName, message);
            _logger.LogDebug("Produced audit entry {Id} to Kafka at offset {Offset}", 
                auditEntry.Id, result.Offset);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to produce audit entry {Id} to Kafka - will retry later", auditEntry.Id);
            // Dispose producer on error to force reconnection
            _producer?.Dispose();
            _producer = null;
            throw;
        }
    }

    public async Task ProduceBatchAsync(IEnumerable<AuditEntry> auditEntries)
    {
        var entriesList = auditEntries.ToList();
        if (entriesList.Count == 0)
            return;

        try
        {
            // Create producer if not already created
            if (_producer == null)
            {
                var config = new ProducerConfig
                {
                    BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
                    ClientId = "auditing-api-producer",
                    // Optimize for batch processing
                    BatchSize = 16384,
                    LingerMs = 5,
                    CompressionType = CompressionType.Snappy
                };

                _producer = new ProducerBuilder<string, string>(config).Build();
                _logger.LogInformation("Kafka producer created and connected for batch processing");
            }

            var tasks = new List<Task<DeliveryResult<string, string>>>();

            foreach (var auditEntry in entriesList)
            {
                var message = new Message<string, string>
                {
                    Key = auditEntry.Id,
                    Value = JsonSerializer.Serialize(auditEntry)
                };

                // ProduceAsync is non-blocking and batches messages internally
                var task = _producer.ProduceAsync(_topicName, message);
                tasks.Add(task);
            }

            // Wait for all messages to be delivered
            var results = await Task.WhenAll(tasks);
            
            _logger.LogDebug("Produced batch of {Count} audit entries to Kafka", entriesList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to produce batch of {Count} audit entries to Kafka - will retry later", entriesList.Count);
            // Dispose producer on error to force reconnection
            _producer?.Dispose();
            _producer = null;
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}
