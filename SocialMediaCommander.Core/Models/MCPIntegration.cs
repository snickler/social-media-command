using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SocialMediaCommander.Core.Models;

/// <summary>
/// Model Context Protocol (MCP) server integration
/// </summary>
public class MCPServer
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public string Endpoint { get; set; } = string.Empty;
    
    public string Version { get; set; } = "1.0.0";
    
    public MCPServerType ServerType { get; set; } = MCPServerType.HTTP;
    
    public MCPAuthConfig AuthConfig { get; set; } = new();
    
    public List<MCPCapability> Capabilities { get; set; } = new();
    
    public List<MCPResource> Resources { get; set; } = new();
    
    public List<MCPTool> Tools { get; set; } = new();
    
    public MCPServerStatus Status { get; set; } = MCPServerStatus.Disconnected;
    
    public DateTime LastConnected { get; set; } = DateTime.UtcNow;
    
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    public bool IsEnabled { get; set; } = true;
}

public class MCPAuthConfig
{
    public MCPAuthType AuthType { get; set; } = MCPAuthType.None;
    
    public string? ApiKey { get; set; }
    
    public string? Username { get; set; }
    
    public string? Password { get; set; }
    
    public string? BearerToken { get; set; }
    
    public Dictionary<string, string> Headers { get; set; } = new();
    
    public string? ClientCertificate { get; set; }
    
    public bool ValidateServerCertificate { get; set; } = true;
}

public class MCPCapability
{
    public string Name { get; set; } = string.Empty;
    
    public string Version { get; set; } = "1.0.0";
    
    public string Description { get; set; } = string.Empty;
    
    public List<string> SupportedMethods { get; set; } = new();
    
    public JsonDocument? Schema { get; set; }
    
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class MCPResource
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string Name { get; set; } = string.Empty;
    
    public string Type { get; set; } = string.Empty;
    
    public string Uri { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public string MimeType { get; set; } = "application/json";
    
    public long? Size { get; set; }
    
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    public bool IsAccessible { get; set; } = true;
}

public class MCPTool
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public JsonDocument? InputSchema { get; set; }
    
    public JsonDocument? OutputSchema { get; set; }
    
    public List<string> RequiredPermissions { get; set; } = new();
    
    public bool IsAsync { get; set; } = false;
    
    public TimeSpan? Timeout { get; set; }
    
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// MCP request and response models
/// </summary>
public class MCPRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string Method { get; set; } = string.Empty;
    
    public JsonDocument? Params { get; set; }
    
    public string? ServerId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public TimeSpan? Timeout { get; set; }
    
    public Dictionary<string, string> Headers { get; set; } = new();
}

public class MCPResponse
{
    public string Id { get; set; } = string.Empty;
    
    public bool Success { get; set; }
    
    public JsonDocument? Result { get; set; }
    
    public MCPError? Error { get; set; }
    
    public DateTime ResponseTime { get; set; } = DateTime.UtcNow;
    
    public double ProcessingTimeMs { get; set; }
    
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class MCPError
{
    public int Code { get; set; }
    
    public string Message { get; set; } = string.Empty;
    
    public JsonDocument? Data { get; set; }
    
    public string? StackTrace { get; set; }
}

/// <summary>
/// MCP session management
/// </summary>
public class MCPSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string ServerId { get; set; } = string.Empty;
    
    public string UserId { get; set; } = string.Empty;
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? EndedAt { get; set; }
    
    public MCPSessionStatus Status { get; set; } = MCPSessionStatus.Active;
    
    public List<MCPRequest> Requests { get; set; } = new();
    
    public Dictionary<string, object> Context { get; set; } = new();
    
    public TimeSpan? MaxDuration { get; set; }
}

/// <summary>
/// MCP integration for social media operations
/// </summary>
public class MCPSocialMediaIntegration
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string Name { get; set; } = string.Empty;
    
    public List<SocialPlatform> SupportedPlatforms { get; set; } = new();
    
    public List<MCPSocialOperation> SupportedOperations { get; set; } = new();
    
    public MCPServer Server { get; set; } = new();
    
    public bool IsEnabled { get; set; } = true;
    
    public Dictionary<string, object> Configuration { get; set; } = new();
}

public class MCPSocialOperation
{
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public MCPOperationType Type { get; set; }
    
    public JsonDocument? InputSchema { get; set; }
    
    public JsonDocument? OutputSchema { get; set; }
    
    public List<string> RequiredScopes { get; set; } = new();
    
    public bool RequiresAuth { get; set; } = true;
}

/// <summary>
/// MCP analytics and monitoring
/// </summary>
public class MCPAnalytics
{
    public string ServerId { get; set; } = string.Empty;
    
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;
    
    public int TotalRequests { get; set; }
    
    public int SuccessfulRequests { get; set; }
    
    public int FailedRequests { get; set; }
    
    public double AverageResponseTime { get; set; }
    
    public double MaxResponseTime { get; set; }
    
    public double MinResponseTime { get; set; }
    
    public int TimeoutCount { get; set; }
    
    public Dictionary<string, int> ErrorCodes { get; set; } = new();
    
    public Dictionary<string, int> MethodCounts { get; set; } = new();
    
    public double UptimePercentage { get; set; }
}

public class MCPHealthCheck
{
    public string ServerId { get; set; } = string.Empty;
    
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    
    public MCPHealthStatus Status { get; set; }
    
    public double ResponseTimeMs { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public Dictionary<string, object> Details { get; set; } = new();
    
    public List<MCPCapability> AvailableCapabilities { get; set; } = new();
}

public enum MCPServerType
{
    HTTP,
    WebSocket,
    gRPC,
    TCP,
    NamedPipe
}

public enum MCPAuthType
{
    None,
    ApiKey,
    Basic,
    Bearer,
    OAuth2,
    Certificate,
    Custom
}

public enum MCPServerStatus
{
    Disconnected,
    Connecting,
    Connected,
    Error,
    Maintenance
}

public enum MCPSessionStatus
{
    Active,
    Idle,
    Expired,
    Terminated,
    Error
}

public enum MCPOperationType
{
    Read,
    Write,
    Execute,
    Query,
    Stream,
    Batch
}

public enum MCPHealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}

/// <summary>
/// MCP configuration for the application
/// </summary>
public class MCPConfiguration
{
    public List<MCPServer> Servers { get; set; } = new();
    
    public MCPSettings Settings { get; set; } = new();
    
    public MCPSecuritySettings Security { get; set; } = new();
    
    public Dictionary<string, object> GlobalContext { get; set; } = new();
}

public class MCPSettings
{
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
    
    public int MaxConcurrentRequests { get; set; } = 10;
    
    public int MaxRetryAttempts { get; set; } = 3;
    
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    
    public bool EnableAnalytics { get; set; } = true;
    
    public bool EnableHealthChecks { get; set; } = true;
    
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(5);
    
    public bool EnableLogging { get; set; } = true;
    
    public string LogLevel { get; set; } = "Information";
}

public class MCPSecuritySettings
{
    public bool RequireEncryption { get; set; } = true;
    
    public List<string> AllowedOrigins { get; set; } = new();
    
    public List<string> BlockedIpAddresses { get; set; } = new();
    
    public bool ValidateServerCertificates { get; set; } = true;
    
    public TimeSpan TokenExpiration { get; set; } = TimeSpan.FromHours(1);
    
    public bool EnableRateLimiting { get; set; } = true;
    
    public int RateLimitRequests { get; set; } = 100;
    
    public TimeSpan RateLimitWindow { get; set; } = TimeSpan.FromMinutes(1);
} 