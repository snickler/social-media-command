export type SocialPlatform = 'bluesky' | 'x' | 'linkedin' | 'threads' | 'facebook';

export interface SocialPlatformConfig {
  id: SocialPlatform;
  name: string;
  color: string;
  characterLimit?: number;
  hashtagSupport: boolean;
  mediaSupport: boolean;
  threadSupport: boolean;
}

export interface Account {
  id: string;
  platformId: SocialPlatform;
  username: string;
  displayName: string;
  avatar?: string;
  isDefault?: boolean;
}

export const PLATFORMS: SocialPlatformConfig[] = [
  {
    id: 'bluesky',
    name: 'BlueSky',
    color: '#1285FE',
    characterLimit: 300,
    hashtagSupport: true,
    mediaSupport: true,
    threadSupport: true
  },
  {
    id: 'x',
    name: 'X',
    color: '#000000',
    characterLimit: 280,
    hashtagSupport: true,
    mediaSupport: true,
    threadSupport: true
  },
  {
    id: 'linkedin',
    name: 'LinkedIn',
    color: '#0077B5',
    hashtagSupport: true,
    mediaSupport: true,
    threadSupport: false
  },
  {
    id: 'threads',
    name: 'Threads',
    color: '#000000',
    characterLimit: 500,
    hashtagSupport: true,
    mediaSupport: true,
    threadSupport: true
  },
  {
    id: 'facebook',
    name: 'Facebook',
    color: '#1877F2',
    hashtagSupport: true,
    mediaSupport: true,
    threadSupport: false
  }
];

// Default accounts for each platform
export const DEFAULT_ACCOUNTS: Account[] = [
  {
    id: 'bluesky-default',
    platformId: 'bluesky',
    username: 'default_user',
    displayName: 'Default BlueSky',
    avatar: 'https://api.dicebear.com/7.x/personas/svg?seed=bluesky-0',
    isDefault: true
  },
  {
    id: 'x-default',
    platformId: 'x',
    username: 'default_user',
    displayName: 'Default X',
    avatar: 'https://api.dicebear.com/7.x/personas/svg?seed=x-0',
    isDefault: true
  },
  {
    id: 'linkedin-default',
    platformId: 'linkedin',
    username: 'default_user',
    displayName: 'Default LinkedIn',
    avatar: 'https://api.dicebear.com/7.x/personas/svg?seed=linkedin-0',
    isDefault: true
  },
  {
    id: 'threads-default',
    platformId: 'threads',
    username: 'default_user',
    displayName: 'Default Threads',
    avatar: 'https://api.dicebear.com/7.x/personas/svg?seed=threads-0',
    isDefault: true
  },
  {
    id: 'facebook-default',
    platformId: 'facebook',
    username: 'default_user',
    displayName: 'Default Facebook',
    avatar: 'https://api.dicebear.com/7.x/personas/svg?seed=facebook-0',
    isDefault: true
  }
];

export interface Media {
  id: string;
  file: File;
  previewUrl: string;
  type: 'image' | 'video';
}

export interface Post {
  content: string;
  platforms: SocialPlatform[];
  hashtags: string[];
  promoMode: boolean;
  media: Media[];
  isThread: boolean;
  threadPosts?: ThreadPost[];
  threadsOnlyMode?: boolean;
  selectedAccounts?: Record<SocialPlatform, string[]>; // Map of platform IDs to selected account IDs
}

export interface ThreadPost {
  id: string;
  content: string;
  media: Media[];
}

export const DEFAULT_HASHTAGS = [
  'socialmedia',
  'digitalmarketing',
  'contentcreation',
  'marketing',
  'socialmediamarketing',
  'branding',
  'business',
  'entrepreneur',
  'success',
  'growth'
];

export function formatPostForPlatform(post: Post, platform: SocialPlatformConfig): string {
  let formattedContent = post.content;
  
  // Add hashtags if promo mode is on
  if (post.promoMode && post.hashtags.length > 0 && platform.hashtagSupport) {
    const hashtagString = post.hashtags.map(tag => `#${tag}`).join(' ');
    formattedContent = `${formattedContent}\n\n${hashtagString}`;
  }
  
  // Add media indicators
  if (post.media.length > 0 && platform.mediaSupport) {
    const mediaCount = post.media.length;
    formattedContent = `${formattedContent}\n\n[${mediaCount} media attachment${mediaCount > 1 ? 's' : ''}]`;
  }
  
  // Indicate if this is a thread
  if ((post.isThread || post.threadsOnlyMode) && platform.threadSupport && post.threadPosts?.length) {
    formattedContent = `${formattedContent}\n\n[Thread with ${post.threadPosts.length + 1} posts]`;
  }
  
  // Truncate if over character limit
  if (platform.characterLimit && formattedContent.length > platform.characterLimit) {
    formattedContent = formattedContent.substring(0, platform.characterLimit - 3) + '...';
  }
  
  return formattedContent;
}

export function getPlatformById(id: SocialPlatform): SocialPlatformConfig {
  return PLATFORMS.find(platform => platform.id === id) || PLATFORMS[0];
}