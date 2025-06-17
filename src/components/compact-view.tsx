import { useState } from 'react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { PostEditor } from '@/components/post-editor';
import { SocialFeed } from '@/components/social-feed';
import { SocialPlatform, ThreadPost, Media, PLATFORMS } from '@/lib/platform-utils';
import { ViewGrid, PencilSimple } from '@phosphor-icons/react';
import { PlatformSelectors } from '@/components/platform-controls';

interface CompactViewProps {
  content: string;
  onContentChange: (content: string) => void;
  selectedPlatforms: SocialPlatform[];
  onPlatformsChange: (platforms: SocialPlatform[]) => void;
  hashtags: string[];
  onHashtagsChange: (hashtags: string[]) => void;
  promoMode: boolean;
  onPromoModeChange: (enabled: boolean) => void;
  onPost: () => void;
  media: Media[];
  onMediaChange: (media: Media[]) => void;
  isThread: boolean;
  onThreadChange: (isThread: boolean) => void;
  threadPosts: ThreadPost[];
  onThreadPostsChange: (posts: ThreadPost[]) => void;
  threadsOnlyMode: boolean;
}

export function CompactView({
  content,
  onContentChange,
  selectedPlatforms,
  onPlatformsChange,
  hashtags,
  onHashtagsChange,
  promoMode,
  onPromoModeChange,
  onPost,
  media,
  onMediaChange,
  isThread,
  onThreadChange,
  threadPosts,
  onThreadPostsChange,
  threadsOnlyMode
}: CompactViewProps) {
  const [activeView, setActiveView] = useState<string>("split");
  
  return (
    <div className="w-full space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Workspace</h2>
        <Tabs value={activeView} onValueChange={setActiveView} className="w-auto">
          <TabsList>
            <TabsTrigger value="split" className="flex items-center gap-1">
              <ViewGrid size={16} />
              <span>Split View</span>
            </TabsTrigger>
            <TabsTrigger value="compose" className="flex items-center gap-1">
              <PencilSimple size={16} />
              <span>Compose Only</span>
            </TabsTrigger>
          </TabsList>
        </Tabs>
      </div>
      
      <div className="mb-4">
        <h3 className="text-base font-medium mb-3">Target Platforms</h3>
        <PlatformSelectors 
          platforms={threadsOnlyMode ? PLATFORMS.filter(p => p.threadSupport) : PLATFORMS} 
          selectedPlatforms={selectedPlatforms}
          onPlatformsChange={onPlatformsChange}
        />
      </div>
      
      <Tabs value={activeView} className="w-full">
        <TabsContent value="split" className="m-0 p-0">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <div className="lg:order-1">
              <PostEditor
                content={content}
                onContentChange={onContentChange}
                selectedPlatforms={selectedPlatforms}
                hashtags={hashtags}
                onHashtagsChange={onHashtagsChange}
                promoMode={promoMode}
                onPromoModeChange={onPromoModeChange}
                onPost={onPost}
                media={media}
                onMediaChange={onMediaChange}
                isThread={isThread}
                onThreadChange={onThreadChange}
                threadPosts={threadPosts}
                onThreadPostsChange={onThreadPostsChange}
                threadsOnlyMode={threadsOnlyMode}
              />
            </div>
            <div className="lg:order-2">
              <SocialFeed platformConfig={PLATFORMS} />
            </div>
          </div>
        </TabsContent>
        
        <TabsContent value="compose" className="m-0 p-0">
          <PostEditor
            content={content}
            onContentChange={onContentChange}
            selectedPlatforms={selectedPlatforms}
            hashtags={hashtags}
            onHashtagsChange={onHashtagsChange}
            promoMode={promoMode}
            onPromoModeChange={onPromoModeChange}
            onPost={onPost}
            media={media}
            onMediaChange={onMediaChange}
            isThread={isThread}
            onThreadChange={onThreadChange}
            threadPosts={threadPosts}
            onThreadPostsChange={onThreadPostsChange}
            threadsOnlyMode={threadsOnlyMode}
          />
        </TabsContent>
      </Tabs>
    </div>
  );
}