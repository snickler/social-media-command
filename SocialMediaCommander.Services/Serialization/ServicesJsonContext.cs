using System.Text.Json.Serialization;
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