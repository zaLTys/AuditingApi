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
            // Create producer if not already created (reuse batch configuration)
            if (_producer == null)
            {
                await CreateProducerAsync();
            }

            var message = new Message<string, string>
            {
                Key = auditEntry.Id,
                Value = JsonSerializer.Serialize(auditEntry)
            };

            var result = await _producer!.ProduceAsync(_topicName, message);
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

    private Task CreateProducerAsync()
    {
        var config = new ProducerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            ClientId = "auditing-api-producer",
            // Optimized configuration for both single and batch operations
            BatchSize = 16384,        // Batch size in bytes
            LingerMs = 1000,           // Wait up to 1000ms to fill batch
            CompressionType = CompressionType.Snappy,
            Acks = Acks.Leader,       // Wait for leader acknowledgment only
            EnableIdempotence = true, // Ensure exactly-once semantics
            MaxInFlight = 5,          // Allow up to 5 batches in flight
            MessageSendMaxRetries = 3
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        _logger.LogInformation("Kafka producer created and connected with optimized batch configuration");
        
        return Task.CompletedTask;
    }

    public async Task ProduceBatchAsync(IEnumerable<AuditEntry> auditEntries)
    {
        var entriesList = auditEntries.ToList();
        if (entriesList.Count == 0)
            return;

        try
        {
            // Create producer if not already created (reuse configuration)
            if (_producer == null)
            {
                await CreateProducerAsync();
            }

            var deliveryReports = new List<Task<DeliveryResult<string, string>>>();
            var completionSource = new TaskCompletionSource<bool>();
            var deliveredCount = 0;
            var totalMessages = entriesList.Count;

            // Use synchronous Produce to let Kafka batch messages internally
            foreach (var auditEntry in entriesList)
            {
                var message = new Message<string, string>
                {
                    Key = auditEntry.Id,
                    Value = JsonSerializer.Serialize(auditEntry)
                };

                // Use Produce (not ProduceAsync) to allow Kafka to batch messages
                _producer!.Produce(_topicName, message, (deliveryReport) =>
                {
                    if (deliveryReport.Error.IsError)
                    {
                        _logger.LogWarning("Failed to deliver message {Key}: {Error}", 
                            deliveryReport.Message.Key, deliveryReport.Error.Reason);
                    }
                    else
                    {
                        _logger.LogTrace("Delivered message {Key} to partition {Partition} at offset {Offset}",
                            deliveryReport.Message.Key, deliveryReport.Partition, deliveryReport.Offset);
                    }

                    // Track completion
                    if (Interlocked.Increment(ref deliveredCount) == totalMessages)
                    {
                        completionSource.SetResult(true);
                    }
                });
            }

            // Trigger immediate send of batched messages
            _producer!.Flush(TimeSpan.FromSeconds(10));

            // Wait for all delivery reports
            await completionSource.Task;
            
            _logger.LogDebug("Successfully produced batch of {Count} audit entries to Kafka", entriesList.Count);
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
