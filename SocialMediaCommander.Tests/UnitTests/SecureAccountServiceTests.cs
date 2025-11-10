using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Unit tests for SecureAccountService functionality
/// </summary>
public class SecureAccountServiceTests : IDisposable
{
    private readonly SecureAccountService _accountService;
    private readonly string _tempDirectory;

    public SecureAccountServiceTests()
    {
        // Create unique temp directory for this test instance
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"SMC_AccountTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);

        // Use the new constructor that accepts a custom directory
        _accountService = new SecureAccountService(_tempDirectory);
    }

    [Fact]
    public async Task GetAllAccountsAsync_EmptyService_ShouldReturnEmptyList()
    {
        // Act
        var accounts = await _accountService.GetAllAccountsAsync();

        // Assert
        accounts.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAccountAsync_NewAccount_ShouldCreateAndReturnAccount()
    {
        // Arrange
        var account = CreateTestAccount();

        // Act
        var createdAccount = await _accountService.CreateAccountAsync(account);

        // Assert
        createdAccount.Should().NotBeNull();
        createdAccount.Id.Should().NotBeNullOrEmpty();
        createdAccount.IsDefault.Should().BeTrue(); // First account for platform should be default
        createdAccount.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        createdAccount.LastUsed.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateAccountAsync_WithoutId_ShouldGenerateId()
    {
        // Arrange
        var account = CreateTestAccount();
        account.Id = string.Empty;

        // Act
        var createdAccount = await _accountService.CreateAccountAsync(account);

        // Assert
        createdAccount.Id.Should().NotBeNullOrEmpty();
        Guid.TryParse(createdAccount.Id, out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAccountAsync_SecondAccountSamePlatform_ShouldNotSetAsDefault()
    {
        // Arrange
        var account1 = CreateTestAccount();
        var account2 = CreateTestAccount();
        account2.Username = "test2";

        // Act
        await _accountService.CreateAccountAsync(account1);
        var createdAccount2 = await _accountService.CreateAccountAsync(account2);

        // Assert
        createdAccount2.IsDefault.Should().BeFalse(); // Second account should not be default
    }

    [Fact]
    public async Task GetAccountByIdAsync_ExistingAccount_ShouldReturnAccount()
    {
        // Arrange
        var account = CreateTestAccount();
        var createdAccount = await _accountService.CreateAccountAsync(account);

        // Act
        var retrievedAccount = await _accountService.GetAccountByIdAsync(createdAccount.Id);

        // Assert
        retrievedAccount.Should().NotBeNull();
        retrievedAccount!.Id.Should().Be(createdAccount.Id);
        retrievedAccount.Username.Should().Be(createdAccount.Username);
    }

    [Fact]
    public async Task GetAccountByIdAsync_NonExistentAccount_ShouldReturnNull()
    {
        // Act
        var account = await _accountService.GetAccountByIdAsync("nonexistent");

        // Assert
        account.Should().BeNull();
    }

    [Fact]
    public async Task GetAccountsForPlatformAsync_ExistingAccounts_ShouldReturnFilteredAccounts()
    {
        // Arrange
        var blueskyAccount = CreateTestAccount(SocialPlatform.BlueSky);

        await _accountService.CreateAccountAsync(blueskyAccount);

        // Act
        var blueskyAccounts = await _accountService.GetAccountsForPlatformAsync(SocialPlatform.BlueSky);

        // Assert
        blueskyAccounts.Should().HaveCount(1);
        blueskyAccounts.First().PlatformId.Should().Be(SocialPlatform.BlueSky);
    }

    [Fact]
    public async Task UpdateAccountAsync_ExistingAccount_ShouldUpdateAccount()
    {
        // Arrange
        var account = CreateTestAccount();
        var createdAccount = await _accountService.CreateAccountAsync(account);

        createdAccount.DisplayName = "Updated Name";
        createdAccount.AuthStatus = AuthenticationStatus.NotAuthenticated;

        // Act
        var updatedAccount = await _accountService.UpdateAccountAsync(createdAccount);

        // Assert
        updatedAccount.DisplayName.Should().Be("Updated Name");
        updatedAccount.AuthStatus.Should().Be(AuthenticationStatus.NotAuthenticated);
        updatedAccount.LastUsed.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateAccountAsync_NonExistentAccount_ShouldThrowException()
    {
        // Arrange
        var account = CreateTestAccount();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _accountService.UpdateAccountAsync(account));
    }

    [Fact]
    public async Task UpdateAccountAsync_SetAsDefault_ShouldUnsetOtherDefaults()
    {
        // Arrange
        var account1 = CreateTestAccount();
        var account2 = CreateTestAccount();
        account2.Username = "test2";

        var created1 = await _accountService.CreateAccountAsync(account1);
        var created2 = await _accountService.CreateAccountAsync(account2);

        created1.IsDefault.Should().BeTrue(); // First should be default
        created2.IsDefault.Should().BeFalse(); // Second should not be default

        // Act - Set second account as default
        created2.IsDefault = true;
        await _accountService.UpdateAccountAsync(created2);

        // Assert
        var updated1 = await _accountService.GetAccountByIdAsync(created1.Id);
        var updated2 = await _accountService.GetAccountByIdAsync(created2.Id);

        updated1!.IsDefault.Should().BeFalse();
        updated2!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAccountAsync_ExistingAccount_ShouldDeleteAccount()
    {
        // Arrange
        var account1 = CreateTestAccount();
        var account2 = CreateTestAccount();
        account2.Username = "test2";

        var created1 = await _accountService.CreateAccountAsync(account1);
        var created2 = await _accountService.CreateAccountAsync(account2);

        // Act
        var deleted = await _accountService.DeleteAccountAsync(created2.Id);

        // Assert
        deleted.Should().BeTrue();
        var remainingAccount = await _accountService.GetAccountByIdAsync(created2.Id);
        remainingAccount.Should().BeNull();

        // First account should still exist
        var stillExists = await _accountService.GetAccountByIdAsync(created1.Id);
        stillExists.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAccountAsync_NonExistentAccount_ShouldReturnFalse()
    {
        // Act
        var deleted = await _accountService.DeleteAccountAsync("nonexistent");

        // Assert
        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAccountAsync_LastAccountForPlatform_ShouldSucceed()
    {
        // Arrange
        var account = CreateTestAccount();
        var createdAccount = await _accountService.CreateAccountAsync(account);

        // Act
        var deleted = await _accountService.DeleteAccountAsync(createdAccount.Id);

        // Assert
        deleted.Should().BeTrue();

        // Verify account is actually deleted
        var deletedAccount = await _accountService.GetAccountByIdAsync(createdAccount.Id);
        deletedAccount.Should().BeNull();

        // Verify no accounts remain for the platform
        var platformAccounts = await _accountService.GetAccountsForPlatformAsync(createdAccount.PlatformId);
        platformAccounts.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAccountAsync_DefaultAccount_ShouldSetNewDefault()
    {
        // Arrange
        var account1 = CreateTestAccount();
        var account2 = CreateTestAccount();
        account2.Username = "test2";

        var created1 = await _accountService.CreateAccountAsync(account1);
        var created2 = await _accountService.CreateAccountAsync(account2);

        // Act - Delete the default account (first one)
        await _accountService.DeleteAccountAsync(created1.Id);

        // Assert
        var remainingAccount = await _accountService.GetAccountByIdAsync(created2.Id);
        remainingAccount!.IsDefault.Should().BeTrue(); // Should become the new default
    }

    [Fact]
    public async Task GetDefaultAccountForPlatformAsync_ExistingDefault_ShouldReturnDefaultAccount()
    {
        // Arrange
        var account = CreateTestAccount();
        var createdAccount = await _accountService.CreateAccountAsync(account);

        // Act
        var defaultAccount = await _accountService.GetDefaultAccountForPlatformAsync(SocialPlatform.BlueSky);

        // Assert
        defaultAccount.Should().NotBeNull();
        defaultAccount!.Id.Should().Be(createdAccount.Id);
        defaultAccount.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task GetDefaultAccountForPlatformAsync_NoAccounts_ShouldReturnNull()
    {
        // Act
        var defaultAccount = await _accountService.GetDefaultAccountForPlatformAsync(SocialPlatform.BlueSky);

        // Assert
        defaultAccount.Should().BeNull();
    }

    [Fact]
    public async Task SetDefaultAccountAsync_ExistingAccount_ShouldSetAsDefault()
    {
        // Arrange
        var account1 = CreateTestAccount();
        var account2 = CreateTestAccount();
        account2.Username = "test2";

        var created1 = await _accountService.CreateAccountAsync(account1);
        var created2 = await _accountService.CreateAccountAsync(account2);

        // Act
        var result = await _accountService.SetDefaultAccountAsync(created2.Id);

        // Assert
        result.Should().BeTrue();

        var updated1 = await _accountService.GetAccountByIdAsync(created1.Id);
        var updated2 = await _accountService.GetAccountByIdAsync(created2.Id);

        updated1!.IsDefault.Should().BeFalse();
        updated2!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task SetDefaultAccountAsync_NonExistentAccount_ShouldReturnFalse()
    {
        // Act
        var result = await _accountService.SetDefaultAccountAsync("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AccountPersistence_ShouldSurviveServiceRecreation()
    {
        // Arrange
        var account = CreateTestAccount();
        var createdAccount = await _accountService.CreateAccountAsync(account);

        // Act - Create new service instance
        var newService = new SecureAccountService(_tempDirectory);
        var loadedAccounts = await newService.GetAllAccountsAsync();

        // Assert
        loadedAccounts.Should().HaveCount(1);
        var loadedAccount = loadedAccounts.First();
        loadedAccount.Id.Should().Be(createdAccount.Id);
        loadedAccount.Username.Should().Be(createdAccount.Username);
        loadedAccount.PlatformId.Should().Be(createdAccount.PlatformId);
    }

    [Fact]
    public async Task AccountService_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task<Account>>();

        // Act - Create multiple accounts concurrently
        for (int i = 0; i < 10; i++)
        {
            var account = CreateTestAccount();
            account.Username = $"test{i}";
            tasks.Add(_accountService.CreateAccountAsync(account));
        }

        var createdAccounts = await Task.WhenAll(tasks);

        // Assert
        createdAccounts.Should().HaveCount(10);
        createdAccounts.Select(a => a.Id).Should().OnlyHaveUniqueItems();
        createdAccounts.Select(a => a.Username).Should().OnlyHaveUniqueItems();

        var allAccounts = await _accountService.GetAllAccountsAsync();
        allAccounts.Should().HaveCount(10);
    }

    private static Account CreateTestAccount(SocialPlatform platform = SocialPlatform.BlueSky)
    {
        return new Account
        {
            Id = Guid.NewGuid().ToString(),
            PlatformId = platform,
            DisplayName = "Test Account",
            Username = "testuser",
            AuthStatus = AuthenticationStatus.Authenticated,
            Avatar = "https://example.com/avatar.jpg",
            LastUsed = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
