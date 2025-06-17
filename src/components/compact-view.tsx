import { useState } from 'react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { PostEditor } from '@/components/post-editor';
import { SocialFeed } from '@/components/social-feed';
import { SocialPlatform, ThreadPost, Media, PLATFORMS, Account } from '@/lib/platform-utils';
import { ViewGrid, PencilSimple } from '@phosphor-icons/react';
import { PlatformSelectors } from '@/components/platform-controls';
import { useKV } from '@github/spark/hooks';
import { Card } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';

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
  const [accounts] = useKV<Account[]>('platform-accounts', []);
  const [selectedAccounts] = useKV<Record<SocialPlatform, string[]>>('selected-accounts', {});
  
  return (
    <div className="w-full space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold">Workspace</h2>
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
      
      <Card className="p-4 shadow-sm">
        <h3 className="text-base font-medium mb-3">Target Platforms</h3>
        <PlatformSelectors 
          platforms={threadsOnlyMode ? PLATFORMS.filter(p => p.threadSupport) : PLATFORMS} 
          selectedPlatforms={selectedPlatforms}
          onPlatformsChange={onPlatformsChange}
          accounts={accounts}
          selectedAccounts={selectedAccounts}
        />
      </Card>
      
      <Tabs value={activeView} className="w-full">
        <TabsContent value="split" className="m-0 p-0">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
            <Card className="p-4 shadow-sm h-full lg:order-1">
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
                compact={true}
              />
            </Card>
            <Card className="p-4 shadow-sm h-full lg:order-2">
              <h3 className="text-base font-medium mb-3">Feeds</h3>
              <Separator className="mb-4" />
              <div className="h-[600px] overflow-y-auto pr-2">
                <SocialFeed platformConfig={PLATFORMS} />
              </div>
            </Card>
          </div>
        </TabsContent>
        
        <TabsContent value="compose" className="m-0 p-0">
          <Card className="p-4 shadow-sm">
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
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}