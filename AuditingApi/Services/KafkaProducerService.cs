using Confluent.Kafka;
using AuditingApi.Models;
using System.Text.Json;

namespace AuditingApi.Services;

public interface IKafkaProducerService
{
    Task ProduceAsync(AuditEntry auditEntry);
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

    public void Dispose()
    {
        _producer?.Dispose();
    }
}
