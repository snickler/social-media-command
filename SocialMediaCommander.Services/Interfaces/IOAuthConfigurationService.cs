using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Services.Interfaces;

/// <summary>
/// Service for managing OAuth configurations per platform
/// </summary>
public interface IOAuthConfigurationService
{
    /// <summary>
    /// Gets OAuth configuration for a specific platform
    /// </summary>
    Task<OAuthConfig?> GetConfigurationAsync(SocialPlatform platform);
    
    /// <summary>
    /// Saves OAuth configuration for a specific platform
    /// </summary>
    Task SaveConfigurationAsync(SocialPlatform platform, OAuthConfig config);
    
    /// <summary>
    /// Gets all OAuth configurations
    /// </summary>
    Task<Dictionary<SocialPlatform, OAuthConfig>> GetAllConfigurationsAsync();
    
    /// <summary>
    /// Deletes OAuth configuration for a specific platform
    /// </summary>
    Task DeleteConfigurationAsync(SocialPlatform platform);
    
    /// <summary>
    /// Validates OAuth configuration
    /// </summary>
    Task<ValidationResult> ValidateConfigurationAsync(SocialPlatform platform, OAuthConfig config);
    
    /// <summary>
    /// Gets default OAuth configuration template for a platform
    /// </summary>
    OAuthConfig GetDefaultConfiguration(SocialPlatform platform);
    
    /// <summary>
    /// Checks if a platform has a valid OAuth configuration
    /// </summary>
    Task<bool> HasValidConfigurationAsync(SocialPlatform platform);
    
    /// <summary>
    /// Imports OAuth configurations from a file
    /// </summary>
    Task ImportConfigurationsAsync(string filePath);
    
    /// <summary>
    /// Exports OAuth configurations to a file
    /// </summary>
    Task ExportConfigurationsAsync(string filePath);
} 