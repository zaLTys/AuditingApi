using Microsoft.AspNetCore.Mvc;
using AuditingApi.Models;
using AuditingApi.Services;

namespace AuditingApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuditingController : ControllerBase
{
    private readonly IMongoDbService _mongoDbService;
    private readonly ILogger<AuditingController> _logger;
    private readonly IConfiguration _configuration;

    public AuditingController(
        IMongoDbService mongoDbService,
        ILogger<AuditingController> logger,
        IConfiguration configuration)
    {
        _mongoDbService = mongoDbService;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<AuditEntry>>> GetAuditEntries(
        [FromQuery] int page = 1,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? method = null,
        [FromQuery] int? statusCode = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            // Validate and apply configuration limits
            var defaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize", 20);
            var maxPageSize = _configuration.GetValue<int>("Pagination:MaxPageSize", 100);
            
            var effectivePageSize = pageSize ?? defaultPageSize;
            effectivePageSize = Math.Min(effectivePageSize, maxPageSize);
            effectivePageSize = Math.Max(effectivePageSize, 1);

            if (page < 1)
                page = 1;

            var request = new PaginationRequest
            {
                Page = page,
                PageSize = effectivePageSize,
                SearchTerm = searchTerm,
                Method = method?.ToUpperInvariant(),
                StatusCode = statusCode,
                StartDate = startDate,
                EndDate = endDate
            };

            _logger.LogDebug("Retrieving audit entries: Page={Page}, PageSize={PageSize}, SearchTerm={SearchTerm}, Method={Method}, StatusCode={StatusCode}",
                page, effectivePageSize, searchTerm, method, statusCode);

            var result = await _mongoDbService.GetAuditEntriesAsync(request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit entries");
            return StatusCode(500, new { error = "An error occurred while retrieving audit entries" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AuditEntry>> GetAuditEntry(string id)
    {
        try
        {
            var request = new PaginationRequest
            {
                Page = 1,
                PageSize = 1000 // Large size to search through entries
            };

            var result = await _mongoDbService.GetAuditEntriesAsync(request);
            var entry = result.Data.FirstOrDefault(e => e.Id == id);

            if (entry == null)
            {
                return NotFound(new { error = $"Audit entry with ID '{id}' not found" });
            }

            return Ok(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit entry {Id}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the audit entry" });
        }
    }

    /// <summary>
    /// Gets audit statistics summary
    /// </summary>
    /// <returns>Basic statistics about audit entries</returns>
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetAuditStats()
    {
        try
        {
            // Get first page to extract total count
            var request = new PaginationRequest { Page = 1, PageSize = 1 };
            var result = await _mongoDbService.GetAuditEntriesAsync(request);

            var stats = new
            {
                TotalEntries = result.Metadata.TotalCount,
                LastUpdated = DateTime.UtcNow
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit statistics");
            return StatusCode(500, new { error = "An error occurred while retrieving audit statistics" });
        }
    }
}
