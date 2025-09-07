using System.Threading.Tasks;
using SocialMediaCommander.Services.Implementation;

namespace SocialMediaCommander.Services.Interfaces;

/// <summary>
/// Service interface for managing application settings and user preferences
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the current application settings
    /// </summary>
    /// <returns>Current application settings</returns>
    Task<AppSettings> GetSettingsAsync();

    /// <summary>
    /// Saves application settings
    /// </summary>
    /// <param name="settings">Settings to save</param>
    Task SaveSettingsAsync(AppSettings settings);

    /// <summary>
    /// Gets a specific setting value
    /// </summary>
    /// <typeparam name="T">Type of the setting value</typeparam>
    /// <param name="key">Setting key</param>
    /// <param name="defaultValue">Default value if setting not found</param>
    /// <returns>Setting value or default</returns>
    Task<T> GetSettingAsync<T>(string key, T defaultValue = default!);

    /// <summary>
    /// Sets a specific setting value
    /// </summary>
    /// <typeparam name="T">Type of the setting value</typeparam>
    /// <param name="key">Setting key</param>
    /// <param name="value">Setting value</param>
    Task SetSettingAsync<T>(string key, T value);

    /// <summary>
    /// Removes a specific setting
    /// </summary>
    /// <param name="key">Setting key to remove</param>
    /// <returns>True if setting was removed</returns>
    Task<bool> RemoveSettingAsync(string key);

    /// <summary>
    /// Resets all settings to default values
    /// </summary>
    Task ResetToDefaultsAsync();

    /// <summary>
    /// Exports settings to a file
    /// </summary>
    /// <param name="filePath">Path to export file</param>
    /// <returns>True if export was successful</returns>
    Task<bool> ExportSettingsAsync(string filePath);

    /// <summary>
    /// Imports settings from a file
    /// </summary>
    /// <param name="filePath">Path to import file</param>
    /// <returns>True if import was successful</returns>
    Task<bool> ImportSettingsAsync(string filePath);
}