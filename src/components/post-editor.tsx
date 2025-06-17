import { useRef, useState, useEffect } from 'react';
import { useKV } from '@github/spark/hooks';
import { Textarea } from '@/components/ui/textarea';
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { HashtagInput } from '@/components/platform-controls';
import { MediaUploader } from '@/components/media-uploader';
import { ThreadComposer } from '@/components/thread-composer';
import { 
  SocialPlatform, 
  formatPostForPlatform, 
  getPlatformById,
  Media,
  ThreadPost,
  Account
} from '@/lib/platform-utils';
import { 
  Copy, 
  PaperPlaneRight, 
  Images,
  ListBullets,
  StackSimple
} from '@phosphor-icons/react';

interface PostEditorProps {
  content: string;
  onContentChange: (content: string) => void;
  selectedPlatforms: SocialPlatform[];
  hashtags: string[];
  onHashtagsChange: (hashtags: string[]) => void;
  promoMode: boolean;
  onPromoModeChange: (enabled: boolean) => void;
  onPost: () => void;
  media?: Media[];
  onMediaChange?: (media: Media[]) => void;
  isThread?: boolean;
  onThreadChange?: (isThread: boolean) => void;
  threadPosts?: ThreadPost[];
  onThreadPostsChange?: (posts: ThreadPost[]) => void;
  threadsOnlyMode?: boolean;
  compact?: boolean;
}

export function PostEditor({
  content,
  onContentChange,
  selectedPlatforms,
  hashtags,
  onHashtagsChange,
  promoMode,
  onPromoModeChange,
  onPost,
  media = [],
  onMediaChange = () => {},
  isThread = false,
  onThreadChange = () => {},
  threadPosts = [],
  onThreadPostsChange = () => {},
  threadsOnlyMode = false,
  compact = false
}: PostEditorProps) {
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const [activeTab, setActiveTab] = useState<string>("editor");
  // Effect to auto-switch to thread tab when entering threads-only mode
  const [prevThreadsOnlyMode, setPrevThreadsOnlyMode] = useState(threadsOnlyMode);
  
  // Check if any selected platform supports threads
  const threadSupported = selectedPlatforms.some(platformId => {
    const platform = getPlatformById(platformId);
    return platform.threadSupport;
  });
  
  if (threadsOnlyMode && !prevThreadsOnlyMode && threadSupported) {
    setActiveTab("thread");
    setPrevThreadsOnlyMode(true);
  } else if (!threadsOnlyMode && prevThreadsOnlyMode) {
    setPrevThreadsOnlyMode(false);
  }
  
  const handleContentChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    onContentChange(e.target.value);
  };
  
  const handleFocus = () => {
    if (textareaRef.current) {
      textareaRef.current.focus();
    }
  };
  
  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
  };

  const platformsSelected = selectedPlatforms.length > 0;
  
  // Force thread mode if threadsOnlyMode is true
  const effectiveIsThread = threadsOnlyMode || isThread;
  
  // Check if any selected platform supports media
  const mediaSupported = selectedPlatforms.some(platformId => {
    const platform = getPlatformById(platformId);
    return platform.mediaSupport;
  });
  
  return (
    <Card className="w-full shadow-sm">
      <CardHeader className="pb-2">
        <div className="flex items-center justify-between">
          <CardTitle className="text-xl font-semibold text-primary">Compose Post</CardTitle>
          <div className="flex items-center gap-4">
            {threadSupported && !threadsOnlyMode && (
              <div className="flex items-center gap-2">
                <Switch 
                  id="thread-mode" 
                  checked={effectiveIsThread} 
                  onCheckedChange={onThreadChange}
                />
                <Label htmlFor="thread-mode" className="cursor-pointer text-sm">
                  Thread
                </Label>
              </div>
            )}
            {threadsOnlyMode && (
              <div className="bg-secondary/20 text-secondary-foreground px-2 py-1 rounded-md text-xs font-medium">
                Threads Only Mode
              </div>
            )}
            <div className="flex items-center gap-2">
              <Switch 
                id="promo-mode" 
                checked={promoMode} 
                onCheckedChange={onPromoModeChange}
              />
              <Label htmlFor="promo-mode" className="cursor-pointer text-sm">
                Promo Mode
              </Label>
            </div>
          </div>
        </div>
      </CardHeader>
      
      <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
        <div className="px-6">
          <TabsList className="w-full grid grid-cols-3">
            <TabsTrigger value="editor">Editor</TabsTrigger>
            {effectiveIsThread && threadSupported && (
              <TabsTrigger value="thread">Thread</TabsTrigger>
            )}
            <TabsTrigger value="preview" disabled={!platformsSelected}>
              Preview
            </TabsTrigger>
          </TabsList>
        </div>
        
        <TabsContent value="editor" className="p-0 mt-0">
          <CardContent className="pt-4 space-y-4">
            <Textarea
              ref={textareaRef}
              placeholder="What would you like to share today?"
              className="min-h-[150px] resize-y"
              value={content}
              onChange={handleContentChange}
            />
            
            {mediaSupported && (
              <MediaUploader 
                media={media}
                onMediaChange={onMediaChange}
                maxFiles={4}
              />
            )}
            
            {promoMode && (
              <div className="bg-muted/50 p-3 rounded-md">
                <div className="mb-2">
                  <h3 className="text-sm font-medium">Hashtags</h3>
                  <p className="text-xs text-muted-foreground">
                    Add hashtags to increase discoverability (shown in promo mode only)
                  </p>
                </div>
                <HashtagInput 
                  hashtags={hashtags} 
                  onHashtagsChange={onHashtagsChange} 
                />
              </div>
            )}
            
            {threadsOnlyMode && (
              <div className="bg-primary/10 border border-primary/30 rounded-md p-3 text-sm flex items-center gap-2">
                <StackSimple size={18} className="text-primary" />
                <span>
                  <strong>Threads Only Mode:</strong> Don't forget to add thread posts in the Thread tab.
                </span>
              </div>
            )}
          </CardContent>
        </TabsContent>
        
        {effectiveIsThread && threadSupported && (
          <TabsContent value="thread" className="p-0 mt-0">
            <CardContent className="pt-4">
              <ThreadComposer
                threadPosts={threadPosts}
                onThreadPostsChange={onThreadPostsChange}
                isThreadsOnlyMode={threadsOnlyMode}
              />
            </CardContent>
          </TabsContent>
        )}
        
        <TabsContent value="preview" className="p-0 mt-0">
          <CardContent className="pt-4 space-y-4">
            {selectedPlatforms.map(platformId => {
              const platform = getPlatformById(platformId);
              const formattedContent = formatPostForPlatform({
                content,
                platforms: [platformId],
                hashtags,
                promoMode,
                media,
                isThread: effectiveIsThread,
                threadPosts,
                threadsOnlyMode
              }, platform);
              
              // Get selected accounts for this platform
              const accounts = window.useKVStorage?.getItem('platform-accounts') || [];
              const selectedAccounts = window.useKVStorage?.getItem('selected-accounts') || {};
              const selectedAccountIds = selectedAccounts[platformId] || [];
              const platformAccounts = accounts.filter(
                (acc: any) => acc.platformId === platformId && selectedAccountIds.includes(acc.id)
              );
              
              return (
                <div key={platformId} className="border rounded-md p-3">
                  <div className="flex items-center justify-between mb-2">
                    <div className="flex items-center gap-2">
                      <h3 className="font-medium" style={{ color: platform.color }}>
                        {platform.name} Preview
                      </h3>
                      {platformAccounts.length > 0 && (
                        <div className="text-xs text-muted-foreground">
                          ({platformAccounts.length} account{platformAccounts.length > 1 ? 's' : ''})
                        </div>
                      )}
                    </div>
                    <Button 
                      variant="outline" 
                      size="sm" 
                      onClick={() => copyToClipboard(formattedContent)}
                      className="h-8"
                    >
                      <Copy size={16} className="mr-2" />
                      Copy
                    </Button>
                  </div>
                  <div className="whitespace-pre-wrap bg-muted/30 p-3 rounded text-sm">
                    {formattedContent}
                    {media.length > 0 && platform.mediaSupport && (
                      <div className="mt-2 pt-2 border-t flex items-center gap-1 text-xs">
                        <Images size={14} />
                        <span>{media.length} media attachment{media.length > 1 ? 's' : ''}</span>
                      </div>
                    )}
                    {effectiveIsThread && platform.threadSupport && threadPosts.length > 0 && (
                      <div className="mt-2 pt-2 border-t flex items-center gap-1 text-xs">
                        <ListBullets size={14} />
                        <span>Thread with {threadPosts.length + 1} post{threadPosts.length > 0 ? 's' : ''}</span>
                      </div>
                    )}
                  </div>
                  
                  {platformAccounts.length > 0 && (
                    <div className="mt-2 pt-2 border-t">
                      <div className="text-xs text-muted-foreground mb-1">Posting as:</div>
                      <div className="flex flex-wrap gap-2">
                        {platformAccounts.map((account: any) => (
                          <div key={account.id} className="flex items-center gap-1 bg-muted/30 px-2 py-1 rounded-md text-xs">
                            <div 
                              className="w-3 h-3 rounded-full"
                              style={{ backgroundColor: platform.color }}
                            />
                            <span>{account.displayName}</span>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              );
            })}
          </CardContent>
        </TabsContent>
      </Tabs>
      
      <CardFooter className="border-t bg-muted/10 px-6 py-4">
        <div className="flex items-center justify-between w-full">
          <div className="text-sm text-muted-foreground">
            {platformsSelected 
              ? `Posting to ${selectedPlatforms.length} platform${selectedPlatforms.length > 1 ? 's' : ''}` 
              : 'Select at least one platform to post'}
            {threadsOnlyMode && ' (threads only)'}
          </div>
          <Button 
            onClick={onPost} 
            disabled={(!content.trim() && threadPosts.length === 0) || !platformsSelected}
            className="bg-accent hover:bg-accent/90 text-accent-foreground"
          >
            <PaperPlaneRight size={18} weight="bold" className="mr-2" />
            {threadsOnlyMode ? 'Post Thread' : 'Post Now'}
          </Button>
        </div>
      </CardFooter>
    </Card>
  );
}