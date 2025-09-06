using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Services.Interfaces;

/// <summary>
/// Service interface for managing social media accounts
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Gets all accounts for all platforms
    /// </summary>
    Task<IEnumerable<Account>> GetAllAccountsAsync();

    /// <summary>
    /// Gets accounts for a specific platform
    /// </summary>
    Task<IEnumerable<Account>> GetAccountsForPlatformAsync(SocialPlatform platform);

    /// <summary>
    /// Gets a specific account by ID
    /// </summary>
    Task<Account?> GetAccountByIdAsync(string id);

    /// <summary>
    /// Creates a new account
    /// </summary>
    Task<Account> CreateAccountAsync(Account account);

    /// <summary>
    /// Updates an existing account
    /// </summary>
    Task<Account> UpdateAccountAsync(Account account);

    /// <summary>
    /// Deletes an account
    /// </summary>
    Task<bool> DeleteAccountAsync(string id);

    /// <summary>
    /// Gets the default account for a platform
    /// </summary>
    Task<Account?> GetDefaultAccountForPlatformAsync(SocialPlatform platform);

    /// <summary>
    /// Sets an account as the default for its platform
    /// </summary>
    Task<bool> SetDefaultAccountAsync(string accountId);

    /// <summary>
    /// Validates account credentials (if authentication is implemented)
    /// </summary>
    Task<bool> ValidateAccountCredentialsAsync(string accountId);

    /// <summary>
    /// Gets accounts by their IDs
    /// </summary>
    Task<IEnumerable<Account>> GetAccountsByIdsAsync(IEnumerable<string> accountIds);

    /// <summary>
    /// Updates the last used timestamp for an account
    /// </summary>
    Task UpdateLastUsedAsync(string accountId);

    /// <summary>
    /// Initializes default accounts if none exist
    /// </summary>
    Task InitializeDefaultAccountsAsync();
}