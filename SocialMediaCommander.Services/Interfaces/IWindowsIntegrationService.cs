using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Services.Interfaces;

/// <summary>
/// Windows integration service interface for system operations
/// </summary>
public interface IWindowsIntegrationService
{
    /// <summary>
    /// Register Windows action
    /// </summary>
    Task<bool> RegisterActionAsync(WindowsAction action);

    /// <summary>
    /// Unregister Windows action
    /// </summary>
    Task<bool> UnregisterActionAsync(string actionId);

    /// <summary>
    /// Execute Windows action
    /// </summary>
    Task<bool> ExecuteActionAsync(string actionId, Dictionary<string, object>? parameters = null);

    /// <summary>
    /// Get registered actions
    /// </summary>
    Task<List<WindowsAction>> GetActionsAsync();

    /// <summary>
    /// Get action by ID
    /// </summary>
    Task<WindowsAction?> GetActionAsync(string actionId);

    /// <summary>
    /// Show Windows notification
    /// </summary>
    Task<bool> ShowNotificationAsync(WindowsNotification notification);

    /// <summary>
    /// Get system information
    /// </summary>
    Task<WindowsSystemInfo> GetSystemInfoAsync();

    /// <summary>
    /// Execute PowerShell script
    /// </summary>
    Task<WindowsPowerShellResult> ExecutePowerShellAsync(WindowsPowerShellScript script, Dictionary<string, object>? parameters = null);

    /// <summary>
    /// Execute PowerShell command
    /// </summary>
    Task<WindowsPowerShellResult> ExecutePowerShellCommandAsync(string command, bool requiresElevation = false, TimeSpan? timeout = null);

    /// <summary>
    /// Perform registry operation
    /// </summary>
    Task<WindowsRegistryOperation> ExecuteRegistryOperationAsync(WindowsRegistryOperation operation);

    /// <summary>
    /// Create scheduled task
    /// </summary>
    Task<bool> CreateScheduledTaskAsync(WindowsScheduledTask task);

    /// <summary>
    /// Delete scheduled task
    /// </summary>
    Task<bool> DeleteScheduledTaskAsync(string taskName);

    /// <summary>
    /// Get scheduled tasks
    /// </summary>
    Task<List<WindowsScheduledTask>> GetScheduledTasksAsync();

    /// <summary>
    /// Start scheduled task
    /// </summary>
    Task<bool> StartScheduledTaskAsync(string taskName);

    /// <summary>
    /// Stop scheduled task
    /// </summary>
    Task<bool> StopScheduledTaskAsync(string taskName);

    /// <summary>
    /// Get running processes
    /// </summary>
    Task<List<WindowsProcessInfo>> GetRunningProcessesAsync();

    /// <summary>
    /// Kill process by ID
    /// </summary>
    Task<bool> KillProcessAsync(int processId);

    /// <summary>
    /// Kill process by name
    /// </summary>
    Task<bool> KillProcessByNameAsync(string processName);

    /// <summary>
    /// Start process
    /// </summary>
    Task<WindowsProcessInfo?> StartProcessAsync(string fileName, string? arguments = null, string? workingDirectory = null);

    /// <summary>
    /// Get Windows services
    /// </summary>
    Task<List<WindowsServiceInfo>> GetServicesAsync();

    /// <summary>
    /// Start Windows service
    /// </summary>
    Task<bool> StartServiceAsync(string serviceName);

    /// <summary>
    /// Stop Windows service
    /// </summary>
    Task<bool> StopServiceAsync(string serviceName);

    /// <summary>
    /// Restart Windows service
    /// </summary>
    Task<bool> RestartServiceAsync(string serviceName);

    /// <summary>
    /// Get network information
    /// </summary>
    Task<WindowsNetworkInfo> GetNetworkInfoAsync();

    /// <summary>
    /// Get drive information
    /// </summary>
    Task<List<WindowsDriveInfo>> GetDriveInfoAsync();

    /// <summary>
    /// Monitor file system changes
    /// </summary>
    Task<bool> StartFileSystemMonitoringAsync(string path, string pattern, Func<string, Task> onChanged);

    /// <summary>
    /// Stop file system monitoring
    /// </summary>
    Task<bool> StopFileSystemMonitoringAsync(string path);

    /// <summary>
    /// Monitor registry changes
    /// </summary>
    Task<bool> StartRegistryMonitoringAsync(string keyPath, Func<string, Task> onChanged);

    /// <summary>
    /// Stop registry monitoring
    /// </summary>
    Task<bool> StopRegistryMonitoringAsync(string keyPath);

    /// <summary>
    /// Monitor process events
    /// </summary>
    Task<bool> StartProcessMonitoringAsync(string processName, Func<WindowsProcessInfo, Task> onStarted, Func<int, Task> onStopped);

    /// <summary>
    /// Stop process monitoring
    /// </summary>
    Task<bool> StopProcessMonitoringAsync(string processName);

    /// <summary>
    /// Monitor Windows events
    /// </summary>
    Task<bool> StartEventMonitoringAsync(string logName, int eventId, Func<Dictionary<string, object>, Task> onEvent);

    /// <summary>
    /// Stop Windows event monitoring
    /// </summary>
    Task<bool> StopEventMonitoringAsync(string logName, int eventId);

    /// <summary>
    /// Set system environment variable
    /// </summary>
    Task<bool> SetEnvironmentVariableAsync(string name, string value, bool systemWide = false);

    /// <summary>
    /// Get system environment variable
    /// </summary>
    Task<string?> GetEnvironmentVariableAsync(string name, bool systemWide = false);

    /// <summary>
    /// Delete system environment variable
    /// </summary>
    Task<bool> DeleteEnvironmentVariableAsync(string name, bool systemWide = false);

    /// <summary>
    /// Check if running as administrator
    /// </summary>
    Task<bool> IsRunningAsAdministratorAsync();

    /// <summary>
    /// Request administrator privileges
    /// </summary>
    Task<bool> RequestAdministratorPrivilegesAsync();

    /// <summary>
    /// Get Windows integration configuration
    /// </summary>
    Task<WindowsIntegrationConfig> GetConfigurationAsync();

    /// <summary>
    /// Update Windows integration configuration
    /// </summary>
    Task<bool> UpdateConfigurationAsync(WindowsIntegrationConfig configuration);

    /// <summary>
    /// Test Windows integration capabilities
    /// </summary>
    Task<Dictionary<string, bool>> TestCapabilitiesAsync();

    /// <summary>
    /// Enable/disable Windows integration features
    /// </summary>
    Task<bool> SetFeatureEnabledAsync(string featureName, bool enabled);
}