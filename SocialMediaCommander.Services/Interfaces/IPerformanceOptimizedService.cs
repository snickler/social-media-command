using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SocialMediaCommander.Services.Interfaces;

/// <summary>
/// Interface for performance optimized service demonstrating Microsoft's best practices
/// </summary>
public interface IPerformanceOptimizedService : IDisposable
{
    /// <summary>
    /// Processes data with memory optimization using ArrayPool
    /// </summary>
    ValueTask<byte[]> ProcessWithPooledMemoryAsync(byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes text with memory optimization
    /// </summary>
    ValueTask<string> ProcessTextAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch processes items with high throughput
    /// </summary>
    ValueTask<int> ProcessHighThroughputBatchAsync<T>(IEnumerable<T> items, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets performance metrics
    /// </summary>
    ValueTask<Dictionary<string, object>> GetPerformanceMetricsAsync();

    /// <summary>
    /// Resets performance metrics
    /// </summary>
    void ResetMetrics();
}