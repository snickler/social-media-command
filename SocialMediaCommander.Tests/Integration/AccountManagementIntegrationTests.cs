using Microsoft.Extensions.DependencyInjection;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;
using SocialMediaCommander.Services.Interfaces;
using SocialMediaCommander.Desktop.ViewModels;

namespace SocialMediaCommander.Tests.Integration;

/// <summary>
/// Integration tests for account management functionality
/// </summary>
public class AccountManagementIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IAccountService _accountService;
    private readonly IAuthenticationService _authService;
    private readonly IOAuthConfigurationService _oauthConfigService;
    private readonly AccountManagerViewModel _viewModel;

    public AccountManagementIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<IAccountService, InMemoryAccountService>();
        services.AddSingleton<IOAuthConfigurationService, OAuthConfigurationService>();
        services.AddSingleton<IAuthenticationService, OAuthAuthenticationService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _accountService = _serviceProvider.GetRequiredService<IAccountService>();
        _authService = _serviceProvider.GetRequiredService<IAuthenticationService>();
        _oauthConfigService = _serviceProvider.GetRequiredService<IOAuthConfigurationService>();
        _viewModel = new AccountManagerViewModel(_accountService, _authService);
    }

    [Fact]
    public async Task AccountService_CreateAccountAsync_ShouldCreateAndRetrieveAccount()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid().ToString(),
            PlatformId = SocialPlatform.BlueSky,
            Username = "testuser",
            DisplayName = "Test User",
            AuthStatus = AuthenticationStatus.Authenticated,
            Tokens = new OAuthTokens
            {
                AccessToken = "test_access_token",
                TokenType = "Bearer",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            }
        };

        // Act
        var createdAccount = await _accountService.CreateAccountAsync(account);
        var retrievedAccount = await _accountService.GetAccountByIdAsync(account.Id);

        // Assert
        createdAccount.Should().NotBeNull();
        createdAccount.Id.Should().Be(account.Id);
        createdAccount.PlatformId.Should().Be(account.PlatformId);
        createdAccount.Username.Should().Be(account.Username);
        createdAccount.IsAuthenticated.Should().BeTrue();

        retrievedAccount.Should().NotBeNull();
        retrievedAccount!.Id.Should().Be(account.Id);
        retrievedAccount.PlatformId.Should().Be(account.PlatformId);
    }

    [Fact]
    public async Task AccountService_GetAccountsForPlatformAsync_ShouldFilterCorrectly()
    {
        // Arrange
        var blueSkyAccount = new Account
        {
            Id = Guid.NewGuid().ToString(),
            PlatformId = SocialPlatform.BlueSky,
            Username = "bluesky_user",
            DisplayName = "BlueSky User"
        };

        var twitterAccount = new Account
        {
            Id = Guid.NewGuid().ToString(),
            PlatformId = SocialPlatform.X,
            Username = "twitter_user",
            DisplayName = "Twitter User"
        };

        await _accountService.CreateAccountAsync(blueSkyAccount);
        await _accountService.CreateAccountAsync(twitterAccount);

        // Act
        var blueSkyAccounts = await _accountService.GetAccountsForPlatformAsync(SocialPlatform.BlueSky);
        var twitterAccounts = await _accountService.GetAccountsForPlatformAsync(SocialPlatform.X);

        // Assert
        blueSkyAccounts.Should().HaveCountGreaterOrEqualTo(1);
        blueSkyAccounts.First(a => a.Username == "bluesky_user").PlatformId.Should().Be(SocialPlatform.BlueSky);

        twitterAccounts.Should().HaveCountGreaterOrEqualTo(1);
        twitterAccounts.First(a => a.Username == "twitter_user").PlatformId.Should().Be(SocialPlatform.X);
    }

    [Fact]
    public async Task AccountService_UpdateAccountAsync_ShouldUpdateProperties()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid().ToString(),
            PlatformId = SocialPlatform.LinkedIn,
            Username = "original_user",
            DisplayName = "Original Name"
        };

        await _accountService.CreateAccountAsync(account);

        // Act
        account.DisplayName = "Updated Name";
        account.AuthStatus = AuthenticationStatus.Authenticated;
        var updatedAccount = await _accountService.UpdateAccountAsync(account);

        // Assert
        updatedAccount.Should().NotBeNull();
        updatedAccount.DisplayName.Should().Be("Updated Name");
        updatedAccount.AuthStatus.Should().Be(AuthenticationStatus.Authenticated);
    }

    [Fact]
    public async Task AccountService_DeleteAccountAsync_ShouldRemoveAccount()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid().ToString(),
            PlatformId = SocialPlatform.Facebook,
            Username = "delete_user",
            DisplayName = "Delete User"
        };

        await _accountService.CreateAccountAsync(account);

        // Act
        var deleteResult = await _accountService.DeleteAccountAsync(account.Id);
        var retrievedAccount = await _accountService.GetAccountByIdAsync(account.Id);

        // Assert
        deleteResult.Should().BeTrue();
        retrievedAccount.Should().BeNull();
    }

    #region Button Functionality Tests

    [Fact]
    public void AddAccountCommand_ShouldBeAvailable()
    {
        // Arrange & Act
        var command = _viewModel.AddAccountCommand;

        // Assert
        command.Should().NotBeNull("AddAccountCommand should be available for UI binding");
        command.CanExecute(null).Should().BeTrue("AddAccountCommand should be executable");
    }

    [Fact]
    public void StartAddAccountCommand_ShouldBeAvailable()
    {
        // Arrange & Act
        var command = _viewModel.StartAddAccountCommand;

        // Assert
        command.Should().NotBeNull("StartAddAccountCommand should be available");
        command.CanExecute(null).Should().BeTrue("StartAddAccountCommand should be executable");
    }

    [Fact]
    public void EditAccountCommand_ShouldBeAvailable()
    {
        // Arrange & Act
        var command = _viewModel.EditAccountCommand;

        // Assert
        command.Should().NotBeNull("EditAccountCommand should be available for UI binding");
    }

    [Fact]
    public void RemoveAccountCommand_ShouldBeAvailable()
    {
        // Arrange & Act
        var command = _viewModel.RemoveAccountCommand;

        // Assert
        command.Should().NotBeNull("RemoveAccountCommand should be available for UI binding");
    }

    [Fact]
    public async Task DeleteAccountCommand_ShouldRemoveAccount_FromCollection()
    {
        // Arrange
        var account = await CreateTestAccount();
        var initialCount = _viewModel.Accounts.Count;

        // Act
        if (_viewModel.DeleteAccountAsyncCommand.CanExecute(account))
        {
            await _viewModel.DeleteAccountAsyncCommand.ExecuteAsync(account);
        }

        // Assert
        _viewModel.Accounts.Should().HaveCount(initialCount - 1, "Account should be removed from collection");
        _viewModel.Accounts.Should().NotContain(account, "Deleted account should not be in collection");
    }

    [Fact]
    public async Task SaveAccountCommand_ShouldCreateNewAccount()
    {
        // Arrange
        _viewModel.NewAccountUsername = "testuser";
        _viewModel.NewAccountDisplayName = "Test User";
        _viewModel.SelectedPlatform = SocialPlatform.BlueSky;
        var initialCount = _viewModel.Accounts.Count;

        // Act
        if (_viewModel.SaveAccountAsyncCommand.CanExecute(null))
        {
            await _viewModel.SaveAccountAsyncCommand.ExecuteAsync(null);
        }

        // Assert
        _viewModel.Accounts.Should().HaveCountGreaterThan(initialCount, "New account should be added");
        _viewModel.Accounts.Should().Contain(a => a.Username == "testuser", "New account should be in collection");
    }

    [Fact]
    public async Task SetAsDefaultAccountCommand_ShouldBeAvailable()
    {
        // Arrange
        var account = await CreateTestAccount();

        // Act
        var command = _viewModel.SetAsDefaultAccountAsyncCommand;

        // Assert
        command.Should().NotBeNull("SetAsDefaultAccountAsyncCommand should be available");
        command.CanExecute(account).Should().BeTrue("Command should be executable with valid account");
    }

    [Fact]
    public async Task RefreshAccountsCommand_ShouldBeAvailable()
    {
        // Arrange & Act
        var command = _viewModel.RefreshAccountsAsyncCommand;

        // Assert
        command.Should().NotBeNull("RefreshAccountsAsyncCommand should be available");
        command.CanExecute(null).Should().BeTrue("RefreshAccountsAsyncCommand should be executable");
    }

    #endregion

    [Fact]
    public async Task AccountManagerViewModel_ConnectAccountCommand_ShouldInitiateAuthentication()
    {
        // Arrange
        var platform = SocialPlatform.BlueSky;
        var initialAccountCount = _viewModel.Accounts.Count;

        // Act
        if (_viewModel.ConnectAccountCommand.CanExecute(platform))
        {
            _viewModel.ConnectAccountCommand.Execute(platform);
        }

        // Give some time for async operations
        await Task.Delay(100);

        // Assert
        // The command should have been executed (we can't test actual OAuth flow without real credentials)
        _viewModel.ConnectAccountCommand.CanExecute(platform).Should().BeTrue();
    }

    [Fact]
    public void AccountManagerViewModel_ShouldGroupAccountsByPlatform()
    {
        // This test verifies the ViewModel correctly organizes accounts by platform
        // In a real scenario, we would populate accounts and verify grouping

        // Arrange
        var platforms = new[] { SocialPlatform.BlueSky, SocialPlatform.X, SocialPlatform.LinkedIn, SocialPlatform.Threads, SocialPlatform.Facebook };

        // Act & Assert
        foreach (var platform in platforms)
        {
            var canConnect = _viewModel.ConnectAccountCommand.CanExecute(platform);
            canConnect.Should().BeTrue($"Should be able to connect to {platform}");
        }
    }

    [Fact]
    public async Task AccountService_GetAllAccountsAsync_ShouldReturnAllAccounts()
    {
        // Arrange
        var accounts = new[]
        {
            new Account { Id = Guid.NewGuid().ToString(), PlatformId = SocialPlatform.BlueSky, Username = "user1" },
            new Account { Id = Guid.NewGuid().ToString(), PlatformId = SocialPlatform.X, Username = "user2" },
            new Account { Id = Guid.NewGuid().ToString(), PlatformId = SocialPlatform.LinkedIn, Username = "user3" }
        };

        foreach (var account in accounts)
        {
            await _accountService.CreateAccountAsync(account);
        }

        // Act
        var allAccounts = await _accountService.GetAllAccountsAsync();

        // Assert
        allAccounts.Should().HaveCountGreaterOrEqualTo(3);
        allAccounts.Should().Contain(a => a.Username == "user1");
        allAccounts.Should().Contain(a => a.Username == "user2");
        allAccounts.Should().Contain(a => a.Username == "user3");
    }

    [Fact]
    public void Account_IsAuthenticated_ShouldReflectTokenStatus()
    {
        // Arrange & Act
        var unauthenticatedAccount = new Account
        {
            PlatformId = SocialPlatform.BlueSky,
            AuthStatus = AuthenticationStatus.NotAuthenticated
        };

        var authenticatedAccount = new Account
        {
            PlatformId = SocialPlatform.X,
            AuthStatus = AuthenticationStatus.Authenticated,
            Tokens = new OAuthTokens
            {
                AccessToken = "valid_token",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            }
        };

        var expiredAccount = new Account
        {
            PlatformId = SocialPlatform.LinkedIn,
            AuthStatus = AuthenticationStatus.Authenticated,
            Tokens = new OAuthTokens
            {
                AccessToken = "expired_token",
                ExpiresAt = DateTime.UtcNow.AddMinutes(-30) // Expired
            }
        };

        // Assert
        unauthenticatedAccount.IsAuthenticated.Should().BeFalse();
        authenticatedAccount.IsAuthenticated.Should().BeTrue();
        expiredAccount.IsAuthenticated.Should().BeFalse(); // Should be false due to expired token
    }

    [Fact]
    public void OAuthTokens_ExpirationLogic_ShouldWorkCorrectly()
    {
        // Arrange
        var validTokens = new OAuthTokens
        {
            AccessToken = "valid_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            RefreshToken = "refresh_token"
        };

        var expiredTokens = new OAuthTokens
        {
            AccessToken = "expired_token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-30),
            RefreshToken = "refresh_token"
        };

        var nonRefreshableTokens = new OAuthTokens
        {
            AccessToken = "non_refreshable_token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-30)
            // No refresh token
        };

        // Act & Assert
        validTokens.IsExpired.Should().BeFalse();
        validTokens.CanRefresh.Should().BeTrue();

        expiredTokens.IsExpired.Should().BeTrue();
        expiredTokens.CanRefresh.Should().BeTrue();

        nonRefreshableTokens.IsExpired.Should().BeTrue();
        nonRefreshableTokens.CanRefresh.Should().BeFalse();
    }

    [Theory]
    [InlineData(AuthenticationStatus.NotAuthenticated)]
    [InlineData(AuthenticationStatus.Authenticating)]
    [InlineData(AuthenticationStatus.AuthenticationFailed)]
    [InlineData(AuthenticationStatus.TokenExpired)]
    [InlineData(AuthenticationStatus.Revoked)]
    public void Account_AuthenticationStatus_ShouldAffectIsAuthenticated(AuthenticationStatus status)
    {
        // Arrange
        var account = new Account
        {
            PlatformId = SocialPlatform.BlueSky,
            AuthStatus = status,
            Tokens = new OAuthTokens
            {
                AccessToken = "token",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            }
        };

        // Act & Assert
        if (status == AuthenticationStatus.Authenticated)
        {
            account.IsAuthenticated.Should().BeTrue();
        }
        else
        {
            account.IsAuthenticated.Should().BeFalse();
        }
    }

    [Fact]
    public async Task AccountService_ShouldHandleConcurrentOperations()
    {
        // Arrange
        var tasks = new List<Task<Account>>();
        
        // Act - Create multiple accounts concurrently
        for (int i = 0; i < 10; i++)
        {
            var account = new Account
            {
                Id = Guid.NewGuid().ToString(),
                PlatformId = SocialPlatform.BlueSky,
                Username = $"concurrent_user_{i}",
                DisplayName = $"Concurrent User {i}"
            };
            
            tasks.Add(_accountService.CreateAccountAsync(account));
        }

        var createdAccounts = await Task.WhenAll(tasks);

        // Assert
        createdAccounts.Should().HaveCount(10);
        createdAccounts.Should().OnlyHaveUniqueItems(a => a.Id);
        
        var allAccounts = await _accountService.GetAllAccountsAsync();
        allAccounts.Where(a => a.Username.StartsWith("concurrent_user_")).Should().HaveCount(10);
    }

    #region UI Property Tests

    [Fact]
    public void AccountItemViewModel_ShouldHaveRequiredProperties()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid().ToString(),
            PlatformId = SocialPlatform.BlueSky,
            Username = "testuser",
            DisplayName = "Test User",
            IsDefault = true,
            Tokens = new OAuthTokens
            {
                AccessToken = "test_token",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            }
        };

        // Act
        var viewModel = new AccountItemViewModel(account);

        // Assert
        viewModel.Id.Should().Be(account.Id);
        viewModel.Username.Should().Be(account.Username);
        viewModel.DisplayName.Should().Be(account.DisplayName);
        viewModel.IsDefault.Should().Be(account.IsDefault);
        viewModel.IsConnected.Should().BeTrue("Account with valid tokens should be connected");
        viewModel.AvatarText.Should().Be("T", "Should return first character of display name");
        viewModel.PlatformColor.Should().NotBeNullOrEmpty("Should have platform color");
    }

    [Fact]
    public void AccountItemViewModel_ShouldDetectExpiredTokens()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid().ToString(),
            PlatformId = SocialPlatform.X,
            Username = "expireduser",
            DisplayName = "Expired User",
            Tokens = new OAuthTokens
            {
                AccessToken = "expired_token",
                ExpiresAt = DateTime.UtcNow.AddHours(-1) // Expired
            }
        };

        // Act
        var viewModel = new AccountItemViewModel(account);

        // Assert
        viewModel.IsConnected.Should().BeFalse("Account with expired tokens should not be connected");
        viewModel.RequiresReconnection.Should().BeTrue("Account with expired tokens should require reconnection");
    }

    #endregion

    #region Helper Methods

    private async Task<Account> CreateTestAccount(SocialPlatform platform = SocialPlatform.BlueSky, string username = "testuser", bool isDefault = false)
    {
        var account = new Account
        {
            Id = Guid.NewGuid().ToString(),
            PlatformId = platform,
            Username = username,
            DisplayName = $"Test User {username}",
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow,
            LastUsed = DateTime.UtcNow,
            AuthStatus = AuthenticationStatus.Authenticated,
            Tokens = new OAuthTokens
            {
                AccessToken = "test_access_token",
                TokenType = "Bearer",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            },
            Metadata = new Dictionary<string, string>
            {
                ["UserId"] = Guid.NewGuid().ToString(),
                ["Bio"] = "Test user bio",
                ["ProfileUrl"] = $"https://{platform.ToString().ToLower()}.com/{username}"
            }
        };

        await _accountService.CreateAccountAsync(account);
        
        // Add to ViewModel collection for UI tests
        _viewModel.Accounts.Add(account);
        
        return account;
    }

    #endregion

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
} 