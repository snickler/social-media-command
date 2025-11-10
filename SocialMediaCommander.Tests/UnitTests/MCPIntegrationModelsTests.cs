using Xunit;
using SocialMediaCommander.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace SocialMediaCommander.Tests.UnitTests;

public class MCPIntegrationModelsTests
{
    [Fact]
    public void MCPServer_ShouldInitializeWithDefaults()
    {
        // Act
        var server = new MCPServer();

        // Assert
        Assert.NotNull(server.Id);
        Assert.False(string.IsNullOrEmpty(server.Id));
        Assert.Equal(string.Empty, server.Name);
        Assert.Equal(string.Empty, server.Description);
        Assert.Equal(string.Empty, server.Endpoint);
        Assert.Equal("1.0.0", server.Version);
        Assert.Equal(MCPServerType.HTTP, server.ServerType);
        Assert.NotNull(server.AuthConfig);
        Assert.NotNull(server.Capabilities);
        Assert.Empty(server.Capabilities);
        Assert.NotNull(server.Resources);
        Assert.Empty(server.Resources);
        Assert.NotNull(server.Tools);
        Assert.Empty(server.Tools);
        Assert.Equal(MCPServerStatus.Disconnected, server.Status);
        Assert.True(server.LastConnected <= DateTime.UtcNow);
        Assert.NotNull(server.Metadata);
        Assert.Empty(server.Metadata);
        Assert.True(server.IsEnabled);
    }

    [Fact]
    public void MCPServer_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var authConfig = new MCPAuthConfig();
        var capabilities = new List<MCPCapability> { new() };
        var resources = new List<MCPResource> { new() };
        var tools = new List<MCPTool> { new() };
        var lastConnected = DateTime.UtcNow;
        var metadata = new Dictionary<string, object> { { "key", "value" } };

        // Act
        var server = new MCPServer
        {
            Id = id,
            Name = "Test Server",
            Description = "Test Description",
            Endpoint = "https://api.test.com",
            Version = "2.0.0",
            ServerType = MCPServerType.WebSocket,
            AuthConfig = authConfig,
            Capabilities = capabilities,
            Resources = resources,
            Tools = tools,
            Status = MCPServerStatus.Connected,
            LastConnected = lastConnected,
            Metadata = metadata,
            IsEnabled = false
        };

        // Assert
        Assert.Equal(id, server.Id);
        Assert.Equal("Test Server", server.Name);
        Assert.Equal("Test Description", server.Description);
        Assert.Equal("https://api.test.com", server.Endpoint);
        Assert.Equal("2.0.0", server.Version);
        Assert.Equal(MCPServerType.WebSocket, server.ServerType);
        Assert.Equal(authConfig, server.AuthConfig);
        Assert.Equal(capabilities, server.Capabilities);
        Assert.Equal(resources, server.Resources);
        Assert.Equal(tools, server.Tools);
        Assert.Equal(MCPServerStatus.Connected, server.Status);
        Assert.Equal(lastConnected, server.LastConnected);
        Assert.Equal(metadata, server.Metadata);
        Assert.False(server.IsEnabled);
    }

    [Fact]
    public void MCPServer_ValidationShouldWork()
    {
        // Arrange
        var server = new MCPServer { Name = "", Endpoint = "" };
        var context = new ValidationContext(server);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(server, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(MCPServer.Name)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(MCPServer.Endpoint)));
    }

    [Fact]
    public void MCPAuthConfig_ShouldInitializeWithDefaults()
    {
        // Act
        var authConfig = new MCPAuthConfig();

        // Assert
        Assert.Equal(MCPAuthType.None, authConfig.AuthType);
        Assert.Null(authConfig.ApiKey);
        Assert.Null(authConfig.Username);
        Assert.Null(authConfig.Password);
        Assert.Null(authConfig.BearerToken);
        Assert.NotNull(authConfig.Headers);
        Assert.Empty(authConfig.Headers);
        Assert.Null(authConfig.ClientCertificate);
        Assert.True(authConfig.ValidateServerCertificate);
    }

    [Fact]
    public void MCPAuthConfig_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var headers = new Dictionary<string, string> { { "Custom-Header", "Value" } };

        // Act
        var authConfig = new MCPAuthConfig
        {
            AuthType = MCPAuthType.ApiKey,
            ApiKey = "test-api-key",
            Username = "testuser",
            Password = "testpass",
            BearerToken = "bearer-token",
            Headers = headers,
            ClientCertificate = "cert-data",
            ValidateServerCertificate = false
        };

        // Assert
        Assert.Equal(MCPAuthType.ApiKey, authConfig.AuthType);
        Assert.Equal("test-api-key", authConfig.ApiKey);
        Assert.Equal("testuser", authConfig.Username);
        Assert.Equal("testpass", authConfig.Password);
        Assert.Equal("bearer-token", authConfig.BearerToken);
        Assert.Equal(headers, authConfig.Headers);
        Assert.Equal("cert-data", authConfig.ClientCertificate);
        Assert.False(authConfig.ValidateServerCertificate);
    }

    [Fact]
    public void MCPCapability_ShouldInitializeWithDefaults()
    {
        // Act
        var capability = new MCPCapability();

        // Assert
        Assert.Equal(string.Empty, capability.Name);
        Assert.Equal("1.0.0", capability.Version);
        Assert.Equal(string.Empty, capability.Description);
        Assert.NotNull(capability.SupportedMethods);
        Assert.Empty(capability.SupportedMethods);
        Assert.Null(capability.Schema);
        Assert.NotNull(capability.Parameters);
        Assert.Empty(capability.Parameters);
    }

    [Fact]
    public void MCPCapability_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var methods = new List<string> { "get", "post" };
        var parameters = new Dictionary<string, object> { { "param", "value" } };
        var schema = JsonDocument.Parse("{}");

        // Act
        var capability = new MCPCapability
        {
            Name = "Test Capability",
            Version = "2.0.0",
            Description = "Test Description",
            SupportedMethods = methods,
            Schema = schema,
            Parameters = parameters
        };

        // Assert
        Assert.Equal("Test Capability", capability.Name);
        Assert.Equal("2.0.0", capability.Version);
        Assert.Equal("Test Description", capability.Description);
        Assert.Equal(methods, capability.SupportedMethods);
        Assert.Equal(schema, capability.Schema);
        Assert.Equal(parameters, capability.Parameters);
    }

    [Fact]
    public void MCPResource_ShouldInitializeWithDefaults()
    {
        // Act
        var resource = new MCPResource();

        // Assert
        Assert.NotNull(resource.Id);
        Assert.False(string.IsNullOrEmpty(resource.Id));
        Assert.Equal(string.Empty, resource.Name);
        Assert.Equal(string.Empty, resource.Type);
        Assert.Equal(string.Empty, resource.Uri);
        Assert.Equal(string.Empty, resource.Description);
        Assert.Equal("application/json", resource.MimeType);
        Assert.Null(resource.Size);
        Assert.True(resource.LastModified <= DateTime.UtcNow);
        Assert.NotNull(resource.Metadata);
        Assert.Empty(resource.Metadata);
        Assert.True(resource.IsAccessible);
    }

    [Fact]
    public void MCPResource_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var lastModified = DateTime.UtcNow;
        var metadata = new Dictionary<string, object> { { "key", "value" } };

        // Act
        var resource = new MCPResource
        {
            Id = id,
            Name = "Test Resource",
            Type = "File",
            Uri = "https://example.com/resource",
            Description = "Test Description",
            MimeType = "text/plain",
            Size = 1024,
            LastModified = lastModified,
            Metadata = metadata,
            IsAccessible = false
        };

        // Assert
        Assert.Equal(id, resource.Id);
        Assert.Equal("Test Resource", resource.Name);
        Assert.Equal("File", resource.Type);
        Assert.Equal("https://example.com/resource", resource.Uri);
        Assert.Equal("Test Description", resource.Description);
        Assert.Equal("text/plain", resource.MimeType);
        Assert.Equal(1024, resource.Size);
        Assert.Equal(lastModified, resource.LastModified);
        Assert.Equal(metadata, resource.Metadata);
        Assert.False(resource.IsAccessible);
    }

    [Fact]
    public void MCPTool_ShouldInitializeWithDefaults()
    {
        // Act
        var tool = new MCPTool();

        // Assert
        Assert.NotNull(tool.Id);
        Assert.False(string.IsNullOrEmpty(tool.Id));
        Assert.Equal(string.Empty, tool.Name);
        Assert.Equal(string.Empty, tool.Description);
        Assert.Null(tool.InputSchema);
        Assert.Null(tool.OutputSchema);
        Assert.NotNull(tool.RequiredPermissions);
        Assert.Empty(tool.RequiredPermissions);
        Assert.False(tool.IsAsync);
        Assert.Null(tool.Timeout);
        Assert.NotNull(tool.Configuration);
        Assert.Empty(tool.Configuration);
    }

    [Fact]
    public void MCPTool_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var inputSchema = JsonDocument.Parse("{}");
        var outputSchema = JsonDocument.Parse("{}");
        var permissions = new List<string> { "read", "write" };
        var timeout = TimeSpan.FromSeconds(30);
        var configuration = new Dictionary<string, object> { { "key", "value" } };

        // Act
        var tool = new MCPTool
        {
            Id = id,
            Name = "Test Tool",
            Description = "Test Description",
            InputSchema = inputSchema,
            OutputSchema = outputSchema,
            RequiredPermissions = permissions,
            IsAsync = true,
            Timeout = timeout,
            Configuration = configuration
        };

        // Assert
        Assert.Equal(id, tool.Id);
        Assert.Equal("Test Tool", tool.Name);
        Assert.Equal("Test Description", tool.Description);
        Assert.Equal(inputSchema, tool.InputSchema);
        Assert.Equal(outputSchema, tool.OutputSchema);
        Assert.Equal(permissions, tool.RequiredPermissions);
        Assert.True(tool.IsAsync);
        Assert.Equal(timeout, tool.Timeout);
        Assert.Equal(configuration, tool.Configuration);
    }

    [Fact]
    public void MCPRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new MCPRequest();

        // Assert
        Assert.NotNull(request.Id);
        Assert.False(string.IsNullOrEmpty(request.Id));
        Assert.Equal(string.Empty, request.Method);
        Assert.Null(request.Params);
        Assert.Null(request.ServerId);
        Assert.True(request.CreatedAt <= DateTime.UtcNow);
        Assert.Null(request.Timeout);
        Assert.NotNull(request.Headers);
        Assert.Empty(request.Headers);
    }

    [Fact]
    public void MCPRequest_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var parameters = JsonDocument.Parse("{}");
        var createdAt = DateTime.UtcNow;
        var timeout = TimeSpan.FromSeconds(30);
        var headers = new Dictionary<string, string> { { "Authorization", "Bearer token" } };

        // Act
        var request = new MCPRequest
        {
            Id = id,
            Method = "test.method",
            Params = parameters,
            ServerId = "server-123",
            CreatedAt = createdAt,
            Timeout = timeout,
            Headers = headers
        };

        // Assert
        Assert.Equal(id, request.Id);
        Assert.Equal("test.method", request.Method);
        Assert.Equal(parameters, request.Params);
        Assert.Equal("server-123", request.ServerId);
        Assert.Equal(createdAt, request.CreatedAt);
        Assert.Equal(timeout, request.Timeout);
        Assert.Equal(headers, request.Headers);
    }

    [Fact]
    public void MCPResponse_ShouldInitializeWithDefaults()
    {
        // Act
        var response = new MCPResponse();

        // Assert
        Assert.Equal(string.Empty, response.Id);
        Assert.False(response.Success);
        Assert.Null(response.Result);
        Assert.Null(response.Error);
        Assert.True(response.ResponseTime <= DateTime.UtcNow);
        Assert.Equal(0.0, response.ProcessingTimeMs);
        Assert.NotNull(response.Metadata);
        Assert.Empty(response.Metadata);
    }

    [Fact]
    public void MCPResponse_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = "response-123";
        var result = JsonDocument.Parse("{}");
        var error = new MCPError();
        var responseTime = DateTime.UtcNow;
        var metadata = new Dictionary<string, object> { { "key", "value" } };

        // Act
        var response = new MCPResponse
        {
            Id = id,
            Success = true,
            Result = result,
            Error = error,
            ResponseTime = responseTime,
            ProcessingTimeMs = 150.5,
            Metadata = metadata
        };

        // Assert
        Assert.Equal(id, response.Id);
        Assert.True(response.Success);
        Assert.Equal(result, response.Result);
        Assert.Equal(error, response.Error);
        Assert.Equal(responseTime, response.ResponseTime);
        Assert.Equal(150.5, response.ProcessingTimeMs);
        Assert.Equal(metadata, response.Metadata);
    }

    [Fact]
    public void MCPError_ShouldInitializeWithDefaults()
    {
        // Act
        var error = new MCPError();

        // Assert
        Assert.Equal(0, error.Code);
        Assert.Equal(string.Empty, error.Message);
        Assert.Null(error.Data);
        Assert.Null(error.StackTrace);
    }

    [Fact]
    public void MCPError_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var data = JsonDocument.Parse("{}");

        // Act
        var error = new MCPError
        {
            Code = 404,
            Message = "Not Found",
            Data = data,
            StackTrace = "Stack trace here"
        };

        // Assert
        Assert.Equal(404, error.Code);
        Assert.Equal("Not Found", error.Message);
        Assert.Equal(data, error.Data);
        Assert.Equal("Stack trace here", error.StackTrace);
    }

    [Fact]
    public void MCPSession_ShouldInitializeWithDefaults()
    {
        // Act
        var session = new MCPSession();

        // Assert
        Assert.NotNull(session.Id);
        Assert.False(string.IsNullOrEmpty(session.Id));
        Assert.Equal(string.Empty, session.ServerId);
        Assert.Equal(string.Empty, session.UserId);
        Assert.True(session.StartedAt <= DateTime.UtcNow);
        Assert.Null(session.EndedAt);
        Assert.Equal(MCPSessionStatus.Active, session.Status);
        Assert.NotNull(session.Requests);
        Assert.Empty(session.Requests);
        Assert.NotNull(session.Context);
        Assert.Empty(session.Context);
        Assert.Null(session.MaxDuration);
    }

    [Fact]
    public void MCPSession_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var startedAt = DateTime.UtcNow;
        var endedAt = DateTime.UtcNow.AddMinutes(5);
        var requests = new List<MCPRequest> { new() };
        var context = new Dictionary<string, object> { { "key", "value" } };
        var maxDuration = TimeSpan.FromMinutes(30);

        // Act
        var session = new MCPSession
        {
            Id = id,
            ServerId = "server-123",
            UserId = "user-456",
            StartedAt = startedAt,
            EndedAt = endedAt,
            Status = MCPSessionStatus.Expired,
            Requests = requests,
            Context = context,
            MaxDuration = maxDuration
        };

        // Assert
        Assert.Equal(id, session.Id);
        Assert.Equal("server-123", session.ServerId);
        Assert.Equal("user-456", session.UserId);
        Assert.Equal(startedAt, session.StartedAt);
        Assert.Equal(endedAt, session.EndedAt);
        Assert.Equal(MCPSessionStatus.Expired, session.Status);
        Assert.Equal(requests, session.Requests);
        Assert.Equal(context, session.Context);
        Assert.Equal(maxDuration, session.MaxDuration);
    }

    [Fact]
    public void MCPSocialMediaIntegration_ShouldInitializeWithDefaults()
    {
        // Act
        var integration = new MCPSocialMediaIntegration();

        // Assert
        Assert.NotNull(integration.Id);
        Assert.False(string.IsNullOrEmpty(integration.Id));
        Assert.Equal(string.Empty, integration.Name);
        Assert.NotNull(integration.SupportedPlatforms);
        Assert.Empty(integration.SupportedPlatforms);
        Assert.NotNull(integration.SupportedOperations);
        Assert.Empty(integration.SupportedOperations);
        Assert.NotNull(integration.Server);
        Assert.True(integration.IsEnabled);
        Assert.NotNull(integration.Configuration);
        Assert.Empty(integration.Configuration);
    }

    [Fact]
    public void MCPSocialMediaIntegration_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var platforms = new List<SocialPlatform> { SocialPlatform.BlueSky, SocialPlatform.BlueSky };
        var operations = new List<MCPSocialOperation> { new() };
        var server = new MCPServer();
        var configuration = new Dictionary<string, object> { { "key", "value" } };

        // Act
        var integration = new MCPSocialMediaIntegration
        {
            Id = id,
            Name = "Test Integration",
            SupportedPlatforms = platforms,
            SupportedOperations = operations,
            Server = server,
            IsEnabled = false,
            Configuration = configuration
        };

        // Assert
        Assert.Equal(id, integration.Id);
        Assert.Equal("Test Integration", integration.Name);
        Assert.Equal(platforms, integration.SupportedPlatforms);
        Assert.Equal(operations, integration.SupportedOperations);
        Assert.Equal(server, integration.Server);
        Assert.False(integration.IsEnabled);
        Assert.Equal(configuration, integration.Configuration);
    }

    [Fact]
    public void MCPSocialOperation_ShouldInitializeWithDefaults()
    {
        // Act
        var operation = new MCPSocialOperation();

        // Assert
        Assert.Equal(string.Empty, operation.Name);
        Assert.Equal(string.Empty, operation.Description);
        Assert.Equal(default(MCPOperationType), operation.Type);
        Assert.Null(operation.InputSchema);
        Assert.Null(operation.OutputSchema);
        Assert.NotNull(operation.RequiredScopes);
        Assert.Empty(operation.RequiredScopes);
        Assert.True(operation.RequiresAuth);
    }

    [Fact]
    public void MCPSocialOperation_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var inputSchema = JsonDocument.Parse("{}");
        var outputSchema = JsonDocument.Parse("{}");
        var scopes = new List<string> { "read", "write" };

        // Act
        var operation = new MCPSocialOperation
        {
            Name = "Test Operation",
            Description = "Test Description",
            Type = MCPOperationType.Write,
            InputSchema = inputSchema,
            OutputSchema = outputSchema,
            RequiredScopes = scopes,
            RequiresAuth = false
        };

        // Assert
        Assert.Equal("Test Operation", operation.Name);
        Assert.Equal("Test Description", operation.Description);
        Assert.Equal(MCPOperationType.Write, operation.Type);
        Assert.Equal(inputSchema, operation.InputSchema);
        Assert.Equal(outputSchema, operation.OutputSchema);
        Assert.Equal(scopes, operation.RequiredScopes);
        Assert.False(operation.RequiresAuth);
    }

    [Fact]
    public void MCPAnalytics_ShouldInitializeWithDefaults()
    {
        // Act
        var analytics = new MCPAnalytics();

        // Assert
        Assert.Equal(string.Empty, analytics.ServerId);
        Assert.Equal(DateTime.UtcNow.Date, analytics.Date);
        Assert.Equal(0, analytics.TotalRequests);
        Assert.Equal(0, analytics.SuccessfulRequests);
        Assert.Equal(0, analytics.FailedRequests);
        Assert.Equal(0.0, analytics.AverageResponseTime);
        Assert.Equal(0.0, analytics.MaxResponseTime);
        Assert.Equal(0.0, analytics.MinResponseTime);
        Assert.Equal(0, analytics.TimeoutCount);
        Assert.NotNull(analytics.ErrorCodes);
        Assert.Empty(analytics.ErrorCodes);
        Assert.NotNull(analytics.MethodCounts);
        Assert.Empty(analytics.MethodCounts);
        Assert.Equal(0.0, analytics.UptimePercentage);
    }

    [Fact]
    public void MCPAnalytics_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var date = DateTime.UtcNow.Date;
        var errorCodes = new Dictionary<string, int> { { "404", 5 }, { "500", 2 } };
        var methodCounts = new Dictionary<string, int> { { "GET", 100 }, { "POST", 50 } };

        // Act
        var analytics = new MCPAnalytics
        {
            ServerId = "server-123",
            Date = date,
            TotalRequests = 150,
            SuccessfulRequests = 143,
            FailedRequests = 7,
            AverageResponseTime = 250.5,
            MaxResponseTime = 1500.0,
            MinResponseTime = 50.0,
            TimeoutCount = 2,
            ErrorCodes = errorCodes,
            MethodCounts = methodCounts,
            UptimePercentage = 98.5
        };

        // Assert
        Assert.Equal("server-123", analytics.ServerId);
        Assert.Equal(date, analytics.Date);
        Assert.Equal(150, analytics.TotalRequests);
        Assert.Equal(143, analytics.SuccessfulRequests);
        Assert.Equal(7, analytics.FailedRequests);
        Assert.Equal(250.5, analytics.AverageResponseTime);
        Assert.Equal(1500.0, analytics.MaxResponseTime);
        Assert.Equal(50.0, analytics.MinResponseTime);
        Assert.Equal(2, analytics.TimeoutCount);
        Assert.Equal(errorCodes, analytics.ErrorCodes);
        Assert.Equal(methodCounts, analytics.MethodCounts);
        Assert.Equal(98.5, analytics.UptimePercentage);
    }

    [Fact]
    public void MCPHealthCheck_ShouldInitializeWithDefaults()
    {
        // Act
        var healthCheck = new MCPHealthCheck();

        // Assert
        Assert.Equal(string.Empty, healthCheck.ServerId);
        Assert.True(healthCheck.CheckedAt <= DateTime.UtcNow);
        Assert.Equal(default(MCPHealthStatus), healthCheck.Status);
        Assert.Equal(0.0, healthCheck.ResponseTimeMs);
        Assert.Null(healthCheck.ErrorMessage);
        Assert.NotNull(healthCheck.Details);
        Assert.Empty(healthCheck.Details);
        Assert.NotNull(healthCheck.AvailableCapabilities);
        Assert.Empty(healthCheck.AvailableCapabilities);
    }

    [Fact]
    public void MCPHealthCheck_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var checkedAt = DateTime.UtcNow;
        var details = new Dictionary<string, object> { { "version", "1.0.0" } };
        var capabilities = new List<MCPCapability> { new() };

        // Act
        var healthCheck = new MCPHealthCheck
        {
            ServerId = "server-123",
            CheckedAt = checkedAt,
            Status = MCPHealthStatus.Healthy,
            ResponseTimeMs = 125.5,
            ErrorMessage = "Test error",
            Details = details,
            AvailableCapabilities = capabilities
        };

        // Assert
        Assert.Equal("server-123", healthCheck.ServerId);
        Assert.Equal(checkedAt, healthCheck.CheckedAt);
        Assert.Equal(MCPHealthStatus.Healthy, healthCheck.Status);
        Assert.Equal(125.5, healthCheck.ResponseTimeMs);
        Assert.Equal("Test error", healthCheck.ErrorMessage);
        Assert.Equal(details, healthCheck.Details);
        Assert.Equal(capabilities, healthCheck.AvailableCapabilities);
    }

    [Fact]
    public void MCPConfiguration_ShouldInitializeWithDefaults()
    {
        // Act
        var configuration = new MCPConfiguration();

        // Assert
        Assert.NotNull(configuration.Servers);
        Assert.Empty(configuration.Servers);
        Assert.NotNull(configuration.Settings);
        Assert.NotNull(configuration.Security);
        Assert.NotNull(configuration.GlobalContext);
        Assert.Empty(configuration.GlobalContext);
    }

    [Fact]
    public void MCPConfiguration_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var servers = new List<MCPServer> { new() };
        var settings = new MCPSettings();
        var security = new MCPSecuritySettings();
        var globalContext = new Dictionary<string, object> { { "key", "value" } };

        // Act
        var configuration = new MCPConfiguration
        {
            Servers = servers,
            Settings = settings,
            Security = security,
            GlobalContext = globalContext
        };

        // Assert
        Assert.Equal(servers, configuration.Servers);
        Assert.Equal(settings, configuration.Settings);
        Assert.Equal(security, configuration.Security);
        Assert.Equal(globalContext, configuration.GlobalContext);
    }

    [Fact]
    public void MCPSettings_ShouldInitializeWithDefaults()
    {
        // Act
        var settings = new MCPSettings();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(30), settings.DefaultTimeout);
        Assert.Equal(10, settings.MaxConcurrentRequests);
        Assert.Equal(3, settings.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), settings.RetryDelay);
        Assert.True(settings.EnableAnalytics);
        Assert.True(settings.EnableHealthChecks);
        Assert.Equal(TimeSpan.FromMinutes(5), settings.HealthCheckInterval);
        Assert.True(settings.EnableLogging);
        Assert.Equal("Information", settings.LogLevel);
    }

    [Fact]
    public void MCPSettings_ShouldSetPropertiesCorrectly()
    {
        // Act
        var settings = new MCPSettings
        {
            DefaultTimeout = TimeSpan.FromMinutes(1),
            MaxConcurrentRequests = 20,
            MaxRetryAttempts = 5,
            RetryDelay = TimeSpan.FromSeconds(2),
            EnableAnalytics = false,
            EnableHealthChecks = false,
            HealthCheckInterval = TimeSpan.FromMinutes(10),
            EnableLogging = false,
            LogLevel = "Debug"
        };

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(1), settings.DefaultTimeout);
        Assert.Equal(20, settings.MaxConcurrentRequests);
        Assert.Equal(5, settings.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(2), settings.RetryDelay);
        Assert.False(settings.EnableAnalytics);
        Assert.False(settings.EnableHealthChecks);
        Assert.Equal(TimeSpan.FromMinutes(10), settings.HealthCheckInterval);
        Assert.False(settings.EnableLogging);
        Assert.Equal("Debug", settings.LogLevel);
    }

    [Fact]
    public void MCPSecuritySettings_ShouldInitializeWithDefaults()
    {
        // Act
        var security = new MCPSecuritySettings();

        // Assert
        Assert.True(security.RequireEncryption);
        Assert.NotNull(security.AllowedOrigins);
        Assert.Empty(security.AllowedOrigins);
        Assert.NotNull(security.BlockedIpAddresses);
        Assert.Empty(security.BlockedIpAddresses);
        Assert.True(security.ValidateServerCertificates);
        Assert.Equal(TimeSpan.FromHours(1), security.TokenExpiration);
        Assert.True(security.EnableRateLimiting);
        Assert.Equal(100, security.RateLimitRequests);
        Assert.Equal(TimeSpan.FromMinutes(1), security.RateLimitWindow);
    }

    [Fact]
    public void MCPSecuritySettings_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var allowedOrigins = new List<string> { "https://trusted.com" };
        var blockedIps = new List<string> { "192.168.1.100" };

        // Act
        var security = new MCPSecuritySettings
        {
            RequireEncryption = false,
            AllowedOrigins = allowedOrigins,
            BlockedIpAddresses = blockedIps,
            ValidateServerCertificates = false,
            TokenExpiration = TimeSpan.FromHours(2),
            EnableRateLimiting = false,
            RateLimitRequests = 200,
            RateLimitWindow = TimeSpan.FromMinutes(2)
        };

        // Assert
        Assert.False(security.RequireEncryption);
        Assert.Equal(allowedOrigins, security.AllowedOrigins);
        Assert.Equal(blockedIps, security.BlockedIpAddresses);
        Assert.False(security.ValidateServerCertificates);
        Assert.Equal(TimeSpan.FromHours(2), security.TokenExpiration);
        Assert.False(security.EnableRateLimiting);
        Assert.Equal(200, security.RateLimitRequests);
        Assert.Equal(TimeSpan.FromMinutes(2), security.RateLimitWindow);
    }

    // Enum tests
    [Theory]
    [InlineData(MCPServerType.HTTP)]
    [InlineData(MCPServerType.WebSocket)]
    [InlineData(MCPServerType.gRPC)]
    [InlineData(MCPServerType.TCP)]
    [InlineData(MCPServerType.NamedPipe)]
    public void MCPServerType_ShouldHaveAllValues(MCPServerType serverType)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(MCPServerType), serverType));
    }

    [Theory]
    [InlineData(MCPAuthType.None)]
    [InlineData(MCPAuthType.ApiKey)]
    [InlineData(MCPAuthType.Basic)]
    [InlineData(MCPAuthType.Bearer)]
    [InlineData(MCPAuthType.OAuth2)]
    [InlineData(MCPAuthType.Certificate)]
    [InlineData(MCPAuthType.Custom)]
    public void MCPAuthType_ShouldHaveAllValues(MCPAuthType authType)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(MCPAuthType), authType));
    }

    [Theory]
    [InlineData(MCPServerStatus.Disconnected)]
    [InlineData(MCPServerStatus.Connecting)]
    [InlineData(MCPServerStatus.Connected)]
    [InlineData(MCPServerStatus.Error)]
    [InlineData(MCPServerStatus.Maintenance)]
    public void MCPServerStatus_ShouldHaveAllValues(MCPServerStatus status)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(MCPServerStatus), status));
    }

    [Theory]
    [InlineData(MCPSessionStatus.Active)]
    [InlineData(MCPSessionStatus.Idle)]
    [InlineData(MCPSessionStatus.Expired)]
    [InlineData(MCPSessionStatus.Terminated)]
    [InlineData(MCPSessionStatus.Error)]
    public void MCPSessionStatus_ShouldHaveAllValues(MCPSessionStatus status)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(MCPSessionStatus), status));
    }

    [Theory]
    [InlineData(MCPOperationType.Read)]
    [InlineData(MCPOperationType.Write)]
    [InlineData(MCPOperationType.Execute)]
    [InlineData(MCPOperationType.Query)]
    [InlineData(MCPOperationType.Stream)]
    [InlineData(MCPOperationType.Batch)]
    public void MCPOperationType_ShouldHaveAllValues(MCPOperationType operationType)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(MCPOperationType), operationType));
    }

    [Theory]
    [InlineData(MCPHealthStatus.Healthy)]
    [InlineData(MCPHealthStatus.Degraded)]
    [InlineData(MCPHealthStatus.Unhealthy)]
    [InlineData(MCPHealthStatus.Unknown)]
    public void MCPHealthStatus_ShouldHaveAllValues(MCPHealthStatus healthStatus)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(MCPHealthStatus), healthStatus));
    }
}
