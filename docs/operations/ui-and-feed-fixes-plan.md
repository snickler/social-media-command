# ?? UI and Feed Fixes - Action Plan

## Status: In Progress

---

## ? **GOOD NEWS: Posting Works!**

Your post **was successful**:
```
[PostService] Result for BlueSky: Success=True, Error=
[PostService] Publishing complete. Success count: 1/1
Post published successfully!
```

**Verify**: Check https://bsky.app/ - your post should be there!

---

## ?? **Issues to Fix**

### 1. CanPost Shows False After Success ?
**Symptom**: After posting, `CanPost: False` even though you could post again

**Cause**: Content is cleared but the property doesn't recalculate immediately

**Fix**: Force property notification after content clears

---

### 2. GridLength Binding Errors ?
**Error**:
```
[Binding]An error occurred binding 'Width' to 'PostEditorColumnWidth': 
'Could not convert '1*' (System.String) to 'Avalonia.Controls.GridLength'.'
```

**Cause**: Trying to bind string `"1*"` to `GridLength` type

**Fix**: Convert string to `GridLength` in ViewModel or use converter

---

### 3. Social Feeds Not Loading ?
**Symptom**: "No posts to show" even with connected BlueSky account

**Cause**: `SocialFeedViewModel.RefreshFeedsAsync()` not implemented

**Fix**: Implement feed loading with `BlueSkyService.GetTimelineAsync()`

---

### 4. Feed Tabs Show All Platforms ?
**Symptom**: Tabs for All, BlueSky, X, LinkedIn, Threads, Facebook - but only BlueSky is connected

**Cause**: Tabs are hardcoded, not filtered by connected accounts

**Fix**: Only show tabs for platforms with authenticated accounts

---

### 5. View Mode Buttons Not Working ?
**Symptom**: Clicking Standard/Compact View buttons doesn't switch views

**Cause**: Buttons visible but bindings broken

**Fix**: Wire up commands or simplify UI

---

## ?? **Implementation Priority**

### **Phase 1: Fix Feed Display** (HIGH)
Most important - user expects to see posts

1. Implement `SocialFeedViewModel.RefreshFeedsAsync()`
2. Call `BlueSkyService.GetTimelineAsync()`
3. Populate `FeedItems` collection
4. Bind to UI

### **Phase 2: Fix Feed Tabs** (MEDIUM)
Better UX - don't show tabs for platforms user doesn't have

1. Filter tabs by authenticated accounts
2. Show only connected platforms
3. Default to "All" or first connected platform

### **Phase 3: Fix GridLength Bindings** (LOW)
Annoying but not breaking functionality

1. Add `GridLengthConverter` or
2. Change ViewModel properties to `GridLength` type

### **Phase 4: Simplify View Mode UI** (OPTIONAL)
May not be needed

1. Review if Standard/Compact/Threads buttons are necessary
2. Consider single toggle or remove if not used

---

## ?? **Quick Wins First**

Let's start with the most impactful fixes:

1. **Feed Loading** - 20 minutes
2. **Feed Tabs Filtering** - 10 minutes  
3. **CanPost Reset** - 5 minutes

GridLength and View Mode can wait.

---

## ?? **Files to Modify**

1. `SocialMediaCommander.Desktop/ViewModels/SocialFeedViewModel.cs`
2. `SocialMediaCommander.Desktop/Views/SocialFeedView.axaml`
3. `SocialMediaCommander.Desktop/ViewModels/PostEditorViewModel.cs`
4. `SocialMediaCommander.Desktop/Views/MainWindow.axaml`

---

**Next**: Implement feed loading first!
