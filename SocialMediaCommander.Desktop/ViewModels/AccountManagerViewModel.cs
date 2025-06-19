using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Desktop.ViewModels;

/// <summary>
/// ViewModel for managing social media accounts across platforms
/// </summary>
public partial class AccountManagerViewModel : ObservableObject
{
    private readonly IAccountService _accountService;
    private readonly IAuthenticationService _authenticationService;
    
    [ObservableProperty]
    private bool _isAddingAccount = false;
    
    [ObservableProperty]
    private bool _isEditingAccount = false;
    
    [ObservableProperty]
    private Account? _currentAccount;
    
    [ObservableProperty]
    private SocialPlatform _selectedPlatform = SocialPlatform.BlueSky;
    
    [ObservableProperty]
    private string _newAccountUsername = string.Empty;
    
    [ObservableProperty]
    private string _newAccountDisplayName = string.Empty;
    
    [ObservableProperty]
    private string _newAccountAvatar = string.Empty;
    
    [ObservableProperty]
    private bool _isAuthenticating = false;
    
    [ObservableProperty]
    private string _authenticationStatus = string.Empty;

    public AccountManagerViewModel(IAccountService accountService, IAuthenticationService authenticationService)
    {
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        
        // Initialize collections
        Accounts = new ObservableCollection<Account>();
        SelectedAccountIds = new Dictionary<SocialPlatform, List<string>>();
        PlatformConfigs = new ObservableCollection<SocialPlatformConfig>(
            PlatformConfigurations.GetAllPlatforms());
        
        // Initialize selected accounts for each platform
        foreach (var platform in Enum.GetValues<SocialPlatform>())
        {
            SelectedAccountIds[platform] = new List<string>();
        }
        
        // Load accounts on startup
        _ = Task.Run(LoadAccountsAsync);
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
        Accounts.Where(a => a.PlatformId == SelectedPlatform);
    
    public bool HasAccountsForPlatform => AccountsForSelectedPlatform.Any();
    
    public bool HasAccounts => Accounts.Any();
    
    public string SelectedPlatformName => 
        PlatformConfigurations.GetPlatformConfig(SelectedPlatform).Name;
    
    public string SelectedPlatformColor => 
        PlatformConfigurations.GetPlatformConfig(SelectedPlatform).Color;
    
    // Platform Groups for UI
    public IEnumerable<PlatformGroupViewModel> PlatformGroups => 
        Enum.GetValues<SocialPlatform>()
            .Where(platform => Accounts.Any(a => a.PlatformId == platform))
            .Select(platform => new PlatformGroupViewModel
            {
                Platform = platform,
                PlatformName = PlatformConfigurations.GetPlatformConfig(platform).Name,
                PlatformColor = PlatformConfigurations.GetPlatformConfig(platform).Color,
                PlatformIcon = GetPlatformIcon(platform),
                Accounts = Accounts.Where(a => a.PlatformId == platform)
                    .Select(a => new AccountItemViewModel(a))
                    .ToList(),
                AccountCount = Accounts.Count(a => a.PlatformId == platform)
            });
    
    // Command aliases for UI binding
    public IRelayCommand AddAccountCommand => StartAddAccountCommand;
    
    #endregion
    
    #region Commands
    
    [RelayCommand]
    private async Task StartAddAccount()
    {
        // Use OAuth authentication instead of manual form
        await StartOAuthAuthentication(SelectedPlatform);
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
                return;
            }
            
            Account account;
            
            if (IsEditingAccount && CurrentAccount != null)
            {
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
                    Metadata = CurrentAccount.Metadata
                };
                
                await _accountService.UpdateAccountAsync(account);
                
                // Update in collection
                var index = Accounts.IndexOf(CurrentAccount);
                if (index >= 0)
                {
                    Accounts[index] = account;
                }
            }
            else
            {
                // Create new account
                account = new Account
                {
                    Id = Guid.NewGuid().ToString(),
                    PlatformId = SelectedPlatform,
                    Username = NewAccountUsername.Trim(),
                    DisplayName = NewAccountDisplayName.Trim(),
                    Avatar = NewAccountAvatar,
                    IsDefault = !AccountsForSelectedPlatform.Any() // First account for platform is default
                };
                
                await _accountService.CreateAccountAsync(account);
                Accounts.Add(account);
            }
            
            CancelAccountOperation();
            OnPropertyChanged(nameof(AccountsForSelectedPlatform));
            OnPropertyChanged(nameof(HasAccountsForPlatform));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Save account failed: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private async Task DeleteAccountAsync(Account account)
    {
        try
        {
            if (account.IsDefault)
            {
                // Don't allow deleting default accounts
                return;
            }
            
            await _accountService.DeleteAccountAsync(account.Id);
            Accounts.Remove(account);
            
            // Remove from selected accounts
            if (SelectedAccountIds.ContainsKey(account.PlatformId))
            {
                SelectedAccountIds[account.PlatformId].Remove(account.Id);
            }
            
            OnPropertyChanged(nameof(AccountsForSelectedPlatform));
            OnPropertyChanged(nameof(HasAccountsForPlatform));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Delete account failed: {ex.Message}");
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
        await StartOAuthAuthentication(platform);
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
    
    #endregion
    
    #region Helper Methods
    
    private void ClearAccountForm()
    {
        NewAccountUsername = string.Empty;
        NewAccountDisplayName = string.Empty;
        NewAccountAvatar = string.Empty;
    }
    
    private async Task LoadAccountsAsync()
    {
        try
        {
            var accounts = await _accountService.GetAllAccountsAsync();
            
            // Update UI on main thread
            await Task.Run(() =>
            {
                Accounts.Clear();
                foreach (var account in accounts)
                {
                    Accounts.Add(account);
                }
            });
            
            UpdateComputedProperties();
        }
        catch (Exception ex)
        {
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
        return Accounts.Where(a => a.PlatformId == platform && selectedIds.Contains(a.Id));
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
        
        if (IsAddingAccount)
        {
            // Generate new avatar for the selected platform
            NewAccountAvatar = $"https://api.dicebear.com/7.x/personas/svg?seed={value}-{DateTime.Now.Ticks}";
        }
    }
    
    [RelayCommand]
    private void AddAccountForPlatform(SocialPlatform platform)
    {
        SelectedPlatform = platform;
        StartAddAccount();
    }
    
    [RelayCommand]
    private void EditAccount(AccountItemViewModel accountViewModel)
    {
        var account = Accounts.FirstOrDefault(a => a.Id == accountViewModel.Id);
        if (account != null)
        {
            StartEditAccount(account);
        }
    }
    
    [RelayCommand]
    private async Task RemoveAccount(AccountItemViewModel accountViewModel)
    {
        var account = Accounts.FirstOrDefault(a => a.Id == accountViewModel.Id);
        if (account != null)
        {
            await DeleteAccountAsync(account);
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
    
    private void UpdateComputedProperties()
    {
        OnPropertyChanged(nameof(HasAccounts));
        OnPropertyChanged(nameof(PlatformGroups));
        OnPropertyChanged(nameof(AccountsForSelectedPlatform));
        OnPropertyChanged(nameof(HasAccountsForPlatform));
    }
    
    #endregion
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