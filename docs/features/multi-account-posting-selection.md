# Multi-Account Selection for Posting

## Overview
When multiple accounts exist for the same platform, users can now select which account to post from directly in the Post Editor. This ensures transparency and control over which account is used for publishing.

## Problem Statement
Previously, when a user had multiple BlueSky accounts (e.g., `@sinclairinat0r.com` and `@sinclarinat0rtest`), the system would automatically use the "default" account without giving the user visibility or control. This created confusion about which account was actually posting.

## Solution
Added account selection UI to the Post Editor that:
1. **Displays all available accounts** for each selected platform
2. **Auto-selects the default account** (or first authenticated account)
3. **Allows users to switch accounts** via dropdown
4. **Shows authentication status** with visual indicators
5. **Passes selected account ID** to the posting service

## Implementation Details

### Data Model Changes

#### `PlatformViewModel` (PostEditorViewModel.cs)
Added properties for account selection:
```csharp
[ObservableProperty]
private ObservableCollection<AccountSelectionItem> _availableAccounts = new();

[ObservableProperty]
private AccountSelectionItem? _selectedAccount;

public bool HasMultipleAccounts => AvailableAccounts.Count > 1;
public bool HasAccounts => AvailableAccounts.Any();
public string AccountDisplayText => SelectedAccount != null 
    ? $"@{SelectedAccount.Username}" 
    : "No account selected";
```

#### `AccountSelectionItem` (New Class)
Represents an account that can be selected:
```csharp
public partial class AccountSelectionItem : ObservableObject
{
    [ObservableProperty] private string _id = string.Empty;
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private bool _isDefault = false;
    [ObservableProperty] private bool _isAuthenticated = false;

    public string DisplayText => !string.IsNullOrEmpty(DisplayName) 
        ? $"{DisplayName} (@{Username}){(IsDefault ? " • Default" : "")}" 
        : $"@{Username}{(IsDefault ? " • Default" : "")}";
}
```

### ViewModel Logic

#### Account Loading (`LoadAccountsForAllPlatformsAsync`)
Called during ViewModel initialization:
```csharp
private async Task LoadAccountsForAllPlatformsAsync()
{
    var allAccounts = await _accountService.GetAllAccountsAsync().ConfigureAwait(false);
    
    foreach (var platformVM in AvailablePlatforms)
    {
        var platformAccounts = allAccounts
            .Where(a => a.PlatformId == platformVM.Platform)
            .ToList();

        // Populate available accounts
        platformVM.AvailableAccounts.Clear();
        foreach (var account in platformAccounts)
        {
            platformVM.AvailableAccounts.Add(new AccountSelectionItem
            {
                Id = account.Id,
                Username = account.Username,
                DisplayName = account.DisplayName,
                IsDefault = account.IsDefault,
                IsAuthenticated = account.IsAuthenticated
            });
        }

        // Auto-select default or first authenticated account
        platformVM.SelectedAccount = platformVM.AvailableAccounts.FirstOrDefault(a => a.IsDefault)
                                   ?? platformVM.AvailableAccounts.FirstOrDefault(a => a.IsAuthenticated)
                                   ?? platformVM.AvailableAccounts.First();
    }
}
```

#### Post Creation (`CreatePostFromViewModel`)
Uses the selected account ID:
```csharp
foreach (var platform in this.SelectedPlatforms)
{
    var platformVM = AvailablePlatforms.FirstOrDefault(p => p.Platform == platform);
    if (platformVM?.SelectedAccount != null)
    {
        post.SelectedAccounts[platform] = new List<string> { platformVM.SelectedAccount.Id };
    }
}
```

### UI Implementation (PostEditorView.axaml)

Added account selector under each platform toggle:
```xml
<StackPanel Spacing="4" Margin="0,0,12,12">
    <!-- Platform Toggle -->
    <ToggleButton Classes="PlatformToggle" 
                  IsChecked="{Binding IsSelected, Mode=TwoWay}">
        <StackPanel Orientation="Horizontal" Spacing="8">
            <TextBlock Text="?" IsVisible="{Binding IsSelected}"/>
            <TextBlock Text="{Binding Name}"/>
        </StackPanel>
    </ToggleButton>
    
    <!-- Account Selector (shown when platform is selected) -->
    <ComboBox IsVisible="{Binding IsSelected}"
              ItemsSource="{Binding AvailableAccounts}"
              SelectedItem="{Binding SelectedAccount, Mode=TwoWay}"
              Width="200">
        <ComboBox.ItemTemplate>
            <DataTemplate>
                <StackPanel Orientation="Horizontal" Spacing="6">
                    <TextBlock Text="??"/>
                    <TextBlock Text="{Binding DisplayText}"/>
                    <TextBlock Text="?" 
                               IsVisible="{Binding IsAuthenticated}"
                               Foreground="Green"/>
                </StackPanel>
            </DataTemplate>
        </ComboBox.ItemTemplate>
    </ComboBox>
    
    <!-- Account display text -->
    <TextBlock Text="{Binding AccountDisplayText}"
               IsVisible="{Binding IsSelected}"
               FontSize="10"
               Opacity="0.7"/>
</StackPanel>
```

## User Experience

### Visual Indicators
1. **Account Icon** (??) - Shows for each account in dropdown
2. **Authentication Check** (?) - Green checkmark for authenticated accounts
3. **Default Badge** (• Default) - Shows which account is set as default
4. **Username Display** - Shows `@username` below the platform toggle

### Account Selection Flow
1. User clicks on a platform toggle (e.g., BlueSky)
2. Platform is selected AND account dropdown appears
3. Dropdown shows all BlueSky accounts with their display names
4. Default account is pre-selected
5. User can click dropdown to switch to a different account
6. Selected account is clearly indicated below the platform button

### Example UI State
```
[? BlueSky (300 chars)]
????????????????????????????????????
? ?? Jeremy Sinclair (@sinclairinat0r.com) • Default ? ?
????????????????????????????????????
@sinclairinat0r.com

[? BlueSky (300 chars)]
????????????????????????????????????
? ?? Sinclarinat0rTest (@sinclarinat0rtest) ? ? ? User can select this
????????????????????????????????????
@sinclarinat0r.com ? Currently selected
```

## Testing Scenarios

### Scenario 1: Single Account per Platform
- **Given**: User has 1 BlueSky account
- **When**: User selects BlueSky platform
- **Then**: Account is auto-selected and shown in dropdown
- **And**: User sees `@username` below the platform toggle

### Scenario 2: Multiple Accounts per Platform
- **Given**: User has 2 BlueSky accounts (one default, one not)
- **When**: User selects BlueSky platform
- **Then**: Default account is auto-selected
- **And**: Dropdown shows both accounts with "Default" badge on the first
- **And**: User can click dropdown to switch to the other account

### Scenario 3: No Default Account
- **Given**: User has 2 BlueSky accounts, neither marked as default
- **When**: User selects BlueSky platform
- **Then**: First authenticated account is auto-selected
- **And**: User can switch accounts via dropdown

### Scenario 4: Publishing with Selected Account
- **Given**: User has selected "Account B" for BlueSky
- **When**: User clicks "Post"
- **Then**: Post is published using "Account B"
- **And**: Debug logs show: `Selected account for BlueSky: account-b-username`

## Debug Output

When posting, debug logs now show:
```
[PostEditorViewModel] Created post with 1 platforms and 1 account selections
[PostEditorViewModel] Selected account for BlueSky: sinclairinat0r.com
[PostService] Using account: sinclairinat0r.com for BlueSky
[BlueSkyService] Posting to BlueSky with 1 media attachments
```

## Benefits

1. **Transparency** - Users always know which account they're posting from
2. **Control** - Users can easily switch between accounts without going to Account Manager
3. **Flexibility** - Supports any number of accounts per platform
4. **Smart Defaults** - Auto-selects the most appropriate account (default > authenticated > first)
5. **Visual Clarity** - Clear indicators for authentication status and default accounts

## Future Enhancements

1. **Remember Last Used Account** - Store user's last selected account per platform
2. **Account Switching Shortcut** - Keyboard shortcut to cycle through accounts
3. **Account-Specific Drafts** - Save drafts with account association
4. **Multi-Account Posting** - Post same content to multiple accounts on same platform
5. **Account Health Indicator** - Show token expiration or connection issues

## Files Modified

### Core Files
- `SocialMediaCommander.Desktop\ViewModels\PostEditorViewModel.cs` - Added account selection logic
- `SocialMediaCommander.Desktop\Views\PostEditorView.axaml` - Added account selection UI

### Key Methods
- `LoadAccountsForAllPlatformsAsync()` - Loads accounts from AccountService
- `CreatePostFromViewModel()` - Uses selected account IDs
- `PlatformViewModel.OnSelectedAccountChanged()` - Updates display text

## Related Documentation
- `docs/features/dual-authentication-bluesky.md` - BlueSky authentication methods
- `docs/user-guide/adding-accounts-oauth.md` - Adding multiple accounts
- `docs/architecture/post-publishing-flow.md` - How posts are published

## Date
2025-01-15

## Author
GitHub Copilot (AI Assistant)
