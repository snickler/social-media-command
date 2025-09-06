import { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { SocialPlatformConfig, PLATFORMS } from '@/lib/platform-utils';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { useKV } from '@/hooks/useKV';
import { ArrowClockwise, Heart, ChatCircle, Repeat } from '@phosphor-icons/react';

interface FeedItem {
  id: string;
  platform: string;
  author: {
    name: string;
    handle: string;
    avatar: string;
  };
  content: string;
  timestamp: string;
  likes: number;
  reposts: number;
  comments: number;
  isLiked: boolean;
  isReposted: boolean;
}

interface SocialFeedProps {
  platformConfig: SocialPlatformConfig[];
}

export function SocialFeed({ platformConfig }: SocialFeedProps) {
  const [activePlatform, setActivePlatform] = useState<string>(platformConfig[0].id);
  const [feedItems, setFeedItems] = useKV<Record<string, FeedItem[]>>('feed-items', generateInitialFeed());
  const [isRefreshing, setIsRefreshing] = useState(false);

  function generateInitialFeed(): Record<string, FeedItem[]> {
    const mockFeed: Record<string, FeedItem[]> = {};
    
    platformConfig.forEach(platform => {
      mockFeed[platform.id] = Array.from({ length: 5 }, (_, i) => ({
        id: `${platform.id}-post-${i}`,
        platform: platform.id,
        author: {
          name: `${platform.name} User ${i+1}`,
          handle: `user${i+1}`,
          avatar: `https://api.dicebear.com/7.x/personas/svg?seed=${platform.id}-${i}`,
        },
        content: getRandomContent(platform.id, i),
        timestamp: getRandomTimestamp(),
        likes: Math.floor(Math.random() * 100),
        reposts: Math.floor(Math.random() * 50),
        comments: Math.floor(Math.random() * 30),
        isLiked: false,
        isReposted: false,
      }));
    });
    
    return mockFeed;
  }

  function getRandomContent(platform: string, index: number): string {
    const contents = [
      `Latest updates from the world of tech! #technology #innovation ${platform === 'linkedin' ? '#professional' : ''}`,
      `Just shared my thoughts on sustainable business practices. Check out my latest article!`,
      `Amazing day at the conference! Met so many inspiring people. ${platform === 'x' || platform === 'bluesky' ? '#networking' : ''}`,
      `Our team just launched a new feature. Let us know what you think!`,
      `Working on something exciting. Stay tuned for updates! ${platform === 'facebook' ? 'What are you working on?' : ''}`
    ];
    return contents[index % contents.length];
  }

  function getRandomTimestamp(): string {
    const minutes = Math.floor(Math.random() * 59);
    const hours = Math.floor(Math.random() * 12);
    return `${hours}h ${minutes}m ago`;
  }

  const refreshFeed = () => {
    setIsRefreshing(true);
    setTimeout(() => {
      setFeedItems(generateInitialFeed());
      setIsRefreshing(false);
    }, 800);
  };

  const toggleLike = (postId: string) => {
    setFeedItems(prev => {
      const newFeed = { ...prev };
      const platform = activePlatform;
      const postIndex = newFeed[platform].findIndex(item => item.id === postId);
      
      if (postIndex !== -1) {
        const post = newFeed[platform][postIndex];
        newFeed[platform][postIndex] = {
          ...post,
          isLiked: !post.isLiked,
          likes: post.isLiked ? post.likes - 1 : post.likes + 1
        };
      }
      
      return newFeed;
    });
  };

  const toggleRepost = (postId: string) => {
    setFeedItems(prev => {
      const newFeed = { ...prev };
      const platform = activePlatform;
      const postIndex = newFeed[platform].findIndex(item => item.id === postId);
      
      if (postIndex !== -1) {
        const post = newFeed[platform][postIndex];
        newFeed[platform][postIndex] = {
          ...post,
          isReposted: !post.isReposted,
          reposts: post.isReposted ? post.reposts - 1 : post.reposts + 1
        };
      }
      
      return newFeed;
    });
  };

  return (
    <Card className="w-full shadow-sm border rounded-lg overflow-hidden">
      <CardHeader className="pb-2 bg-card border-b">
        <div className="flex items-center justify-between">
          <CardTitle className="text-xl font-semibold">Social Feeds</CardTitle>
          <Button 
            variant="outline" 
            size="sm" 
            onClick={refreshFeed}
            disabled={isRefreshing}
            className="h-8"
          >
            <ArrowClockwise size={16} className={`mr-2 ${isRefreshing ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
        </div>
      </CardHeader>
      <Tabs defaultValue={activePlatform} onValueChange={setActivePlatform}>
        <div className="px-6">
          <TabsList className="w-full">
            {platformConfig.map(platform => (
              <TabsTrigger 
                key={platform.id} 
                value={platform.id}
                className="flex-1"
                style={{ borderBottomColor: platform.color }}
              >
                {platform.name}
              </TabsTrigger>
            ))}
          </TabsList>
        </div>
        
        {platformConfig.map(platform => (
          <TabsContent key={platform.id} value={platform.id} className="p-0 mt-0">
            <CardContent className="pt-4 space-y-4 max-h-[600px] overflow-y-auto">
              {feedItems[platform.id]?.map(item => (
                <div 
                  key={item.id}
                  className="border rounded-md p-4 space-y-3 hover:bg-muted/20 transition-colors"
                >
                  <div className="flex gap-3">
                    <Avatar>
                      <AvatarImage src={item.author.avatar} />
                      <AvatarFallback style={{ backgroundColor: platform.color }}>
                        {item.author.name.charAt(0)}
                      </AvatarFallback>
                    </Avatar>
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="font-medium">{item.author.name}</span>
                        <span className="text-sm text-muted-foreground">@{item.author.handle}</span>
                      </div>
                      <div className="text-xs text-muted-foreground">{item.timestamp}</div>
                    </div>
                  </div>
                  
                  <p className="text-sm">{item.content}</p>
                  
                  <div className="flex items-center gap-4 pt-1">
                    <button 
                      className="flex items-center gap-1 text-sm text-muted-foreground hover:text-primary transition-colors"
                      onClick={() => {}}
                    >
                      <ChatCircle size={16} />
                      <span>{item.comments}</span>
                    </button>
                    <button 
                      className={`flex items-center gap-1 text-sm hover:text-accent transition-colors ${item.isReposted ? 'text-accent' : 'text-muted-foreground'}`}
                      onClick={() => toggleRepost(item.id)}
                    >
                      <Repeat size={16} />
                      <span>{item.reposts}</span>
                    </button>
                    <button 
                      className={`flex items-center gap-1 text-sm hover:text-destructive transition-colors ${item.isLiked ? 'text-destructive' : 'text-muted-foreground'}`}
                      onClick={() => toggleLike(item.id)}
                    >
                      <Heart size={16} weight={item.isLiked ? 'fill' : 'regular'} />
                      <span>{item.likes}</span>
                    </button>
                  </div>
                </div>
              ))}
            </CardContent>
          </TabsContent>
        ))}
      </Tabs>
    </Card>
  );
}