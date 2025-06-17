export type SocialPlatform = 'bluesky' | 'x' | 'linkedin' | 'threads' | 'facebook';

export interface SocialPlatformConfig {
  id: SocialPlatform;
  name: string;
  color: string;
  characterLimit?: number;
  hashtagSupport: boolean;
}

export const PLATFORMS: SocialPlatformConfig[] = [
  {
    id: 'bluesky',
    name: 'BlueSky',
    color: '#1285FE',
    characterLimit: 300,
    hashtagSupport: true
  },
  {
    id: 'x',
    name: 'X',
    color: '#000000',
    characterLimit: 280,
    hashtagSupport: true
  },
  {
    id: 'linkedin',
    name: 'LinkedIn',
    color: '#0077B5',
    hashtagSupport: true
  },
  {
    id: 'threads',
    name: 'Threads',
    color: '#000000',
    characterLimit: 500,
    hashtagSupport: true
  },
  {
    id: 'facebook',
    name: 'Facebook',
    color: '#1877F2',
    hashtagSupport: true
  }
];

export interface Post {
  content: string;
  platforms: SocialPlatform[];
  hashtags: string[];
  promoMode: boolean;
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
  
  // Truncate if over character limit
  if (platform.characterLimit && formattedContent.length > platform.characterLimit) {
    formattedContent = formattedContent.substring(0, platform.characterLimit - 3) + '...';
  }
  
  return formattedContent;
}

export function getPlatformById(id: SocialPlatform): SocialPlatformConfig {
  return PLATFORMS.find(platform => platform.id === id) || PLATFORMS[0];
}