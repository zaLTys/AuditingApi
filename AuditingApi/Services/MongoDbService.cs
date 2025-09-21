using MongoDB.Driver;
using AuditingApi.Models;

namespace AuditingApi.Services;

public interface IMongoDbService
{
    Task InsertAuditEntryAsync(AuditEntry auditEntry);
}

public class MongoDbService : IMongoDbService
{
    private readonly IMongoCollection<AuditEntry> _auditCollection;
    private readonly ILogger<MongoDbService> _logger;

    public MongoDbService(IConfiguration configuration, ILogger<MongoDbService> logger)
    {
        _logger = logger;
        
        var connectionString = configuration["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017";
        var databaseName = configuration["MongoDB:DatabaseName"] ?? "AuditingDb";
        var collectionName = configuration["MongoDB:CollectionName"] ?? "AuditEntries";

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _auditCollection = database.GetCollection<AuditEntry>(collectionName);
    }

    public async Task InsertAuditEntryAsync(AuditEntry auditEntry)
    {
        try
        {
            await _auditCollection.InsertOneAsync(auditEntry);
            _logger.LogDebug("Inserted audit entry {Id} into MongoDB", auditEntry.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert audit entry {Id} into MongoDB", auditEntry.Id);
            throw;
        }
    }
}
