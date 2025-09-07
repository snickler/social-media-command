using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SocialMediaCommander.Services.Interfaces;

/// <summary>
/// Interface for optimized async service demonstrating Microsoft's best practices
/// </summary>
public interface IOptimizedAsyncService : IDisposable
{
    /// <summary>
    /// Processes data using optimized async patterns
    /// </summary>
    ValueTask<T> ProcessDataAsync<T>(T data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch processes multiple items efficiently
    /// </summary>
    ValueTask<IEnumerable<T>> ProcessBatchAsync<T>(IEnumerable<T> items, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cached result or computes if not available
    /// </summary>
    ValueTask<T> GetOrComputeAsync<T>(string key, Func<ValueTask<T>> factory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the internal cache
    /// </summary>
    void ClearCache();
}