using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// Advanced logging service implementing Microsoft's high-performance logging patterns
/// Uses LoggerMessage delegates for optimal performance and structured logging
/// </summary>
public partial class AdvancedLoggingService : IDisposable
{
    private readonly ILogger<AdvancedLoggingService> _logger;
    private readonly ConcurrentQueue<LogEntry> _logQueue = new();
    private readonly Timer _logFlushTimer;
    private readonly SemaphoreSlim _flushSemaphore = new(1, 1);
    private volatile bool _disposed = false;

    // High-performance logging delegates using LoggerMessage pattern
    #region High-Performance Log Messages

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Account operation completed: {Operation} for account {AccountId} on platform {Platform} in {Duration}ms")]
    public static partial void LogAccountOperation(
        ILogger logger,
        string operation,
        string accountId,
        string platform,
        double duration);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Warning,
        Message = "Authentication failed for account {AccountId} on platform {Platform}: {Reason}")]
    public static partial void LogAuthenticationFailure(
        ILogger logger,
        string accountId,
        string platform,
        string reason);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Information,
        Message = "Post published successfully: {PostId} to {Platform} from account {AccountId}")]
    public static partial void LogPostPublished(
        ILogger logger,
        string postId,
        string platform,
        string accountId);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Error,
        Message = "Post publishing failed: {PostId} to {Platform} from account {AccountId} - {ErrorMessage}")]
    public static partial void LogPostPublishingFailed(
        ILogger logger,
        string postId,
        string platform,
        string accountId,
        string errorMessage);

    [LoggerMessage(
        EventId = 2005,
        Level = LogLevel.Debug,
        Message = "OAuth token refresh initiated for account {AccountId} on platform {Platform}")]
    public static partial void LogTokenRefreshStarted(
        ILogger logger,
        string accountId,
        string platform);

    [LoggerMessage(
        EventId = 2006,
        Level = LogLevel.Information,
        Message = "OAuth token refreshed successfully for account {AccountId} on platform {Platform}")]
    public static partial void LogTokenRefreshSuccess(
        ILogger logger,
        string accountId,
        string platform);

    [LoggerMessage(
        EventId = 2007,
        Level = LogLevel.Critical,
        Message = "Data integrity check failed: {IssueCount} issues found - {IssueDetails}")]
    public static partial void LogDataIntegrityFailure(
        ILogger logger,
        int issueCount,
        string issueDetails);

    [LoggerMessage(
        EventId = 2008,
        Level = LogLevel.Information,
        Message = "Backup operation completed: {BackupFileName} - Size: {SizeBytes} bytes, Duration: {Duration}ms")]
    public static partial void LogBackupCompleted(
        ILogger logger,
        string backupFileName,
        long sizeBytes,
        double duration);

    [LoggerMessage(
        EventId = 2009,
        Level = LogLevel.Debug,
        Message = "Performance metric recorded: {MetricName} = {Value} {Unit} at {Timestamp}")]
    public static partial void LogPerformanceMetric(
        ILogger logger,
        string metricName,
        double value,
        string unit,
        DateTime timestamp);

    [LoggerMessage(
        EventId = 2010,
        Level = LogLevel.Warning,
        Message = "Rate limit approaching for platform {Platform}: {CurrentRequests}/{MaxRequests} requests")]
    public static partial void LogRateLimitWarning(
        ILogger logger,
        string platform,
        int currentRequests,
        int maxRequests);

    #endregion

    public AdvancedLoggingService(ILogger<AdvancedLoggingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Initialize log flush timer (every 5 seconds)
        _logFlushTimer = new Timer(FlushLogsCallback, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        
        _logger.LogInformation("Advanced logging service initialized with high-performance patterns");
    }

    /// <summary>
    /// Logs account operations with performance tracking
    /// </summary>
    public void LogAccountOperationWithTiming(string operation, Account account, Action action)
    {
        if (_disposed) return;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            action();
            LogAccountOperation(_logger, operation, account.Id, account.PlatformId.ToString(), stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Account operation failed: {Operation} for account {AccountId} on platform {Platform}", 
                operation, account.Id, account.PlatformId.ToString());
            throw;
        }
    }

    /// <summary>
    /// Logs account operations with performance tracking (async version)
    /// </summary>
    public async Task LogAccountOperationWithTimingAsync(string operation, Account account, Func<Task> asyncAction)
    {
        if (_disposed) return;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await asyncAction().ConfigureAwait(false);
            LogAccountOperation(_logger, operation, account.Id, account.PlatformId.ToString(), stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Async account operation failed: {Operation} for account {AccountId} on platform {Platform}", 
                operation, account.Id, account.PlatformId.ToString());
            throw;
        }
    }

    /// <summary>
    /// Logs authentication events with structured data
    /// </summary>
    public void LogAuthenticationEvent(Account account, bool success, string? reason = null)
    {
        if (_disposed) return;

        if (success)
        {
            _logger.LogInformation("Authentication successful for account {AccountId} on platform {Platform}", 
                account.Id, account.PlatformId.ToString());
        }
        else
        {
            LogAuthenticationFailure(_logger, account.Id, account.PlatformId.ToString(), reason ?? "Unknown reason");
        }

        // Queue for batch processing
        EnqueueLogEntry(new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = success ? LogLevel.Information : LogLevel.Warning,
            Category = "Authentication",
            AccountId = account.Id,
            Platform = account.PlatformId.ToString(),
            Success = success,
            Details = reason
        });
    }

    /// <summary>
    /// Logs post publishing events with comprehensive metadata
    /// </summary>
    public void LogPostEvent(string postId, Account account, bool success, string? errorMessage = null)
    {
        if (_disposed) return;

        if (success)
        {
            LogPostPublished(_logger, postId, account.PlatformId.ToString(), account.Id);
        }
        else
        {
            LogPostPublishingFailed(_logger, postId, account.PlatformId.ToString(), account.Id, errorMessage ?? "Unknown error");
        }

        // Queue for analytics
        EnqueueLogEntry(new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = success ? LogLevel.Information : LogLevel.Error,
            Category = "PostPublishing",
            AccountId = account.Id,
            Platform = account.PlatformId.ToString(),
            Success = success,
            Details = success ? postId : errorMessage,
            PostId = postId
        });
    }

    /// <summary>
    /// Logs OAuth token operations
    /// </summary>
    public void LogOAuthTokenEvent(Account account, string operation, bool success)
    {
        if (_disposed) return;

        switch (operation.ToLowerInvariant())
        {
            case "refresh_start":
                LogTokenRefreshStarted(_logger, account.Id, account.PlatformId.ToString());
                break;
            case "refresh_success":
                LogTokenRefreshSuccess(_logger, account.Id, account.PlatformId.ToString());
                break;
            default:
                _logger.LogDebug("OAuth token operation: {Operation} for account {AccountId} on platform {Platform} - Success: {Success}",
                    operation, account.Id, account.PlatformId.ToString(), success);
                break;
        }
    }

    /// <summary>
    /// Logs system-level events with high performance
    /// </summary>
    public void LogSystemEvent(string eventName, Dictionary<string, object>? properties = null)
    {
        if (_disposed) return;

        using var scope = _logger.BeginScope("SystemEvent: {EventName}", eventName);
        
        if (properties != null && properties.Count > 0)
        {
            _logger.LogInformation("System event occurred: {EventName} with properties: {@Properties}", eventName, properties);
        }
        else
        {
            _logger.LogInformation("System event occurred: {EventName}", eventName);
        }
    }

    /// <summary>
    /// Logs performance metrics efficiently
    /// </summary>
    public void LogPerformanceMetrics(string metricName, double value, string unit = "ms")
    {
        if (_disposed) return;

        // Only log if debug level is enabled to avoid performance overhead
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            LogPerformanceMetric(_logger, metricName, value, unit, DateTime.UtcNow);
        }
    }

    /// <summary>
    /// Logs data integrity issues
    /// </summary>
    public void LogDataIntegrityIssues(int issueCount, IEnumerable<string> issues)
    {
        if (_disposed) return;

        var issueDetails = string.Join("; ", issues);
        LogDataIntegrityFailure(_logger, issueCount, issueDetails);
    }

    /// <summary>
    /// Logs backup operations
    /// </summary>
    public void LogBackupOperation(string fileName, long sizeBytes, TimeSpan duration)
    {
        if (_disposed) return;

        LogBackupCompleted(_logger, fileName, sizeBytes, duration.TotalMilliseconds);
    }

    /// <summary>
    /// Logs rate limiting warnings
    /// </summary>
    public void LogRateLimit(string platform, int currentRequests, int maxRequests)
    {
        if (_disposed) return;

        LogRateLimitWarning(_logger, platform, currentRequests, maxRequests);
    }

    /// <summary>
    /// Enqueue log entry for batch processing
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnqueueLogEntry(LogEntry entry)
    {
        _logQueue.Enqueue(entry);
    }

    /// <summary>
    /// Flush accumulated logs (called by timer)
    /// </summary>
    private async void FlushLogsCallback(object? state)
    {
        if (_disposed || !await _flushSemaphore.WaitAsync(100).ConfigureAwait(false))
            return;

        try
        {
            var processedCount = 0;
            var entries = new List<LogEntry>();

            // Dequeue up to 100 entries at a time
            while (_logQueue.TryDequeue(out var entry) && entries.Count < 100)
            {
                entries.Add(entry);
                processedCount++;
            }

            if (processedCount > 0)
            {
                // Process entries in batches for analytics or external logging
                await ProcessLogEntriesAsync(entries).ConfigureAwait(false);
                
                _logger.LogDebug("Processed {Count} log entries in batch", processedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing log entries batch");
        }
        finally
        {
            _flushSemaphore.Release();
        }
    }

    /// <summary>
    /// Process log entries for analytics or external systems
    /// </summary>
    private async Task ProcessLogEntriesAsync(IEnumerable<LogEntry> entries)
    {
        // This could send to analytics systems, external logging services, etc.
        // For now, we'll just simulate processing
        await Task.Delay(1).ConfigureAwait(false);
        
        // In a real implementation, you might:
        // - Send to Application Insights
        // - Store in a database for analytics
        // - Forward to external logging services
        // - Generate reports or alerts
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _logFlushTimer?.Dispose();
            
            // Flush any remaining logs
            FlushLogsCallback(null);
            
            _flushSemaphore?.Dispose();
        }
        finally
        {
            _disposed = true;
        }
    }
}

/// <summary>
/// Log entry for batch processing and analytics
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Category { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Details { get; set; }
    public string? PostId { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
} 