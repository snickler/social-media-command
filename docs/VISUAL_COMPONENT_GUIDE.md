# Visual Component Guide

This guide provides quick reference examples for using the enhanced UI components.

## Button Styles

### Primary Button
```xml
<Button Content="Post Now" Classes="primary" Command="{Binding PostCommand}"/>
```
**Use for:** Main actions, CTAs, confirmations

### Secondary Button
```xml
<Button Content="Save Draft" Classes="secondary" Command="{Binding SaveDraftCommand}"/>
```
**Use for:** Alternative actions, less emphasis than primary

### Outline Button
```xml
<Button Content="Preview" Classes="outline" Command="{Binding PreviewCommand}"/>
```
**Use for:** Tertiary actions, alternative choices

### Ghost Button
```xml
<Button Content="⚙️" Classes="ghost" ToolTip.Tip="Settings"/>
```
**Use for:** Icon buttons, minimal UI elements

### Destructive Button
```xml
<Button Content="Delete Post" Classes="destructive" Command="{Binding DeleteCommand}"/>
```
**Use for:** Dangerous actions requiring confirmation

## Form Controls

### TextBox with Modern Style
```xml
<TextBox Text="{Binding Username}" 
         Watermark="Enter username"
         Classes="modern"/>
```

### ComboBox with Modern Style
```xml
<ComboBox ItemsSource="{Binding Platforms}"
          SelectedItem="{Binding SelectedPlatform}"
          Classes="modern"/>
```

## Card Components

### Standard Card
```xml
<Border Classes="Card">
    <StackPanel Spacing="16">
        <TextBlock Text="Card Title" FontWeight="Bold" FontSize="18"/>
        <TextBlock Text="Card content goes here" TextWrapping="Wrap"/>
    </StackPanel>
</Border>
```

### Interactive Card (Clickable)
```xml
<Border Classes="InteractiveCard" Tapped="OnCardTapped">
    <!-- Content -->
</Border>
```

### Success Message
```xml
<Border Classes="SuccessMessage" IsVisible="{Binding ShowSuccess}">
    <StackPanel Orientation="Horizontal" Spacing="12">
        <TextBlock Text="✓" FontSize="20" FontWeight="Bold"/>
        <TextBlock Text="Post published successfully!"/>
    </StackPanel>
</Border>
```

## Animations

### Fade In
```xml
<Border Classes="FadeIn">
    <!-- Content appears smoothly -->
</Border>
```

### Slide In from Bottom
```xml
<Border Classes="SlideInBottom">
    <!-- Content slides up from bottom -->
</Border>
```

### Loading with Pulse
```xml
<Border Classes="Pulse" IsVisible="{Binding IsLoading}">
    <TextBlock Text="Loading..." HorizontalAlignment="Center"/>
</Border>
```

### Spinning Loader
```xml
<Border Classes="Spin" Width="32" Height="32">
    <TextBlock Text="⟳" FontSize="24"/>
</Border>
```

### Skeleton Loader
```xml
<Border Classes="SkeletonLoader" 
        Width="200" 
        Height="20" 
        Margin="0,0,0,8"
        IsVisible="{Binding IsLoadingContent}"/>
```

### Success Animation
```xml
<Border Classes="SuccessCheckmark" IsVisible="{Binding ShowSuccess}">
    <TextBlock Text="✓" FontSize="32" Foreground="Green"/>
</Border>
```

### Error Shake
```xml
<Border Classes="ShakeError" IsVisible="{Binding HasError}">
    <TextBlock Text="{Binding ErrorMessage}" Foreground="Red"/>
</Border>
```

## Badges

### Status Badges
```xml
<!-- Success Badge -->
<Border Classes="Badge Success">
    <TextBlock Text="Connected"/>
</Border>

<!-- Warning Badge -->
<Border Classes="Badge Warning">
    <TextBlock Text="Pending"/>
</Border>

<!-- Error Badge -->
<Border Classes="Badge Error">
    <TextBlock Text="Failed"/>
</Border>

<!-- Default Badge -->
<Border Classes="Badge">
    <TextBlock Text="New"/>
</Border>
```

## Progress Indicators

### Modern Progress Bar
```xml
<ProgressBar Classes="Modern" 
             Value="{Binding Progress}" 
             Maximum="100"/>
```

### Indeterminate Progress
```xml
<ProgressBar Classes="ModernIndeterminate" 
             IsVisible="{Binding IsProcessing}"/>
```

## Platform Toggles

```xml
<ToggleButton Classes="PlatformToggle" 
              IsChecked="{Binding IsTwitterSelected}">
    <StackPanel Orientation="Horizontal" Spacing="8">
        <TextBlock Text="✓" IsVisible="{Binding IsTwitterSelected}"/>
        <TextBlock Text="Twitter"/>
        <TextBlock Text="(280 chars)" FontSize="12" Opacity="0.6"/>
    </StackPanel>
</ToggleButton>
```

## Tab Navigation

```xml
<StackPanel Orientation="Horizontal">
    <RadioButton Content="Compose" 
                 Classes="TabButton" 
                 GroupName="EditorTabs" 
                 IsChecked="{Binding IsComposeTabActive}"/>
    <RadioButton Content="Preview" 
                 Classes="TabButton" 
                 GroupName="EditorTabs" 
                 IsChecked="{Binding IsPreviewTabActive}"/>
</StackPanel>
```

## Thread Post Card

```xml
<Border Classes="ThreadPostCard">
    <StackPanel Spacing="12">
        <Grid ColumnDefinitions="Auto,*,Auto">
            <!-- Post number -->
            <Border Grid.Column="0" 
                    Background="{DynamicResource SMC-Accent}" 
                    CornerRadius="12" 
                    Width="24" Height="24">
                <TextBlock Text="1" 
                           HorizontalAlignment="Center" 
                           VerticalAlignment="Center"
                           Foreground="White"
                           FontWeight="Bold"/>
            </Border>
            
            <!-- Content -->
            <TextBlock Grid.Column="1" 
                       Text="Thread Post Title" 
                       FontWeight="Medium" 
                       Margin="12,0,0,0"/>
            
            <!-- Actions -->
            <StackPanel Grid.Column="2" Orientation="Horizontal" Spacing="4">
                <Button Content="⬆️" Classes="ghost"/>
                <Button Content="⬇️" Classes="ghost"/>
                <Button Content="🗑️" Classes="ghost"/>
            </StackPanel>
        </Grid>
        
        <TextBox Text="{Binding Content}" 
                 Watermark="Thread post content..."
                 AcceptsReturn="True" 
                 TextWrapping="Wrap" 
                 MinHeight="80"/>
    </StackPanel>
</Border>
```

## Loading States

### Loading Overlay
```xml
<Border Background="#80000000" 
        IsVisible="{Binding IsLoading}"
        ZIndex="999">
    <StackPanel HorizontalAlignment="Center" 
                VerticalAlignment="Center" 
                Spacing="16">
        <Border Classes="Spin" Width="48" Height="48">
            <TextBlock Text="⟳" FontSize="32" Foreground="White"/>
        </Border>
        <TextBlock Text="Loading..." 
                   Foreground="White" 
                   FontSize="16" 
                   HorizontalAlignment="Center"/>
    </StackPanel>
</Border>
```

### Skeleton Screen
```xml
<StackPanel Spacing="12" IsVisible="{Binding IsLoadingContent}">
    <Border Classes="SkeletonLoader" Height="24" Width="200"/>
    <Border Classes="SkeletonLoader" Height="16" Width="300"/>
    <Border Classes="SkeletonLoader" Height="16" Width="280"/>
    <Border Classes="SkeletonLoader" Height="100" Width="350"/>
</StackPanel>
```

## Empty States

### No Content
```xml
<StackPanel HorizontalAlignment="Center" 
            VerticalAlignment="Center" 
            Spacing="16"
            IsVisible="{Binding !HasContent}">
    <Border Background="{DynamicResource SMC-Muted}" 
            CornerRadius="50" 
            Width="80" Height="80">
        <TextBlock Text="📭" 
                   FontSize="32" 
                   HorizontalAlignment="Center" 
                   VerticalAlignment="Center"/>
    </Border>
    <StackPanel Spacing="8">
        <TextBlock Text="No posts yet" 
                   FontSize="18" 
                   FontWeight="SemiBold"
                   HorizontalAlignment="Center"/>
        <TextBlock Text="Create your first post to get started" 
                   FontSize="14" 
                   Opacity="0.7"
                   HorizontalAlignment="Center"/>
    </StackPanel>
    <Button Content="Create Post" Classes="primary"/>
</StackPanel>
```

## Tooltips

```xml
<Button Content="⚙️" 
        Classes="ghost"
        ToolTip.Tip="Open settings"/>
```

Tooltips are automatically styled with dark theme, accent border, and proper spacing.

## Color Scheme Reference

### Primary Colors
- **Accent:** `#4338CA` (Indigo)
- **Accent Hover:** `#5a52d0`
- **Accent Pressed:** `#716dd6`

### Status Colors
- **Success:** `#059669` (Green)
- **Warning:** `#F59E0B` (Orange)
- **Error/Destructive:** `#f04141` (Red)

### Neutral Colors
- **Background:** `#f5f6fa`
- **Foreground:** `#2e2d33`
- **Muted:** `#f1f2f6`
- **Muted Foreground:** `#6c757d`
- **Card Background:** `#f7f8fc`
- **Card Border:** `#dedce3`

## Spacing System

- **Tiny:** 4px
- **Small:** 8px
- **Medium:** 12px
- **Default:** 16px
- **Large:** 20px
- **XLarge:** 24px
- **XXLarge:** 32px

## Typography Scale

- **Display:** 24px Bold
- **Title:** 20px SemiBold
- **Subtitle:** 18px Medium
- **Body:** 14-16px Regular
- **Caption:** 12-13px Regular
- **Tiny:** 10-11px Regular

## Best Practices

1. **Combine Classes**: You can combine animation classes with component classes
   ```xml
   <Border Classes="Card FadeIn">
   ```

2. **Loading States**: Always provide loading feedback for async operations
   ```xml
   <Button IsEnabled="{Binding !IsLoading}">
       <StackPanel Orientation="Horizontal">
           <Border Classes="Spin" IsVisible="{Binding IsLoading}">...</Border>
           <TextBlock Text="Submit"/>
       </StackPanel>
   </Button>
   ```

3. **Accessibility**: Always include ToolTip.Tip for icon-only buttons
   ```xml
   <Button Content="🗑️" ToolTip.Tip="Delete"/>
   ```

4. **Consistent Spacing**: Use the spacing system throughout
   ```xml
   <StackPanel Spacing="16">
   ```

5. **Theme Resources**: Always use theme resources for colors
   ```xml
   <Setter Property="Background" Value="{DynamicResource SMC-Accent}"/>
   ```

## Example: Complete Form

```xml
<Border Classes="Card FadeIn">
    <StackPanel Spacing="24">
        <!-- Header -->
        <StackPanel Spacing="8">
            <TextBlock Text="Create Account" FontSize="24" FontWeight="Bold"/>
            <TextBlock Text="Fill in your details below" Opacity="0.7"/>
        </StackPanel>
        
        <!-- Form Fields -->
        <StackPanel Spacing="16">
            <StackPanel Spacing="8">
                <TextBlock Text="Username" FontWeight="Medium"/>
                <TextBox Text="{Binding Username}" 
                         Watermark="Enter username"
                         Classes="modern"/>
            </StackPanel>
            
            <StackPanel Spacing="8">
                <TextBlock Text="Email" FontWeight="Medium"/>
                <TextBox Text="{Binding Email}" 
                         Watermark="your@email.com"
                         Classes="modern"/>
            </StackPanel>
        </StackPanel>
        
        <!-- Status Message -->
        <Border Classes="SuccessMessage" IsVisible="{Binding ShowSuccess}">
            <TextBlock Text="Account created successfully!"/>
        </Border>
        
        <!-- Actions -->
        <StackPanel Orientation="Horizontal" 
                   Spacing="12" 
                   HorizontalAlignment="Right">
            <Button Content="Cancel" Classes="outline"/>
            <Button Content="Create" Classes="primary"/>
        </StackPanel>
    </StackPanel>
</Border>
```

This creates a beautiful, animated form with proper spacing, validation feedback, and clear visual hierarchy.
