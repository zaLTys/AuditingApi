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
        
        _logger.LogInformation("Audit Kafka Worker started with interval {IntervalMs}ms", intervalMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var entries = _auditStore.GetAndClearEntries();
                var entriesList = entries.ToList();

                if (entriesList.Count > 0)
                {
                    _logger.LogDebug("Processing {Count} audit entries", entriesList.Count);

                    foreach (var entry in entriesList)
                    {
                        await _kafkaProducer.ProduceAsync(entry);
                    }

                    _logger.LogInformation("Successfully sent {Count} audit entries to Kafka", entriesList.Count);
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
