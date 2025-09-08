using System.Text.Json;
using System.Text.Json.Serialization;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Core.Serialization;
using SocialMediaCommander.Services.Implementation;

namespace SocialMediaCommander.Services.Serialization;

/// <summary>
/// JSON source generator context for Services-specific types
/// </summary>
[JsonSerializableAttribute(typeof(AppSettings))]
[JsonSerializableAttribute(typeof(WindowSettings))]
[JsonSerializableAttribute(typeof(Dictionary<string, object?>))]
[JsonSerializableAttribute(typeof(Dictionary<string, string>))]
[JsonSerializableAttribute(typeof(Dictionary<string, int>))]
[JsonSerializableAttribute(typeof(Dictionary<string, OAuthConfig>))]
[JsonSerializableAttribute(typeof(OAuthConfig))]
[JsonSerializableAttribute(typeof(JsonElement))]
[JsonSerializableAttribute(typeof(OpenAIChatRequest))]
[JsonSerializableAttribute(typeof(OpenAIMessage))]
[JsonSerializableAttribute(typeof(BlueSkySessionData))]
[JsonSerializableAttribute(typeof(BlueSkyPostData))]
[JsonSerializableAttribute(typeof(BlueSkyPostRecord))]
[JsonSerializableAttribute(typeof(BlueSkyReply))]
[JsonSerializableAttribute(typeof(BlueSkyParent))]
[JsonSerializableAttribute(typeof(BlueSkyDeleteData))]
[JsonSerializableAttribute(typeof(string))]
[JsonSerializableAttribute(typeof(int))]
[JsonSerializableAttribute(typeof(bool))]
[JsonSerializableAttribute(typeof(DateTime))]
[JsonSerializableAttribute(typeof(TimeSpan))]
[JsonSerializableAttribute(typeof(object))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class ServicesJsonContext : JsonSerializerContext
{
}

// OpenAI API types for AOT compatibility
public class OpenAIChatRequest
{
    public string Model { get; set; } = string.Empty;
    public OpenAIMessage[] Messages { get; set; } = Array.Empty<OpenAIMessage>();
    public double Temperature { get; set; }
    public int MaxTokens { get; set; }
    public bool Stream { get; set; }
}

public class OpenAIMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
public class BlueSkySessionData
{
    public string Identifier { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class BlueSkyPostData
{
    public string Repo { get; set; } = string.Empty;
    public string Collection { get; set; } = string.Empty;
    public BlueSkyPostRecord Record { get; set; } = new();
}

public class BlueSkyPostRecord
{
    public string Text { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public object[]? Facets { get; set; }
    public BlueSkyReply? Reply { get; set; }
}

public class BlueSkyReply
{
    public BlueSkyParent Parent { get; set; } = new();
}

public class BlueSkyParent
{
    public string Uri { get; set; } = string.Empty;
    public string Cid { get; set; } = string.Empty;
}

public class BlueSkyDeleteData
{
    public string Repo { get; set; } = string.Empty;
    public string Collection { get; set; } = string.Empty;
    public string Rkey { get; set; } = string.Empty;
}