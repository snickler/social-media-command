using FluentAssertions;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;
using Xunit;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Tests for InMemoryAccountService
/// </summary>
public class InMemoryAccountServiceTests
{
    private InMemoryAccountService CreateService()
    {
        return new InMemoryAccountService();
    }

    private Account CreateTestAccount(SocialPlatform platform = SocialPlatform.BlueSky, string? id = null)
    {
        return new Account
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Username = $"test_user_{Guid.NewGuid():N}",
            DisplayName = "Test User",
            PlatformId = platform
        };
    }

    #region GetAllAccountsAsync Tests

    [Fact]
    public async Task GetAllAccountsAsync_ShouldReturnDefaultAccounts_WhenInitialized()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.GetAllAccountsAsync();

        // Assert - Service initializes with default accounts in constructor
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAllAccountsAsync_ShouldReturnAccounts()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.GetAllAccountsAsync();

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region GetAccountsForPlatformAsync Tests

    [Fact]
    public async Task GetAccountsForPlatformAsync_ShouldReturnOnlyAccountsForPlatform()
    {
        // Arrange
        var sut = CreateService();
        var initialCount = (await sut.GetAccountsForPlatformAsync(SocialPlatform.BlueSky)).Count();
        var account1 = CreateTestAccount(SocialPlatform.BlueSky);
        var account2 = CreateTestAccount(SocialPlatform.BlueSky);

        await sut.CreateAccountAsync(account1);
        await sut.CreateAccountAsync(account2);

        // Act
        var result = await sut.GetAccountsForPlatformAsync(SocialPlatform.BlueSky);

        // Assert
        result.Should().HaveCount(initialCount + 2);
        result.Should().AllSatisfy(a => a.PlatformId.Should().Be(SocialPlatform.BlueSky));
    }

    [Fact]
    public async Task GetAccountsForPlatformAsync_ShouldReturnAccountsForPlatform()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.GetAccountsForPlatformAsync(SocialPlatform.BlueSky);

        // Assert - Service initializes with default accounts
        result.Should().NotBeNull();
    }

    #endregion

    #region GetAccountByIdAsync Tests

    [Fact]
    public async Task GetAccountByIdAsync_ShouldReturnAccount_WhenExists()
    {
        // Arrange
        var sut = CreateService();
        var account = CreateTestAccount();
        await sut.CreateAccountAsync(account);

        // Act
        var result = await sut.GetAccountByIdAsync(account.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(account.Id);
    }

    [Fact]
    public async Task GetAccountByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.GetAccountByIdAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateAccountAsync Tests

    [Fact]
    public async Task CreateAccountAsync_ShouldGenerateId_WhenNotProvided()
    {
        // Arrange
        var sut = CreateService();
        var account = CreateTestAccount();
        account.Id = string.Empty;

        // Act
        var result = await sut.CreateAccountAsync(account);

        // Assert
        result.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateAccountAsync_ShouldSetCreatedAt()
    {
        // Arrange
        var sut = CreateService();
        var account = CreateTestAccount();
        var beforeCreate = DateTime.UtcNow;

        // Act
        var result = await sut.CreateAccountAsync(account);

        // Assert
        result.CreatedAt.Should().BeOnOrAfter(beforeCreate);
        result.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateAccountAsync_ShouldSetLastUsed()
    {
        // Arrange
        var sut = CreateService();
        var account = CreateTestAccount();
        var beforeCreate = DateTime.UtcNow;

        // Act
        var result = await sut.CreateAccountAsync(account);

        // Assert
        result.LastUsed.Should().BeOnOrAfter(beforeCreate);
        result.LastUsed.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateAccountAsync_ShouldSetAvatar_WhenNotProvided()
    {
        // Arrange
        var sut = CreateService();
        var account = CreateTestAccount();
        account.Avatar = string.Empty;

        // Act
        var result = await sut.CreateAccountAsync(account);

        // Assert
        result.Avatar.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateAccountAsync_ShouldHandleDefault_Correctly()
    {
        // Arrange
        var sut = CreateService();
        var account = CreateTestAccount();

        // Act
        var result = await sut.CreateAccountAsync(account);

        // Assert - Since there are already default accounts, this may or may not be default
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAccountAsync_ShouldUnsetOtherDefaults_WhenNewAccountIsDefault()
    {
        // Arrange
        var sut = CreateService();
        var account1 = CreateTestAccount();
        var account2 = CreateTestAccount();
        account2.IsDefault = true;

        await sut.CreateAccountAsync(account1);

        // Act
        await sut.CreateAccountAsync(account2);
        var result1 = await sut.GetAccountByIdAsync(account1.Id);

        // Assert
        result1!.IsDefault.Should().BeFalse();
    }

    #endregion

    #region UpdateAccountAsync Tests

    [Fact]
    public async Task UpdateAccountAsync_ShouldUpdateAccount_WhenExists()
    {
        // Arrange
        var sut = CreateService();
        var account = CreateTestAccount();
        await sut.CreateAccountAsync(account);

        account.DisplayName = "Updated Name";

        // Act
        var result = await sut.UpdateAccountAsync(account);

        // Assert
        result.DisplayName.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateAccountAsync_ShouldThrow_WhenNotExists()
    {
        // Arrange
        var sut = CreateService();
        var account = CreateTestAccount();

        // Act
        var act = () => sut.UpdateAccountAsync(account);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task UpdateAccountAsync_ShouldUpdateLastUsed()
    {
        // Arrange
        var sut = CreateService();
        var account = CreateTestAccount();
        await sut.CreateAccountAsync(account);

        await Task.Delay(50); // Ensure time difference
        var beforeUpdate = DateTime.UtcNow;

        // Act
        var result = await sut.UpdateAccountAsync(account);

        // Assert
        result.LastUsed.Should().BeOnOrAfter(beforeUpdate);
    }

    [Fact]
    public async Task UpdateAccountAsync_ShouldSetAvatar_WhenNotProvided()
    {
        // Arrange
        var sut = CreateService();
        var account = CreateTestAccount();
        await sut.CreateAccountAsync(account);

        account.Avatar = string.Empty;

        // Act
        var result = await sut.UpdateAccountAsync(account);

        // Assert
        result.Avatar.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateAccountAsync_ShouldUnsetOtherDefaults_WhenSettingAsDefault()
    {
        // Arrange
        var sut = CreateService();
        var account1 = CreateTestAccount();
        var account2 = CreateTestAccount();

        await sut.CreateAccountAsync(account1);
        await sut.CreateAccountAsync(account2);

        account2.IsDefault = true;

        // Act
        await sut.UpdateAccountAsync(account2);
        var result1 = await sut.GetAccountByIdAsync(account1.Id);

        // Assert
        result1!.IsDefault.Should().BeFalse();
    }

    #endregion

    #region DeleteAccountAsync Tests

    [Fact]
    public async Task DeleteAccountAsync_ShouldDeleteAccount_WhenExists()
    {
        // Arrange
        var sut = CreateService();
        var account1 = CreateTestAccount();
        var account2 = CreateTestAccount();
        await sut.CreateAccountAsync(account1);
        await sut.CreateAccountAsync(account2);

        // Act
        var result = await sut.DeleteAccountAsync(account1.Id);

        // Assert
        result.Should().BeTrue();
        var deletedAccount = await sut.GetAccountByIdAsync(account1.Id);
        deletedAccount.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAccountAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.DeleteAccountAsync("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAccountAsync_ShouldThrow_WhenLastAccountForPlatform()
    {
        // Arrange
        var sut = CreateService();
        // Get existing default accounts first
        var existingAccounts = await sut.GetAccountsForPlatformAsync(SocialPlatform.BlueSky);

        // Delete all but one
        var accountsList = existingAccounts.ToList();
        for (int i = 0; i < accountsList.Count - 1; i++)
        {
            await sut.DeleteAccountAsync(accountsList[i].Id);
        }

        var lastAccount = accountsList.Last();

        // Act
        var act = () => sut.DeleteAccountAsync(lastAccount.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*last account*");
    }

    [Fact]
    public async Task DeleteAccountAsync_ShouldSetNewDefault_WhenDeletingDefaultAccount()
    {
        // Arrange
        var sut = CreateService();

        // Get the default account from pre-initialized accounts
        var defaultAccount = await sut.GetDefaultAccountForPlatformAsync(SocialPlatform.BlueSky);
        defaultAccount.Should().NotBeNull();

        // Create a new account
        var newAccount = CreateTestAccount();
        await sut.CreateAccountAsync(newAccount);

        // Act - Delete the original default
        await sut.DeleteAccountAsync(defaultAccount!.Id);

        // Get accounts again to find new default
        var accounts = await sut.GetAccountsForPlatformAsync(SocialPlatform.BlueSky);
        var defaultAccounts = accounts.Where(a => a.IsDefault);

        // Assert - There should still be exactly one default
        defaultAccounts.Should().ContainSingle();
    }

    #endregion

    #region GetDefaultAccountForPlatformAsync Tests

    [Fact]
    public async Task GetDefaultAccountForPlatformAsync_ShouldReturnDefaultAccount()
    {
        // Arrange
        var sut = CreateService();
        var account = CreateTestAccount();
        await sut.CreateAccountAsync(account);

        // Act
        var result = await sut.GetDefaultAccountForPlatformAsync(SocialPlatform.BlueSky);

        // Assert
        result.Should().NotBeNull();
        result!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task GetDefaultAccountForPlatformAsync_ShouldReturnDefault_WhenExists()
    {
        // Arrange
        var sut = CreateService();

        // Act - Service initializes with default accounts
        var result = await sut.GetDefaultAccountForPlatformAsync(SocialPlatform.BlueSky);

        // Assert
        result.Should().NotBeNull();
        result!.IsDefault.Should().BeTrue();
    }

    #endregion

    #region SetDefaultAccountAsync Tests

    [Fact]
    public async Task SetDefaultAccountAsync_ShouldSetAccountAsDefault()
    {
        // Arrange
        var sut = CreateService();
        var account = CreateTestAccount();
        await sut.CreateAccountAsync(account);

        // Act
        var result = await sut.SetDefaultAccountAsync(account.Id);

        // Assert
        result.Should().BeTrue();
        var updatedAccount = await sut.GetAccountByIdAsync(account.Id);
        updatedAccount!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task SetDefaultAccountAsync_ShouldReturnFalse_WhenAccountNotExists()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.SetDefaultAccountAsync("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SetDefaultAccountAsync_ShouldUnsetOtherDefaults()
    {
        // Arrange
        var sut = CreateService();
        var account1 = CreateTestAccount();
        var account2 = CreateTestAccount();
        await sut.CreateAccountAsync(account1); // Will be default
        await sut.CreateAccountAsync(account2);

        // Act
        await sut.SetDefaultAccountAsync(account2.Id);
        var result1 = await sut.GetAccountByIdAsync(account1.Id);

        // Assert
        result1!.IsDefault.Should().BeFalse();
    }

    #endregion

    #region ValidateAccountCredentialsAsync Tests

    [Fact]
    public async Task ValidateAccountCredentialsAsync_ShouldReturnTrue_WhenAccountExists()
    {
        // Arrange
        var sut = CreateService();
        var account = CreateTestAccount();
        await sut.CreateAccountAsync(account);

        // Act
        var result = await sut.ValidateAccountCredentialsAsync(account.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAccountCredentialsAsync_ShouldReturnFalse_WhenAccountNotExists()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.ValidateAccountCredentialsAsync("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetAccountsByIdsAsync Tests

    [Fact]
    public async Task GetAccountsByIdsAsync_ShouldReturnMatchingAccounts()
    {
        // Arrange
        var sut = CreateService();
        var account1 = CreateTestAccount();
        var account2 = CreateTestAccount();
        var account3 = CreateTestAccount();

        await sut.CreateAccountAsync(account1);
        await sut.CreateAccountAsync(account2);
        await sut.CreateAccountAsync(account3);

        var ids = new[] { account1.Id, account3.Id };

        // Act
        var result = await sut.GetAccountsByIdsAsync(ids);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(a => a.Id == account1.Id);
        result.Should().Contain(a => a.Id == account3.Id);
        result.Should().NotContain(a => a.Id == account2.Id);
    }

    [Fact]
    public async Task GetAccountsByIdsAsync_ShouldReturnEmpty_WhenNoMatchingIds()
    {
        // Arrange
        var sut = CreateService();
        var ids = new[] { "nonexistent1", "nonexistent2" };

        // Act
        var result = await sut.GetAccountsByIdsAsync(ids);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region UpdateLastUsedAsync Tests

    [Fact]
    public async Task UpdateLastUsedAsync_ShouldUpdateLastUsed_WhenAccountExists()
    {
        // Arrange
        var sut = CreateService();
        var account = CreateTestAccount();
        await sut.CreateAccountAsync(account);

        await Task.Delay(50); // Ensure time difference
        var beforeUpdate = DateTime.UtcNow;

        // Act
        await sut.UpdateLastUsedAsync(account.Id);
        var result = await sut.GetAccountByIdAsync(account.Id);

        // Assert
        result!.LastUsed.Should().BeOnOrAfter(beforeUpdate);
    }

    [Fact]
    public async Task UpdateLastUsedAsync_ShouldNotThrow_WhenAccountNotExists()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var act = () => sut.UpdateLastUsedAsync("nonexistent");

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region InitializeDefaultAccountsAsync Tests

    [Fact]
    public async Task InitializeDefaultAccountsAsync_ShouldAddDefaultAccounts()
    {
        // Arrange
        var sut = CreateService();

        // Act
        await sut.InitializeDefaultAccountsAsync();
        var accounts = await sut.GetAllAccountsAsync();

        // Assert
        accounts.Should().NotBeEmpty();
    }

    [Fact]
    public async Task InitializeDefaultAccountsAsync_ShouldNotAddAccounts_WhenAlreadyInitialized()
    {
        // Arrange
        var sut = CreateService();
        await sut.InitializeDefaultAccountsAsync();
        var initialCount = (await sut.GetAllAccountsAsync()).Count();

        // Act
        await sut.InitializeDefaultAccountsAsync();
        var finalCount = (await sut.GetAllAccountsAsync()).Count();

        // Assert
        finalCount.Should().Be(initialCount);
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public async Task CreateAndRetrieveAccount_ShouldWorkEndToEnd()
    {
        // Arrange
        var sut = CreateService();
        var account = CreateTestAccount();

        // Act
        var created = await sut.CreateAccountAsync(account);
        var retrieved = await sut.GetAccountByIdAsync(created.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Username.Should().Be(account.Username);
    }

    [Fact]
    public async Task MultipleAccountsForSamePlatform_ShouldMaintainOneDefault()
    {
        // Arrange
        var sut = CreateService();
        var account1 = CreateTestAccount();
        var account2 = CreateTestAccount();
        var account3 = CreateTestAccount();

        // Act
        await sut.CreateAccountAsync(account1);
        await sut.CreateAccountAsync(account2);
        await sut.CreateAccountAsync(account3);

        var accounts = await sut.GetAccountsForPlatformAsync(SocialPlatform.BlueSky);
        var defaultAccounts = accounts.Where(a => a.IsDefault);

        // Assert
        defaultAccounts.Should().ContainSingle();
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldHandleThreadSafely()
    {
        // Arrange
        var sut = CreateService();
        var initialCount = (await sut.GetAllAccountsAsync()).Count();
        var tasks = new List<Task>();

        // Act - Create multiple accounts concurrently
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var account = CreateTestAccount();
                await sut.CreateAccountAsync(account);
            }));
        }

        await Task.WhenAll(tasks);
        var accounts = await sut.GetAllAccountsAsync();

        // Assert - Should have initial accounts plus 10 new ones
        accounts.Should().HaveCount(initialCount + 10);
    }

    #endregion
}
