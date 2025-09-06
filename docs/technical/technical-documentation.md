# Social Media Management Hub - Technical Documentation

## Architecture Overview

The Social Media Management Hub is built using a modern React application architecture with TypeScript for type safety. This document provides technical details about the application structure, components, and data flow.

## Technology Stack

- **Framework**: React with TypeScript
- **State Management**: React hooks with persistent storage via `useKV`
- **UI Components**: Shadcn UI component library
- **Styling**: Tailwind CSS
- **Animations**: Framer Motion
- **Icons**: Phosphor Icons
- **Notifications**: Sonner toast library

## Core Components

### App Component

The main application component (`App.tsx`) serves as the container for the entire application and manages the global state through the `useKV` hook. Key responsibilities:

- Platform and account selection state
- View mode switching (Standard/Compact)
- Post submission logic
- Thread mode management

### Platform Controls

The `PlatformSelectors` component handles the selection of target social media platforms for posting. Features:

- Visual representation of each platform
- Selection state management
- Platform compatibility filtering for thread mode
- Account selection for each platform

### Post Editor

The `PostEditor` component is responsible for content creation and editing. Key features:

- Text input for post content
- Character count and limits based on selected platforms
- Media attachment functionality
- Thread creation interface
- Promo mode for automatic hashtag inclusion
- Platform-specific content previews

### Account Manager

The `AccountManager` component handles the creation and management of social media accounts. Features:

- Add, edit, and remove accounts
- Set default accounts
- Select accounts for posting
- Multi-account support for each platform

### Compact View

The `CompactView` component provides an alternative interface with split-screen layout. Features:

- Split view with feeds and composer
- Compose-only mode
- Optimized space utilization
- Responsive design for different screen sizes

### Thread Composer

The `ThreadComposer` component manages the creation of thread posts. Features:

- Add, edit, and remove thread posts
- Reorder posts via drag and drop
- Media attachment for individual thread posts
- Thread preview

## Data Models

### SocialPlatform

```typescript
export type SocialPlatform = 'bluesky' | 'x' | 'linkedin' | 'threads' | 'facebook';
```

### SocialPlatformConfig

```typescript
export interface SocialPlatformConfig {
  id: SocialPlatform;
  name: string;
  color: string;
  characterLimit?: number;
  hashtagSupport: boolean;
  mediaSupport: boolean;
  threadSupport: boolean;
}
```

### Account

```typescript
export interface Account {
  id: string;
  platformId: SocialPlatform;
  username: string;
  displayName: string;
  avatar?: string;
  isDefault?: boolean;
}
```

### Media

```typescript
export interface Media {
  id: string;
  file: File;
  previewUrl: string;
  type: 'image' | 'video';
}
```

### ThreadPost

```typescript
export interface ThreadPost {
  id: string;
  content: string;
  media: Media[];
}
```

### Post

```typescript
export interface Post {
  content: string;
  platforms: SocialPlatform[];
  hashtags: string[];
  promoMode: boolean;
  media: Media[];
  isThread: boolean;
  threadPosts?: ThreadPost[];
  threadsOnlyMode?: boolean;
  selectedAccounts?: Record<SocialPlatform, string[]>;
}
```

## State Management

The application uses the `useKV` hook for persistent state storage across sessions. Key state elements:

- `post-content`: Current post content
- `selected-platforms`: Array of selected platform IDs
- `hashtags`: Array of hashtag strings
- `promo-mode`: Boolean toggle for hashtag inclusion
- `media`: Array of media files
- `is-thread`: Boolean toggle for thread mode
- `threads-only-mode`: Boolean toggle for threads-only mode
- `thread-posts`: Array of thread post objects
- `view-mode`: View mode selection ('standard' or 'compact')
- `platform-accounts`: Array of account objects
- `selected-accounts`: Record mapping platform IDs to selected account IDs

## Helper Functions

### formatPostForPlatform

Formats post content for specific platforms, handling:
- Character limits
- Hashtag inclusion
- Media indicators
- Thread indicators

### getPlatformById

Retrieves platform configuration by ID.

## UI Theme

The application uses a custom theme defined in `index.css` with CSS variables for colors and sizing. The theme is designed to provide a modern, native application feel with:

- Carefully selected color palette
- Consistent border radius
- Custom scrollbars
- Focus states
- Shadows and elevation

## Responsive Design

The application is fully responsive with:
- Grid-based layouts that adjust to screen size
- Mobile-friendly controls
- Appropriate spacing at different breakpoints
- Touch-friendly interactive elements

## Extending the Application

### Adding New Platforms

To add a new social media platform:

1. Add a new platform ID to the `SocialPlatform` type
2. Create a platform config object in the `PLATFORMS` array
3. Add a default account for the platform
4. Update any platform-specific handling in relevant components

### Adding New Features

The component-based architecture makes it easy to extend functionality:

1. Create new components in the `/components` directory
2. Add new state variables using the `useKV` hook
3. Update the relevant parent components to include the new functionality
4. Add any necessary utility functions to `/lib` directory

## Performance Considerations

- The application uses memoization where appropriate to prevent unnecessary re-renders
- Media is handled efficiently with proper cleanup on unmount
- State updates are batched where possible
- Animation is used judiciously to avoid performance impacts

## Security Notes

- User credentials are not stored in the application
- Content is validated before submission
- Cross-platform posting is simulated and would require actual API integration in a production environment