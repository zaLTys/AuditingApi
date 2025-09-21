using AuditingApi.Services;

namespace AuditingApi.Workers;

public class AuditKafkaWorker : BackgroundService
{
    private readonly IAuditMemoryStore _auditStore;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly ILogger<AuditKafkaWorker> _logger;
    private readonly IConfiguration _configuration;

    public AuditKafkaWorker(
        IAuditMemoryStore auditStore,
        IKafkaProducerService kafkaProducer,
        ILogger<AuditKafkaWorker> logger,
        IConfiguration configuration)
    {
        _auditStore = auditStore;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMs = _configuration.GetValue<int>("Workers:KafkaWorkerIntervalMs", 1000);
        var batchSize = _configuration.GetValue<int>("Workers:KafkaBatchSize", 100);
        
        _logger.LogInformation("Audit Kafka Worker started with interval {IntervalMs}ms and batch size {BatchSize}", intervalMs, batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var totalProcessed = 0;
                
                // Process entries in batches until queue is empty or we've processed enough
                while (!stoppingToken.IsCancellationRequested)
                {
                    var entries = _auditStore.GetAndClearEntries(batchSize);
                    var entriesList = entries.ToList();

                    if (entriesList.Count == 0)
                        break; // No more entries to process

                    _logger.LogDebug("Processing batch of {Count} audit entries", entriesList.Count);

                    await _kafkaProducer.ProduceBatchAsync(entriesList);
                    totalProcessed += entriesList.Count;

                    _logger.LogDebug("Successfully sent batch of {Count} audit entries to Kafka", entriesList.Count);
                    
                    // If we got less than the batch size, we've likely emptied the queue
                    if (entriesList.Count < batchSize)
                        break;
                }

                if (totalProcessed > 0)
                {
                    _logger.LogInformation("Successfully sent {TotalCount} audit entries to Kafka in batches", totalProcessed);
                }

                await Task.Delay(intervalMs, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Audit Kafka Worker");
                await Task.Delay(5000, stoppingToken); // Wait longer on error
            }
        }
    }
}
