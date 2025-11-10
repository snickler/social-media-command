# ?? BlueSky Posting - Diagnostic Testing Guide

## Status: Ready for Testing

**Build**: ? Passing  
**Logging**: ? Enhanced  
**Next Step**: Run app and collect logs

---

## ?? What We Fixed

1. ? Replaced `MockPostService` with real `PostService`
2. ? Added comprehensive logging throughout the pipeline
3. ? Fixed `CreatePostFromViewModel()` to populate `SelectedAccounts`
4. ? Added logging to `SecureAccountService` for account discovery

---

## ?? Testing Procedure

### Step 1: Run the Application

```bash
cd C:\repos\social-media-command
dotnet run --project SocialMediaCommander.Desktop
```

### Step 2: Open Debug Output

In Visual Studio:
- **View** ? **Output** ? Select **Debug** from dropdown

Or run with logging to file:
```bash
dotnet run --project SocialMediaCommander.Desktop > test-log.txt 2>&1
```

### Step 3: Verify Account Loading

**Expected Log Messages on Startup**:
```
[SecureAccountService] LoadAccountsAsync - File path: C:\Users\...\AppData\Roaming\SocialMediaCommander\Data\accounts.encrypted
[SecureAccountService] File exists: True/False
[SecureAccountService] Successfully loaded X accounts
[SecureAccountService]   Loaded: BlueSky - your_username
```

**If No Accounts**:
```
[SecureAccountService] No accounts file found - initializing with empty collection
```

?? **Action**: Add a BlueSky account through Account Manager first

### Step 4: Add BlueSky Account (if needed)

1. Click **"Manage Accounts"** button (top right)
2. Find **BlueSky** platform
3. Click **"Connect Account"** or **"+"** button
4. Fill in:
   - **Username**: your-handle.bsky.social (or just your-handle)
   - **App Password**: (generate at https://bsky.app/settings/app-passwords)
5. Click **"Save"** or **"Connect"**

**Expected Logs**:
```
[SecureAccountService] Created new account {GUID} for platform BlueSky
[SecureAccountService] Saved X accounts to encrypted storage
```

### Step 5: Try to Post

1. Write some test content: **"Test post from Social Media Commander!"**
2. Ensure **BlueSky** platform is selected (should be blue/highlighted)
3. Click **"Post"** button

**Expected Logs** (CRITICAL - This shows what's happening):

```
PostAsync command called!
Starting post publishing...
[PostEditorViewModel] Created post with 1 platforms and 1 account selections

[PostService] Publishing to 1 platforms
[PostService] Found X total accounts
[PostService] Found Y authenticated accounts
[PostService]   - BlueSky: your-username (Authenticated: True)

[PostService] Processing platform: BlueSky
[PostService] Using account: your-username for BlueSky
[PostService] Calling platform service for BlueSky...

[PostService] Result for BlueSky: Success=True/False, Error=...
[PostService] Publishing complete. Success count: 1/1

Post published successfully! Results: 1 platforms
```

### Step 6: Interpret Results

#### ? **SUCCESS Case**
```
[PostService] Result for BlueSky: Success=True, Error=
```

?? **Verify**: Go to https://bsky.app/ and check if your post appears!

#### ? **FAILURE Case 1: No Accounts**
```
[PostService] Found 0 total accounts
[PostService] No authenticated account found for BlueSky
```

**Cause**: No BlueSky account added or account file not loading  
**Fix**: Add account through Account Manager, check file exists at:
```
%APPDATA%\SocialMediaCommander\Data\accounts.encrypted
```

#### ? **FAILURE Case 2: Account Not Authenticated**
```
[PostService] Found 1 total accounts
[PostService] Found 0 authenticated accounts
```

**Cause**: Account exists but `IsAuthenticated` is false  
**Possible reasons**:
- No app password set
- Tokens expired
- `AuthStatus` not set to `Authenticated`

**Fix**: 
1. Re-add the account with correct app password
2. Check account authentication status in Account Manager

#### ? **FAILURE Case 3: Authentication Failed**
```
[PostService] Using account: your-username for BlueSky
[PostService] Calling platform service for BlueSky...
[PostService] Result for BlueSky: Success=False, Error=App password is not configured
```

**Cause**: `account.AppPassword` is null or empty  
**Fix**: Verify app password is being saved correctly

#### ? **FAILURE Case 4: BlueSky API Error**
```
[PostService] Result for BlueSky: Success=False, Error=Login failed
```

**Cause**: App password is invalid or account credentials wrong  
**Fix**: 
1. Generate new app password at https://bsky.app/settings/app-passwords
2. Re-add account with new app password

---

## ?? Diagnostic Checklist

Use this to systematically identify the problem:

### Account Storage
- [ ] Accounts file exists at `%APPDATA%\SocialMediaCommander\Data\accounts.encrypted`
- [ ] Log shows "Successfully loaded X accounts"
- [ ] At least 1 account is BlueSky platform
- [ ] Account has username filled in

### Account Authentication
- [ ] Account has `AuthStatus = Authenticated`
- [ ] Account has `AppPassword` filled in (not null/empty)
- [ ] OR Account has `Tokens` with valid AccessToken
- [ ] `IsAuthenticated` property returns `true`

### Posting Flow
- [ ] PostEditorViewModel creates Post with TargetPlatforms
- [ ] Post.SelectedAccounts is populated with "default"
- [ ] PostService finds >= 1 total accounts
- [ ] PostService finds >= 1 authenticated accounts
- [ ] PostService matches account to selected platform
- [ ] BlueSkyService receives account and post
- [ ] BlueSkyService creates authenticated agent
- [ ] BlueSkyService calls `agent.Post()`
- [ ] Response returns Success=true

---

## ?? Common Issues & Solutions

### Issue: "No posts to show" in Social Feeds

**Cause**: Feed loading not implemented yet  
**Status**: Known issue, will fix in Phase 4  
**Workaround**: Check posts directly on BlueSky website

### Issue: Multiple clicks needed to post

**Cause**: UI state management issue  
**Temporary**: Just click Post button once and wait  
**Fix**: Will add loading indicator

### Issue: No success/error messages

**Cause**: Toast notifications not implemented  
**Status**: Will add in Phase 2  
**Workaround**: Check Debug Output window for results

---

## ?? Log Collection

If posting fails, collect these logs:

### 1. Startup Logs
```
[SecureAccountService] LoadAccountsAsync...
[SecureAccountService] Successfully loaded X accounts
```

### 2. Account Details
```
[SecureAccountService] GetAllAccountsAsync...
[SecureAccountService]   - BlueSky: username (IsAuthenticated: ...)
```

### 3. Posting Attempt
```
[PostService] Publishing to...
[PostService] Found X accounts...
[PostService] Result for BlueSky: ...
```

### 4. Any Errors
```
[PostService] ERROR: ...
[SecureAccountService] ERROR: ...
```

**Send these logs** for further diagnosis if posting still fails.

---

## ? Success Criteria

Posting works if:

1. ? Logs show account loaded and authenticated
2. ? Logs show "Result for BlueSky: Success=True"
3. ? Post appears on https://bsky.app/ within 1-2 seconds
4. ? UI shows "Post published successfully!"
5. ? Content clears from editor after posting

---

## ?? Next Steps After Testing

### If Posting Works ?
- [ ] Implement feed display (Phase 4)
- [ ] Add toast notifications (Phase 2)
- [ ] Add account selector UI (Phase 3)
- [ ] Test with multiple accounts
- [ ] Test thread posting

### If Posting Fails ?
- [ ] Collect all logs listed above
- [ ] Check each item in diagnostic checklist
- [ ] Identify which step fails
- [ ] Apply specific fix from "Common Issues"
- [ ] Re-test

---

## ?? Support Information

**Test Date**: January 2025  
**Build Version**: Latest from main branch  
**Platform**: Windows, .NET 9  

**Related Documentation**:
- `docs/operations/critical-fixes-action-plan.md` - Full action plan
- `docs/bugfixes/bluesky-posting-not-working-fix.md` - Initial fix details
- `docs/features/dual-authentication-bluesky.md` - Authentication implementation

---

## ?? Quick Test Script

For rapid testing, follow these steps in order:

```
1. Run: dotnet run --project SocialMediaCommander.Desktop
2. Wait for startup logs
3. Check: Account loaded? (Look for [SecureAccountService] logs)
4. If no account: Add BlueSky account via "Manage Accounts"
5. Write: "Test post"
6. Select: BlueSky platform
7. Click: "Post" button
8. Wait: 2-3 seconds
9. Check logs for: [PostService] Result for BlueSky: Success=True
10. Verify: Check https://bsky.app/ for your post
```

**Expected Duration**: 2-5 minutes for complete test

---

**Status**: ? Ready for Testing  
**Last Updated**: January 2025  
**Tester**: Run the app now and report results! ??
