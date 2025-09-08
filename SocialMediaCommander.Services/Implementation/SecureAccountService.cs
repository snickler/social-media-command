using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;
using SocialMediaCommander.Services.Serialization;
using Serilog;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// Secure implementation of IAccountService with encrypted persistent storage
/// </summary>
public class SecureAccountService : IAccountService
{
    private readonly string _dataDirectory;
    private readonly string _accountsFileName = "accounts.encrypted";
    private readonly ILogger _logger;
    private readonly object _lock = new();
    private readonly SemaphoreSlim _fileSemaphore = new(1, 1);
    private Dictionary<string, Account> _accounts = new();
    private bool _isLoaded = false;

    public SecureAccountService()
    {
        _logger = Log.ForContext<SecureAccountService>();
        _dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SocialMediaCommander",
            "Data"
        );

        Directory.CreateDirectory(_dataDirectory);
        _logger.Information("SecureAccountService initialized with data directory: {DataDirectory}", _dataDirectory);
    }

    /// <summary>
    /// Constructor for testing with custom directory path
    /// </summary>
    /// <param name="customDirectory">Custom directory path for testing</param>
    internal SecureAccountService(string customDirectory)
    {
        _logger = Log.ForContext<SecureAccountService>();
        _dataDirectory = Path.Combine(customDirectory, "SocialMediaCommander", "Data");
        Directory.CreateDirectory(_dataDirectory);
        _logger.Information("SecureAccountService initialized with test data directory: {DataDirectory}", _dataDirectory);
    }

    public async Task<IEnumerable<Account>> GetAllAccountsAsync()
    {
        await EnsureLoadedAsync();
        lock (_lock)
        {
            return _accounts.Values.ToList();
        }
    }

    public async Task<IEnumerable<Account>> GetAccountsForPlatformAsync(SocialPlatform platform)
    {
        await EnsureLoadedAsync();
        lock (_lock)
        {
            return _accounts.Values.Where(a => a.PlatformId == platform).ToList();
        }
    }

    public async Task<Account?> GetAccountByIdAsync(string id)
    {
        await EnsureLoadedAsync();
        lock (_lock)
        {
            return _accounts.TryGetValue(id, out var account) ? account : null;
        }
    }

    public async Task<Account> CreateAccountAsync(Account account)
    {
        await EnsureLoadedAsync();

        if (string.IsNullOrEmpty(account.Id))
            account.Id = Guid.NewGuid().ToString();

        account.CreatedAt = DateTime.UtcNow;
        account.LastUsed = DateTime.UtcNow;

        // Ensure avatar is set
        if (string.IsNullOrEmpty(account.Avatar))
            account.Avatar = account.GetAvatarUrl();

        lock (_lock)
        {
            // If this is the first account for the platform, make it default
            var existingAccounts = _accounts.Values.Where(a => a.PlatformId == account.PlatformId);
            if (!existingAccounts.Any())
                account.IsDefault = true;

            // If this account is being set as default, unset other defaults for the platform
            if (account.IsDefault)
            {
                foreach (var existingAccount in existingAccounts.Where(a => a.IsDefault))
                {
                    existingAccount.IsDefault = false;
                }
            }

            _accounts[account.Id] = account;
        }

        await SaveAccountsAsync();
        _logger.Information("Created new account {AccountId} for platform {Platform}", account.Id, account.PlatformId);
        return account;
    }

    public async Task<Account> UpdateAccountAsync(Account account)
    {
        await EnsureLoadedAsync();

        lock (_lock)
        {
            if (!_accounts.ContainsKey(account.Id))
                throw new ArgumentException($"Account with ID {account.Id} not found");

            account.LastUsed = DateTime.UtcNow;

            // Ensure avatar is set
            if (string.IsNullOrEmpty(account.Avatar))
                account.Avatar = account.GetAvatarUrl();

            // If this account is being set as default, unset other defaults for the platform
            if (account.IsDefault)
            {
                var otherAccounts = _accounts.Values.Where(a => a.PlatformId == account.PlatformId && a.Id != account.Id);
                foreach (var otherAccount in otherAccounts.Where(a => a.IsDefault))
                {
                    otherAccount.IsDefault = false;
                }
            }

            _accounts[account.Id] = account;
        }

        await SaveAccountsAsync();
        _logger.Information("Updated account {AccountId} for platform {Platform}", account.Id, account.PlatformId);
        return account;
    }

    public async Task<bool> DeleteAccountAsync(string id)
    {
        await EnsureLoadedAsync();

        lock (_lock)
        {
            if (!_accounts.TryGetValue(id, out var account))
                return false;

            // Don't allow deleting the last account for a platform
            var otherAccountsForPlatform = _accounts.Values.Where(a => a.PlatformId == account.PlatformId && a.Id != id);
            if (!otherAccountsForPlatform.Any())
                throw new InvalidOperationException($"Cannot delete the last account for platform {account.PlatformId}");

            // If we're deleting the default account, make another one default
            if (account.IsDefault && otherAccountsForPlatform.Any())
            {
                var newDefault = otherAccountsForPlatform.First();
                newDefault.IsDefault = true;
            }

            _accounts.Remove(id);
        }

        await SaveAccountsAsync();
        _logger.Information("Deleted account {AccountId}", id);
        return true;
    }

    public async Task<Account?> GetDefaultAccountForPlatformAsync(SocialPlatform platform)
    {
        await EnsureLoadedAsync();
        lock (_lock)
        {
            return _accounts.Values.FirstOrDefault(a => a.PlatformId == platform && a.IsDefault);
        }
    }

    public async Task<bool> SetDefaultAccountAsync(string accountId)
    {
        await EnsureLoadedAsync();

        lock (_lock)
        {
            if (!_accounts.TryGetValue(accountId, out var account))
                return false;

            // Unset other defaults for the platform
            var otherAccounts = _accounts.Values.Where(a => a.PlatformId == account.PlatformId && a.Id != accountId);
            foreach (var otherAccount in otherAccounts.Where(a => a.IsDefault))
            {
                otherAccount.IsDefault = false;
            }

            // Set this account as default
            account.IsDefault = true;
        }

        await SaveAccountsAsync();
        _logger.Information("Set account {AccountId} as default for platform {Platform}", accountId, _accounts[accountId].PlatformId);
        return true;
    }

    public async Task<bool> ValidateAccountCredentialsAsync(string accountId)
    {
        await EnsureLoadedAsync();
        lock (_lock)
        {
            return _accounts.ContainsKey(accountId);
        }
    }

    public async Task<IEnumerable<Account>> GetAccountsByIdsAsync(IEnumerable<string> accountIds)
    {
        await EnsureLoadedAsync();
        lock (_lock)
        {
            return accountIds
                .Where(id => _accounts.ContainsKey(id))
                .Select(id => _accounts[id])
                .ToList();
        }
    }

    public async Task UpdateLastUsedAsync(string accountId)
    {
        await EnsureLoadedAsync();

        lock (_lock)
        {
            if (_accounts.TryGetValue(accountId, out var account))
            {
                account.LastUsed = DateTime.UtcNow;
            }
        }

        await SaveAccountsAsync();
    }

    public async Task InitializeDefaultAccountsAsync()
    {
        await EnsureLoadedAsync();

        lock (_lock)
        {
            // Only initialize if no accounts exist
            if (_accounts.Any())
                return;

            var defaultAccounts = DefaultAccounts.GetAllDefaultAccounts();
            foreach (var account in defaultAccounts)
            {
                _accounts[account.Id] = account;
            }
        }

        await SaveAccountsAsync();
        _logger.Information("Initialized default accounts");
    }

    private async Task EnsureLoadedAsync()
    {
        if (_isLoaded) return;

        await LoadAccountsAsync();
        _isLoaded = true;
    }

    private async Task LoadAccountsAsync()
    {
        var filePath = Path.Combine(_dataDirectory, _accountsFileName);

        if (!File.Exists(filePath))
        {
            _logger.Information("No existing accounts file found, starting with empty accounts");
            return;
        }

        await _fileSemaphore.WaitAsync();
        try
        {
            var encryptedData = await File.ReadAllBytesAsync(filePath);
            var decryptedData = CrossPlatformEncryption.Unprotect(encryptedData, "SocialMediaCommander_Accounts");
            var json = Encoding.UTF8.GetString(decryptedData);

            var accounts = JsonSerializer.Deserialize(json, ServicesJsonContext.Default.ListAccount) ?? new List<Account>();

            lock (_lock)
            {
                _accounts = accounts.ToDictionary(a => a.Id, a => a);
            }

            _logger.Information("Loaded {AccountCount} accounts from encrypted storage", accounts.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load accounts from encrypted storage");
            // Continue with empty accounts rather than failing
            lock (_lock)
            {
                _accounts = new Dictionary<string, Account>();
            }
        }
        finally
        {
            _fileSemaphore.Release();
        }
    }

    private async Task SaveAccountsAsync()
    {
        var filePath = Path.Combine(_dataDirectory, _accountsFileName);

        await _fileSemaphore.WaitAsync();
        try
        {
            List<Account> accountsList;
            lock (_lock)
            {
                accountsList = _accounts.Values.ToList();
            }

            var json = JsonSerializer.Serialize(accountsList, ServicesJsonContext.Default.ListAccount);

            var data = Encoding.UTF8.GetBytes(json);
            var encryptedData = CrossPlatformEncryption.Protect(data, "SocialMediaCommander_Accounts");

            await File.WriteAllBytesAsync(filePath, encryptedData);
            _logger.Debug("Saved {AccountCount} accounts to encrypted storage", accountsList.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save accounts to encrypted storage");
            throw;
        }
        finally
        {
            _fileSemaphore.Release();
        }
    }
}