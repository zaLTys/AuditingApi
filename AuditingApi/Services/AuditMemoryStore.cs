using AuditingApi.Models;
using System.Collections.Concurrent;

namespace AuditingApi.Services;

public interface IAuditMemoryStore
{
    void AddAuditEntry(AuditEntry entry);
    IEnumerable<AuditEntry> GetAndClearEntries();
    IEnumerable<AuditEntry> GetAndClearEntries(int batchSize);
    int Count { get; }
}

public class AuditMemoryStore : IAuditMemoryStore
{
    private readonly ConcurrentQueue<AuditEntry> _auditEntries = new();
    private volatile int _count = 0;

    public void AddAuditEntry(AuditEntry entry)
    {
        _auditEntries.Enqueue(entry);
        Interlocked.Increment(ref _count);
    }

    public IEnumerable<AuditEntry> GetAndClearEntries()
    {
        var entries = new List<AuditEntry>();
        
        while (_auditEntries.TryDequeue(out var entry))
        {
            entries.Add(entry);
            Interlocked.Decrement(ref _count);
        }
        
        return entries;
    }

    public IEnumerable<AuditEntry> GetAndClearEntries(int batchSize)
    {
        var entries = new List<AuditEntry>();
        
        for (int i = 0; i < batchSize && _auditEntries.TryDequeue(out var entry); i++)
        {
            entries.Add(entry);
            Interlocked.Decrement(ref _count);
        }
        
        return entries;
    }

    public int Count => _count;
}
