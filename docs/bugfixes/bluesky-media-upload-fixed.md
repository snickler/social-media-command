# BlueSky Media Upload Fix

## Issue
Media attachments were not being included in BlueSky posts. The post would succeed, but images were not attached.

## Root Causes

### Primary Issue (Fixed in BlueSkyService.cs)
The implementation was incorrectly wrapping the collection of `EmbeddedImage` objects in an `ImageEmbed` wrapper before passing to `agent.Post()`. This was not the correct pattern for the idunno.Bluesky library.

### Secondary Issue (Fixed by creating MediaService.cs)
The application was using `MockMediaService` which set `FilePath` to `mock://uploads/{fileName}` instead of preserving actual file paths. This caused `File.Exists()` checks to fail and prevented file uploads.

**Solution**: Created a real `MediaService` implementation and registered it in DI, replacing the mock service.

## Investigation
Reviewed the official idunno.Bluesky repository samples:
- https://github.com/blowdart/idunno.Bluesky/blob/main/samples/Samples.Posting/Program.cs
- https://github.com/blowdart/idunno.Bluesky/blob/main/docs/docs/posting.md

The correct pattern is:
1. Upload each image using `agent.UploadImage()` to get an `EmbeddedImage` result
2. Pass the `EmbeddedImage` or collection directly to `agent.Post()`
   - Single image: `agent.Post(text, embeddedImage, cancellationToken)`
   - Multiple images: `agent.Post(text, ICollection<EmbeddedImage>, cancellationToken)`

## Solution

### Fix 1: BlueSkyService.PostAsync()
1. Upload each media file and collect `EmbeddedImage` results in a `List<EmbeddedImage>`
2. Pass the collection directly to `agent.Post()` without wrapping
3. Handle single vs multiple images appropriately (idunno.Bluesky has different overloads)

### Fix 2: Create Real MediaService Implementation
1. Created `SocialMediaCommander.Services\Implementation\MediaService.cs`
2. Preserves actual file paths in `Media.FilePath` property
3. Allows BlueSky service (and other platform services) to read files from their real locations
4. Registered in DI at `ServiceCollectionExtensions.cs`, replacing `MockMediaService`

## Code Changes

### File 1: `SocialMediaCommander.Services\Implementation\BlueSkyService.cs`

**Before** (incorrect):
```csharp
// Create ImageEmbed wrapper
var response = await agent.Post(postText, embed: new ImageEmbed(embeddedImages)).ConfigureAwait(false);
```

**After** (correct):
```csharp
// Pass collection directly
AtProtoHttpResult<CreateRecordResult> response;
if (embeddedImages.Count == 1)
{
    response = await agent.Post(postText, embeddedImages[0]).ConfigureAwait(false);
}
else
{
    response = await agent.Post(postText, embeddedImages).ConfigureAwait(false);
}
```

### File 2: `SocialMediaCommander.Services\Implementation\MediaService.cs` (NEW FILE)

**Key implementation:**
```csharp
public async Task<Media> UploadMediaAsync(string filePath)
{
    // Validate file exists
    if (!File.Exists(filePath))
    {
        throw new FileNotFoundException($"Media file not found: {filePath}", filePath);
    }

    var fileInfo = new FileInfo(filePath);
    var fileName = fileInfo.Name;
    var extension = fileInfo.Extension;
    var mimeType = GetMimeTypeFromExtension(extension);

    // Create media object with ACTUAL file path preserved
    var media = new Media
    {
        Id = Guid.NewGuid().ToString(),
        FileName = fileName,
        MimeType = mimeType,
        FileSize = fileInfo.Length,
        Type = GetMediaTypeFromExtension(extension),
        FilePath = filePath, // ? Preserve actual file path for platform services to read
        PreviewUrl = null,
        CreatedAt = DateTime.UtcNow
    };

    _mediaCache[media.Id] = media;
    return await Task.FromResult(media).ConfigureAwait(false);
}
```

### File 3: `SocialMediaCommander.Desktop\ServiceCollectionExtensions.cs`

**Before:**
```csharp
services.AddScoped<IMediaService, MockMediaService>();
```

**After:**
```csharp
services.AddScoped<IMediaService, MediaService>(); // Use real MediaService
```

## Testing
1. Build verification: `dotnet build SocialMediaCommander.sln` - ? Success
2. Manual testing required: 
   - Create a post with 1-4 images
   - Verify images appear in BlueSky post
   - Verify diagnostic logs show correct file paths (not mock:// URLs)

## Expected Diagnostic Output

**Before fix:**
```
[BlueSkyService] Uploading media: OIG2.jpg from mock://uploads/OIG2.jpg
[BlueSkyService] ERROR: File not found at mock://uploads/OIG2.jpg
[BlueSkyService] No media uploaded successfully, posting text only
```

**After fix:**
```
[BlueSkyService] Uploading media: OIG2.jpg from C:\Users\jerem\Downloads\OIG2.jpg
[BlueSkyService] Read 1234567 bytes from C:\Users\jerem\Downloads\OIG2.jpg
[BlueSkyService] Successfully uploaded media: OIG2.jpg
[BlueSkyService] Posting with 1 embedded image(s)
[BlueSkyService] Post with media succeeded! URI: at://did:plc:.../app.bsky.feed.post/...
```

## Key Learnings
- Always reference official SDK samples and documentation when implementing third-party library integrations
- The idunno.Bluesky library provides convenience overloads for common scenarios (single image vs multiple images)
- Media upload is a two-step process: upload blob first, then reference in post
- Mock services should not be used in production - create proper implementations with real file system access
- Production services should preserve actual file paths when they need to be accessible by other services
- Use DI properly to separate concerns - media management should be in Services layer, not Desktop layer
- Use diagnostic logging extensively to track data flow through the application

## Architecture Improvement
This fix demonstrates proper layered architecture:
- **Services Layer** (`SocialMediaCommander.Services`) - Contains `MediaService` for production use
- **Desktop Layer** (`SocialMediaCommander.Desktop`) - Contains `MockMediaService` only for UI testing/prototyping
- **Dependency Injection** - Properly wires up the correct implementation based on environment

The `MediaService` can now be reused across all platform services (BlueSky, X, LinkedIn, etc.) without modification.

## Related Files
- `SocialMediaCommander.Services\Implementation\MediaService.cs` - New production media service (NEW)
- `SocialMediaCommander.Services\Implementation\BlueSkyService.cs` - Platform service implementation
- `SocialMediaCommander.Desktop\Services\MockMediaService.cs` - Mock service (kept for testing)
- `SocialMediaCommander.Desktop\ServiceCollectionExtensions.cs` - DI registration
- `docs/developer/idunno-bluesky-api-reference.md` - API reference documentation
- `docs/features/idunno-bluesky-integration.md` - Integration overview

## Date
2025-01-15

## Author
GitHub Copilot (AI Assistant)
