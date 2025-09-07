using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Core.Services;
using SocialMediaCommander.Services.Interfaces;
using SocialMediaCommander.Services.Implementation;
using Serilog;
using Avalonia.Threading;

namespace SocialMediaCommander.Desktop.ViewModels;

/// <summary>
/// ViewModel for managing social media accounts across platforms
/// </summary>
public partial class AccountManagerViewModel : ObservableObject
{
    private readonly IAccountService _accountService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IOAuthConfigurationService _oauthConfigService;
    private readonly ILogger _logger;

    [ObservableProperty]
    private bool _isAddingAccount = false;

    [ObservableProperty]
    private bool _isEditingAccount = false;

    [ObservableProperty]
    private Account? _currentAccount;

    [ObservableProperty]
    private SocialPlatform _selectedPlatform = SocialPlatform.BlueSky;

    [ObservableProperty]
    private SocialPlatformConfig? _selectedPlatformConfig;

    [ObservableProperty]
    private string _newAccountUsername = string.Empty;

    [ObservableProperty]
    private string _newAccountDisplayName = string.Empty;

    [ObservableProperty]
    private string _newAccountAvatar = string.Empty;

    // OAuth Configuration Properties
    [ObservableProperty]
    private string _oAuthClientId = string.Empty;

    [ObservableProperty]
    private string _oAuthClientSecret = string.Empty;

    [ObservableProperty]
    private string _oAuthRedirectUri = "http://localhost:8080/callback";

    [ObservableProperty]
    private bool _isAuthenticating = false;

    [ObservableProperty]
    private string _authenticationStatus = string.Empty;

    public AccountManagerViewModel(IAccountService accountService, IAuthenticationService authenticationService, IOAuthConfigurationService oauthConfigService)
    {
        Console.WriteLine("AccountManagerViewModel constructor: Starting");

        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        Console.WriteLine("AccountManagerViewModel constructor: accountService assigned");

        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        Console.WriteLine("AccountManagerViewModel constructor: authenticationService assigned");

        _oauthConfigService = oauthConfigService ?? throw new ArgumentNullException(nameof(oauthConfigService));
        Console.WriteLine("AccountManagerViewModel constructor: oauthConfigService assigned");

        _logger = LoggingService.ForContext<AccountManagerViewModel>();
        Console.WriteLine("AccountManagerViewModel constructor: logger created");

        // Initialize collections
        Accounts = new ObservableCollection<Account>();
        Console.WriteLine("AccountManagerViewModel constructor: Accounts collection created");

        SelectedAccountIds = new Dictionary<SocialPlatform, List<string>>();
        Console.WriteLine("AccountManagerViewModel constructor: SelectedAccountIds dictionary created");

        Console.WriteLine("AccountManagerViewModel constructor: About to call PlatformConfigurations.GetAllPlatforms()");
        var allPlatforms = PlatformConfigurations.GetAllPlatforms();
        Console.WriteLine("AccountManagerViewModel constructor: PlatformConfigurations.GetAllPlatforms() completed");

        PlatformConfigs = new ObservableCollection<SocialPlatformConfig>(allPlatforms);
        Console.WriteLine("AccountManagerViewModel constructor: PlatformConfigs collection created");

        // Initialize SelectedPlatformConfig to match the default SelectedPlatform
        SelectedPlatformConfig = PlatformConfigs.FirstOrDefault(p => p.Id == SelectedPlatform);
        Console.WriteLine("AccountManagerViewModel constructor: SelectedPlatformConfig initialized");

        // Initialize selected accounts for each platform
        Console.WriteLine("AccountManagerViewModel constructor: About to initialize selected accounts for each platform");
        foreach (var platform in Enum.GetValues<SocialPlatform>())
        {
            SelectedAccountIds[platform] = new List<string>();
        }
        Console.WriteLine("AccountManagerViewModel constructor: Selected accounts initialized");

        // Subscribe to OAuth callback
        Console.WriteLine("AccountManagerViewModel constructor: About to check OAuth service type");
        if (_authenticationService is OAuthAuthenticationService oauthService)
        {
            Console.WriteLine("AccountManagerViewModel constructor: Subscribing to OAuth callback");
            oauthService.OnAuthenticationCallback += (code, state) => _ = HandleOAuthCallbackAsync(code, state);
            Console.WriteLine("AccountManagerViewModel constructor: OAuth callback subscribed");
        }
        else
        {
            Console.WriteLine($"AccountManagerViewModel constructor: AuthenticationService is not OAuthAuthenticationService, it's {_authenticationService.GetType().Name}");
        }

        Console.WriteLine("AccountManagerViewModel constructor: Completed successfully");

        // Initialize accounts loading - ensure it runs on UI thread
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                await LoadAccountsAsync();
                Console.WriteLine("AccountManagerViewModel: Initial account loading completed on UI thread");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AccountManagerViewModel: Initial account loading failed: {ex.Message}");
                _logger.Error(ex, "Failed to load accounts during initialization");
            }
        });

        Console.WriteLine("AccountManagerViewModel constructor: Account loading task started on UI thread");
    }

    #region Properties

    public ObservableCollection<Account> Accounts { get; }
    public ObservableCollection<SocialPlatformConfig> PlatformConfigs { get; }
    public Dictionary<SocialPlatform, List<string>> SelectedAccountIds { get; }

    // Properties for UI binding
    public IEnumerable<SocialPlatformConfig> AvailablePlatforms => PlatformConfigs;

    [ObservableProperty]
    private bool _isLoading = false;

    // Computed properties
    public IEnumerable<Account> AccountsForSelectedPlatform =>
        Accounts.Where(a => a != null && a.PlatformId == SelectedPlatform);

    public bool HasAccountsForPlatform => AccountsForSelectedPlatform.Any();

    public bool HasAccounts => Accounts.Any(a => a != null);

    // UI-friendly account view models
    public IEnumerable<AccountItemViewModel> AccountViewModels =>
        Accounts.Where(a => a != null).Select(a => new AccountItemViewModel(a));

    public string SelectedPlatformName =>
        PlatformConfigurations.GetPlatformConfig(SelectedPlatform).Name;

    public string SelectedPlatformColor =>
        PlatformConfigurations.GetPlatformConfig(SelectedPlatform).Color;

    public string OAuthSetupGuidance => GetOAuthSetupGuidance(SelectedPlatform);

    // Platform Groups for UI
    public IEnumerable<PlatformGroupViewModel> PlatformGroups =>
        Enum.GetValues<SocialPlatform>()
            .Where(platform => Accounts.Any(a => a != null && a.PlatformId == platform))
            .Select(platform => new PlatformGroupViewModel
            {
                Platform = platform,
                PlatformName = PlatformConfigurations.GetPlatformConfig(platform).Name,
                PlatformColor = PlatformConfigurations.GetPlatformConfig(platform).Color,
                PlatformIcon = GetPlatformIcon(platform),
                Accounts = Accounts.Where(a => a != null && a.PlatformId == platform)
                    .Select(a => new AccountItemViewModel(a))
                    .ToList(),
                AccountCount = Accounts.Count(a => a != null && a.PlatformId == platform)
            });

    // Command aliases for UI binding
    public IRelayCommand AddAccountCommand => StartAddAccountCommand;

    [RelayCommand]
    private void TestButton()
    {
        Console.WriteLine("🔴 TEST BUTTON CLICKED - COMMAND BINDING WORKS!");
        AuthenticationStatus = "🔴 TEST BUTTON CLICKED - COMMAND BINDING WORKS!";
    }

    [RelayCommand]
    private async Task TestOAuthConfig()
    {
        Console.WriteLine("🔐 TestOAuthConfig command executed");
        _logger.Information("Testing OAuth configuration for platform: {Platform}", SelectedPlatform);

        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(OAuthClientId) ||
                string.IsNullOrWhiteSpace(OAuthClientSecret) ||
                string.IsNullOrWhiteSpace(OAuthRedirectUri))
            {
                AuthenticationStatus = "❌ Please fill in all OAuth configuration fields";
                return;
            }

            AuthenticationStatus = "🔍 Testing OAuth configuration...";

            // Create OAuth config object
            var oauthConfig = new OAuthConfig
            {
                ClientId = OAuthClientId.Trim(),
                ClientSecret = OAuthClientSecret.Trim(),
                RedirectUri = OAuthRedirectUri.Trim()
            };

            // Test the configuration
            var validation = await _oauthConfigService.ValidateConfigurationAsync(SelectedPlatform, oauthConfig);

            if (validation.IsValid)
            {
                AuthenticationStatus = "✅ OAuth configuration is valid!";
                _logger.Information("OAuth configuration test successful for platform: {Platform}", SelectedPlatform);
            }
            else
            {
                var errorMessage = string.Join(", ", validation.Errors);
                AuthenticationStatus = $"❌ OAuth configuration issues: {errorMessage}";
                _logger.Warning("OAuth configuration test failed for platform: {Platform}, Errors: {Errors}", SelectedPlatform, errorMessage);
            }
        }
        catch (Exception ex)
        {
            AuthenticationStatus = $"❌ OAuth test failed: {ex.Message}";
            _logger.Error(ex, "OAuth configuration test failed for platform: {Platform}", SelectedPlatform);
        }
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task StartAddAccount()
    {
        Console.WriteLine("🔴 StartAddAccount command executed - BUTTON CLICKED!");
        _logger.Information("StartAddAccount command executed");

        try
        {
            // Ensure we're on the UI thread
            if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            {
                Console.WriteLine("⚠️ StartAddAccount not on UI thread, dispatching...");
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => StartAddAccount());
                return;
            }

            Console.WriteLine("✅ StartAddAccount running on UI thread");

            // Set visible status immediately
            AuthenticationStatus = "🚀 Starting account setup...";
            Console.WriteLine($"🔧 AuthenticationStatus set to: {AuthenticationStatus}");

            _logger.Information("Starting account addition for platform: {Platform}", SelectedPlatform);

            // For now, let's use a simpler approach - go directly to the edit form
            // This bypasses OAuth complexity for basic account creation

            Console.WriteLine("🔧 Setting IsAddingAccount = true");
            IsAddingAccount = true;

            Console.WriteLine("🔧 Setting IsEditingAccount = true");
            IsEditingAccount = true;

            Console.WriteLine("🔧 Setting CurrentAccount = null");
            CurrentAccount = null;

            // Clear the form
            Console.WriteLine("🔧 Clearing account form");
            ClearAccountForm();

            // Pre-fill platform
            Console.WriteLine($"🔧 SelectedPlatform: {SelectedPlatform}");
            SelectedPlatform = SelectedPlatform; // Ensure it's set

            // Generate default values
            NewAccountUsername = $"user_{DateTime.Now:HHmmss}";
            NewAccountDisplayName = $"New {PlatformConfigurations.GetPlatformConfig(SelectedPlatform).Name} User";
            NewAccountAvatar = $"https://api.dicebear.com/7.x/personas/svg?seed={SelectedPlatform}-{DateTime.Now.Ticks}";

            Console.WriteLine($"🔧 Form pre-filled - Username: {NewAccountUsername}, DisplayName: {NewAccountDisplayName}");

            AuthenticationStatus = "✏️ Fill in your account details below";

            Console.WriteLine($"🔧 Final state - IsEditingAccount: {IsEditingAccount}, IsAddingAccount: {IsAddingAccount}");

            _logger.Information("Add account form opened for platform: {Platform}", SelectedPlatform);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ StartAddAccount error: {ex.Message}");
            Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            AuthenticationStatus = $"❌ Error opening account form: {ex.Message}";
            _logger.Error(ex, "StartAddAccount failed for platform: {Platform}", SelectedPlatform);

            // Clear status after delay
            _ = Task.Delay(5000).ContinueWith(_ => AuthenticationStatus = string.Empty);
        }
    }

    [RelayCommand]
    private void StartEditAccount(Account account)
    {
        IsEditingAccount = true;
        IsAddingAccount = false;
        CurrentAccount = account;

        // Populate form with account data
        NewAccountUsername = account.Username;
        NewAccountDisplayName = account.DisplayName;
        NewAccountAvatar = account.Avatar ?? string.Empty;
        SelectedPlatform = account.PlatformId;
    }

    [RelayCommand]
    private void CancelAccountOperation()
    {
        IsAddingAccount = false;
        IsEditingAccount = false;
        CurrentAccount = null;
        ClearAccountForm();
    }

    [RelayCommand]
    private async Task SaveAccountAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NewAccountUsername) ||
                string.IsNullOrWhiteSpace(NewAccountDisplayName))
            {
                AuthenticationStatus = "⚠️ Please fill in both username and display name";
                _ = ClearAuthenticationStatusAfterDelayAsync();
                return;
            }

            Account account;

            if (IsEditingAccount && CurrentAccount != null)
            {
                _logger.Information("Updating existing account: {AccountId} ({DisplayName})", CurrentAccount.Id, CurrentAccount.DisplayName);

                // Update OAuth configuration if provided
                OAuthConfig? updatedOAuthConfig = CurrentAccount.OAuthConfiguration;
                if (!string.IsNullOrWhiteSpace(OAuthClientId) && !string.IsNullOrWhiteSpace(OAuthClientSecret))
                {
                    updatedOAuthConfig = _oauthConfigService.GetDefaultConfiguration(CurrentAccount.PlatformId);
                    updatedOAuthConfig.ClientId = OAuthClientId;
                    updatedOAuthConfig.ClientSecret = OAuthClientSecret;
                    updatedOAuthConfig.RedirectUri = OAuthRedirectUri;

                    // Save OAuth configuration globally for the platform
                    await _oauthConfigService.SaveConfigurationAsync(CurrentAccount.PlatformId, updatedOAuthConfig);
                    _logger.Information("OAuth configuration updated for platform: {Platform}", CurrentAccount.PlatformId);
                }

                // Update existing account
                account = new Account
                {
                    Id = CurrentAccount.Id,
                    PlatformId = CurrentAccount.PlatformId,
                    Username = NewAccountUsername.Trim(),
                    DisplayName = NewAccountDisplayName.Trim(),
                    Avatar = NewAccountAvatar,
                    IsDefault = CurrentAccount.IsDefault,
                    CreatedAt = CurrentAccount.CreatedAt,
                    LastUsed = DateTime.UtcNow,
                    Tokens = CurrentAccount.Tokens,
                    OAuthConfiguration = updatedOAuthConfig,
                    Metadata = CurrentAccount.Metadata
                };

                await _accountService.UpdateAccountAsync(account);

                // Update in collection
                var index = Accounts.IndexOf(CurrentAccount);
                if (index >= 0)
                {
                    Accounts[index] = account;
                    _logger.Information("Account updated in local collection at index: {Index}", index);
                }

                AuthenticationStatus = $"✅ Account '{account.DisplayName}' updated successfully";
            }
            else
            {
                _logger.Information("Creating new account for platform: {Platform}", SelectedPlatform);

                // Create OAuth configuration from the form if provided
                OAuthConfig? oauthConfig = null;
                if (!string.IsNullOrWhiteSpace(OAuthClientId) && !string.IsNullOrWhiteSpace(OAuthClientSecret))
                {
                    oauthConfig = _oauthConfigService.GetDefaultConfiguration(SelectedPlatform);
                    oauthConfig.ClientId = OAuthClientId;
                    oauthConfig.ClientSecret = OAuthClientSecret;
                    oauthConfig.RedirectUri = OAuthRedirectUri;

                    // Save OAuth configuration globally for the platform
                    await _oauthConfigService.SaveConfigurationAsync(SelectedPlatform, oauthConfig);
                    _logger.Information("OAuth configuration saved for platform: {Platform}", SelectedPlatform);
                }

                // Create new account
                account = new Account
                {
                    Id = Guid.NewGuid().ToString(),
                    PlatformId = SelectedPlatform,
                    Username = NewAccountUsername.Trim(),
                    DisplayName = NewAccountDisplayName.Trim(),
                    Avatar = NewAccountAvatar,
                    IsDefault = !AccountsForSelectedPlatform.Any(), // First account for platform is default
                    OAuthConfiguration = oauthConfig // Store OAuth config with the account
                };

                await _accountService.CreateAccountAsync(account);
                Accounts.Add(account);

                var statusMessage = oauthConfig != null
                    ? $"✅ Account '{account.DisplayName}' created with OAuth configuration!"
                    : $"✅ Account '{account.DisplayName}' created successfully";
                AuthenticationStatus = statusMessage;
                _logger.Information("New account created and added to collection: {AccountId}", account.Id);
            }

            // Update computed properties
            UpdateComputedProperties();

            // Close the form after a brief delay to show the success message
            _ = Task.Delay(1500).ContinueWith(_ => CancelAccountOperation());
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Save account failed. IsEditing: {IsEditing}, Username: {Username}", IsEditingAccount, NewAccountUsername);
            AuthenticationStatus = $"❌ Failed to save account: {ex.Message}";
            _ = ClearAuthenticationStatusAfterDelayAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteAccountAsync(Account account)
    {
        try
        {
            _logger.Information("DeleteAccountAsync called for account: {DisplayName} ({PlatformId}), IsDefault: {IsDefault}",
                account.DisplayName, account.PlatformId, account.IsDefault);

            if (account.IsDefault)
            {
                // Don't allow deleting default accounts - provide user feedback
                _logger.Warning("Attempted to delete default account: {DisplayName}. Default accounts cannot be deleted.", account.DisplayName);
                AuthenticationStatus = $"❌ Cannot delete default account '{account.DisplayName}'. Set another account as default first.";
                _ = ClearAuthenticationStatusAfterDelayAsync();
                return;
            }

            _logger.Information("Proceeding with account deletion: {AccountId}", account.Id);

            await _accountService.DeleteAccountAsync(account.Id);

            // Remove from local collection
            var accountToRemove = Accounts.FirstOrDefault(a => a?.Id == account.Id);
            if (accountToRemove != null)
            {
                Accounts.Remove(accountToRemove);
                _logger.Information("Account removed from local collection: {AccountId}", account.Id);
            }

            // Remove from selected accounts
            if (SelectedAccountIds.ContainsKey(account.PlatformId))
            {
                SelectedAccountIds[account.PlatformId].Remove(account.Id);
            }

            // Update computed properties
            UpdateComputedProperties();

            _logger.Information("Account deletion completed successfully: {DisplayName}", account.DisplayName);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Delete account failed for account: {AccountId}", account.Id);
            AuthenticationStatus = $"❌ Failed to delete account: {ex.Message}";
            _ = ClearAuthenticationStatusAfterDelayAsync();
        }
    }

    [RelayCommand]
    private void ToggleAccountSelection(Account account)
    {
        if (!SelectedAccountIds.ContainsKey(account.PlatformId))
        {
            SelectedAccountIds[account.PlatformId] = new List<string>();
        }

        var selectedList = SelectedAccountIds[account.PlatformId];

        if (selectedList.Contains(account.Id))
        {
            selectedList.Remove(account.Id);
        }
        else
        {
            selectedList.Add(account.Id);
        }
    }

    [RelayCommand]
    private async Task RefreshAccountsAsync()
    {
        await LoadAccountsAsync();
    }

    [RelayCommand]
    private async Task SetAsDefaultAccountAsync(Account account)
    {
        try
        {
            // Remove default from other accounts on this platform
            var platformAccounts = Accounts.Where(a => a.PlatformId == account.PlatformId).ToList();
            foreach (var acc in platformAccounts)
            {
                if (acc.IsDefault && acc.Id != account.Id)
                {
                    var updatedAcc = new Account
                    {
                        Id = acc.Id,
                        PlatformId = acc.PlatformId,
                        Username = acc.Username,
                        DisplayName = acc.DisplayName,
                        Avatar = acc.Avatar,
                        IsDefault = false,
                        CreatedAt = acc.CreatedAt,
                        LastUsed = acc.LastUsed,
                        Tokens = acc.Tokens,
                        Metadata = acc.Metadata
                    };

                    await _accountService.UpdateAccountAsync(updatedAcc);

                    var index = Accounts.IndexOf(acc);
                    if (index >= 0)
                    {
                        Accounts[index] = updatedAcc;
                    }
                }
            }

            // Set this account as default
            var defaultAccount = new Account
            {
                Id = account.Id,
                PlatformId = account.PlatformId,
                Username = account.Username,
                DisplayName = account.DisplayName,
                Avatar = account.Avatar,
                IsDefault = true,
                CreatedAt = account.CreatedAt,
                LastUsed = DateTime.UtcNow,
                Tokens = account.Tokens,
                Metadata = account.Metadata
            };

            await _accountService.UpdateAccountAsync(defaultAccount);

            var accountIndex = Accounts.IndexOf(account);
            if (accountIndex >= 0)
            {
                Accounts[accountIndex] = defaultAccount;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Set default account failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ReconnectAccount(AccountItemViewModel accountViewModel)
    {
        var account = Accounts.FirstOrDefault(a => a.Id == accountViewModel.Id);
        if (account != null)
        {
            await StartOAuthAuthentication(account.PlatformId, account);
        }
    }

    [RelayCommand]
    private async Task ConnectAccount(SocialPlatform platform)
    {
        _logger.Information("ConnectAccount command executed for platform: {Platform}", platform);
        AuthenticationStatus = $"🔗 Connecting to {PlatformConfigurations.GetPlatformConfig(platform).Name}...";

        try
        {
            // Get OAuth configuration from the global configuration service
            var oauthConfig = await _oauthConfigService.GetConfigurationAsync(platform);

            // For testing, let's create a mock account directly without OAuth
            var mockAccount = new Account
            {
                Id = Guid.NewGuid().ToString(),
                PlatformId = platform,
                Username = $"demo_{platform.ToString().ToLower()}",
                DisplayName = $"Demo {PlatformConfigurations.GetPlatformConfig(platform).Name} Account",
                Avatar = $"https://api.dicebear.com/7.x/personas/svg?seed={platform}-demo",
                IsDefault = !Accounts.Any(a => a.PlatformId == platform),
                CreatedAt = DateTime.UtcNow,
                LastUsed = DateTime.UtcNow,
                AuthStatus = Core.Models.AuthenticationStatus.Authenticated,
                OAuthConfiguration = oauthConfig, // Store OAuth config with the account
                Tokens = new Core.Models.OAuthTokens
                {
                    AccessToken = $"demo_token_{Guid.NewGuid():N}",
                    TokenType = "Bearer",
                    ExpiresAt = DateTime.UtcNow.AddHours(1)
                },
                Metadata = new Dictionary<string, string>
                {
                    ["demo"] = "true",
                    ["created_by"] = "connect_button"
                }
            };

            await _accountService.CreateAccountAsync(mockAccount);
            Accounts.Add(mockAccount);

            AuthenticationStatus = $"✅ Demo account for {PlatformConfigurations.GetPlatformConfig(platform).Name} connected successfully!";

            UpdateComputedProperties();

            _logger.Information("Mock account created successfully for platform: {Platform}, AccountId: {AccountId}", platform, mockAccount.Id);

            // Clear status after delay
            _ = Task.Delay(3000).ContinueWith(_ => AuthenticationStatus = string.Empty);
        }
        catch (Exception ex)
        {
            AuthenticationStatus = $"❌ Failed to connect {PlatformConfigurations.GetPlatformConfig(platform).Name}: {ex.Message}";
            _logger.Error(ex, "ConnectAccount failed for platform: {Platform}", platform);

            // Clear status after delay
            _ = Task.Delay(5000).ContinueWith(_ => AuthenticationStatus = string.Empty);
        }
    }

    private async Task StartOAuthAuthentication(SocialPlatform platform, Account? existingAccount = null)
    {
        try
        {
            IsAuthenticating = true;
            AuthenticationStatus = $"Starting authentication for {PlatformConfigurations.GetPlatformConfig(platform).Name}...";

            var result = await _authenticationService.StartAuthenticationAsync(platform);

            if (result.IsSuccess && !string.IsNullOrEmpty(result.AuthorizationUrl))
            {
                AuthenticationStatus = "Opening browser for authentication...";

                // Open the authorization URL in the default browser
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = result.AuthorizationUrl,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(startInfo);

                AuthenticationStatus = "Waiting for authorization...";

                // TODO: In a real implementation, you would listen for the redirect callback
                // For now, we'll simulate a successful authentication after a delay
                await Task.Delay(3000);

                // Simulate completing the OAuth flow
                var completeResult = await _authenticationService.CompleteAuthenticationAsync(platform, "mock_auth_code", result.State ?? "");

                if (completeResult.IsSuccess && completeResult.Tokens != null)
                {
                    AuthenticationStatus = "Authentication successful! Creating account...";

                    if (existingAccount != null)
                    {
                        // Update existing account with new tokens
                        existingAccount.Tokens = completeResult.Tokens;
                        existingAccount.LastUsed = DateTime.UtcNow;
                        existingAccount.AuthStatus = Core.Models.AuthenticationStatus.Authenticated;

                        await _accountService.UpdateAccountAsync(existingAccount);

                        // Update in collection
                        var index = Accounts.IndexOf(existingAccount);
                        if (index >= 0)
                        {
                            Accounts[index] = existingAccount;
                        }

                        AuthenticationStatus = "Account reconnected successfully!";
                    }
                    else
                    {
                        // Create new account
                        var newAccount = new Account
                        {
                            Id = Guid.NewGuid().ToString(),
                            PlatformId = platform,
                            Username = completeResult.UserProfile?.Username ?? $"user_{DateTime.Now.Ticks}",
                            DisplayName = completeResult.UserProfile?.DisplayName ?? $"User {DateTime.Now:HH:mm}",
                            Avatar = completeResult.UserProfile?.Avatar ?? $"https://api.dicebear.com/7.x/personas/svg?seed={platform}-{DateTime.Now.Ticks}",
                            IsDefault = !AccountsForSelectedPlatform.Any(),
                            CreatedAt = DateTime.UtcNow,
                            LastUsed = DateTime.UtcNow,
                            Tokens = completeResult.Tokens,
                            AuthStatus = Core.Models.AuthenticationStatus.Authenticated,
                            Metadata = new Dictionary<string, string>()
                        };

                        await _accountService.CreateAccountAsync(newAccount);
                        Accounts.Add(newAccount);

                        AuthenticationStatus = "Account connected successfully!";
                    }

                    UpdateComputedProperties();
                }
                else
                {
                    AuthenticationStatus = $"Authentication failed: {completeResult.ErrorMessage}";
                }
            }
            else
            {
                AuthenticationStatus = $"Failed to start authentication: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            AuthenticationStatus = $"Authentication error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"OAuth authentication failed: {ex}");
        }
        finally
        {
            IsAuthenticating = false;

            // Clear status after delay
            _ = Task.Delay(3000).ContinueWith(_ => AuthenticationStatus = string.Empty);
        }
    }

    [RelayCommand]
    private async Task RemoveAccount(AccountItemViewModel? accountViewModel)
    {
        Console.WriteLine("🔴 RemoveAccount command executed - DELETE BUTTON CLICKED!");
        _logger.Information("RemoveAccount command executed");

        try
        {
            Console.WriteLine($"🔧 RemoveAccount called with accountViewModel: {accountViewModel?.DisplayName ?? "NULL"}");

            // Enhanced validation with better user feedback
            if (accountViewModel == null)
            {
                Console.WriteLine("❌ RemoveAccount: accountViewModel is null - UI binding issue");
                _logger.Warning("RemoveAccount: accountViewModel is null - this suggests a UI binding issue. Check XAML CommandParameter binding.");
                AuthenticationStatus = "⚠️ Unable to remove account - please try again";
                _ = ClearAuthenticationStatusAfterDelayAsync();
                return;
            }

            Console.WriteLine($"🔧 Account to remove: ID={accountViewModel.Id}, DisplayName={accountViewModel.DisplayName}");

            if (string.IsNullOrEmpty(accountViewModel.Id))
            {
                Console.WriteLine("❌ RemoveAccount: accountViewModel.Id is null or empty");
                _logger.Warning("RemoveAccount: accountViewModel.Id is null or empty for account: {DisplayName}. UI binding may be incomplete.", accountViewModel.DisplayName);
                AuthenticationStatus = "⚠️ Account data incomplete - cannot remove";
                _ = ClearAuthenticationStatusAfterDelayAsync();
                return;
            }

            if (Accounts == null)
            {
                Console.WriteLine("❌ RemoveAccount: Accounts collection is null");
                _logger.Error("RemoveAccount: Accounts collection is null - this is a critical error");
                AuthenticationStatus = "❌ Internal error - please restart the application";
                return;
            }

            Console.WriteLine($"🔧 Searching for account in {Accounts.Count} total accounts");
            _logger.Debug("RemoveAccount: Looking for account with ID: {AccountId}", accountViewModel.Id);

            var account = Accounts.Where(a => a != null && !string.IsNullOrEmpty(a.Id))
                                  .FirstOrDefault(a => a.Id == accountViewModel.Id);

            if (account != null)
            {
                Console.WriteLine($"✅ Found account to delete: {account.DisplayName}");
                _logger.Information("Removing account: {DisplayName} ({PlatformId})", account.DisplayName, account.PlatformId);
                AuthenticationStatus = $"🗑️ Removing {account.DisplayName}...";

                await DeleteAccountAsync(account);

                AuthenticationStatus = $"✅ {account.DisplayName} removed successfully";
                Console.WriteLine($"✅ Account {account.DisplayName} removed successfully");
                _ = ClearAuthenticationStatusAfterDelayAsync();
            }
            else
            {
                Console.WriteLine($"❌ Account not found for removal: {accountViewModel.Id}");
                _logger.Warning("Account not found for removal: {AccountId}. Available accounts: {AvailableAccounts}",
                    accountViewModel.Id,
                    string.Join(", ", Accounts.Where(a => a != null).Select(a => $"{a.Id}:{a.DisplayName}")));

                AuthenticationStatus = "⚠️ Account not found - it may have already been removed";
                _ = ClearAuthenticationStatusAfterDelayAsync();

                // Refresh accounts to sync UI
                await RefreshAccountsAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ RemoveAccount error: {ex.Message}");
            Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            _logger.Error(ex, "RemoveAccount failed for account: {AccountId}", accountViewModel?.Id);
            AuthenticationStatus = $"❌ Failed to remove account: {ex.Message}";
            _ = ClearAuthenticationStatusAfterDelayAsync();
        }
    }

    #endregion

    #region Helper Methods

    private void ClearAccountForm()
    {
        NewAccountUsername = string.Empty;
        NewAccountDisplayName = string.Empty;
        NewAccountAvatar = string.Empty;
        OAuthClientId = string.Empty;
        OAuthClientSecret = string.Empty;
        OAuthRedirectUri = "http://localhost:8080/callback";
    }

    private async Task LoadAccountsAsync()
    {
        try
        {
            var accounts = await _accountService.GetAllAccountsAsync();

            // Update UI collections on the current thread (which should be the UI thread)
            // ObservableCollection operations must be performed on the UI thread
            Accounts.Clear();
            foreach (var account in accounts)
            {
                // Only add non-null accounts to prevent NullReferenceExceptions
                if (account != null)
                {
                    Accounts.Add(account);
                }
            }

            UpdateComputedProperties();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Load accounts failed");
            System.Diagnostics.Debug.WriteLine($"Load accounts failed: {ex.Message}");
        }
    }

    public bool IsAccountSelected(Account account)
    {
        return SelectedAccountIds.ContainsKey(account.PlatformId) &&
               SelectedAccountIds[account.PlatformId].Contains(account.Id);
    }

    public IEnumerable<Account> GetSelectedAccountsForPlatform(SocialPlatform platform)
    {
        if (!SelectedAccountIds.ContainsKey(platform))
            return Enumerable.Empty<Account>();

        var selectedIds = SelectedAccountIds[platform];
        return Accounts.Where(a => a != null && a.PlatformId == platform && selectedIds.Contains(a.Id));
    }

    public Dictionary<SocialPlatform, List<string>> GetAllSelectedAccounts()
    {
        return new Dictionary<SocialPlatform, List<string>>(SelectedAccountIds);
    }

    #endregion

    #region Property Change Handling

    partial void OnSelectedPlatformChanged(SocialPlatform value)
    {
        OnPropertyChanged(nameof(AccountsForSelectedPlatform));
        OnPropertyChanged(nameof(HasAccountsForPlatform));
        OnPropertyChanged(nameof(SelectedPlatformName));
        OnPropertyChanged(nameof(SelectedPlatformColor));
        OnPropertyChanged(nameof(OAuthSetupGuidance));

        // Update SelectedPlatformConfig to match the platform
        var config = PlatformConfigs.FirstOrDefault(p => p.Id == value);
        if (config != null && SelectedPlatformConfig != config)
        {
            SelectedPlatformConfig = config;
        }

        if (IsAddingAccount)
        {
            // Generate new avatar for the selected platform
            NewAccountAvatar = $"https://api.dicebear.com/7.x/personas/svg?seed={value}-{DateTime.Now.Ticks}";

            // Load default OAuth configuration for the platform (fire-and-forget)
            _ = LoadDefaultOAuthConfiguration(value);
        }
    }

    partial void OnSelectedPlatformConfigChanged(SocialPlatformConfig? value)
    {
        if (value != null && SelectedPlatform != value.Id)
        {
            SelectedPlatform = value.Id;
        }
    }

    [RelayCommand]
    private void AddAccountForPlatform(SocialPlatform platform)
    {
        SelectedPlatform = platform;
        _ = StartAddAccount(); // Fire-and-forget with discard to suppress CS4014
    }

    [RelayCommand]
    private void EditAccount(AccountItemViewModel? accountViewModel)
    {
        _logger.Information("EditAccount command executed");

        // Enhanced validation with more detailed logging
        if (accountViewModel == null)
        {
            _logger.Warning("EditAccount: accountViewModel is null - this suggests a UI binding issue. Check XAML CommandParameter binding.");
            // Provide user feedback
            AuthenticationStatus = "⚠️ Unable to edit account - please try again";
            _ = ClearAuthenticationStatusAfterDelayAsync();
            return;
        }

        if (string.IsNullOrEmpty(accountViewModel.Id))
        {
            _logger.Warning("EditAccount: accountViewModel.Id is null or empty for account: {DisplayName}. UI binding may be incomplete.", accountViewModel.DisplayName);
            AuthenticationStatus = "⚠️ Account data incomplete - cannot edit";
            _ = ClearAuthenticationStatusAfterDelayAsync();
            return;
        }

        if (Accounts == null)
        {
            _logger.Error("EditAccount: Accounts collection is null - this is a critical error");
            AuthenticationStatus = "❌ Internal error - please restart the application";
            return;
        }

        _logger.Debug("EditAccount: Looking for account with ID: {AccountId}", accountViewModel.Id);

        var account = Accounts.Where(a => a != null && !string.IsNullOrEmpty(a.Id))
                              .FirstOrDefault(a => a.Id == accountViewModel.Id);

        if (account != null)
        {
            _logger.Information("Editing account: {DisplayName} ({PlatformId})", account.DisplayName, account.PlatformId);
            StartEditAccount(account);
        }
        else
        {
            _logger.Warning("Account not found for editing: {AccountId}. Available accounts: {AvailableAccounts}",
                accountViewModel.Id,
                string.Join(", ", Accounts.Where(a => a != null).Select(a => $"{a.Id}:{a.DisplayName}")));

            AuthenticationStatus = "⚠️ Account not found - it may have been removed";
            _ = ClearAuthenticationStatusAfterDelayAsync();

            // Refresh accounts in case of sync issues
            _ = Task.Run(async () => await RefreshAccountsAsync());
        }
    }

    private string GetPlatformIcon(SocialPlatform platform)
    {
        return platform switch
        {
            SocialPlatform.BlueSky => "BS",
            SocialPlatform.X => "𝕏",
            SocialPlatform.LinkedIn => "in",
            SocialPlatform.Threads => "T",
            SocialPlatform.Facebook => "f",
            _ => "?"
        };
    }

    private string GetOAuthSetupGuidance(SocialPlatform platform)
    {
        return platform switch
        {
            SocialPlatform.BlueSky => "1. Go to BlueSky Developer Portal\n2. Create new App\n3. Copy Client ID & Secret to OAuth Config",
            SocialPlatform.X => "1. Visit developer.twitter.com\n2. Create Project & App\n3. Generate OAuth 2.0 Client ID & Secret\n4. Add to OAuth Config",
            SocialPlatform.LinkedIn => "1. Go to LinkedIn Developer Console\n2. Create new Application\n3. Get Client ID & Client Secret\n4. Configure OAuth settings",
            SocialPlatform.Threads => "1. Visit developers.facebook.com\n2. Create Threads App\n3. Get App ID & App Secret\n4. Configure OAuth redirect",
            SocialPlatform.Facebook => "1. Go to developers.facebook.com\n2. Create App for Pages\n3. Get App ID & App Secret\n4. Add OAuth settings",
            _ => "Check platform's developer documentation for OAuth setup instructions."
        };
    }

    private async Task LoadDefaultOAuthConfiguration(SocialPlatform platform)
    {
        try
        {
            OAuthConfig? config = null;

            // If editing an existing account, load its OAuth configuration
            if (IsEditingAccount && CurrentAccount?.OAuthConfiguration != null)
            {
                config = CurrentAccount.OAuthConfiguration;
                _logger.Information("Loading OAuth configuration from existing account: {AccountId}", CurrentAccount.Id);
            }
            else
            {
                // Try to load from global configuration service
                config = await _oauthConfigService.GetConfigurationAsync(platform);
                if (config == null)
                {
                    config = _oauthConfigService.GetDefaultConfiguration(platform);
                    _logger.Information("Loading default OAuth configuration for platform: {Platform}", platform);
                }
                else
                {
                    _logger.Information("Loading saved OAuth configuration for platform: {Platform}", platform);
                }
            }

            // Load configuration values
            OAuthClientId = config.ClientId ?? string.Empty;
            OAuthClientSecret = config.ClientSecret ?? string.Empty;
            OAuthRedirectUri = config.RedirectUri ?? "http://localhost:8080/callback";
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to load OAuth configuration for platform: {Platform}", platform);
            // Set sensible defaults
            OAuthClientId = string.Empty;
            OAuthClientSecret = string.Empty;
            OAuthRedirectUri = "http://localhost:8080/callback";
        }
    }

    private void UpdateComputedProperties()
    {
        OnPropertyChanged(nameof(AccountsForSelectedPlatform));
        OnPropertyChanged(nameof(HasAccountsForPlatform));
        OnPropertyChanged(nameof(HasAccounts));
        OnPropertyChanged(nameof(AccountViewModels));
        OnPropertyChanged(nameof(PlatformGroups));
    }

    #endregion

    private async Task HandleOAuthCallbackAsync(string authorizationCode, string state)
    {
        try
        {
            IsAuthenticating = true;
            AuthenticationStatus = "Completing authentication...";

            // Find which platform is being authenticated based on state or current context
            // For now, we'll use the selected platform
            var result = await _authenticationService.CompleteAuthenticationAsync(SelectedPlatform, authorizationCode, state);

            if (result.IsSuccess && result.Tokens != null && result.UserProfile != null)
            {
                // Create new account with OAuth tokens
                var account = new Account
                {
                    Id = Guid.NewGuid().ToString(),
                    PlatformId = SelectedPlatform,
                    Username = result.UserProfile.Username,
                    DisplayName = result.UserProfile.DisplayName,
                    Avatar = result.UserProfile.Avatar,
                    AuthStatus = Core.Models.AuthenticationStatus.Authenticated,
                    Tokens = result.Tokens,
                    Metadata = new Dictionary<string, string>
                    {
                        ["UserId"] = result.UserProfile.Id,
                        ["Bio"] = result.UserProfile.Bio ?? "",
                        ["ProfileUrl"] = result.UserProfile.ProfileUrl ?? ""
                    }
                };

                await _accountService.CreateAccountAsync(account);
                await LoadAccountsAsync();

                AuthenticationStatus = $"Successfully connected {result.UserProfile.DisplayName}!";

                // Clear status after delay - using proper async pattern
                _ = ClearAuthenticationStatusAfterDelayAsync();
            }
            else
            {
                AuthenticationStatus = $"Authentication failed: {result.ErrorMessage}";
                IsAuthenticating = false;
            }
        }
        catch (Exception ex)
        {
            AuthenticationStatus = $"Authentication error: {ex.Message}";
            IsAuthenticating = false;
        }
    }

    private async Task ClearAuthenticationStatusAfterDelayAsync()
    {
        await Task.Delay(3000);
        AuthenticationStatus = "";
        IsAuthenticating = false;
    }
}

// Supporting ViewModels
public class PlatformGroupViewModel
{
    public SocialPlatform Platform { get; set; }
    public string PlatformName { get; set; } = string.Empty;
    public string PlatformColor { get; set; } = string.Empty;
    public string PlatformIcon { get; set; } = string.Empty;
    public List<AccountItemViewModel> Accounts { get; set; } = new();
    public int AccountCount { get; set; }
}

public class AccountItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public SocialPlatform PlatformId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsConnected { get; set; } = true;
    public bool RequiresReconnection { get; set; } = false;

    // UI Properties
    public string AvatarText => DisplayName.FirstOrDefault().ToString().ToUpper();
    public string PlatformColor => PlatformConfigurations.GetPlatformConfig(PlatformId).Color;

    public AccountItemViewModel() { }

    public AccountItemViewModel(Account account)
    {
        Id = account.Id;
        PlatformId = account.PlatformId;
        Username = account.Username;
        DisplayName = account.DisplayName;
        Avatar = account.Avatar ?? string.Empty;
        IsDefault = account.IsDefault;
        IsConnected = account.Tokens != null && !account.Tokens.IsExpired;
        RequiresReconnection = account.Tokens?.IsExpired == true;
    }
}