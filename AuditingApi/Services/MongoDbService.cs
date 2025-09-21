using MongoDB.Driver;
using AuditingApi.Models;

namespace AuditingApi.Services;

public interface IMongoDbService
{
    Task InsertAuditEntryAsync(AuditEntry auditEntry);
    Task<PaginatedResponse<AuditEntry>> GetAuditEntriesAsync(PaginationRequest request);
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

    public async Task<PaginatedResponse<AuditEntry>> GetAuditEntriesAsync(PaginationRequest request)
    {
        try
        {
            var filterBuilder = Builders<AuditEntry>.Filter;
            var filter = filterBuilder.Empty;

            // Apply filters
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchFilter = filterBuilder.Or(
                    filterBuilder.Regex(x => x.Path, new MongoDB.Bson.BsonRegularExpression(request.SearchTerm, "i")),
                    filterBuilder.Regex(x => x.QueryString, new MongoDB.Bson.BsonRegularExpression(request.SearchTerm, "i")),
                    filterBuilder.Regex(x => x.RequestBody, new MongoDB.Bson.BsonRegularExpression(request.SearchTerm, "i")),
                    filterBuilder.Regex(x => x.ResponseBody, new MongoDB.Bson.BsonRegularExpression(request.SearchTerm, "i"))
                );
                filter = filterBuilder.And(filter, searchFilter);
            }

            if (!string.IsNullOrEmpty(request.Method))
            {
                filter = filterBuilder.And(filter, filterBuilder.Eq(x => x.Method, request.Method));
            }

            if (request.StatusCode.HasValue)
            {
                filter = filterBuilder.And(filter, filterBuilder.Eq(x => x.StatusCode, request.StatusCode.Value));
            }

            if (request.StartDate.HasValue)
            {
                filter = filterBuilder.And(filter, filterBuilder.Gte(x => x.Timestamp, request.StartDate.Value));
            }

            if (request.EndDate.HasValue)
            {
                filter = filterBuilder.And(filter, filterBuilder.Lte(x => x.Timestamp, request.EndDate.Value));
            }

            // Get total count
            var totalCount = await _auditCollection.CountDocumentsAsync(filter);

            // Calculate pagination
            var skip = (request.Page - 1) * request.PageSize;
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            // Get paginated data ordered by timestamp descending (latest first)
            var data = await _auditCollection
                .Find(filter)
                .Sort(Builders<AuditEntry>.Sort.Descending(x => x.Timestamp))
                .Skip(skip)
                .Limit(request.PageSize)
                .ToListAsync();

            var metadata = new PaginationMetadata
            {
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPrevious = request.Page > 1,
                HasNext = request.Page < totalPages
            };

            _logger.LogDebug("Retrieved {Count} audit entries (page {Page} of {TotalPages})", 
                data.Count, request.Page, totalPages);

            return new PaginatedResponse<AuditEntry>
            {
                Data = data,
                Metadata = metadata
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve paginated audit entries");
            throw;
        }
    }
}
