# ?? Post Publishing Architecture - Updated

## Overview

The post publishing system now uses **real platform services** instead of mock implementations.

```
User Input ? ViewModel ? PostService ? Platform Services ? External APIs
     ?                      ?               ?                    ?
   UI Layer          Orchestration    Service Layer        BlueSky/X/etc.
```

---

## Component Responsibilities

### 1. `PostEditorViewModel` (UI Layer)
**Location**: `SocialMediaCommander.Desktop/ViewModels/PostEditorViewModel.cs`

**Responsibilities**:
- Collect user input (content, hashtags, media)
- Validate UI state
- Call `IPostService.PublishPostAsync()`
- Display results to user

**Key Code**:
```csharp
[RelayCommand]
private async Task PostAsync()
{
    var post = CreatePostFromViewModel();
    var result = await _postService.PublishPostAsync(post);  // ? Calls IPostService
    // Handle result
}
```

---

### 2. `PostService` (Orchestration Layer)
**Location**: `SocialMediaCommander.Services/Implementation/PostService.cs`

**Responsibilities**:
- Get authenticated accounts from `IAccountService`
- Validate posts for each platform
- **Delegate to actual platform services**
- Aggregate results
- Update post status

**Key Code**:
```csharp
public async Task<Dictionary<SocialPlatform, PublishResult>> PublishPostToPlatformsAsync(
    Post post, IEnumerable<SocialPlatform> platforms)
{
    // Get accounts
    var accounts = await _accountService.GetAllAccountsAsync();
    var accountsByPlatform = accounts
        .Where(a => a.IsAuthenticated)
        .GroupBy(a => a.PlatformId)
        .ToDictionary(g => g.Key, g => g.FirstOrDefault());
        
    // For each platform
    foreach (var platform in platforms)
    {
        var platformService = GetPlatformService(platform);  // ? Get real service
        var account = accountsByPlatform[platform];
        
        // Actually post!
        var publishResult = await platformService.PostAsync(post, account);
        results[platform] = publishResult;
    }
    
    return results;
}

private IPlatformService? GetPlatformService(SocialPlatform platform)
{
    return platform switch
    {
        SocialPlatform.BlueSky => _blueSkyService,  // ? Real BlueSkyService
        SocialPlatform.X => _twitterService,
        SocialPlatform.LinkedIn => _linkedInService,
        SocialPlatform.Threads => _threadsService,
        SocialPlatform.Facebook => _facebookService,
        _ => null
    };
}
```

---

### 3. Platform Services (Service Layer)

Each platform has its own service implementing `IPlatformService`:

#### `BlueSkyService`
**Location**: `SocialMediaCommander.Services/Implementation/BlueSkyService.cs`

```csharp
public async Task<PublishResult> PostAsync(Post post, Account account)
{
    // Create authenticated agent
    using var agent = (await CreateAuthenticatedAgentAsync(account)).Agent;
    
    // Format post
    var postText = post.FormatForPlatform(Platform);
    
    // ACTUALLY POST TO BLUESKY API
    var response = await agent.Post(postText);
    
    if (response.Succeeded)
    {
        return new PublishResult
        {
            Success = true,
            PlatformPostId = response.Result.Uri.ToString(),
            PublishedAt = DateTime.UtcNow
        };
    }
    
    return new PublishResult { Success = false, ErrorMessage = ... };
}
```

#### Other Services
- `TwitterService` - Posts to X/Twitter API
- `LinkedInService` - Posts to LinkedIn API
- `ThreadsService` - Posts to Threads API
- `FacebookService` - Posts to Facebook API

---

## Data Flow

### Successful Post Flow

```
1. User clicks "Post" button
   ?
2. PostEditorViewModel.PostAsync() called
   ?
3. Creates Post model from UI state
   ?
4. Calls PostService.PublishPostAsync(post)
   ?
5. PostService gets authenticated accounts
   ?
6. For each selected platform:
   ?? Gets platform service (e.g., BlueSkyService)
   ?? Gets account for that platform
   ?? Calls service.PostAsync(post, account)
   ?  ?
   ?  Platform Service:
   ?  ?? Authenticates with platform API
   ?  ?? Formats post for platform
   ?  ?? Makes HTTP request to platform
   ?  ?? Returns PublishResult
   ?
   ?? Aggregates result
   ?
7. PostService returns Dictionary<Platform, Result>
   ?
8. ViewModel displays success/error to user
```

### Error Handling Flow

```
Error at Platform API
   ?
Platform Service catches exception
   ?
Returns PublishResult { Success = false, ErrorMessage = ... }
   ?
PostService aggregates (may be partial success)
   ?
ViewModel shows specific error per platform
```

---

## Dependency Injection Setup

**File**: `SocialMediaCommander.Desktop/ServiceCollectionExtensions.cs`

```csharp
public static IServiceCollection AddSocialMediaCommanderServices(
    this IServiceCollection services, IConfiguration configuration)
{
    // Platform Services
    services.AddScoped<IBlueSkyService, BlueSkyService>();
    services.AddScoped<ITwitterService, TwitterService>();
    services.AddScoped<ILinkedInService, LinkedInService>();
    services.AddScoped<IThreadsService, ThreadsService>();
    services.AddScoped<IFacebookService, FacebookService>();

    // Application Services
    services.AddScoped<IPostService, PostService>();  // ? Real service!
    services.AddScoped<IAccountService, SecureAccountService>();
    
    // ... other services
    
    return services;
}
```

---

## Service Interfaces

### `IPostService`
**Location**: `SocialMediaCommander.Services/Interfaces/IPostService.cs`

Main methods:
- `PublishPostAsync(Post post)` - Publish to all target platforms
- `PublishPostToPlatformsAsync(Post post, IEnumerable<SocialPlatform> platforms)` - Publish to specific platforms
- `ValidatePostAsync(Post post)` - Validate for all platforms
- `SaveDraftAsync(Post post)` - Save as draft
- `GetPostPreviewsAsync(Post post)` - Get formatted previews

### `IPlatformService`
**Location**: `SocialMediaCommander.Services/Interfaces/IPlatformService.cs`

All platform services implement:
- `PostAsync(Post post, Account account)` - Post single message
- `PostThreadAsync(Post post, Account account)` - Post thread
- `DeletePostAsync(string postId, Account account)` - Delete post
- `GetUserPostsAsync(Account account, int limit)` - Get user's posts
- `GetTimelineAsync(Account account, int limit)` - Get timeline
- `SearchPostsAsync(string query, Account account, int limit)` - Search
- `ValidateContentAsync(Post post)` - Validate content
- `UploadMediaAsync(Media media, Account account)` - Upload media
- `GetPlatformLimitsAsync()` - Get platform limits

---

## Account Management Integration

### Account Selection

`PostService` automatically selects accounts:

```csharp
// Get all authenticated accounts
var accounts = await _accountService.GetAllAccountsAsync();

// Group by platform, taking first authenticated account per platform
var accountsByPlatform = accounts
    .Where(a => a.IsAuthenticated)
    .GroupBy(a => a.PlatformId)
    .ToDictionary(g => g.Key, g => g.FirstOrDefault());

// Use the account for posting
var account = accountsByPlatform[platform];
```

### Account Requirements

For a post to succeed, the account must:
1. ? Exist in `IAccountService`
2. ? Have `IsAuthenticated == true`
3. ? Have valid, non-expired tokens/credentials
4. ? Match the target platform

---

## Platform-Specific Details

### BlueSky
- **SDK**: `idunno.Bluesky` v1.1.0
- **Auth**: App Password or OAuth
- **Method**: `agent.Post(text)`
- **Thread**: `agent.ReplyTo(previousRef, text)`
- **Limits**: 300 chars, 4 media, 25 thread length

### Twitter/X
- **API**: Twitter API v2
- **Auth**: OAuth 2.0
- **Endpoint**: `POST /2/tweets`
- **Thread**: Reply with `reply.in_reply_to_tweet_id`
- **Limits**: 280 chars, 4 media, 25 thread length

### LinkedIn
- **API**: LinkedIn Share API
- **Auth**: OAuth 2.0
- **Endpoint**: `POST /v2/ugcPosts`
- **Thread**: Not supported (thread posts ignored)
- **Limits**: 3000 chars, 9 media

### Threads
- **API**: Threads API
- **Auth**: OAuth 2.0
- **Endpoint**: `POST /v1.0/{user_id}/threads`
- **Thread**: Native threading support
- **Limits**: 500 chars, 10 media

### Facebook
- **API**: Graph API
- **Auth**: OAuth 2.0
- **Endpoint**: `POST /{user_id}/feed`
- **Thread**: Not supported
- **Limits**: No strict char limit, 10 media

---

## Error Scenarios

### No Authenticated Account

```csharp
if (!accountsByPlatform.TryGetValue(platform, out var account))
{
    results[platform] = new PublishResult
    {
        Success = false,
        ErrorMessage = $"No authenticated account found for {platform}"
    };
}
```

### API Error

```csharp
catch (HttpRequestException ex)
{
    return new PublishResult
    {
        Success = false,
        ErrorMessage = $"Network error: {ex.Message}"
    };
}
```

### Validation Error

```csharp
var validationResult = await ValidateContentAsync(post);
if (!validationResult.IsValid)
{
    results[platform] = new PublishResult
    {
        Success = false,
        ErrorMessage = string.Join("; ", validationResult.Errors)
    };
}
```

---

## Testing

### Unit Tests
```csharp
// Test PostService with mocked platform services
var mockBlueSky = new Mock<IBlueSkyService>();
mockBlueSky.Setup(s => s.PostAsync(It.IsAny<Post>(), It.IsAny<Account>()))
    .ReturnsAsync(new PublishResult { Success = true });

var postService = new PostService(
    mockBlueSky.Object,
    /* ... other services */
);
```

### Integration Tests
```csharp
// Test with real platform services but test accounts
var postService = serviceProvider.GetRequiredService<IPostService>();
var result = await postService.PublishPostAsync(testPost);

Assert.True(result[SocialPlatform.BlueSky].Success);
```

---

## Migration from MockPostService

### Old (Mock)
```csharp
services.AddScoped<IPostService, MockPostService>();

// MockPostService just simulated posting:
// - Random success/failure
// - Fake post IDs
// - No actual API calls
```

### New (Real)
```csharp
services.AddScoped<IPostService, PostService>();

// PostService actually posts:
// - Calls real platform services
// - Makes actual HTTP requests
// - Returns real post IDs
// - Real error handling
```

---

## Performance Considerations

### Parallel Posting
Consider posting to multiple platforms in parallel:

```csharp
var tasks = platformList.Select(async platform =>
{
    var service = GetPlatformService(platform);
    var account = accountsByPlatform[platform];
    return await service.PostAsync(post, account);
});

var results = await Task.WhenAll(tasks);
```

### Retries
Add retry logic for transient failures:

```csharp
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

var result = await retryPolicy.ExecuteAsync(() => 
    platformService.PostAsync(post, account));
```

---

## Debugging

### Enable Logging

```csharp
public async Task<PublishResult> PostAsync(Post post, Account account)
{
    _logger.LogInformation("Posting to {Platform} for account {Username}", 
        Platform, account.Username);
    
    var result = await agent.Post(postText);
    
    _logger.LogInformation("Post result: {Success}, ID: {PostId}", 
        result.Succeeded, result.Result?.Uri);
    
    return publishResult;
}
```

### Check Logs

```
%LOCALAPPDATA%\SocialMediaCommander\Logs\app-*.log
```

---

**Architecture**: ? **UPDATED AND WORKING**  
**Real Posting**: ? **ENABLED**  
**Mock Service**: ? **DISABLED** (kept for reference)

---

For bug fix details, see: `docs/bugfixes/bluesky-posting-not-working-fix.md`
