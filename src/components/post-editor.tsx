import { useRef, useState, useEffect } from 'react';
import { useKV } from '@/hooks/useKV';
import { Textarea } from '@/components/ui/textarea';
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { HashtagInput } from '@/components/platform-controls';
import { MediaUploader } from '@/components/media-uploader';
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
  StackSimple,
  Plus,
  X,
  ArrowUp,
  ArrowDown,
  Trash
} from '@phosphor-icons/react';
import { cn } from '@/lib/utils';
import { Separator } from '@/components/ui/separator';

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
  const [activeTab, setActiveTab] = useState<string>("composer");
  // Effect to auto-switch to thread tab when entering threads-only mode
  const [prevThreadsOnlyMode, setPrevThreadsOnlyMode] = useState(threadsOnlyMode);
  
  // Check if any selected platform supports threads
  const threadSupported = selectedPlatforms.some(platformId => {
    const platform = getPlatformById(platformId);
    return platform.threadSupport;
  });
  
  if (threadsOnlyMode && !prevThreadsOnlyMode) {
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
  
  // Check if there's valid content to enable post button
  const hasValidContent = content.trim().length > 0 || threadPosts.some(post => post.content.trim().length > 0);
  
  // Check if any selected platform supports media
  const mediaSupported = selectedPlatforms.some(platformId => {
    const platform = getPlatformById(platformId);
    return platform.mediaSupport;
  });
  
  // Thread post management functions
  const addThreadPost = () => {
    const newPost: ThreadPost = {
      id: `thread-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
      content: '',
      media: []
    };
    
    onThreadPostsChange([...threadPosts, newPost]);
  };
  
  const updateThreadPost = (id: string, updates: Partial<ThreadPost>) => {
    const updatedPosts = threadPosts.map(post => 
      post.id === id ? { ...post, ...updates } : post
    );
    
    onThreadPostsChange(updatedPosts);
  };
  
  const removeThreadPost = (id: string) => {
    // Revoke any object URLs to prevent memory leaks
    const postToRemove = threadPosts.find(post => post.id === id);
    if (postToRemove) {
      postToRemove.media.forEach(media => {
        URL.revokeObjectURL(media.previewUrl);
      });
    }
    
    const updatedPosts = threadPosts.filter(post => post.id !== id);
    onThreadPostsChange(updatedPosts);
  };
  
  const moveThreadPost = (id: string, direction: 'up' | 'down') => {
    const currentIndex = threadPosts.findIndex(post => post.id === id);
    if (currentIndex === -1) return;
    
    const newIndex = direction === 'up' 
      ? Math.max(currentIndex - 1, 0)
      : Math.min(currentIndex + 1, threadPosts.length - 1);
      
    if (newIndex === currentIndex) return;
    
    const updatedPosts = [...threadPosts];
    const [removedPost] = updatedPosts.splice(currentIndex, 1);
    updatedPosts.splice(newIndex, 0, removedPost);
    
    onThreadPostsChange(updatedPosts);
  };
  
  return (
    <Card className="w-full shadow-sm border rounded-lg overflow-hidden">
      <CardHeader className="pb-2 bg-card border-b">
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
          <TabsList className="w-full grid grid-cols-2">
            <TabsTrigger value="composer">Composer</TabsTrigger>
            <TabsTrigger value="preview" disabled={!platformsSelected}>
              Preview
            </TabsTrigger>
          </TabsList>
        </div>
        
        <TabsContent value="composer" className="p-0 mt-0">
          <CardContent className="pt-4 space-y-4">
            {/* Main post */}
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <h3 className="text-base font-medium">Main Post</h3>
                {effectiveIsThread && (
                  <div className="text-xs text-muted-foreground bg-secondary/10 px-2 py-1 rounded">
                    First post in thread
                  </div>
                )}
              </div>
              <Textarea
                ref={textareaRef}
                placeholder="What would you like to share today?"
                className="min-h-[120px] resize-y"
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
            </div>
            
            {/* Thread posts section */}
            {effectiveIsThread && threadSupported && (
              <div className="space-y-4 pt-4">
                <Separator />
                <div className="flex items-center justify-between">
                  <h3 className="text-base font-medium">Thread Posts</h3>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={addThreadPost}
                  >
                    <Plus size={16} className="mr-1" />
                    Add Post
                  </Button>
                </div>
                
                {threadsOnlyMode && threadPosts.length === 0 && (
                  <div className="bg-primary/10 border border-primary/30 rounded-md p-3 text-sm">
                    <strong>Threads Only Mode:</strong> You must add at least one thread post to publish.
                  </div>
                )}
                
                {threadPosts.length === 0 ? (
                  <div className="text-center py-4 border border-dashed rounded-md text-muted-foreground">
                    <p>No thread posts added yet</p>
                    <p className="text-xs mt-1">Add posts to create a thread</p>
                  </div>
                ) : (
                  <div className="space-y-4">
                    {threadPosts.map((post, index) => (
                      <ThreadPostCard
                        key={post.id}
                        post={post}
                        index={index}
                        isFirst={index === 0}
                        isLast={index === threadPosts.length - 1}
                        onUpdate={(updates) => updateThreadPost(post.id, updates)}
                        onRemove={() => removeThreadPost(post.id)}
                        onMove={(direction) => moveThreadPost(post.id, direction)}
                      />
                    ))}
                  </div>
                )}
              </div>
            )}
            
            {/* Hashtags (Promo Mode) */}
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
          </CardContent>
        </TabsContent>
        
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
            {effectiveIsThread && threadPosts.length > 0 && ' as thread'}
          </div>
          <Button 
            onClick={onPost} 
            disabled={!hasValidContent || !platformsSelected || (threadsOnlyMode && threadPosts.length === 0)}
            className="bg-accent hover:bg-accent/90 text-accent-foreground"
          >
            <PaperPlaneRight size={18} weight="bold" className="mr-2" />
            {effectiveIsThread ? 'Post Thread' : 'Post Now'}
          </Button>
        </div>
      </CardFooter>
    </Card>
  );
}

interface ThreadPostCardProps {
  post: ThreadPost;
  index: number;
  isFirst: boolean;
  isLast: boolean;
  onUpdate: (updates: Partial<ThreadPost>) => void;
  onRemove: () => void;
  onMove: (direction: 'up' | 'down') => void;
}

function ThreadPostCard({
  post,
  index,
  isFirst,
  isLast,
  onUpdate,
  onRemove,
  onMove
}: ThreadPostCardProps) {
  
  const handleContentChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    onUpdate({ content: e.target.value });
  };
  
  const handleMediaChange = (media: Media[]) => {
    onUpdate({ media });
  };
  
  return (
    <Card className="border shadow-sm transition-all rounded-lg overflow-hidden">
      <div className="px-4 py-2 bg-muted/30 border-b flex items-center justify-between">
        <div className="font-medium text-sm">Post {index + 1}</div>
        <div className="flex items-center gap-1">
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="h-7 w-7"
            onClick={() => onMove('up')}
            disabled={isFirst}
          >
            <ArrowUp size={14} />
          </Button>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="h-7 w-7"
            onClick={() => onMove('down')}
            disabled={isLast}
          >
            <ArrowDown size={14} />
          </Button>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="h-7 w-7 text-destructive hover:text-destructive hover:bg-destructive/10"
            onClick={onRemove}
          >
            <Trash size={14} />
          </Button>
        </div>
      </div>
      
      <CardContent className="p-4 space-y-4">
        <Textarea
          placeholder="Write your thread post..."
          value={post.content}
          onChange={handleContentChange}
          className="min-h-[100px] resize-y"
        />
        
        <MediaUploader
          media={post.media}
          onMediaChange={handleMediaChange}
          maxFiles={4}
        />
      </CardContent>
    </Card>
  );
}