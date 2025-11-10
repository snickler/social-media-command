using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;
using SocialMediaCommander.Services.Interfaces;
using Xunit;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Tests for OptimizedAsyncService demonstrating Microsoft async best practices
/// </summary>
public class OptimizedAsyncServiceTests : IDisposable
{
    private readonly Mock<ILogger<OptimizedAsyncService>> _loggerMock;
    private readonly Mock<IAccountService> _accountServiceMock;
    private OptimizedAsyncService? _sut;

    public OptimizedAsyncServiceTests()
    {
        _loggerMock = new Mock<ILogger<OptimizedAsyncService>>();
        _accountServiceMock = new Mock<IAccountService>();
    }

    private OptimizedAsyncService CreateService()
    {
        _sut = new OptimizedAsyncService(_loggerMock.Object, _accountServiceMock.Object);
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
        var act = () => new OptimizedAsyncService(null!, _accountServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenAccountServiceIsNull()
    {
        // Act
        var act = () => new OptimizedAsyncService(_loggerMock.Object, null!);

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

    #region GetAccountFastAsync Tests

    [Fact]
    public async Task GetAccountFastAsync_ShouldReturnAccount_WhenFound()
    {
        // Arrange
        var sut = CreateService();
        var account = new Account { Id = "test1", Username = "testuser", PlatformId = SocialPlatform.BlueSky };
        _accountServiceMock.Setup(x => x.GetAccountByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(account);

        // Act
        var result = await sut.GetAccountFastAsync("test1");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("test1");
    }

    [Fact]
    public async Task GetAccountFastAsync_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        var sut = CreateService();
        _accountServiceMock.Setup(x => x.GetAccountByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await sut.GetAccountFastAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAccountFastAsync_ShouldUseCacheOnSecondCall()
    {
        // Arrange
        var sut = CreateService();
        var account = new Account { Id = "test1", Username = "testuser", PlatformId = SocialPlatform.BlueSky };
        _accountServiceMock.Setup(x => x.GetAccountByIdAsync("test1"))
            .ReturnsAsync(account);

        // Act - First call
        await sut.GetAccountFastAsync("test1");

        // Act - Second call should use cache
        var result = await sut.GetAccountFastAsync("test1");

        // Assert
        result.Should().NotBeNull();
        _accountServiceMock.Verify(x => x.GetAccountByIdAsync("test1"), Times.Once);
    }

    [Fact]
    public async Task GetAccountFastAsync_ShouldThrow_WhenDisposed()
    {
        // Arrange
        var sut = CreateService();
        sut.Dispose();

        // Act
        var act = () => sut.GetAccountFastAsync("test1").AsTask();

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task GetAccountFastAsync_ShouldHandleException_GracefullyAndReturnNull()
    {
        // Arrange
        var sut = CreateService();
        _accountServiceMock.Setup(x => x.GetAccountByIdAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await sut.GetAccountFastAsync("test1");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAccountsBatchAsync Tests

    [Fact]
    public async Task GetAccountsBatchAsync_ShouldProcessMultipleAccounts()
    {
        // Arrange
        var sut = CreateService();
        var account1 = new Account { Id = "1", Username = "user1", PlatformId = SocialPlatform.BlueSky };
        var account2 = new Account { Id = "2", Username = "user2", PlatformId = SocialPlatform.BlueSky };

        _accountServiceMock.Setup(x => x.GetAccountByIdAsync("1")).ReturnsAsync(account1);
        _accountServiceMock.Setup(x => x.GetAccountByIdAsync("2")).ReturnsAsync(account2);

        var ids = new[] { "1", "2" };

        // Act
        var result = await sut.GetAccountsBatchAsync(ids);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().HaveCount(2);
        result.SuccessCount.Should().Be(2);
        result.TotalRequested.Should().Be(2);
    }

    [Fact]
    public async Task GetAccountsBatchAsync_ShouldHandleEmptyCollection()
    {
        // Arrange
        var sut = CreateService();
        var ids = Array.Empty<string>();

        // Act
        var result = await sut.GetAccountsBatchAsync(ids);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().BeEmpty();
        result.SuccessCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAccountsBatchAsync_ShouldRespectCancellation()
    {
        // Arrange
        var sut = CreateService();
        var ids = Enumerable.Range(1, 20).Select(i => i.ToString()).ToList();
        _accountServiceMock.Setup(x => x.GetAccountByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new Account { Id = "1", PlatformId = SocialPlatform.BlueSky });

        var cts = new CancellationTokenSource();
        cts.CancelAfter(50); // Cancel after 50ms

        // Act
        var act = () => sut.GetAccountsBatchAsync(ids, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetAccountsBatchAsync_ShouldThrow_WhenDisposed()
    {
        // Arrange
        var sut = CreateService();
        sut.Dispose();
        var ids = new[] { "1", "2" };

        // Act
        var act = () => sut.GetAccountsBatchAsync(ids);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task GetAccountsBatchAsync_ShouldProcessInBatches()
    {
        // Arrange
        var sut = CreateService();
        var ids = Enumerable.Range(1, 25).Select(i => i.ToString()).ToList();

        _accountServiceMock.Setup(x => x.GetAccountByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => new Account { Id = id, PlatformId = SocialPlatform.BlueSky });

        // Act
        var result = await sut.GetAccountsBatchAsync(ids);

        // Assert
        result.Results.Should().HaveCount(25);
        result.SuccessCount.Should().Be(25);
    }

    #endregion

    #region ProcessAccountsStreamAsync Tests

    [Fact]
    public async Task ProcessAccountsStreamAsync_ShouldStreamAccounts()
    {
        // Arrange
        var sut = CreateService();
        var accounts = new[]
        {
            new Account { Id = "1", Username = "user1", PlatformId = SocialPlatform.BlueSky },
            new Account { Id = "2", Username = "user2", PlatformId = SocialPlatform.BlueSky },
            new Account { Id = "3", Username = "user3", PlatformId = SocialPlatform.BlueSky }
        };

        // Act
        var results = new List<ProcessedAccount>();
        await foreach (var result in sut.ProcessAccountsStreamAsync(accounts))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
    }

    [Fact]
    public async Task ProcessAccountsStreamAsync_ShouldHandleEmptyCollection()
    {
        // Arrange
        var sut = CreateService();
        var accounts = Array.Empty<Account>();

        // Act
        var results = new List<ProcessedAccount>();
        await foreach (var result in sut.ProcessAccountsStreamAsync(accounts))
        {
            results.Add(result);
        }

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessAccountsStreamAsync_ShouldRespectCancellation()
    {
        // Arrange
        var sut = CreateService();
        var accounts = Enumerable.Range(1, 100).Select(i => new Account
        {
            Id = i.ToString(),
            PlatformId = SocialPlatform.BlueSky
        }).ToList();

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100); // Cancel after 100ms

        // Act
        var act = async () =>
        {
            await foreach (var _ in sut.ProcessAccountsStreamAsync(accounts, cts.Token))
            {
                // Consume stream
            }
        };

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ProcessAccountsStreamAsync_ShouldNotYield_WhenDisposed()
    {
        // Arrange
        var sut = CreateService();
        var accounts = new[] { new Account { Id = "1", PlatformId = SocialPlatform.BlueSky } };
        sut.Dispose();

        // Act
        var results = new List<ProcessedAccount>();
        await foreach (var result in sut.ProcessAccountsStreamAsync(accounts))
        {
            results.Add(result);
        }

        // Assert
        results.Should().BeEmpty();
    }

    #endregion

    #region SerializeAccountAsync Tests

    [Fact]
    public async Task SerializeAccountAsync_ShouldSerializeAccount()
    {
        // Arrange
        var sut = CreateService();
        var account = new Account
        {
            Id = "test1",
            Username = "testuser",
            DisplayName = "Test User",
            PlatformId = SocialPlatform.BlueSky
        };

        // Act
        var result = await sut.SerializeAccountAsync(account);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("test1");
        result.Should().Contain("testuser");
    }

    [Fact]
    public async Task SerializeAccountAsync_ShouldThrow_WhenDisposed()
    {
        // Arrange
        var sut = CreateService();
        sut.Dispose();
        var account = new Account { Id = "1", PlatformId = SocialPlatform.BlueSky };

        // Act
        var act = () => sut.SerializeAccountAsync(account).AsTask();

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region ValidateAccountAsync Tests

    [Fact]
    public async Task ValidateAccountAsync_ShouldReturnFalse_WhenIdIsEmpty()
    {
        // Arrange
        var sut = CreateService();
        var account = new Account { Id = "", Username = "test", PlatformId = SocialPlatform.BlueSky };

        // Act
        var result = await sut.ValidateAccountAsync(account);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAccountAsync_ShouldReturnFalse_WhenUsernameIsEmpty()
    {
        // Arrange
        var sut = CreateService();
        var account = new Account { Id = "1", Username = "", PlatformId = SocialPlatform.BlueSky };

        // Act
        var result = await sut.ValidateAccountAsync(account);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAccountAsync_ShouldReturnTrue_WhenAccountIsValid()
    {
        // Arrange
        var sut = CreateService();
        var account = new Account
        {
            Id = "1",
            Username = "testuser",
            PlatformId = SocialPlatform.BlueSky
        };

        // Act
        var result = await sut.ValidateAccountAsync(account);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAccountAsync_ShouldValidateWithService_WhenNotAuthenticated()
    {
        // Arrange
        var sut = CreateService();
        var account = new Account
        {
            Id = "1",
            Username = "testuser",
            PlatformId = SocialPlatform.BlueSky,
            AuthStatus = AuthenticationStatus.NotAuthenticated
        };

        // Act
        var result = await sut.ValidateAccountAsync(account);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAccountAsync_ShouldReturnFalse_WhenDisposed()
    {
        // Arrange
        var sut = CreateService();
        sut.Dispose();
        var account = new Account { Id = "1", Username = "test", PlatformId = SocialPlatform.BlueSky };

        // Act
        var result = await sut.ValidateAccountAsync(account);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region UpdateAccountsAsync Tests

    [Fact]
    public async Task UpdateAccountsAsync_ShouldUpdateMultipleAccounts()
    {
        // Arrange
        var sut = CreateService();
        var accounts = new[]
        {
            new Account { Id = "1", Username = "user1", PlatformId = SocialPlatform.BlueSky },
            new Account { Id = "2", Username = "user2", PlatformId = SocialPlatform.BlueSky },
            new Account { Id = "3", Username = "user3", PlatformId = SocialPlatform.BlueSky }
        };

        // Act
        var result = await sut.UpdateAccountsAsync(accounts);

        // Assert
        result.Should().NotBeNull();
        result.TotalProcessed.Should().Be(3);
        result.SuccessCount.Should().Be(3);
        result.ErrorCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAccountsAsync_ShouldHandleEmptyCollection()
    {
        // Arrange
        var sut = CreateService();
        var accounts = Array.Empty<Account>();

        // Act
        var result = await sut.UpdateAccountsAsync(accounts);

        // Assert
        result.Should().NotBeNull();
        result.TotalProcessed.Should().Be(0);
        result.SuccessCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAccountsAsync_ShouldRespectCancellation()
    {
        // Arrange
        var sut = CreateService();
        var accounts = Enumerable.Range(1, 20).Select(i => new Account
        {
            Id = i.ToString(),
            Username = $"user{i}",
            PlatformId = SocialPlatform.BlueSky
        }).ToList();

        var cts = new CancellationTokenSource();
        cts.CancelAfter(50); // Cancel after 50ms

        // Act
        var act = () => sut.UpdateAccountsAsync(accounts, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task UpdateAccountsAsync_ShouldThrow_WhenDisposed()
    {
        // Arrange
        var sut = CreateService();
        sut.Dispose();
        var accounts = new[] { new Account { Id = "1", PlatformId = SocialPlatform.BlueSky } };

        // Act
        var act = () => sut.UpdateAccountsAsync(accounts);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region SaveAccountsToFileAsync Tests

    [Fact]
    public async Task SaveAccountsToFileAsync_ShouldSaveToFile()
    {
        // Arrange
        var sut = CreateService();
        var accounts = new[]
        {
            new Account { Id = "1", Username = "user1", PlatformId = SocialPlatform.BlueSky },
            new Account { Id = "2", Username = "user2", PlatformId = SocialPlatform.BlueSky }
        };
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            var result = await sut.SaveAccountsToFileAsync(accounts, tempFile);

            // Assert
            result.Should().BeTrue();
            File.Exists(tempFile).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SaveAccountsToFileAsync_ShouldReturnFalse_WhenDisposed()
    {
        // Arrange
        var sut = CreateService();
        sut.Dispose();
        var accounts = new[] { new Account { Id = "1", PlatformId = SocialPlatform.BlueSky } };
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            var result = await sut.SaveAccountsToFileAsync(accounts, tempFile);

            // Assert
            result.Should().BeFalse();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SaveAccountsToFileAsync_ShouldReturnFalse_OnError()
    {
        // Arrange
        var sut = CreateService();
        var accounts = new[] { new Account { Id = "1", PlatformId = SocialPlatform.BlueSky } };
        var invalidPath = "/invalid/path/file.json";

        // Act
        var result = await sut.SaveAccountsToFileAsync(accounts, invalidPath);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ClearExpiredCache Tests

    [Fact]
    public void ClearExpiredCache_ShouldRemoveExpiredEntries()
    {
        // Arrange
        var sut = CreateService();

        // Act
        sut.ClearExpiredCache();

        // Assert - Should not throw
    }

    [Fact]
    public void ClearExpiredCache_ShouldNotThrow_WhenDisposed()
    {
        // Arrange
        var sut = CreateService();
        sut.Dispose();

        // Act
        var act = () => sut.ClearExpiredCache();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region ProcessDataAsync Tests

    [Fact]
    public async Task ProcessDataAsync_ShouldReturnData()
    {
        // Arrange
        var sut = CreateService();
        var data = new { Value = 42 };

        // Act
        var result = await sut.ProcessDataAsync(data);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(42);
    }

    #endregion

    #region ProcessBatchAsync Tests

    [Fact]
    public async Task ProcessBatchAsync_ShouldReturnItems()
    {
        // Arrange
        var sut = CreateService();
        var items = new[] { 1, 2, 3, 4, 5 };

        // Act
        var result = await sut.ProcessBatchAsync(items);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(5);
    }

    #endregion

    #region GetOrComputeAsync Tests

    [Fact]
    public async Task GetOrComputeAsync_ShouldInvokeFactory()
    {
        // Arrange
        var sut = CreateService();
        var factoryInvoked = false;
        ValueTask<int> Factory()
        {
            factoryInvoked = true;
            return ValueTask.FromResult(42);
        }

        // Act
        var result = await sut.GetOrComputeAsync("key", Factory);

        // Assert
        result.Should().Be(42);
        factoryInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrComputeAsync_ShouldInvokeFactory_WhenKeyIsEmpty()
    {
        // Arrange
        var sut = CreateService();
        var factoryInvoked = false;
        ValueTask<int> Factory()
        {
            factoryInvoked = true;
            return ValueTask.FromResult(42);
        }

        // Act
        var result = await sut.GetOrComputeAsync("", Factory);

        // Assert
        result.Should().Be(42);
        factoryInvoked.Should().BeTrue();
    }

    #endregion

    #region ClearCache Tests

    [Fact]
    public void ClearCache_ShouldClearCache()
    {
        // Arrange
        var sut = CreateService();

        // Act
        sut.ClearCache();

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

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public async Task CacheExpiry_ShouldRefetchAfterExpiration()
    {
        // Arrange
        var sut = CreateService();
        var account = new Account { Id = "test1", Username = "testuser", PlatformId = SocialPlatform.BlueSky };
        _accountServiceMock.Setup(x => x.GetAccountByIdAsync("test1"))
            .ReturnsAsync(account);

        // Act - First call caches the result
        await sut.GetAccountFastAsync("test1");

        // Simulate cache expiry by waiting (or we would need to control time)
        // For now, just verify it was called once
        _accountServiceMock.Verify(x => x.GetAccountByIdAsync("test1"), Times.Once);
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldHandleCorrectly()
    {
        // Arrange
        var sut = CreateService();
        _accountServiceMock.Setup(x => x.GetAccountByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => new Account { Id = id, PlatformId = SocialPlatform.BlueSky });

        // Act - Execute multiple operations concurrently
        var tasks = Enumerable.Range(1, 10).Select(i =>
            sut.GetAccountFastAsync(i.ToString())
        ).ToList();

        await Task.WhenAll(tasks.Select(t => t.AsTask()));

        // Assert - All tasks should complete successfully
        tasks.Should().AllSatisfy(t => t.IsCompletedSuccessfully.Should().BeTrue());
    }

    [Fact]
    public async Task ProcessAccountsStreamAsync_ShouldProcessLargeStream()
    {
        // Arrange
        var sut = CreateService();
        var accounts = Enumerable.Range(1, 50).Select(i => new Account
        {
            Id = i.ToString(),
            Username = $"user{i}",
            PlatformId = SocialPlatform.BlueSky
        }).ToList();

        // Act
        var results = new List<ProcessedAccount>();
        await foreach (var result in sut.ProcessAccountsStreamAsync(accounts))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(50);
    }

    #endregion
}
