using System.ComponentModel.DataAnnotations;

namespace SocialMediaCommander.Core.Models;

/// <summary>
/// Windows Actions and system integration
/// </summary>
public class WindowsAction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public WindowsActionType ActionType { get; set; }
    
    public string Command { get; set; } = string.Empty;
    
    public List<string> Arguments { get; set; } = new();
    
    public string WorkingDirectory { get; set; } = string.Empty;
    
    public WindowsActionTrigger Trigger { get; set; } = new();
    
    public WindowsActionConditions Conditions { get; set; } = new();
    
    public bool IsEnabled { get; set; } = true;
    
    public bool RunAsAdmin { get; set; } = false;
    
    public TimeSpan? Timeout { get; set; }
    
    public Dictionary<string, string> Environment { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastExecuted { get; set; }
    
    public int ExecutionCount { get; set; } = 0;
}

public class WindowsActionTrigger
{
    public WindowsTriggerType TriggerType { get; set; }
    
    public string? Schedule { get; set; } // Cron expression
    
    public List<WindowsEvent> Events { get; set; } = new();
    
    public List<string> FilePatterns { get; set; } = new();
    
    public List<string> RegistryKeys { get; set; } = new();
    
    public string? ApplicationName { get; set; }
    
    public Dictionary<string, object> CustomTriggers { get; set; } = new();
}

public class WindowsEvent
{
    public string Source { get; set; } = string.Empty;
    
    public int EventId { get; set; }
    
    public string Level { get; set; } = "Information";
    
    public string? Keywords { get; set; }
}

public class WindowsActionConditions
{
    public List<string> RequiredProcesses { get; set; } = new();
    
    public List<string> ForbiddenProcesses { get; set; } = new();
    
    public TimeSpan? MinSystemUptime { get; set; }
    
    public int? MinAvailableMemoryMB { get; set; }
    
    public int? MinDiskSpaceGB { get; set; }
    
    public List<string> RequiredNetworkConnections { get; set; } = new();
    
    public bool RequireUserLoggedIn { get; set; } = false;
    
    public bool RequireSystemIdle { get; set; } = false;
    
    public Dictionary<string, object> CustomConditions { get; set; } = new();
}

/// <summary>
/// Windows notification integration
/// </summary>
public class WindowsNotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    public string Message { get; set; } = string.Empty;
    
    public WindowsNotificationType Type { get; set; } = WindowsNotificationType.Info;
    
    public string? ImagePath { get; set; }
    
    public string? SoundPath { get; set; }
    
    public TimeSpan? DisplayDuration { get; set; }
    
    public List<WindowsNotificationAction> Actions { get; set; } = new();
    
    public Dictionary<string, string> CustomData { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? DisplayedAt { get; set; }
    
    public DateTime? ClickedAt { get; set; }
    
    public DateTime? DismissedAt { get; set; }
    
    public bool IsSticky { get; set; } = false;
}

public class WindowsNotificationAction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string Text { get; set; } = string.Empty;
    
    public string Action { get; set; } = string.Empty;
    
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    public bool IsDefault { get; set; } = false;
}

/// <summary>
/// Windows system integration
/// </summary>
public class WindowsSystemInfo
{
    public string ComputerName { get; set; } = string.Empty;
    
    public string UserName { get; set; } = string.Empty;
    
    public string OSVersion { get; set; } = string.Empty;
    
    public string OSBuild { get; set; } = string.Empty;
    
    public string Architecture { get; set; } = string.Empty;
    
    public long TotalMemoryMB { get; set; }
    
    public long AvailableMemoryMB { get; set; }
    
    public List<WindowsDriveInfo> Drives { get; set; } = new();
    
    public List<WindowsProcessInfo> RunningProcesses { get; set; } = new();
    
    public List<WindowsServiceInfo> Services { get; set; } = new();
    
    public WindowsNetworkInfo Network { get; set; } = new();
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class WindowsDriveInfo
{
    public string Name { get; set; } = string.Empty;
    
    public string Label { get; set; } = string.Empty;
    
    public string DriveType { get; set; } = string.Empty;
    
    public long TotalSizeGB { get; set; }
    
    public long AvailableSpaceGB { get; set; }
    
    public double UsedPercentage { get; set; }
}

public class WindowsProcessInfo
{
    public int ProcessId { get; set; }
    
    public string ProcessName { get; set; } = string.Empty;
    
    public string? WindowTitle { get; set; }
    
    public long MemoryUsageMB { get; set; }
    
    public double CpuUsagePercent { get; set; }
    
    public DateTime StartTime { get; set; }
    
    public bool IsResponding { get; set; } = true;
}

public class WindowsServiceInfo
{
    public string ServiceName { get; set; } = string.Empty;
    
    public string DisplayName { get; set; } = string.Empty;
    
    public string Status { get; set; } = string.Empty;
    
    public string StartType { get; set; } = string.Empty;
    
    public string? Description { get; set; }
}

public class WindowsNetworkInfo
{
    public bool IsConnected { get; set; }
    
    public string? ActiveConnectionName { get; set; }
    
    public List<string> AvailableNetworks { get; set; } = new();
    
    public string? PublicIpAddress { get; set; }
    
    public List<string> LocalIpAddresses { get; set; } = new();
    
    public long DownloadSpeedBps { get; set; }
    
    public long UploadSpeedBps { get; set; }
}

/// <summary>
/// Windows registry integration
/// </summary>
public class WindowsRegistryOperation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public WindowsRegistryOperationType OperationType { get; set; }
    
    public string KeyPath { get; set; } = string.Empty;
    
    public string? ValueName { get; set; }
    
    public object? Value { get; set; }
    
    public string? ValueType { get; set; }
    
    public bool RequiresElevation { get; set; } = false;
    
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    
    public bool Success { get; set; }
    
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Windows Task Scheduler integration
/// </summary>
public class WindowsScheduledTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public string Author { get; set; } = string.Empty;
    
    public WindowsTaskTrigger Trigger { get; set; } = new();
    
    public WindowsTaskAction Action { get; set; } = new();
    
    public WindowsTaskSettings Settings { get; set; } = new();
    
    public WindowsTaskStatus Status { get; set; } = WindowsTaskStatus.Ready;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastRunTime { get; set; }
    
    public DateTime? NextRunTime { get; set; }
    
    public int RunCount { get; set; } = 0;
}

public class WindowsTaskTrigger
{
    public WindowsTaskTriggerType TriggerType { get; set; }
    
    public DateTime? StartTime { get; set; }
    
    public DateTime? EndTime { get; set; }
    
    public TimeSpan? RepetitionInterval { get; set; }
    
    public TimeSpan? RepetitionDuration { get; set; }
    
    public string? Schedule { get; set; } // Daily, Weekly, Monthly, etc.
    
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class WindowsTaskAction
{
    public WindowsTaskActionType ActionType { get; set; }
    
    public string Path { get; set; } = string.Empty;
    
    public string? Arguments { get; set; }
    
    public string? WorkingDirectory { get; set; }
    
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class WindowsTaskSettings
{
    public bool Enabled { get; set; } = true;
    
    public bool Hidden { get; set; } = false;
    
    public bool RunOnlyIfNetworkAvailable { get; set; } = false;
    
    public bool RunOnlyIfIdle { get; set; } = false;
    
    public bool WakeToRun { get; set; } = false;
    
    public bool AllowDemandStart { get; set; } = true;
    
    public bool AllowHardTerminate { get; set; } = true;
    
    public TimeSpan? ExecutionTimeLimit { get; set; }
    
    public int Priority { get; set; } = 7; // Normal priority
    
    public string? RunAsUser { get; set; }
    
    public bool RunWithHighestPrivileges { get; set; } = false;
}

/// <summary>
/// Windows PowerShell integration
/// </summary>
public class WindowsPowerShellScript
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public string Script { get; set; } = string.Empty;
    
    public List<WindowsPowerShellParameter> Parameters { get; set; } = new();
    
    public WindowsPowerShellExecutionPolicy ExecutionPolicy { get; set; } = WindowsPowerShellExecutionPolicy.Bypass;
    
    public bool RequiresElevation { get; set; } = false;
    
    public TimeSpan? Timeout { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastExecuted { get; set; }
    
    public int ExecutionCount { get; set; } = 0;
}

public class WindowsPowerShellParameter
{
    public string Name { get; set; } = string.Empty;
    
    public string Type { get; set; } = "String";
    
    public object? DefaultValue { get; set; }
    
    public bool IsRequired { get; set; } = false;
    
    public string? Description { get; set; }
}

public class WindowsPowerShellResult
{
    public bool Success { get; set; }
    
    public string? Output { get; set; }
    
    public string? Error { get; set; }
    
    public int ExitCode { get; set; }
    
    public TimeSpan ExecutionTime { get; set; }
    
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

public enum WindowsActionType
{
    Command,
    PowerShell,
    Application,
    Service,
    Registry,
    File,
    Network,
    Notification,
    Custom
}

public enum WindowsTriggerType
{
    Manual,
    Scheduled,
    Event,
    FileSystem,
    Registry,
    Process,
    Network,
    System,
    Custom
}

public enum WindowsNotificationType
{
    Info,
    Warning,
    Error,
    Success,
    Question
}

public enum WindowsRegistryOperationType
{
    Read,
    Write,
    Delete,
    CreateKey,
    DeleteKey,
    EnumerateKeys,
    EnumerateValues
}

public enum WindowsTaskStatus
{
    Unknown,
    Disabled,
    Queued,
    Ready,
    Running,
    Terminated,
    Stopped,
    Error
}

public enum WindowsTaskTriggerType
{
    Time,
    Daily,
    Weekly,
    Monthly,
    Boot,
    Logon,
    Idle,
    Event,
    Registration,
    SessionStateChange
}

public enum WindowsTaskActionType
{
    Execute,
    ComHandler,
    SendEmail,
    ShowMessage
}

public enum WindowsPowerShellExecutionPolicy
{
    Restricted,
    AllSigned,
    RemoteSigned,
    Unrestricted,
    Bypass,
    Undefined
}

/// <summary>
/// Windows integration configuration
/// </summary>
public class WindowsIntegrationConfig
{
    public bool EnableNotifications { get; set; } = true;
    
    public bool EnableTaskScheduler { get; set; } = true;
    
    public bool EnablePowerShell { get; set; } = true;
    
    public bool EnableRegistryOperations { get; set; } = false;
    
    public bool EnableSystemMonitoring { get; set; } = true;
    
    public bool RequireElevationConfirmation { get; set; } = true;
    
    public TimeSpan SystemInfoUpdateInterval { get; set; } = TimeSpan.FromMinutes(5);
    
    public List<string> AllowedPowerShellCommands { get; set; } = new();
    
    public List<string> BlockedPowerShellCommands { get; set; } = new();
    
    public Dictionary<string, object> CustomSettings { get; set; } = new();
} 