using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;
using System.Collections.Concurrent;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// In-memory implementation of IAccountService for development and testing
/// </summary>
public class InMemoryAccountService : IAccountService
{
    private readonly ConcurrentDictionary<string, Account> _accounts = new();
    private readonly object _defaultAccountLock = new();

    public InMemoryAccountService()
    {
        // Initialize with default accounts
        _ = InitializeDefaultAccountsAsync();
    }

    public Task<IEnumerable<Account>> GetAllAccountsAsync()
    {
        return Task.FromResult(_accounts.Values.AsEnumerable());
    }

    public Task<IEnumerable<Account>> GetAccountsForPlatformAsync(SocialPlatform platform)
    {
        var accounts = _accounts.Values.Where(a => a.PlatformId == platform);
        return Task.FromResult(accounts);
    }

    public Task<Account?> GetAccountByIdAsync(string id)
    {
        _accounts.TryGetValue(id, out var account);
        return Task.FromResult(account);
    }

    public Task<Account> CreateAccountAsync(Account account)
    {
        if (string.IsNullOrEmpty(account.Id))
            account.Id = Guid.NewGuid().ToString();

        account.CreatedAt = DateTime.UtcNow;
        account.LastUsed = DateTime.UtcNow;

        // Ensure avatar is set
        if (string.IsNullOrEmpty(account.Avatar))
            account.Avatar = account.GetAvatarUrl();

        // If this is the first account for the platform, make it default
        var existingAccounts = _accounts.Values.Where(a => a.PlatformId == account.PlatformId);
        if (!existingAccounts.Any())
            account.IsDefault = true;

        // If this account is being set as default, unset other defaults for the platform
        if (account.IsDefault)
        {
            lock (_defaultAccountLock)
            {
                foreach (var existingAccount in existingAccounts.Where(a => a.IsDefault))
                {
                    existingAccount.IsDefault = false;
                }
            }
        }

        _accounts.TryAdd(account.Id, account);
        return Task.FromResult(account);
    }

    public Task<Account> UpdateAccountAsync(Account account)
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
            lock (_defaultAccountLock)
            {
                var otherAccounts = _accounts.Values.Where(a => a.PlatformId == account.PlatformId && a.Id != account.Id);
                foreach (var otherAccount in otherAccounts.Where(a => a.IsDefault))
                {
                    otherAccount.IsDefault = false;
                }
            }
        }

        _accounts.TryUpdate(account.Id, account, _accounts[account.Id]);
        return Task.FromResult(account);
    }

    public Task<bool> DeleteAccountAsync(string id)
    {
        if (!_accounts.TryGetValue(id, out var account))
            return Task.FromResult(false);

        // Don't allow deleting the last account for a platform
        var otherAccountsForPlatform = _accounts.Values.Where(a => a.PlatformId == account.PlatformId && a.Id != id);
        if (!otherAccountsForPlatform.Any())
            throw new InvalidOperationException($"Cannot delete the last account for platform {account.PlatformId}");

        // If we're deleting the default account, make another one default
        if (account.IsDefault && otherAccountsForPlatform.Any())
        {
            lock (_defaultAccountLock)
            {
                var newDefault = otherAccountsForPlatform.First();
                newDefault.IsDefault = true;
            }
        }

        return Task.FromResult(_accounts.TryRemove(id, out _));
    }

    public Task<Account?> GetDefaultAccountForPlatformAsync(SocialPlatform platform)
    {
        var defaultAccount = _accounts.Values.FirstOrDefault(a => a.PlatformId == platform && a.IsDefault);
        return Task.FromResult(defaultAccount);
    }

    public Task<bool> SetDefaultAccountAsync(string accountId)
    {
        if (!_accounts.TryGetValue(accountId, out var account))
            return Task.FromResult(false);

        lock (_defaultAccountLock)
        {
            // Unset other defaults for the platform
            var otherAccounts = _accounts.Values.Where(a => a.PlatformId == account.PlatformId && a.Id != accountId);
            foreach (var otherAccount in otherAccounts.Where(a => a.IsDefault))
            {
                otherAccount.IsDefault = false;
            }

            // Set this account as default
            account.IsDefault = true;
        }

        return Task.FromResult(true);
    }

    public Task<bool> ValidateAccountCredentialsAsync(string accountId)
    {
        // For in-memory implementation, always return true
        // In a real implementation, this would validate with the platform APIs
        return Task.FromResult(_accounts.ContainsKey(accountId));
    }

    public Task<IEnumerable<Account>> GetAccountsByIdsAsync(IEnumerable<string> accountIds)
    {
        var accounts = accountIds
            .Where(id => _accounts.ContainsKey(id))
            .Select(id => _accounts[id]);
        return Task.FromResult(accounts);
    }

    public Task UpdateLastUsedAsync(string accountId)
    {
        if (_accounts.TryGetValue(accountId, out var account))
        {
            account.LastUsed = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task InitializeDefaultAccountsAsync()
    {
        // Only initialize if no accounts exist
        if (_accounts.Any())
            return Task.CompletedTask;

        var defaultAccounts = DefaultAccounts.GetAllDefaultAccounts();
        foreach (var account in defaultAccounts)
        {
            _accounts.TryAdd(account.Id, account);
        }

        return Task.CompletedTask;
    }
}