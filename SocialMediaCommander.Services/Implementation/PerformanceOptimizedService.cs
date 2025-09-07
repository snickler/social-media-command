using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// High-performance service demonstrating Microsoft's best practices for C# performance optimization
/// Following guidance from Microsoft Docs for async patterns, memory management, and structured logging
/// </summary>
public class PerformanceOptimizedService : IPerformanceOptimizedService
{
    private readonly ILogger<PerformanceOptimizedService> _logger;
    private readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Shared;
    private readonly ArrayPool<char> _charPool = ArrayPool<char>.Shared;
    private readonly ConcurrentQueue<PerformanceMetric> _metricsQueue = new();
    private readonly SemaphoreSlim _semaphore = new(Environment.ProcessorCount, Environment.ProcessorCount);
    private readonly Timer _metricsTimer;
    private volatile bool _disposed = false;

    // High-performance logging delegates using LoggerMessage pattern
    private static readonly Action<ILogger, string, double, Exception?> LogPerformanceMetric =
        LoggerMessage.Define<string, double>(
            LogLevel.Information,
            new EventId(1001, nameof(LogPerformanceMetric)),
            "Performance metric recorded: {MetricName} = {Value}ms");

    private static readonly Action<ILogger, int, Exception?> LogBatchProcessed =
        LoggerMessage.Define<int>(
            LogLevel.Debug,
            new EventId(1002, nameof(LogBatchProcessed)),
            "Batch processed successfully with {ItemCount} items");

    private static readonly Action<ILogger, string, Exception?> LogOperationStarted =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(1003, nameof(LogOperationStarted)),
            "Starting high-performance operation: {OperationName}");

    public PerformanceOptimizedService(ILogger<PerformanceOptimizedService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize metrics collection timer (every 30 seconds)
        _metricsTimer = new Timer(ProcessMetricsCallback, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

        LogOperationStarted(_logger, "PerformanceOptimizedService", null);
    }

    /// <summary>
    /// High-performance async operation using ValueTask for optimal memory allocation
    /// Demonstrates ConfigureAwait(false) usage for library code
    /// </summary>
    public async ValueTask<ProcessingResult> ProcessDataAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PerformanceOptimizedService));

        var activity = StartActivity(nameof(ProcessDataAsync));

        // Use semaphore to limit concurrent operations
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // Process data using high-performance patterns
            var result = await ProcessDataInternalAsync(data, cancellationToken).ConfigureAwait(false);

            // Record performance metrics
            RecordMetric("ProcessDataAsync", activity.Elapsed.TotalMilliseconds);

            return result;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Batch processing with optimal memory usage and parallel processing
    /// </summary>
    public async ValueTask<BatchProcessingResult> ProcessBatchAsync(
        IEnumerable<Account> accounts,
        CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PerformanceOptimizedService));

        var activity = StartActivity(nameof(ProcessBatchAsync));
        var processedCount = 0;
        var errors = new List<string>();

        // Use concurrent collection for thread-safe operations
        var results = new ConcurrentBag<ProcessedAccount>();

        // Process in parallel with degree of parallelism based on CPU cores
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        try
        {
            await Parallel.ForEachAsync(accounts, parallelOptions, async (account, ct) =>
            {
                try
                {
                    var processed = await ProcessAccountAsync(account, ct).ConfigureAwait(false);
                    results.Add(processed);
                    Interlocked.Increment(ref processedCount);
                }
                catch (Exception ex)
                {
                    lock (errors)
                    {
                        errors.Add($"Error processing account {account.Id}: {ex.Message}");
                    }
                    _logger.LogWarning(ex, "Failed to process account {AccountId}", account.Id);
                }
            }).ConfigureAwait(false);

            LogBatchProcessed(_logger, processedCount, null);
            RecordMetric("ProcessBatchAsync", activity.Elapsed.TotalMilliseconds);

            return new BatchProcessingResult
            {
                ProcessedCount = processedCount,
                TotalCount = results.Count + errors.Count,
                ProcessedAccounts = results.ToArray(),
                Errors = errors,
                ProcessingTimeMs = activity.Elapsed.TotalMilliseconds
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Batch processing was cancelled after processing {ProcessedCount} items", processedCount);
            throw;
        }
    }

    /// <summary>
    /// High-performance JSON serialization with memory pooling
    /// </summary>
    public async ValueTask<string> SerializeToJsonAsync<T>(T data, CancellationToken cancellationToken = default)
        where T : class
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PerformanceOptimizedService));

        var activity = StartActivity(nameof(SerializeToJsonAsync));

        // Rent buffer from pool to avoid allocations
        var buffer = _bytePool.Rent(8192);
        try
        {
            using var stream = new MemoryStream(buffer);
            await JsonSerializer.SerializeAsync(stream, data, cancellationToken: cancellationToken).ConfigureAwait(false);

            var jsonBytes = stream.ToArray();
            var result = Encoding.UTF8.GetString(jsonBytes);

            RecordMetric("SerializeToJsonAsync", activity.Elapsed.TotalMilliseconds);
            return result;
        }
        finally
        {
            _bytePool.Return(buffer);
        }
    }

    /// <summary>
    /// Efficient string processing using Span<T> and memory pooling
    /// </summary>
    public ValueTask<string> ProcessTextAsync(ReadOnlySpan<char> input, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PerformanceOptimizedService));

        var activity = StartActivity(nameof(ProcessTextAsync));

        // Rent char buffer from pool
        var buffer = _charPool.Rent(input.Length * 2);
        try
        {
            var span = buffer.AsSpan();
            var written = 0;

            // Efficient string processing using Span<T>
            foreach (var c in input)
            {
                if (char.IsLetterOrDigit(c))
                {
                    span[written++] = char.ToUpperInvariant(c);
                }
                else if (char.IsWhiteSpace(c))
                {
                    span[written++] = '_';
                }
            }

            var result = new string(span.Slice(0, written));
            RecordMetric("ProcessTextAsync", activity.Elapsed.TotalMilliseconds);

            return ValueTask.FromResult(result);
        }
        finally
        {
            _charPool.Return(buffer);
        }
    }

    /// <summary>
    /// Internal processing method demonstrating async best practices
    /// </summary>
    private async ValueTask<ProcessingResult> ProcessDataInternalAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken)
    {
        // Simulate processing with proper async patterns
        await Task.Delay(10, cancellationToken).ConfigureAwait(false);

        return new ProcessingResult
        {
            Success = true,
            ProcessedBytes = data.Length,
            ProcessingTimeMs = 10
        };
    }

    /// <summary>
    /// Process individual account with optimal async patterns
    /// </summary>
    private async ValueTask<ProcessedAccount> ProcessAccountAsync(Account account, CancellationToken cancellationToken)
    {
        // Use ConfigureAwait(false) for library code to avoid deadlocks
        await Task.Delay(5, cancellationToken).ConfigureAwait(false);

        return new ProcessedAccount
        {
            AccountId = account.Id,
            Platform = account.PlatformId.ToString(),
            ProcessedAt = DateTime.UtcNow,
            Success = true
        };
    }

    /// <summary>
    /// Record performance metrics efficiently
    /// </summary>
    private void RecordMetric(string metricName, double valueMs)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            LogPerformanceMetric(_logger, metricName, valueMs, null);
        }

        _metricsQueue.Enqueue(new PerformanceMetric
        {
            Name = metricName,
            Value = valueMs,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Start activity for performance tracking
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Stopwatch StartActivity(string operationName)
    {
        var stopwatch = Stopwatch.StartNew();
        return stopwatch;
    }

    /// <summary>
    /// Process accumulated metrics (called by timer)
    /// </summary>
    private void ProcessMetricsCallback(object? state)
    {
        if (_disposed) return;

        var metricsProcessed = 0;
        var totalProcessingTime = 0.0;

        while (_metricsQueue.TryDequeue(out var metric))
        {
            totalProcessingTime += metric.Value;
            metricsProcessed++;
        }

        if (metricsProcessed > 0 && _logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Processed {MetricsCount} performance metrics. Average processing time: {AverageTime:F2}ms",
                metricsProcessed,
                totalProcessingTime / metricsProcessed);
        }
    }

    // Interface implementations
    public async ValueTask<byte[]> ProcessWithPooledMemoryAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null || data.Length == 0)
            return Array.Empty<byte>();

        var result = await ProcessDataAsync(data.AsMemory(), cancellationToken);
        return data; // Return the original data for now - this is a mock implementation
    }

    public async ValueTask<string> ProcessTextAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return await ProcessTextAsync(text.AsSpan(), cancellationToken);
    }

    public async ValueTask<int> ProcessHighThroughputBatchAsync<T>(IEnumerable<T> items, CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var item in items)
        {
            count++;
            if (cancellationToken.IsCancellationRequested)
                break;
        }
        return await Task.FromResult(count);
    }

    public async ValueTask<Dictionary<string, object>> GetPerformanceMetricsAsync()
    {
        return await Task.FromResult(new Dictionary<string, object>
        {
            ["TotalOperations"] = 0,
            ["AverageLatency"] = 0.0,
            ["ErrorRate"] = 0.0
        });
    }

    public void ResetMetrics()
    {
        // Clear any internal metrics
        while (_metricsQueue.TryDequeue(out _)) { }
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _metricsTimer?.Dispose();
            _semaphore?.Dispose();

            // Process any remaining metrics
            ProcessMetricsCallback(null);
        }
        finally
        {
            _disposed = true;
        }
    }
}

// Performance-related models
public class ProcessingResult
{
    public bool Success { get; set; }
    public int ProcessedBytes { get; set; }
    public double ProcessingTimeMs { get; set; }
}

public class BatchProcessingResult
{
    public int ProcessedCount { get; set; }
    public int TotalCount { get; set; }
    public ProcessedAccount[] ProcessedAccounts { get; set; } = Array.Empty<ProcessedAccount>();
    public List<string> Errors { get; set; } = new();
    public double ProcessingTimeMs { get; set; }
}

public class ProcessedAccount
{
    public string AccountId { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public bool Success { get; set; }
}

public class PerformanceMetric
{
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
}