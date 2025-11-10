using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;
using Xunit;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Tests for PerformanceOptimizedService demonstrating Microsoft performance best practices
/// </summary>
public class PerformanceOptimizedServiceTests : IDisposable
{
    private readonly Mock<ILogger<PerformanceOptimizedService>> _loggerMock;
    private PerformanceOptimizedService? _sut;

    public PerformanceOptimizedServiceTests()
    {
        _loggerMock = new Mock<ILogger<PerformanceOptimizedService>>();
        // Setup logger to always return true for IsEnabled to test logging paths
        _loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }

    private PerformanceOptimizedService CreateService()
    {
        _sut = new PerformanceOptimizedService(_loggerMock.Object);
        return _sut;
    }

    public void Dispose()
    {
        _sut?.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldThrow_WhenLoggerIsNull()
    {
        // Act
        var act = () => new PerformanceOptimizedService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ShouldInitialize_Successfully()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region ProcessDataAsync Tests

    [Fact]
    public async Task ProcessDataAsync_ShouldProcessData_Successfully()
    {
        // Arrange
        var sut = CreateService();
        var data = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var result = await sut.ProcessDataAsync(data.AsMemory());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ProcessedBytes.Should().Be(5);
    }

    [Fact]
    public async Task ProcessDataAsync_ShouldHandleEmptyData()
    {
        // Arrange
        var sut = CreateService();
        var data = Array.Empty<byte>();

        // Act
        var result = await sut.ProcessDataAsync(data.AsMemory());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessDataAsync_ShouldRespectCancellation()
    {
        // Arrange
        var sut = CreateService();
        var data = new byte[1000];
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => sut.ProcessDataAsync(data.AsMemory(), cts.Token).AsTask();

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ProcessDataAsync_ShouldThrow_WhenDisposed()
    {
        // Arrange
        var sut = CreateService();
        sut.Dispose();
        var data = new byte[] { 1, 2, 3 };

        // Act
        var act = () => sut.ProcessDataAsync(data.AsMemory()).AsTask();

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region ProcessBatchAsync Tests

    [Fact]
    public async Task ProcessBatchAsync_ShouldProcessAccounts_Successfully()
    {
        // Arrange
        var sut = CreateService();
        var accounts = new List<Account>
        {
            new Account { Id = "1", Username = "user1", PlatformId = SocialPlatform.BlueSky },
            new Account { Id = "2", Username = "user2", PlatformId = SocialPlatform.BlueSky },
            new Account { Id = "3", Username = "user3", PlatformId = SocialPlatform.BlueSky }
        };

        // Act
        var result = await sut.ProcessBatchAsync(accounts);

        // Assert
        result.Should().NotBeNull();
        result.ProcessedCount.Should().Be(3);
        result.TotalCount.Should().Be(3);
        result.ProcessedAccounts.Should().HaveCount(3);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessBatchAsync_ShouldHandleEmptyCollection()
    {
        // Arrange
        var sut = CreateService();
        var accounts = new List<Account>();

        // Act
        var result = await sut.ProcessBatchAsync(accounts);

        // Assert
        result.Should().NotBeNull();
        result.ProcessedCount.Should().Be(0);
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task ProcessBatchAsync_ShouldRespectCancellation()
    {
        // Arrange
        var sut = CreateService();
        var accounts = Enumerable.Range(0, 100).Select(i => new Account
        {
            Id = i.ToString(),
            Username = $"user{i}",
            PlatformId = SocialPlatform.BlueSky
        }).ToList();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => sut.ProcessBatchAsync(accounts, cts.Token).AsTask();

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ProcessBatchAsync_ShouldThrow_WhenDisposed()
    {
        // Arrange
        var sut = CreateService();
        sut.Dispose();
        var accounts = new List<Account> { new Account { Id = "1", PlatformId = SocialPlatform.BlueSky } };

        // Act
        var act = () => sut.ProcessBatchAsync(accounts).AsTask();

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task ProcessBatchAsync_ShouldProcessInParallel()
    {
        // Arrange
        var sut = CreateService();
        var accounts = Enumerable.Range(0, 20).Select(i => new Account
        {
            Id = i.ToString(),
            Username = $"user{i}",
            PlatformId = SocialPlatform.BlueSky
        }).ToList();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await sut.ProcessBatchAsync(accounts);
        stopwatch.Stop();

        // Assert
        result.ProcessedCount.Should().Be(20);
        // Parallel processing should be faster than sequential (20 * 5ms = 100ms)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(150);
    }

    #endregion

    #region SerializeToJsonAsync Tests

    [Fact]
    public async Task SerializeToJsonAsync_ShouldSerializeObject()
    {
        // Arrange
        var sut = CreateService();
        var account = new Account
        {
            Id = "test123",
            Username = "testuser",
            DisplayName = "Test User",
            PlatformId = SocialPlatform.BlueSky
        };

        // Act
        var result = await sut.SerializeToJsonAsync(account);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("test123");
        result.Should().Contain("testuser");
    }

    [Fact]
    public async Task SerializeToJsonAsync_ShouldThrow_WhenDisposed()
    {
        // Arrange
        var sut = CreateService();
        sut.Dispose();
        var account = new Account { Id = "1", PlatformId = SocialPlatform.BlueSky };

        // Act
        var act = () => sut.SerializeToJsonAsync(account).AsTask();

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task SerializeToJsonAsync_ShouldRespectCancellation()
    {
        // Arrange
        var sut = CreateService();
        var account = new Account { Id = "1", PlatformId = SocialPlatform.BlueSky };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => sut.SerializeToJsonAsync(account, cts.Token).AsTask();

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region ProcessTextAsync (Span) Tests

    [Fact]
    public async Task ProcessTextAsync_Span_ShouldProcessText()
    {
        // Arrange
        var sut = CreateService();
        var input = "Hello World 123".AsSpan();

        // Act
        var result = await sut.ProcessTextAsync(input);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Be("HELLO_WORLD_123");
    }

    [Fact]
    public async Task ProcessTextAsync_Span_ShouldReplaceSpacesWithUnderscores()
    {
        // Arrange
        var sut = CreateService();
        var input = "test with spaces".AsSpan();

        // Act
        var result = await sut.ProcessTextAsync(input);

        // Assert
        result.Should().Be("TEST_WITH_SPACES");
    }

    [Fact]
    public async Task ProcessTextAsync_Span_ShouldHandleEmptyInput()
    {
        // Arrange
        var sut = CreateService();
        var input = "".AsSpan();

        // Act
        var result = await sut.ProcessTextAsync(input);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessTextAsync_Span_ShouldThrow_WhenDisposed()
    {
        // Arrange
        var sut = CreateService();
        sut.Dispose();
        var input = "test";

        // Act
        var act = () => sut.ProcessTextAsync(input.AsSpan()).AsTask();

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task ProcessTextAsync_Span_ShouldFilterSpecialCharacters()
    {
        // Arrange
        var sut = CreateService();
        var input = "test@#$%123".AsSpan();

        // Act
        var result = await sut.ProcessTextAsync(input);

        // Assert
        result.Should().Be("TEST123");
    }

    #endregion

    #region ProcessWithPooledMemoryAsync Tests

    [Fact]
    public async Task ProcessWithPooledMemoryAsync_ShouldProcessData()
    {
        // Arrange
        var sut = CreateService();
        var data = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var result = await sut.ProcessWithPooledMemoryAsync(data);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task ProcessWithPooledMemoryAsync_ShouldReturnEmpty_WhenDataIsNull()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.ProcessWithPooledMemoryAsync(null!);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessWithPooledMemoryAsync_ShouldReturnEmpty_WhenDataIsEmpty()
    {
        // Arrange
        var sut = CreateService();
        var data = Array.Empty<byte>();

        // Act
        var result = await sut.ProcessWithPooledMemoryAsync(data);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region ProcessTextAsync (String) Tests

    [Fact]
    public async Task ProcessTextAsync_String_ShouldProcessText()
    {
        // Arrange
        var sut = CreateService();
        var input = "Hello World";

        // Act
        var result = await sut.ProcessTextAsync(input);

        // Assert
        result.Should().Be("HELLO_WORLD");
    }

    [Fact]
    public async Task ProcessTextAsync_String_ShouldReturnEmpty_WhenNull()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.ProcessTextAsync((string)null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessTextAsync_String_ShouldReturnEmpty_WhenEmpty()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.ProcessTextAsync(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region ProcessHighThroughputBatchAsync Tests

    [Fact]
    public async Task ProcessHighThroughputBatchAsync_ShouldCountItems()
    {
        // Arrange
        var sut = CreateService();
        var items = Enumerable.Range(1, 100).ToList();

        // Act
        var result = await sut.ProcessHighThroughputBatchAsync(items);

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    public async Task ProcessHighThroughputBatchAsync_ShouldHandleEmptyCollection()
    {
        // Arrange
        var sut = CreateService();
        var items = new List<int>();

        // Act
        var result = await sut.ProcessHighThroughputBatchAsync(items);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task ProcessHighThroughputBatchAsync_ShouldRespectCancellation()
    {
        // Arrange
        var sut = CreateService();
        var items = Enumerable.Range(1, 1000);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await sut.ProcessHighThroughputBatchAsync(items, cts.Token);

        // Assert
        result.Should().BeLessThan(1000);
    }

    #endregion

    #region GetPerformanceMetricsAsync Tests

    [Fact]
    public async Task GetPerformanceMetricsAsync_ShouldReturnMetrics()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.GetPerformanceMetricsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("TotalOperations");
        result.Should().ContainKey("AverageLatency");
        result.Should().ContainKey("ErrorRate");
    }

    #endregion

    #region ResetMetrics Tests

    [Fact]
    public void ResetMetrics_ShouldClearMetrics()
    {
        // Arrange
        var sut = CreateService();

        // Act
        sut.ResetMetrics();

        // Assert - Should not throw
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldDisposeResources()
    {
        // Arrange
        var sut = CreateService();

        // Act
        sut.Dispose();

        // Assert - Should not throw
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        // Arrange
        var sut = CreateService();

        // Act
        sut.Dispose();
        sut.Dispose();

        // Assert - Should not throw
    }

    [Fact]
    public async Task Operations_ShouldThrow_AfterDispose()
    {
        // Arrange
        var sut = CreateService();
        sut.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            sut.ProcessDataAsync(new byte[10].AsMemory()).AsTask());

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            sut.ProcessBatchAsync(new List<Account>()).AsTask());

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            sut.SerializeToJsonAsync(new Account { Id = "1", PlatformId = SocialPlatform.BlueSky }).AsTask());
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public async Task ProcessDataAsync_ShouldHandleLargeData()
    {
        // Arrange
        var sut = CreateService();
        var data = new byte[1024 * 1024]; // 1MB

        // Act
        var result = await sut.ProcessDataAsync(data.AsMemory());

        // Assert
        result.Success.Should().BeTrue();
        result.ProcessedBytes.Should().Be(1024 * 1024);
    }

    [Fact]
    public async Task ProcessBatchAsync_ShouldHandleLargeBatch()
    {
        // Arrange
        var sut = CreateService();
        var accounts = Enumerable.Range(0, 100).Select(i => new Account
        {
            Id = i.ToString(),
            Username = $"user{i}",
            PlatformId = SocialPlatform.BlueSky
        }).ToList();

        // Act
        var result = await sut.ProcessBatchAsync(accounts);

        // Assert
        result.ProcessedCount.Should().Be(100);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task SerializeToJsonAsync_ShouldHandleComplexObject()
    {
        // Arrange
        var sut = CreateService();
        var account = new Account
        {
            Id = "test",
            Username = "testuser",
            DisplayName = "Test User",
            PlatformId = SocialPlatform.BlueSky,
            Avatar = "https://example.com/avatar.jpg",
            CreatedAt = DateTime.UtcNow,
            LastUsed = DateTime.UtcNow
        };

        // Act
        var result = await sut.SerializeToJsonAsync(account);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("testuser");
    }

    [Fact]
    public async Task ProcessTextAsync_ShouldHandleLongText()
    {
        // Arrange
        var sut = CreateService();
        var input = string.Join(" ", Enumerable.Repeat("test", 1000));

        // Act
        var result = await sut.ProcessTextAsync(input);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("TEST");
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldWorkCorrectly()
    {
        // Arrange
        var sut = CreateService();
        var tasks = new List<Task>();

        // Act - Execute multiple operations concurrently
        for (int i = 0; i < 10; i++)
        {
            var data = new byte[] { (byte)i };
            tasks.Add(sut.ProcessDataAsync(data.AsMemory()).AsTask());
        }

        await Task.WhenAll(tasks);

        // Assert - All tasks should complete successfully
        tasks.Should().AllSatisfy(t => t.IsCompletedSuccessfully.Should().BeTrue());
    }

    #endregion
}
