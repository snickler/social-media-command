using System.Text.Json.Serialization;
using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Core.Serialization;

/// <summary>
/// JSON source generator context for AOT compilation support.
/// This defines all the types that need to be serialized/deserialized in the application.
/// </summary>
[JsonSerializableAttribute(typeof(Account))]
[JsonSerializableAttribute(typeof(Post))]
[JsonSerializableAttribute(typeof(UserProfile))]
[JsonSerializableAttribute(typeof(OAuthConfig))]
[JsonSerializableAttribute(typeof(AIModelConfig))]
[JsonSerializableAttribute(typeof(OAuthTokens))]
[JsonSerializableAttribute(typeof(ValidationResult))]
[JsonSerializableAttribute(typeof(BackupData))]
[JsonSerializableAttribute(typeof(BackupInfo))]
[JsonSerializableAttribute(typeof(TweetData))]
[JsonSerializableAttribute(typeof(ThreadTweetData))]
[JsonSerializableAttribute(typeof(RetweetData))]
[JsonSerializableAttribute(typeof(LikeData))]
[JsonSerializableAttribute(typeof(FacebookPostData))]
[JsonSerializableAttribute(typeof(FacebookShareData))]
[JsonSerializableAttribute(typeof(ThreadsPostData))]
[JsonSerializableAttribute(typeof(ThreadsPublishData))]
[JsonSerializableAttribute(typeof(ThreadsLikeData))]
[JsonSerializableAttribute(typeof(ThreadsRepostData))]
[JsonSerializableAttribute(typeof(OpenAIChatResponse))]
[JsonSerializableAttribute(typeof(Dictionary<string, object?>))]
[JsonSerializableAttribute(typeof(Dictionary<string, string>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class SocialMediaCommanderJsonContext : JsonSerializerContext
{
}

// Data transfer objects for API calls
public class TweetData
{
    public string Text { get; set; } = string.Empty;
}

public class ThreadTweetData
{
    public string Text { get; set; } = string.Empty;
    public ReplyInfo? Reply { get; set; }
}

public class ReplyInfo
{
    public string InReplyToTweetId { get; set; } = string.Empty;
}

public class RetweetData
{
    public string TweetId { get; set; } = string.Empty;
}

public class LikeData
{
    public string TweetId { get; set; } = string.Empty;
}

public class FacebookPostData
{
    public string Message { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
}

public class FacebookShareData
{
    public string Link { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
}

public class ThreadsPostData
{
    public string Text { get; set; } = string.Empty;
    public string MediaType { get; set; } = "TEXT";
    public string AccessToken { get; set; } = string.Empty;
}

public class ThreadsPublishData
{
    public string CreationId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
}

public class ThreadsLikeData
{
    public string ThreadId { get; set; } = string.Empty;
}

public class ThreadsRepostData
{
    public string MediaType { get; set; } = "TEXT";
    public string Text { get; set; } = string.Empty;
    public string QuotePostId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
}

public class OpenAIChatResponse
{
    public string Id { get; set; } = string.Empty;
    public string Object { get; set; } = string.Empty;
    public long Created { get; set; }
    public string Model { get; set; } = string.Empty;
    public List<Choice> Choices { get; set; } = new();
    public Usage? Usage { get; set; }
}

public class Choice
{
    public int Index { get; set; }
    public Message? Message { get; set; }
    public string FinishReason { get; set; } = string.Empty;
}

public class Message
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class Usage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}