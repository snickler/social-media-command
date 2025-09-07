using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Services.Interfaces;

/// <summary>
/// Model Context Protocol (MCP) service interface
/// </summary>
public interface IMCPService
{
    /// <summary>
    /// Register MCP server
    /// </summary>
    Task<bool> RegisterServerAsync(MCPServer server);

    /// <summary>
    /// Unregister MCP server
    /// </summary>
    Task<bool> UnregisterServerAsync(string serverId);

    /// <summary>
    /// Get registered servers
    /// </summary>
    Task<List<MCPServer>> GetServersAsync();

    /// <summary>
    /// Get server by ID
    /// </summary>
    Task<MCPServer?> GetServerAsync(string serverId);

    /// <summary>
    /// Connect to MCP server
    /// </summary>
    Task<bool> ConnectAsync(string serverId);

    /// <summary>
    /// Disconnect from MCP server
    /// </summary>
    Task<bool> DisconnectAsync(string serverId);

    /// <summary>
    /// Send request to MCP server
    /// </summary>
    Task<MCPResponse> SendRequestAsync(MCPRequest request);

    /// <summary>
    /// Send request to specific server
    /// </summary>
    Task<MCPResponse> SendRequestAsync(string serverId, string method, object? parameters = null);

    /// <summary>
    /// Get server capabilities
    /// </summary>
    Task<List<MCPCapability>> GetCapabilitiesAsync(string serverId);

    /// <summary>
    /// Get server resources
    /// </summary>
    Task<List<MCPResource>> GetResourcesAsync(string serverId);

    /// <summary>
    /// Get server tools
    /// </summary>
    Task<List<MCPTool>> GetToolsAsync(string serverId);

    /// <summary>
    /// Execute tool on server
    /// </summary>
    Task<MCPResponse> ExecuteToolAsync(string serverId, string toolId, object? parameters = null);

    /// <summary>
    /// Read resource from server
    /// </summary>
    Task<MCPResponse> ReadResourceAsync(string serverId, string resourceUri);

    /// <summary>
    /// Create MCP session
    /// </summary>
    Task<MCPSession> CreateSessionAsync(string serverId, string userId);

    /// <summary>
    /// Get active sessions
    /// </summary>
    Task<List<MCPSession>> GetActiveSessionsAsync(string? serverId = null);

    /// <summary>
    /// Terminate session
    /// </summary>
    Task<bool> TerminateSessionAsync(string sessionId);

    /// <summary>
    /// Get session by ID
    /// </summary>
    Task<MCPSession?> GetSessionAsync(string sessionId);

    /// <summary>
    /// Health check for server
    /// </summary>
    Task<MCPHealthCheck> HealthCheckAsync(string serverId);

    /// <summary>
    /// Get analytics for server
    /// </summary>
    Task<MCPAnalytics> GetAnalyticsAsync(string serverId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Get social media integrations
    /// </summary>
    Task<List<MCPSocialMediaIntegration>> GetSocialMediaIntegrationsAsync();

    /// <summary>
    /// Register social media integration
    /// </summary>
    Task<bool> RegisterSocialMediaIntegrationAsync(MCPSocialMediaIntegration integration);

    /// <summary>
    /// Execute social media operation via MCP
    /// </summary>
    Task<MCPResponse> ExecuteSocialOperationAsync(string integrationId, string operationName, object parameters);

    /// <summary>
    /// Stream data from MCP server
    /// </summary>
    IAsyncEnumerable<MCPResponse> StreamAsync(string serverId, string method, object? parameters = null);

    /// <summary>
    /// Batch execute multiple requests
    /// </summary>
    Task<List<MCPResponse>> BatchExecuteAsync(List<MCPRequest> requests);

    /// <summary>
    /// Subscribe to server events
    /// </summary>
    Task<bool> SubscribeToEventsAsync(string serverId, string eventType, Func<MCPResponse, Task> handler);

    /// <summary>
    /// Unsubscribe from server events
    /// </summary>
    Task<bool> UnsubscribeFromEventsAsync(string serverId, string eventType);

    /// <summary>
    /// Get MCP configuration
    /// </summary>
    Task<MCPConfiguration> GetConfigurationAsync();

    /// <summary>
    /// Update MCP configuration
    /// </summary>
    Task<bool> UpdateConfigurationAsync(MCPConfiguration configuration);

    /// <summary>
    /// Test server connection
    /// </summary>
    Task<bool> TestConnectionAsync(string serverId);

    /// <summary>
    /// Get server status
    /// </summary>
    Task<MCPServerStatus> GetServerStatusAsync(string serverId);

    /// <summary>
    /// Enable/disable server
    /// </summary>
    Task<bool> SetServerEnabledAsync(string serverId, bool enabled);
}