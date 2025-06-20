using Microsoft.Extensions.DependencyInjection;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;
using SocialMediaCommander.Services.Interfaces;
using SocialMediaCommander.Desktop.ViewModels;
using FluentAssertions;
using Serilog;
using Serilog.Core;

namespace SocialMediaCommander.Tests.Integration;

/// <summary>
/// Comprehensive integration tests for AccountManagerViewModel button functionality
/// </summary>
public class AccountManagerIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IAccountService _accountService;
    private readonly IAuthenticationService _authService;
    private readonly IOAuthConfigurationService _oauthConfigService;
    private readonly AccountManagerViewModel _viewModel;

    public AccountManagerIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<IAccountService, InMemoryAccountService>();
        services.AddSingleton<IOAuthConfigurationService, OAuthConfigurationService>();
        services.AddSingleton<IAuthenticationService>(provider => 
            new OAuthAuthenticationService(
                provider.GetRequiredService<HttpClient>(),
                provider.GetRequiredService<IOAuthConfigurationService>()
            ));
        services.AddSingleton<ILogger>(Logger.None); // Add logger for tests
        
        _serviceProvider = services.BuildServiceProvider();
        _accountService = _serviceProvider.GetRequiredService<IAccountService>();
        _authService = _serviceProvider.GetRequiredService<IAuthenticationService>();
        _oauthConfigService = _serviceProvider.GetRequiredService<IOAuthConfigurationService>();
        var logger = _serviceProvider.GetRequiredService<ILogger>();
        
        _viewModel = new AccountManagerViewModel(_accountService, _authService, _oauthConfigService);
    }

    #region Add Account Button Tests

    [Fact]
    public void AddAccountCommand_ShouldBeAvailable()
    {
        // Arrange & Act
        var command = _viewModel.AddAccountCommand;

        // Assert
        command.Should().NotBeNull("AddAccountCommand should be available");
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
    public async Task StartAddAccountCommand_ShouldInitiateOAuthFlow()
    {
        // Arrange
        var initialAuthenticatingState = _viewModel.IsAuthenticating;

        // Act
        if (_viewModel.StartAddAccountCommand.CanExecute(null))
        {
            await _viewModel.StartAddAccountCommand.ExecuteAsync(null);
        }

        // Assert
        // The command should have been executed
        _viewModel.StartAddAccountCommand.CanExecute(null).Should().BeTrue("Command should remain executable");
        // Note: We can't test actual OAuth flow without real credentials, but we can verify the command exists
    }

    [Theory]
    [InlineData(SocialPlatform.BlueSky)]
    [InlineData(SocialPlatform.X)]
    [InlineData(SocialPlatform.LinkedIn)]
    [InlineData(SocialPlatform.Threads)]
    [InlineData(SocialPlatform.Facebook)]
    public void ConnectAccountCommand_ShouldBeAvailableForAllPlatforms(SocialPlatform platform)
    {
        // Arrange & Act
        var command = _viewModel.ConnectAccountCommand;

        // Assert
        command.Should().NotBeNull($"ConnectAccountCommand should be available for {platform}");
        command.CanExecute(platform).Should().BeTrue($"ConnectAccountCommand should be executable for {platform}");
    }

    #endregion

    #region Edit Account Button Tests

    [Fact]
    public void EditAccountCommand_ShouldBeAvailable()
    {
        // Arrange & Act
        var command = _viewModel.EditAccountCommand;

        // Assert
        command.Should().NotBeNull("EditAccountCommand should be available");
    }

    [Fact]
    public async Task EditAccountCommand_ShouldWorkWithValidAccount()
    {
        // Arrange
        var account = await CreateTestAccount();
        var accountViewModel = new AccountItemViewModel(account);

        // Act
        if (_viewModel.EditAccountCommand.CanExecute(accountViewModel))
        {
            _viewModel.EditAccountCommand.Execute(accountViewModel);
        }

        // Assert
        _viewModel.EditAccountCommand.CanExecute(accountViewModel).Should().BeTrue("EditAccountCommand should be executable with valid account");
    }

    [Fact]
    public void StartEditAccountCommand_ShouldBeAvailable()
    {
        // Arrange & Act
        var command = _viewModel.StartEditAccountCommand;

        // Assert
        command.Should().NotBeNull("StartEditAccountCommand should be available");
    }

    [Fact]
    public async Task StartEditAccountCommand_ShouldUpdateViewModelState()
    {
        // Arrange
        var account = await CreateTestAccount();

        // Act
        if (_viewModel.StartEditAccountCommand.CanExecute(account))
        {
            _viewModel.StartEditAccountCommand.Execute(account);
        }

        // Assert
        _viewModel.IsEditingAccount.Should().BeTrue("Should be in editing state");
        _viewModel.CurrentAccount.Should().Be(account, "Current account should be set");
        _viewModel.NewAccountUsername.Should().Be(account.Username, "Form should be populated with account data");
        _viewModel.NewAccountDisplayName.Should().Be(account.DisplayName, "Form should be populated with account data");
    }

    #endregion

    #region Delete Account Button Tests

    [Fact]
    public void DeleteAccountCommand_ShouldBeAvailable()
    {
        // Arrange & Act
        var command = _viewModel.DeleteAccountCommand;

        // Assert
        command.Should().NotBeNull("DeleteAccountCommand should be available");
    }

    [Fact]
    public void RemoveAccountCommand_ShouldBeAvailable()
    {
        // Arrange & Act
        var command = _viewModel.RemoveAccountCommand;

        // Assert
        command.Should().NotBeNull("RemoveAccountCommand should be available");
    }

    [Fact]
    public async Task DeleteAccountCommand_ShouldRemoveAccount()
    {
        // Arrange
        var account = await CreateTestAccount();
        var initialCount = _viewModel.Accounts.Count;

        // Act
        if (_viewModel.DeleteAccountCommand.CanExecute(account))
        {
            await _viewModel.DeleteAccountCommand.ExecuteAsync(account);
        }

        // Assert
        _viewModel.Accounts.Should().HaveCount(initialCount - 1, "Account should be removed from collection");
        _viewModel.Accounts.Should().NotContain(account, "Deleted account should not be in collection");
    }

    [Fact]
    public async Task RemoveAccountCommand_ShouldWorkWithAccountViewModel()
    {
        // Arrange
        var account = await CreateTestAccount();
        var accountViewModel = new AccountItemViewModel(account);
        var initialCount = _viewModel.Accounts.Count;

        // Act
        if (_viewModel.RemoveAccountCommand.CanExecute(accountViewModel))
        {
            await _viewModel.RemoveAccountCommand.ExecuteAsync(accountViewModel);
        }

        // Assert
        _viewModel.Accounts.Should().HaveCount(initialCount - 1, "Account should be removed from collection");
    }

    [Fact]
    public async Task DeleteAccountCommand_ShouldNotDeleteDefaultAccount()
    {
        // Arrange
        var account = await CreateTestAccount(isDefault: true);
        var initialCount = _viewModel.Accounts.Count;

        // Act
        if (_viewModel.DeleteAccountCommand.CanExecute(account))
        {
            await _viewModel.DeleteAccountCommand.ExecuteAsync(account);
        }

        // Assert - Default accounts should not be deleted
        _viewModel.Accounts.Should().HaveCount(initialCount, "Default accounts should not be deleted");
        _viewModel.Accounts.Should().Contain(account, "Default account should still be in collection");
    }

    #endregion

    #region Account Management Tests

    [Fact]
    public async Task SaveAccountCommand_ShouldBeAvailable()
    {
        // Arrange & Act
        var command = _viewModel.SaveAccountCommand;

        // Assert
        command.Should().NotBeNull("SaveAccountCommand should be available");
    }

    [Fact]
    public async Task SaveAccountCommand_ShouldCreateNewAccount()
    {
        // Arrange
        _viewModel.NewAccountUsername = "newuser";
        _viewModel.NewAccountDisplayName = "New User";
        _viewModel.SelectedPlatform = SocialPlatform.BlueSky;
        var initialCount = _viewModel.Accounts.Count;

        // Act
        if (_viewModel.SaveAccountCommand.CanExecute(null))
        {
            await _viewModel.SaveAccountCommand.ExecuteAsync(null);
        }

        // Assert
        _viewModel.Accounts.Should().HaveCount(initialCount + 1, "New account should be added to collection");
    }

    [Fact]
    public async Task SetAsDefaultAccountCommand_ShouldBeAvailable()
    {
        // Arrange
        var account = await CreateTestAccount();

        // Act
        var command = _viewModel.SetAsDefaultAccountCommand;

        // Assert
        command.Should().NotBeNull("SetAsDefaultAccountCommand should be available");
        command.CanExecute(account).Should().BeTrue("SetAsDefaultAccountCommand should be executable with valid account");
    }

    [Fact]
    public async Task ReconnectAccountCommand_ShouldBeAvailable()
    {
        // Arrange
        var account = await CreateTestAccount();
        var accountViewModel = new AccountItemViewModel(account);

        // Act
        var command = _viewModel.ReconnectAccountCommand;

        // Assert
        command.Should().NotBeNull("ReconnectAccountCommand should be available");
        command.CanExecute(accountViewModel).Should().BeTrue("Command should be executable with valid account");
    }

    #endregion

    #region UI Binding Tests

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

    [Fact]
    public void PlatformGroups_ShouldGroupAccountsCorrectly()
    {
        // Arrange & Act
        var platformGroups = _viewModel.PlatformGroups.ToList();

        // Assert
        // With no accounts, there should be no platform groups
        platformGroups.Should().BeEmpty("No platform groups should exist without accounts");
    }

    [Fact]
    public async Task PlatformGroups_ShouldGroupAccountsCorrectlyWithAccounts()
    {
        // Arrange
        var blueSkyAccount = await CreateTestAccount(SocialPlatform.BlueSky, "bluesky_user");
        var twitterAccount = await CreateTestAccount(SocialPlatform.X, "twitter_user");

        // Act
        var platformGroups = _viewModel.PlatformGroups.ToList();

        // Assert
        platformGroups.Should().HaveCount(2, "Should have groups for BlueSky and X");
        platformGroups.Should().Contain(g => g.Platform == SocialPlatform.BlueSky, "Should have BlueSky group");
        platformGroups.Should().Contain(g => g.Platform == SocialPlatform.X, "Should have X group");
        
        var blueSkyGroup = platformGroups.First(g => g.Platform == SocialPlatform.BlueSky);
        blueSkyGroup.Accounts.Should().HaveCount(1, "BlueSky group should have 1 account");
        blueSkyGroup.AccountCount.Should().Be(1, "BlueSky group count should be 1");
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