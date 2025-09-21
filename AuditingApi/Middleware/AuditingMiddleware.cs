using AuditingApi.Models;
using AuditingApi.Services;
using System.Diagnostics;
using System.Text;

namespace AuditingApi.Middleware;

public class AuditingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAuditMemoryStore _auditStore;
    private readonly ILogger<AuditingMiddleware> _logger;

    public AuditingMiddleware(RequestDelegate next, IAuditMemoryStore auditStore, ILogger<AuditingMiddleware> logger)
    {
        _next = next;
        _auditStore = auditStore;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var auditEntry = new AuditEntry
        {
            Method = context.Request.Method,
            Path = context.Request.Path,
            QueryString = context.Request.QueryString.ToString(),
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString()
        };

        // Capture request body
        string? requestBody = null;
        if (context.Request.ContentLength > 0 && 
            (context.Request.Method == "POST" || context.Request.Method == "PUT"))
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }
        auditEntry.RequestBody = requestBody;

        // Capture response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during request processing");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            auditEntry.ResponseTime = stopwatch.ElapsedMilliseconds;
            auditEntry.StatusCode = context.Response.StatusCode;

            // Capture response body
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(responseBody).ReadToEndAsync();
            auditEntry.ResponseBody = responseText;

            // Copy response back to original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);

            // Store audit entry
            _auditStore.AddAuditEntry(auditEntry);
        }
    }
}
