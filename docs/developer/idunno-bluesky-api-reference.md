# idunno.Bluesky API Quick Reference

## ? Quick API Patterns

### Authentication

```csharp
using var agent = new BlueskyAgent();

// Login with username + password (or app password)
var loginResult = await agent.Login(username, password);

if (loginResult.Succeeded)
{
    // Authenticated!
}
else
{
    // Check loginResult.AtErrorDetail for error info
    var error = loginResult.AtErrorDetail?.Error;
    var message = loginResult.AtErrorDetail?.Message;
}
```

### Creating Posts

```csharp
// Simple text post
var result = await agent.Post("Hello World");

// Post with image
byte[] imageBytes = File.ReadAllBytes(imagePath);
var imageUpload = await agent.UploadImage(
    imageBytes,
    "image/jpeg",
    "Alt text",
    new AspectRatio(1000, 1000));

if (imageUpload.Succeeded)
{
    var postResult = await agent.Post("Check out this image!", imageUpload.Result);
}

// Post with multiple images
var post = await agent.Post("Multiple images", 
    [image1.Result, image2.Result, image3.Result]);
```

### Threads and Replies

```csharp
// Create a post
var mainPost = await agent.Post("Main post");

// Reply to it
var reply1 = await agent.ReplyTo(
    mainPost.Result.StrongReference,  // Important: Use StrongReference
    "First reply");

// Reply to the reply (thread)
var reply2 = await agent.ReplyTo(
    reply1.Result.StrongReference,
    "Second reply");
```

### Deleting Posts

```csharp
// Delete using StrongReference
await agent.DeletePost(postResult.Result.StrongReference);

// Or using URI
await agent.DeletePost(postResult.Result.Uri);
```

### Social Actions

```csharp
// Like a post
var likeResult = await agent.Like(postRef.StrongReference);

// Repost
var repostResult = await agent.Repost(postRef.StrongReference);

// Quote post
var quoteResult = await agent.Quote(
    postRef.StrongReference,
    "My commentary on this post");

// Delete like
await agent.DeleteLike(likeResult.Result.StrongReference);

// Delete repost
await agent.DeleteRepost(repostResult.Result.StrongReference);
```

### Getting Feeds

```csharp
// Get user's timeline
var timeline = await agent.GetTimeline(limit: 50);

if (timeline.Succeeded)
{
    foreach (var feedItem in timeline.Result)  // Result is enumerable
    {
        var post = feedItem.Post;
        var text = post.Record.Text;
        var author = post.Author.Handle;
        var likes = post.LikeCount ?? 0;
    }
}

// Get specific user's posts
var userFeed = await agent.GetAuthorFeed("username.bsky.social", limit: 20);

foreach (var feedItem in userFeed.Result)
{
    // Process posts
}
```

### Search

```csharp
var searchResults = await agent.SearchPosts("dotnet", limit: 20);

if (searchResults.Succeeded)
{
    foreach (var post in searchResults.Result)
    {
        var text = post.Record.Text;
        var author = post.Author.DisplayName;
    }
}
```

### Profiles

```csharp
// Get a user's profile
var profile = await agent.GetProfile("username.bsky.social");

if (profile.Succeeded)
{
    var p = profile.Result;
    var displayName = p.DisplayName;
    var bio = p.Description;
    var followers = p.FollowersCount ?? 0;
    var following = p.FollowsCount ?? 0;
    var posts = p.PostsCount ?? 0;
}
```

### Following/Unfollowing

```csharp
// Follow a user
var followResult = await agent.Follow(did);

// Unfollow
await agent.Unfollow(followResult.Result.Uri);
```

### Rich Text (Facets)

```csharp
using idunno.Bluesky.RichText;

var builder = new PostBuilder("Check out ");

// Add a mention
var did = await agent.ResolveHandle("username.bsky.social");
builder.Append(new Mention(did, "@username.bsky.social"));

// Add a link
builder.Append(" and visit ");
builder.Append(new Link("https://example.com"));

// Add a hashtag
builder.Append(" ");
builder.Append(new HashTag("dotnet"));

// Post it
var result = await agent.Post(builder);
```

## ?? Key Types

### AtProtoHttpResult<T>

All API calls return `AtProtoHttpResult<T>`:

```csharp
public class AtProtoHttpResult<T>
{
    public bool Succeeded { get; }
    public T? Result { get; }
    public HttpStatusCode StatusCode { get; }
    public AtErrorDetail? AtErrorDetail { get; }
}
```

Usage pattern:

```csharp
var result = await agent.SomeMethod();

if (result.Succeeded)
{
    // Use result.Result
    var data = result.Result;
}
else
{
    // Handle error
    var errorMessage = result.AtErrorDetail?.Message;
    var statusCode = result.StatusCode;
}
```

### StrongReference

Used to reference specific posts/records:

```csharp
public class StrongReference
{
    public AtUri Uri { get; }
    public Cid Cid { get; }
}
```

Get from create operations:

```csharp
var postResult = await agent.Post("Hello");
var strongRef = postResult.Result.StrongReference;

// Use for replies, likes, etc.
await agent.ReplyTo(strongRef, "Reply");
await agent.Like(strongRef);
```

### CreateRecordResult

Returned from create operations:

```csharp
public class CreateRecordResult
{
    public AtUri Uri { get; }
    public Cid Cid { get; }
    public StrongReference StrongReference { get; }
}
```

## ?? Common Mistakes

### ? Wrong: Using Error property
```csharp
if (!result.Succeeded)
{
    var error = result.Error;  // ? Doesn't exist
}
```

### ? Correct: Using AtErrorDetail
```csharp
if (!result.Succeeded)
{
    var error = result.AtErrorDetail?.Message;  // ? Correct
}
```

### ? Wrong: Accessing Feed/Posts directly
```csharp
var timeline = await agent.GetTimeline();
var posts = timeline.Result.Feed;  // ? Doesn't exist
```

### ? Correct: Result is directly enumerable
```csharp
var timeline = await agent.GetTimeline();
foreach (var item in timeline.Result)  // ? Correct
{
    // Process items
}
```

### ? Wrong: Session resumption
```csharp
await agent.ResumeSession(token, refreshToken);  // ? Doesn't exist
```

### ? Correct: Just login again
```csharp
await agent.Login(username, password);  // ? Sessions managed automatically
```

## ?? Namespaces

```csharp
using idunno.Bluesky;              // BlueskyAgent
using idunno.Bluesky.Embed;        // EmbeddedImage, AspectRatio
using idunno.Bluesky.RichText;     // PostBuilder, Mention, Link, HashTag
using idunno.AtProto;              // AtUri, StrongReference
using idunno.AtProto.Repo;         // CreateRecordResult, Commit
```

## ?? Important Links

- **Documentation**: https://bluesky.idunno.dev/
- **API Status**: https://bluesky.idunno.dev/docs/endpointStatus.html
- **GitHub**: https://github.com/blowdart/idunno.Bluesky
- **NuGet**: https://www.nuget.org/packages/idunno.Bluesky/
- **Samples**: https://github.com/blowdart/idunno.Bluesky/tree/main/samples

## ?? Best Practices

1. **Use `using` for agents** - They're disposable
   ```csharp
   using var agent = new BlueskyAgent();
   ```

2. **Check `Succeeded` before accessing `Result`**
   ```csharp
   if (result.Succeeded && result.Result != null)
   {
       // Safe to use result.Result
   }
   ```

3. **Use StrongReference for operations**
   ```csharp
   var strongRef = createResult.Result.StrongReference;
   await agent.Like(strongRef);  // Not just the URI
   ```

4. **Handle nullables properly**
   ```csharp
   var likes = post.LikeCount ?? 0;  // Counts can be null
   ```

5. **Use cancellation tokens**
   ```csharp
   await agent.Post("Hello", cancellationToken: cancellationToken);
   ```

## ?? Complete Example

```csharp
using idunno.Bluesky;
using idunno.Bluesky.Embed;

public async Task<bool> PostToBluesky(string username, string appPassword, string text, string? imagePath = null)
{
    using var agent = new BlueskyAgent();
    
    // Authenticate
    var loginResult = await agent.Login(username, appPassword);
    if (!loginResult.Succeeded)
    {
        Console.WriteLine($"Login failed: {loginResult.AtErrorDetail?.Message}");
        return false;
    }
    
    // Upload image if provided
    EmbeddedImage? image = null;
    if (!string.IsNullOrEmpty(imagePath))
    {
        var imageBytes = await File.ReadAllBytesAsync(imagePath);
        var uploadResult = await agent.UploadImage(
            imageBytes,
            "image/jpeg",
            "Photo",
            new AspectRatio(1000, 1000));
            
        if (uploadResult.Succeeded)
        {
            image = uploadResult.Result;
        }
    }
    
    // Create post
    var postResult = image != null
        ? await agent.Post(text, image)
        : await agent.Post(text);
        
    if (postResult.Succeeded)
    {
        Console.WriteLine($"Posted: {postResult.Result.Uri}");
        return true;
    }
    
    Console.WriteLine($"Post failed: {postResult.AtErrorDetail?.Message}");
    return false;
}
```
