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

    public AccountManagerViewModel(IAccountService accountService)
    {
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        
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
    
    public string SelectedPlatformName => 
        PlatformConfigurations.GetPlatformConfig(SelectedPlatform).Name;
    
    public string SelectedPlatformColor => 
        PlatformConfigurations.GetPlatformConfig(SelectedPlatform).Color;
    
    // Command aliases for UI binding
    public IRelayCommand AddAccountCommand => StartAddAccountCommand;
    
    #endregion
    
    #region Commands
    
    [RelayCommand]
    private void StartAddAccount()
    {
        IsAddingAccount = true;
        IsEditingAccount = false;
        CurrentAccount = null;
        ClearAccountForm();
        
        // Generate default avatar
        NewAccountAvatar = $"https://api.dicebear.com/7.x/personas/svg?seed={SelectedPlatform}-{DateTime.Now.Ticks}";
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
                    AccessToken = CurrentAccount.AccessToken,
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
                        AccessToken = acc.AccessToken,
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
                AccessToken = account.AccessToken,
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
            
            OnPropertyChanged(nameof(AccountsForSelectedPlatform));
            OnPropertyChanged(nameof(HasAccountsForPlatform));
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
    
    #endregion
} 