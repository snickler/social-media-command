# ?? Critical Fixes Needed - Complete Action Plan

## Status: **IN PROGRESS**

Date: January 2025

---

## ?? Critical Issues

### 1. Posts Not Publishing ?
**Symptom**: UI shows "Post published successfully!" but nothing posts to BlueSky

**Root Causes**:
1. ? **FIXED**: `MockPostService` was being used instead of real `PostService`
2. ? **FIXED**: `CreatePostFromViewModel()` wasn't populating `SelectedAccounts`
3. ?? **NEEDS VERIFICATION**: Accounts may not be properly authenticated
4. ?? **NEEDS VERIFICATION**: App password may not be stored/loaded correctly

**Solution Path**:
- [x] Replace `MockPostService` with real `PostService` 
- [x] Add diagnostic logging to `PostService`
- [x] Populate `SelectedAccounts` in `CreatePostFromViewModel()`
- [ ] Verify account storage and retrieval
- [ ] Test with real BlueSky account
- [ ] Add error toast notifications

---

### 2. No Feed Display ?
**Symptom**: "No posts to show" in Social Feeds tab

**Root Causes**:
1. `SocialFeedViewModel` may not be calling `BlueSkyService.GetTimelineAsync()`
2. No accounts connected to feed service
3. Feed refresh not triggered on app start

**Solution Path**:
- [ ] Implement `SocialFeedViewModel.RefreshFeedsAsync()`
- [ ] Wire up feed loading on tab activation
- [ ] Add loading indicator
- [ ] Show account selector in feed view

---

### 3. No Account Selection UI ?
**Symptom**: Users can't see which account they're posting from per platform

**Root Causes**:
1. `PostEditorView` doesn't show account selection
2. No UI component to select between multiple accounts per platform
3. Hardcoded "default" account selection

**Solution Path**:
- [ ] Add account selector dropdown per platform
- [ ] Show selected account avatar/name in UI
- [ ] Allow switching between multiple accounts per platform
- [ ] Save account selection preference

---

### 4. Silent Failures ?
**Symptom**: No error messages when posting fails

**Root Causes**:
1. Errors caught but not displayed to user
2. No toast notifications for errors
3. Logging only goes to debug output

**Solution Path**:
- [ ] Add toast notifications using Avalonia notifications
- [ ] Show error details in UI
- [ ] Add retry button for failed posts
- [ ] Better error messages

---

## ?? Diagnostic Steps

### Step 1: Run App with Logging
```bash
dotnet run --project SocialMediaCommander.Desktop
```

Look for these log messages:
```
[PostService] Publishing to X platforms
[PostService] Found X total accounts
[PostService] Found X authenticated accounts
[PostService]   - BlueSky: username (Authenticated: True/False)
[PostService] Processing platform: BlueSky
[PostService] Using account: username for BlueSky
[PostService] Calling platform service for BlueSky...
[PostService] Result for BlueSky: Success=True/False, Error=...
```

### Step 2: Verify Account Storage
Check if accounts are being saved:
```
%APPDATA%\SocialMediaCommander\Data\accounts.encrypted
```

If file exists, accounts are being saved. If posting fails, likely authentication issue.

### Step 3: Test Account Service Directly
Add temporary logging in `SecureAccountService`:
```csharp
public async Task<IEnumerable<Account>> GetAllAccountsAsync()
{
    var accounts = await LoadAccountsAsync();
    System.Diagnostics.Debug.WriteLine($"[SecureAccountService] Loaded {accounts.Count()} accounts");
    foreach (var acc in accounts)
    {
        System.Diagnostics.Debug.WriteLine($"  - {acc.PlatformId}: {acc.Username} (Auth: {acc.IsAuthenticated})");
    }
    return accounts;
}
```

---

## ??? Implementation Priority

### **Phase 1: Verify Posting Works** (HIGH PRIORITY)
1. ? Add logging to PostService - **DONE**
2. ? Fix account selection population - **DONE**
3. ? Run app and check logs - **IN PROGRESS**
4. ? Verify account is authenticated
5. ? Test actual post to BlueSky

**Success Criteria**: Post actually appears on BlueSky at https://bsky.app/

---

### **Phase 2: Add Error Notifications** (HIGH PRIORITY)
1. Install Avalonia notification package
2. Add toast service to DI
3. Show success/error toasts on post
4. Display detailed error messages

**Files to Modify**:
- `Directory.Packages.props` - Add notification package
- `ServiceCollectionExtensions.cs` - Register toast service
- `PostEditorViewModel.cs` - Show toasts

---

### **Phase 3: Account Selection UI** (MEDIUM PRIORITY)
1. Create `AccountSelectorControl.axaml`
2. Add account selector to `PostEditorView`
3. Bind to ViewModel property
4. Save selection preference

**Files to Create**:
- `SocialMediaCommander.Desktop/Controls/AccountSelectorControl.axaml`
- `SocialMediaCommander.Desktop/Controls/AccountSelectorControl.axaml.cs`

**Files to Modify**:
- `PostEditorViewModel.cs` - Add account selection properties
- `PostEditorView.axaml` - Add account selector UI

---

### **Phase 4: Feed Display** (MEDIUM PRIORITY)
1. Implement feed loading in `SocialFeedViewModel`
2. Wire up refresh button
3. Add loading indicator
4. Show feed items in UI

**Files to Modify**:
- `SocialFeedViewModel.cs` - Implement `RefreshFeedsAsync()`
- `SocialFeedView.axaml` - Add feed item template

---

## ?? Code Changes Needed

### 1. Add Toast Notifications

**Directory.Packages.props**:
```xml
<PackageVersion Include="Avalonia.Controls.Notifications" Version="11.2.2" />
```

**PostEditorViewModel.cs**:
```csharp
private readonly INotificationService _notificationService;

[RelayCommand]
private async Task PostAsync()
{
    try
    {
        var result = await _postService.PublishPostAsync(post);
        
        var successCount = result.Values.Count(r => r.Success);
        var failCount = result.Values.Count(r => !r.Success);
        
        if (successCount > 0 && failCount == 0)
        {
            _notificationService.Show("Success", 
                $"Posted to {successCount} platform(s)", 
                NotificationType.Success);
        }
        else if (successCount > 0)
        {
            _notificationService.Show("Partial Success", 
                $"Posted to {successCount}/{result.Count} platforms. {failCount} failed.", 
                NotificationType.Warning);
        }
        else
        {
            var errors = string.Join(", ", result.Values.Select(r => r.ErrorMessage));
            _notificationService.Show("Error", 
                $"Failed to post: {errors}", 
                NotificationType.Error);
        }
    }
    catch (Exception ex)
    {
        _notificationService.Show("Error", 
            $"Exception: {ex.Message}", 
            NotificationType.Error);
    }
}
```

### 2. Account Selector UI

**PostEditorView.axaml** (add after platform selector):
```xml
<!-- Account Selection per Platform -->
<ItemsControl ItemsSource="{Binding SelectedPlatforms}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal" Margin="0,5">
                <TextBlock Text="{Binding PlatformName}" Width="100" />
                <ComboBox ItemsSource="{Binding AvailableAccounts}"
                          SelectedItem="{Binding SelectedAccount}"
                          Width="200">
                    <ComboBox.ItemTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal">
                                <Ellipse Width="24" Height="24" 
                                         Fill="{Binding AvatarColor}" 
                                         Margin="0,0,8,0"/>
                                <TextBlock Text="{Binding DisplayName}"/>
                                <TextBlock Text="{Binding Username}" 
                                           Foreground="Gray" 
                                           Margin="5,0,0,0"/>
                            </StackPanel>
                        </DataTemplate>
                    </ComboBox.ItemTemplate>
                </ComboBox>
            </StackPanel>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

### 3. Feed Loading

**SocialFeedViewModel.cs**:
```csharp
[ObservableProperty]
private bool _isLoading = false;

[ObservableProperty]
private ObservableCollection<SocialFeedItem> _feedItems = new();

[RelayCommand]
private async Task RefreshFeedsAsync()
{
    if (IsLoading) return;
    
    try
    {
        IsLoading = true;
        FeedItems.Clear();
        
        var accounts = await _accountService.GetAllAccountsAsync();
        var blueSkyAccount = accounts.FirstOrDefault(a => 
            a.PlatformId == SocialPlatform.BlueSky && a.IsAuthenticated);
            
        if (blueSkyAccount != null)
        {
            var timeline = await _blueSkyService.GetTimelineAsync(blueSkyAccount, 50);
            foreach (var item in timeline)
            {
                FeedItems.Add(item);
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to refresh feeds");
    }
    finally
    {
        IsLoading = false;
    }
}
```

---

## ?? Testing Checklist

### Manual Testing Steps

**Test 1: Account Authentication**
- [ ] Open Account Manager
- [ ] Add BlueSky account with app password
- [ ] Verify account shows as "Connected"
- [ ] Check logs show account loaded

**Test 2: Posting**
- [ ] Write post content
- [ ] Select BlueSky platform
- [ ] Click "Post"
- [ ] Check debug logs for:
  - Account found
  - Authentication success
  - API call made
  - Response received
- [ ] Verify post appears on https://bsky.app/

**Test 3: Feed Display**
- [ ] Switch to Social Feeds tab
- [ ] Click Refresh
- [ ] Verify posts load
- [ ] Check for your own posts

**Test 4: Error Handling**
- [ ] Remove app password from account
- [ ] Try to post
- [ ] Verify error toast shows
- [ ] Verify error message is helpful

---

## ?? Current Status

| Component | Status | Notes |
|-----------|--------|-------|
| PostService | ? FIXED | Real service registered, logging added |
| Account Selection | ?? PARTIAL | Populates "default", needs UI |
| Error Notifications | ? MISSING | Need toast notifications |
| Feed Display | ? BROKEN | Not implemented |
| BlueSky API Integration | ? WORKING | Service corrected, builds successfully |

---

## ?? Next Immediate Steps

1. **RUN THE APP** with current changes
2. **CHECK DEBUG OUTPUT** for PostService logs
3. **TRY TO POST** to BlueSky
4. **VERIFY** post appears on BlueSky website
5. **COLLECT LOGS** to determine next fix

If posting still fails, the logs will tell us exactly why:
- No accounts found?
- Account not authenticated?
- API error?
- Network issue?

---

## ?? Related Documentation

- `docs/bugfixes/bluesky-posting-not-working-fix.md` - Initial fix documentation
- `docs/architecture/post-publishing-flow.md` - Architecture overview
- `docs/features/dual-authentication-bluesky.md` - Authentication implementation
- `docs/developer/idunno-bluesky-api-reference.md` - BlueSky API reference

---

**Last Updated**: January 2025  
**Status**: Awaiting test results with enhanced logging  
**Next Action**: Run app and analyze debug logs
