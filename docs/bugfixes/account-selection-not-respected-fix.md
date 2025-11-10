# Account Selection Not Respected When Posting - Fix

## Issue
When user selects a specific account from the dropdown in the Post Editor (e.g., switching from `@sinclairinat0r.com` to `@sinclarinat0rtest`), the post was still published to the first account instead of the selected account.

## Root Cause
The `PostService.PublishPostToPlatformsAsync()` method was **completely ignoring** the `post.SelectedAccounts` dictionary that was carefully populated by the UI. Instead, it was using this flawed logic:

```csharp
// BEFORE (WRONG):
var accountsByPlatform = authenticatedAccounts
    .GroupBy(a => a.PlatformId)
    .ToDictionary(g => g.Key, g => g.FirstOrDefault()); // ? Always takes first account!
```

This meant that no matter what the user selected in the UI, the system would always use the first authenticated account for each platform.

## Solution
Modified `PostService.PublishPostToPlatformsAsync()` to:
1. **Check `post.SelectedAccounts` first** for the user's selected account ID
2. **Look up the account by ID** from the authenticated accounts list
3. **Fall back to first account** only if the selected account is not found
4. **Add detailed logging** to show which account is being used

### Code Changes

**File**: `SocialMediaCommander.Services\Implementation\PostService.cs`

**Before** (lines 114-162):
```csharp
var accountsByPlatform = authenticatedAccounts
    .GroupBy(a => a.PlatformId)
    .ToDictionary(g => g.Key, g => g.FirstOrDefault());

foreach (var platform in platformList)
{
    // ...
    if (!accountsByPlatform.TryGetValue(platform, out var account) || account == null)
    {
        // No account found
        continue;
    }
    
    System.Diagnostics.Debug.WriteLine($"[PostService] Using account: {account.Username} for {platform}");
    // ... post to platform
}
```

**After** (fixed logic):
```csharp
foreach (var platform in platformList)
{
    // ...
    
    // Get the selected account ID for this platform from the post
    Account? account = null;
    if (post.SelectedAccounts.TryGetValue(platform, out var selectedAccountIds) && selectedAccountIds.Any())
    {
        var selectedAccountId = selectedAccountIds.First();
        System.Diagnostics.Debug.WriteLine($"[PostService] Looking for selected account ID: {selectedAccountId}");
        
        account = authenticatedAccounts.FirstOrDefault(a => a.Id == selectedAccountId && a.PlatformId == platform);
        
        if (account != null)
        {
            System.Diagnostics.Debug.WriteLine($"[PostService] Found selected account: {account.Username} (ID: {account.Id})");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[PostService] Selected account ID {selectedAccountId} not found or not authenticated, falling back to default");
        }
    }
    
    // Fallback to first authenticated account for this platform if no account was selected or found
    if (account == null)
    {
        account = authenticatedAccounts.FirstOrDefault(a => a.PlatformId == platform);
        if (account != null)
        {
            System.Diagnostics.Debug.WriteLine($"[PostService] Using fallback account: {account.Username} (ID: {account.Id})");
        }
    }

    if (account == null)
    {
        // No account found error
        continue;
    }

    System.Diagnostics.Debug.WriteLine($"[PostService] Using account: {account.Username} (ID: {account.Id}) for {platform}");
    // ... post to platform
}
```

## Key Improvements

### 1. **Respects User Selection**
The `post.SelectedAccounts` dictionary is now the **primary source of truth** for determining which account to use.

### 2. **Account Lookup by ID**
Instead of blindly taking the first account, the system now:
- Extracts the selected account ID from `post.SelectedAccounts[platform]`
- Searches the authenticated accounts list for an account with that exact ID
- Ensures the account is authenticated before using it

### 3. **Graceful Fallback**
If the selected account is not found (e.g., was deleted or is no longer authenticated):
- Falls back to first authenticated account for the platform
- Logs the fallback behavior for debugging

### 4. **Enhanced Logging**
Added comprehensive debug logging:
```
[PostService] Looking for selected account ID: account-xyz-123
[PostService] Found selected account: sinclarinat0rtest (ID: account-xyz-123)
[PostService] Using account: sinclarinat0rtest (ID: account-xyz-123) for BlueSky
```

This makes it crystal clear which account is being used and why.

## Testing

### Test Scenario 1: Single Account
**Given**: User has 1 BlueSky account (`@sinclairinat0r.com`)
**When**: User creates a post
**Then**: Post is published to `@sinclairinat0r.com`

### Test Scenario 2: Multiple Accounts, Default Selected
**Given**: User has 2 BlueSky accounts (default: `@sinclairinat0r.com`, other: `@sinclarinat0rtest`)
**When**: User creates a post without changing account selection
**Then**: Post is published to `@sinclairinat0r.com` (the default)

### Test Scenario 3: Multiple Accounts, Non-Default Selected ? **FIXED**
**Given**: User has 2 BlueSky accounts (default: `@sinclairinat0r.com`, other: `@sinclarinat0rtest`)
**When**: User selects `@sinclarinat0rtest` from dropdown and creates a post
**Then**: Post is published to `@sinclarinat0rtest` ? **This now works!**

### Test Scenario 4: Selected Account No Longer Authenticated
**Given**: User has selected account B, but account B logs out
**When**: User creates a post
**Then**: System falls back to first authenticated account and logs the fallback

## Debug Output Examples

### Before Fix (Wrong Account Used)
```
[PostEditorViewModel] Selected account for BlueSky: sinclarinat0rtest
[PostService] Using account: sinclairinat0r.com for BlueSky  ? WRONG!
```

### After Fix (Correct Account Used)
```
[PostEditorViewModel] Selected account for BlueSky: sinclarinat0rtest
[PostService] Looking for selected account ID: account-xyz-123
[PostService] Found selected account: sinclarinat0rtest (ID: account-xyz-123)
[PostService] Using account: sinclarinat0rtest (ID: account-xyz-123) for BlueSky  ? CORRECT!
```

## Impact

### Critical Bug Fixed
This was a **critical bug** that completely broke the multi-account selection feature. Users could select an account in the UI, but their posts would always go to the wrong account.

### User Trust
This fix is essential for user trust - users need to know that when they select an account, their post will actually be published to that account, not a different one.

### Multi-Account Workflows
This fix enables proper multi-account workflows where users can:
- Manage multiple accounts for the same platform
- Switch between accounts easily
- Post to specific accounts intentionally
- Trust that their selection is respected

## Related Features
- **Multi-Account Selection UI** - `docs/features/multi-account-posting-selection.md`
- **Account Management** - `docs/user-guide/adding-accounts-oauth.md`
- **Post Publishing Flow** - `docs/architecture/post-publishing-flow.md`

## Build Status
? **Build successful**: `dotnet build SocialMediaCommander.sln` completed without errors

## Date
2025-01-15

## Author
GitHub Copilot (AI Assistant)
