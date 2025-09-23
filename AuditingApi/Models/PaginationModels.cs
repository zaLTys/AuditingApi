namespace AuditingApi.Models;

public class PaginationRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public string? Method { get; set; }
    public int? StatusCode { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class PaginatedResponse<T>
{
    public IEnumerable<T> Data { get; set; } = new List<T>();
    public PaginationMetadata Metadata { get; set; } = new();
}

public class PaginationMetadata
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public long TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}
