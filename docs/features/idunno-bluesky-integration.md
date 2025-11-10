# idunno.Bluesky Integration Guide

## Overview

The `idunno.Bluesky` package (v1.1.0) has been added to provide production-ready BlueSky/AT Protocol support. This replaces the manual HTTP client implementation with a fully-featured BlueSky client library.

## Package Added

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="idunno.Bluesky" Version="1.1.0" />
<PackageVersion Include="System.Text.Json" Version="9.0.9" /> <!-- Updated for compatibility -->
```

## Next Steps: Update BlueSkyService

The `BlueSkyService.cs` needs to be refactored to use `idunno.Bluesky.BlueskyAgent` instead of manual HTTP calls.

### Current Implementation Issues
- Manual HTTP client usage
- Manual JSON serialization/deserialization
- Manual OAuth token management
- Incomplete session management

### New Implementation with idunno.Bluesky

#### 1. Authentication with App Password

```csharp
using idunno.Bluesky;
using SocialMediaCommander.Core.Models;

public class BlueSkyService : IBlueSkyService
{
    private readonly Dictionary<string, BlueskyAgent> _agents = new();
    
    private async Task<BlueskyAgent?> GetOrCreateAgentAsync(Account account)
    {
        if (_agents.TryGetValue(account.Id, out var existingAgent))
        {
            return existingAgent;
        }
        
        var agent = new BlueskyAgent();
        
        if (account.AuthMethod == AuthenticationMethod.AppPassword)
        {
            // Login with App Password
            var loginResult = await agent.Login(account.Username, account.AppPassword!);
            
            if (loginResult.Succeeded)
            {
                _agents[account.Id] = agent;
                return agent;
            }
        }
        else if (account.AuthMethod == AuthenticationMethod.OAuth)
        {
            // Login with OAuth tokens
            // idunno.Bluesky supports session resumption
            if (account.Tokens != null)
            {
                // Note: idunno.Bluesky uses session tokens differently
                // May need to re-authenticate or use refresh tokens
                var loginResult = await agent.ResumeSession(
                    account.Tokens.AccessToken,
                    account.Tokens.RefreshToken);
                    
                if (loginResult.Succeeded)
                {
                    _agents[account.Id] = agent;
                    return agent;
                }
            }
        }
        
        return null;
    }
}
```

#### 2. Creating Posts

```csharp
public async Task<PublishResult> PostAsync(Post post, Account account)
{
    try
    {
        var agent = await GetOrCreateAgentAsync(account);
        if (agent == null)
        {
            return new PublishResult
            {
                Success = false,
                ErrorMessage = "Failed to authenticate with BlueSky"
            };
        }
        
        // Create post with idunno.Bluesky
        var postText = post.FormatForPlatform(Platform);
        var response = await agent.Post(postText);
        
        if (response.Succeeded)
        {
            return new PublishResult
            {
                Success = true,
                PlatformPostId = response.Result!.Uri.ToString(),
                PublishedAt = DateTime.UtcNow
            };
        }
        
        return new PublishResult
        {
            Success = false,
            ErrorMessage = response.Error ?? "Unknown error"
        };
    }
    catch (Exception ex)
    {
        return new PublishResult
        {
            Success = false,
            ErrorMessage = $"Failed to post to BlueSky: {ex.Message}"
        };
    }
}
```

#### 3. Creating Posts with Media

```csharp
public async Task<PublishResult> PostWithMediaAsync(Post post, Account account)
{
    var agent = await GetOrCreateAgentAsync(account);
    if (agent == null) return FailureResult("Authentication failed");
    
    var postBuilder = new PostBuilder();
    postBuilder.WithText(post.FormatForPlatform(Platform));
    
    // Add images
    foreach (var media in post.Media.Where(m => m.Type == MediaType.Image))
    {
        using var stream = File.OpenRead(media.Path);
        await postBuilder.WithImage(stream, media.AltText);
    }
    
    var response = await agent.Post(postBuilder);
    
    if (response.Succeeded)
    {
        return new PublishResult
        {
            Success = true,
            PlatformPostId = response.Result!.Uri.ToString(),
            PublishedAt = DateTime.UtcNow
        };
    }
    
    return new PublishResult
    {
        Success = false,
        ErrorMessage = response.Error ?? "Unknown error"
    };
}
```

#### 4. Creating Threads

```csharp
public async Task<PublishResult> PostThreadAsync(Post post, Account account)
{
    var agent = await GetOrCreateAgentAsync(account);
    if (agent == null) return FailureResult("Authentication failed");
    
    if (!post.IsThread || !post.ThreadPosts.Any())
    {
        return await PostAsync(post, account);
    }
    
    // Post main post
    var mainResponse = await agent.Post(post.FormatForPlatform(Platform));
    if (!mainResponse.Succeeded)
    {
        return FailureResult(mainResponse.Error ?? "Failed to post main thread post");
    }
    
    var previousUri = mainResponse.Result!.Uri;
    
    // Post replies
    foreach (var threadPost in post.ThreadPosts)
    {
        var replyBuilder = new PostBuilder();
        replyBuilder.WithText(threadPost.Content);
        replyBuilder.InReplyTo(previousUri);
        
        var replyResponse = await agent.Post(replyBuilder);
        if (!replyResponse.Succeeded)
        {
            return FailureResult($"Thread failed at reply: {replyResponse.Error}");
        }
        
        previousUri = replyResponse.Result!.Uri;
    }
    
    return new PublishResult
    {
        Success = true,
        PlatformPostId = mainResponse.Result.Uri.ToString(),
        PublishedAt = DateTime.UtcNow
    };
}
```

#### 5. Getting Timeline

```csharp
public async Task<IEnumerable<SocialFeedItem>> GetTimelineAsync(Account account, int limit = 50)
{
    var agent = await GetOrCreateAgentAsync(account);
    if (agent == null) return Enumerable.Empty<SocialFeedItem>();
    
    var timelineResponse = await agent.GetTimeline(limit: limit);
    
    if (!timelineResponse.Succeeded || timelineResponse.Result == null)
    {
        return Enumerable.Empty<SocialFeedItem>();
    }
    
    return timelineResponse.Result.Feed.Select(feedView => new SocialFeedItem
    {
        Id = feedView.Post.Uri.ToString(),
        Content = feedView.Post.Record.Text,
        AuthorUsername = feedView.Post.Author.Handle,
        AuthorName = feedView.Post.Author.DisplayName ?? feedView.Post.Author.Handle,
        PostedAt = feedView.Post.Record.CreatedAt.DateTime,
        Platform = Platform,
        LikeCount = feedView.Post.LikeCount ?? 0,
        RepostCount = feedView.Post.RepostCount ?? 0,
        ReplyCount = feedView.Post.ReplyCount ?? 0
    });
}
```

#### 6. Getting User Profile

```csharp
public async Task<UserProfile?> GetProfileAsync(Account account)
{
    var agent = await GetOrCreateAgentAsync(account);
    if (agent == null) return null;
    
    var profileResponse = await agent.GetProfile(account.Username);
    
    if (!profileResponse.Succeeded || profileResponse.Result == null)
    {
        return null;
    }
    
    var profile = profileResponse.Result;
    
    return new UserProfile
    {
        Id = profile.Did.ToString(),
        Username = profile.Handle,
        DisplayName = profile.DisplayName ?? profile.Handle,
        Bio = profile.Description,
        Avatar = profile.Avatar?.ToString(),
        Banner = profile.Banner?.ToString(),
        FollowerCount = profile.FollowersCount ?? 0,
        FollowingCount = profile.FollowsCount ?? 0,
        PostCount = profile.PostsCount ?? 0,
        CreatedAt = DateTime.UtcNow, // AT Protocol doesn't expose account creation date
        Platform = Platform
    };
}
```

#### 7. Deleting Posts

```csharp
public async Task<bool> DeletePostAsync(string postId, Account account)
{
    var agent = await GetOrCreateAgentAsync(account);
    if (agent == null) return false;
    
    try
    {
        // Extract AT URI from post ID
        var uri = new Uri(postId);
        var response = await agent.DeletePost(uri);
        
        return response.Succeeded;
    }
    catch
    {
        return false;
    }
}
```

#### 8. Searching Posts

```csharp
public async Task<IEnumerable<SocialFeedItem>> SearchPostsAsync(string query, Account account, int limit = 20)
{
    var agent = await GetOrCreateAgentAsync(account);
    if (agent == null) return Enumerable.Empty<SocialFeedItem>();
    
    var searchResponse = await agent.SearchPosts(query, limit: limit);
    
    if (!searchResponse.Succeeded || searchResponse.Result == null)
    {
        return Enumerable.Empty<SocialFeedItem>();
    }
    
    return searchResponse.Result.Posts.Select(post => new SocialFeedItem
    {
        Id = post.Uri.ToString(),
        Content = post.Record.Text,
        AuthorUsername = post.Author.Handle,
        AuthorName = post.Author.DisplayName ?? post.Author.Handle,
        PostedAt = post.Record.CreatedAt.DateTime,
        Platform = Platform,
        LikeCount = post.LikeCount ?? 0,
        RepostCount = post.RepostCount ?? 0,
        ReplyCount = post.ReplyCount ?? 0
    });
}
```

## Benefits of idunno.Bluesky

### 1. **Automatic Session Management**
- Handles session creation and refresh
- Manages authentication tokens
- Supports both username/password and app passwords

### 2. **Rich Post Creation**
- `PostBuilder` for complex posts
- Automatic facet detection (mentions, links, hashtags)
- Image and video upload support
- Thread creation helpers

### 3. **Type-Safe API**
- Strongly-typed models for all AT Protocol types
- No manual JSON parsing needed
- Better error handling

### 4. **Comprehensive Features**
- Notifications
- Direct messages
- Following/unfollowing
- Likes, reposts, quotes
- Muting and blocking
- Thread viewing

### 5. **Performance**
- Connection pooling
- Efficient serialization
- Async/await throughout
- .NET 9 trimming support

## Integration Checklist

- [x] Add `idunno.Bluesky` package (v1.1.0)
- [x] Update `System.Text.Json` to 9.0.9
- [ ] Refactor `BlueSkyService` to use `BlueskyAgent`
- [ ] Update `CreateSessionAsync` to use idunno.Bluesky login
- [ ] Update `PostAsync` to use `agent.Post()`
- [ ] Update `PostThreadAsync` to use reply chains
- [ ] Update `GetTimelineAsync` to use `agent.GetTimeline()`
- [ ] Update `GetProfileAsync` to use `agent.GetProfile()`
- [ ] Update `DeletePostAsync` to use `agent.DeletePost()`
- [ ] Update `SearchPostsAsync` to use `agent.SearchPosts()`
- [ ] Add media upload support
- [ ] Add notification support
- [ ] Test App Password authentication
- [ ] Test OAuth authentication (if supported)
- [ ] Update documentation

## App Password Flow

### User Setup
1. User goes to BlueSky Settings ? App Passwords
2. Click "Add App Password"
3. Name it "Social Media Commander"
4. Copy generated password (format: `xxxx-xxxx-xxxx-xxxx`)

### In Application
```csharp
var agent = new BlueskyAgent();
var loginResult = await agent.Login("alice.bsky.social", "abcd-efgh-ijkl-mnop");

if (loginResult.Succeeded)
{
    // Agent is authenticated and ready to use
    var postResponse = await agent.Post("Hello from Social Media Commander!");
}
```

## OAuth Flow (Future Enhancement)

idunno.Bluesky may support OAuth in future versions. Currently focuses on App Password authentication which is simpler and more appropriate for desktop applications.

## Error Handling

All idunno.Bluesky methods return `AtProtoHttpResult<T>` with:
- `Succeeded` - boolean indicating success
- `Result` - the actual data if successful
- `Error` - error message if failed
- `StatusCode` - HTTP status code

Example:
```csharp
var response = await agent.Post("Hello World");

if (response.Succeeded)
{
    Console.WriteLine($"Post created: {response.Result.Uri}");
}
else
{
    Console.WriteLine($"Post failed: {response.Error}");
    Console.WriteLine($"Status: {response.StatusCode}");
}
```

## Testing

### Unit Tests
Mock `BlueskyAgent` for testing:
```csharp
[Fact]
public async Task PostAsync_WithValidAccount_ReturnsSuccess()
{
    // Arrange
    var mockAgent = new Mock<IBlueskyAgent>();
    mockAgent.Setup(a => a.Post(It.IsAny<string>()))
             .ReturnsAsync(new AtProtoHttpResult<CreatePostResponse> 
             {
                 Succeeded = true,
                 Result = new CreatePostResponse { Uri = new Uri("at://did/post/123") }
             });
    
    var service = new BlueSkyService(mockAgent.Object);
    
    // Act
    var result = await service.PostAsync(testPost, testAccount);
    
    // Assert
    Assert.True(result.Success);
}
```

### Integration Tests
Use real BlueSky test account:
```csharp
[Fact(Skip = "Requires real BlueSky account")]
public async Task PostAsync_Integration_RealAccount()
{
    var service = new BlueSkyService();
    var account = new Account
    {
        Username = "test.bsky.social",
        AppPassword = Environment.GetEnvironmentVariable("BLUESKY_TEST_PASSWORD"),
        AuthMethod = AuthenticationMethod.AppPassword
    };
    
    var post = new Post { Content = "Integration test post" };
    var result = await service.PostAsync(post, account);
    
    Assert.True(result.Success);
    
    // Clean up
    await service.DeletePostAsync(result.PlatformPostId!, account);
}
```

## Documentation References

- **idunno.Bluesky Docs**: https://bluesky.idunno.dev/
- **API Reference**: https://bluesky.idunno.dev/api/
- **AT Protocol Spec**: https://atproto.com/
- **BlueSky API**: https://docs.bsky.app/

## Next Implementation Steps

1. **Backup Current BlueSkyService.cs**
   ```bash
   cp SocialMediaCommander.Services/Implementation/BlueSkyService.cs SocialMediaCommander.Services/Implementation/BlueSkyService.cs.backup
   ```

2. **Create New Implementation**
   - Start with authentication methods
   - Add posting functionality
   - Add timeline/feed retrieval
   - Add profile management
   - Add media upload

3. **Update Tests**
   - Update existing BlueSky tests
   - Add new tests for idunno.Bluesky features

4. **Update Documentation**
   - Update user guides
   - Update API documentation
   - Add idunno.Bluesky examples

## Summary

The `idunno.Bluesky` package provides:
- ? Production-ready BlueSky client
- ? Type-safe AT Protocol implementation
- ? Automatic session management
- ? Rich post creation with media
- ? Comprehensive feature set
- ? Better error handling
- ? Performance optimizations

This is a significant upgrade from the manual HTTP client implementation and will provide a much better experience for users posting to BlueSky.
