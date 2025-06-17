import { useState, useEffect } from 'react';
import { useKV } from '@github/spark/hooks';
import { AnimatePresence, motion } from 'framer-motion';
import { toast, Toaster } from 'sonner';
import { PlatformSelectors } from '@/components/platform-controls';
import { PostEditor } from '@/components/post-editor';
import { DEFAULT_HASHTAGS, PLATFORMS, SocialPlatform, Media, ThreadPost, getPlatformById, DEFAULT_ACCOUNTS, Account } from '@/lib/platform-utils';
import { Rocket, StackSimple, Atom, Gear, Question, BookOpen } from '@phosphor-icons/react';
import { CompactView } from '@/components/compact-view';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { AccountManager } from '@/components/account-manager';
import { 
  Sheet, 
  SheetContent, 
  SheetDescription, 
  SheetHeader, 
  SheetTitle, 
  SheetTrigger,
  SheetFooter,
  SheetClose
} from "@/components/ui/sheet";
import { Button } from "@/components/ui/button";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";

function App() {
  // State management with persistence
  const [content, setContent] = useKV('post-content', '');
  const [selectedPlatforms, setSelectedPlatforms] = useKV<SocialPlatform[]>('selected-platforms', []);
  const [hashtags, setHashtags] = useKV('hashtags', DEFAULT_HASHTAGS.slice(0, 5));
  const [promoMode, setPromoMode] = useKV('promo-mode', false);
  const [media, setMedia] = useKV<Media[]>('media', []);
  const [isThread, setIsThread] = useKV('is-thread', false);
  const [threadsOnlyMode, setThreadsOnlyMode] = useKV('threads-only-mode', false);
  const [threadPosts, setThreadPosts] = useKV<ThreadPost[]>('thread-posts', []);
  const [viewMode, setViewMode] = useKV('view-mode', 'standard');
  const [accounts, setAccounts] = useKV<Account[]>('platform-accounts', DEFAULT_ACCOUNTS);
  const [selectedAccounts, setSelectedAccounts] = useKV<Record<SocialPlatform, string[]>>('selected-accounts', {});
  const [isPosting, setIsPosting] = useState(false);
  const [showSuccess, setShowSuccess] = useState(false);
  const [activeTab, setActiveTab] = useState<string>("user-guide");

  // Initialize selected accounts with default accounts if not set
  useEffect(() => {
    const newSelectedAccounts = { ...selectedAccounts };
    let updated = false;

    selectedPlatforms.forEach(platformId => {
      if (!selectedAccounts[platformId] || selectedAccounts[platformId].length === 0) {
        const defaultAccount = accounts.find(a => a.platformId === platformId && a.isDefault);
        if (defaultAccount) {
          newSelectedAccounts[platformId] = [defaultAccount.id];
          updated = true;
        }
      }
    });

    if (updated) {
      setSelectedAccounts(newSelectedAccounts);
    }
  }, [selectedPlatforms, accounts, selectedAccounts, setSelectedAccounts]);

  const handlePlatformsChange = (platforms: SocialPlatform[]) => {
    // Filter out platforms that don't support threads when in threads-only mode
    if (threadsOnlyMode) {
      const threadSupportedPlatforms = platforms.filter(platformId => {
        const platform = getPlatformById(platformId);
        return platform.threadSupport;
      });
      
      if (threadSupportedPlatforms.length !== platforms.length) {
        toast.info('Platform compatibility', {
          description: 'Some selected platforms do not support threads and were removed in threads-only mode.'
        });
      }
      
      setSelectedPlatforms(threadSupportedPlatforms);
    } else {
      setSelectedPlatforms(platforms);
    }
  };
  
  // Effect to update selected platforms when entering threads-only mode
  const updatePlatformsForThreadMode = () => {
    if (threadsOnlyMode && selectedPlatforms.length > 0) {
      const filteredPlatforms = selectedPlatforms.filter(platformId => {
        const platform = getPlatformById(platformId);
        return platform.threadSupport;
      });
      
      if (filteredPlatforms.length !== selectedPlatforms.length) {
        toast.info('Platform compatibility', {
          description: 'Some previously selected platforms do not support threads and were removed.'
        });
        setSelectedPlatforms(filteredPlatforms);
      }
    }
  };

  const handlePost = () => {
    // Check for valid content (either main post or thread posts)
    const hasContent = content.trim().length > 0 || threadPosts.some(post => post.content.trim().length > 0);
    
    if (!hasContent || selectedPlatforms.length === 0) {
      toast.error('Missing content', {
        description: 'Please add content and select at least one platform before posting'
      });
      return;
    }
    
    // In threads-only mode, verify there's at least one thread post
    if (threadsOnlyMode && threadPosts.length === 0) {
      toast.error('Thread required', {
        description: 'You must add at least one thread post in threads-only mode'
      });
      return;
    }
    
    // Check if all selected platforms have at least one account selected
    const missingAccountPlatforms = selectedPlatforms.filter(
      platformId => !selectedAccounts[platformId] || selectedAccounts[platformId].length === 0
    );
    
    if (missingAccountPlatforms.length > 0) {
      const platformNames = missingAccountPlatforms.map(id => getPlatformById(id).name).join(', ');
      toast.error('Missing accounts', {
        description: `Please select at least one account for: ${platformNames}`
      });
      return;
    }
    
    setIsPosting(true);
    
    // Simulate posting with a delay
    setTimeout(() => {
      setIsPosting(false);
      setShowSuccess(true);
      
      // Reset form
      setContent('');
      setMedia([]);
      if (isThread || threadsOnlyMode) {
        setThreadPosts([]);
        if (!threadsOnlyMode) {
          setIsThread(false);
        }
      }
      
      // Show success toast
      const totalAccounts = Object.values(selectedAccounts)
        .reduce((count, accountIds) => count + accountIds.length, 0);
      
      toast.success('Post published successfully!', {
        description: `Published to ${selectedPlatforms.length} platform${selectedPlatforms.length > 1 ? 's' : ''} using ${totalAccounts} account${totalAccounts > 1 ? 's' : ''}`
      });
      
      // Hide success message after a delay
      setTimeout(() => setShowSuccess(false), 3000);
    }, 1500);
  };

  return (
    <div className="min-h-screen bg-background font-sans flex flex-col">
      <header className="bg-primary border-b border-primary/40 text-primary-foreground py-4 px-4 shadow-sm backdrop-blur-md supports-[backdrop-filter]:bg-primary/90">
        <div className="container mx-auto max-w-6xl">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="p-1.5 bg-primary-foreground/10 rounded-md">
                <Rocket size={24} weight="fill" className="text-primary-foreground" />
              </div>
              <h1 className="text-xl font-bold font-['Inter']">Social Media Management Hub</h1>
            </div>
            
            <div className="flex items-center gap-3">
              <TooltipProvider>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <Sheet>
                      <SheetTrigger asChild>
                        <Button variant="ghost" size="icon" className="text-primary-foreground/80 hover:text-primary-foreground hover:bg-primary-foreground/10">
                          <BookOpen size={20} weight="regular" />
                        </Button>
                      </SheetTrigger>
                      <SheetContent className="w-full sm:max-w-md overflow-y-auto" side="right">
                        <SheetHeader className="pb-4">
                          <SheetTitle>Documentation</SheetTitle>
                          <SheetDescription>
                            Learn how to use the Social Media Management Hub
                          </SheetDescription>
                        </SheetHeader>
                        
                        <Tabs defaultValue="user-guide" value={activeTab} onValueChange={setActiveTab} className="w-full">
                          <TabsList className="grid w-full grid-cols-2">
                            <TabsTrigger value="user-guide">User Guide</TabsTrigger>
                            <TabsTrigger value="technical">Technical Docs</TabsTrigger>
                          </TabsList>
                          
                          <TabsContent value="user-guide" className="mt-4 space-y-4">
                            <div className="prose prose-sm max-w-none">
                              <h3>Getting Started</h3>
                              <p>Welcome to the Social Media Management Hub! This guide will help you get started with the platform.</p>
                              
                              <h4>Creating Posts</h4>
                              <ol>
                                <li>Select your target platforms using the platform selectors</li>
                                <li>Choose which accounts to use for each platform</li>
                                <li>Write your post content in the editor</li>
                                <li>Add media files if desired</li>
                                <li>Toggle "Promo Mode" to automatically add hashtags</li>
                                <li>Click "Post" to publish to all selected platforms</li>
                              </ol>
                              
                              <h4>Creating Threads</h4>
                              <ol>
                                <li>Toggle "Thread Mode" in the post editor</li>
                                <li>Write your first post in the main editor</li>
                                <li>Click "Add Thread Post" to add additional posts to your thread</li>
                                <li>Add content to each thread post</li>
                                <li>Arrange posts in the desired order using the up/down controls</li>
                                <li>Click "Post Thread" to publish the entire thread</li>
                              </ol>
                              
                              <h4>Managing Multiple Accounts</h4>
                              <ol>
                                <li>Click the "Manage Accounts" button in the top toolbar</li>
                                <li>Add new accounts for any platform</li>
                                <li>Set a default account for each platform</li>
                                <li>When posting, select which accounts to use for each platform</li>
                              </ol>
                              
                              <h4>View Modes</h4>
                              <p>The application offers two main view modes:</p>
                              <ul>
                                <li><strong>Standard View:</strong> Full interface with separated sections</li>
                                <li><strong>Compact View:</strong> Split-screen interface with feeds and composer side by side</li>
                              </ul>
                            </div>
                          </TabsContent>
                          
                          <TabsContent value="technical" className="mt-4 space-y-4">
                            <div className="prose prose-sm max-w-none">
                              <h3>Technical Documentation</h3>
                              <p>This section provides technical details about the application architecture and implementation.</p>
                              
                              <h4>Application Architecture</h4>
                              <ul>
                                <li><strong>Framework:</strong> React with TypeScript</li>
                                <li><strong>State Management:</strong> React hooks with persistence via <code>useKV</code></li>
                                <li><strong>UI Components:</strong> Shadcn UI component library</li>
                                <li><strong>Animations:</strong> Framer Motion</li>
                                <li><strong>Icons:</strong> Phosphor Icons</li>
                                <li><strong>Notifications:</strong> Sonner toast library</li>
                              </ul>
                              
                              <h4>Key Components</h4>
                              <ul>
                                <li><strong>PostEditor:</strong> Unified interface for post and thread creation</li>
                                <li><strong>PlatformSelectors:</strong> Manages platform selection</li>
                                <li><strong>AccountManager:</strong> Handles account management</li>
                                <li><strong>MediaUploader:</strong> Handles media file uploads</li>
                                <li><strong>CompactView:</strong> Provides the split-screen interface</li>
                                <li><strong>SocialFeed:</strong> Displays platform feeds</li>
                              </ul>
                              
                              <h4>Data Structures</h4>
                              <p>Key data types include:</p>
                              <ul>
                                <li><strong>SocialPlatform:</strong> Platform identifier</li>
                                <li><strong>SocialPlatformConfig:</strong> Platform configuration</li>
                                <li><strong>Account:</strong> User account information</li>
                                <li><strong>Media:</strong> Media file information</li>
                                <li><strong>ThreadPost:</strong> Individual post in a thread</li>
                                <li><strong>Post:</strong> Complete post data</li>
                              </ul>
                              
                              <h4>Persistence</h4>
                              <p>The application uses the <code>useKV</code> hook to persist state across sessions, including:</p>
                              <ul>
                                <li>Post content and media</li>
                                <li>Selected platforms and accounts</li>
                                <li>Thread posts</li>
                                <li>Application preferences</li>
                              </ul>
                            </div>
                          </TabsContent>
                        </Tabs>
                        
                        <SheetFooter className="mt-6">
                          <SheetClose asChild>
                            <Button type="button">Close</Button>
                          </SheetClose>
                        </SheetFooter>
                      </SheetContent>
                    </Sheet>
                  </TooltipTrigger>
                  <TooltipContent side="bottom">
                    <p>Documentation</p>
                  </TooltipContent>
                </Tooltip>
              </TooltipProvider>
              
              <TooltipProvider>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <Button variant="ghost" size="icon" className="text-primary-foreground/80 hover:text-primary-foreground hover:bg-primary-foreground/10">
                      <Gear size={20} weight="regular" />
                    </Button>
                  </TooltipTrigger>
                  <TooltipContent side="bottom">
                    <p>Settings</p>
                  </TooltipContent>
                </Tooltip>
              </TooltipProvider>
              
              <AccountManager 
                platforms={PLATFORMS}
                selectedPlatforms={selectedPlatforms}
                selectedAccounts={selectedAccounts}
                onSelectedAccountsChange={setSelectedAccounts}
              />
            </div>
          </div>
          
          <div className="flex flex-col sm:flex-row items-start sm:items-center gap-4 mt-4">
            <Tabs value={viewMode} onValueChange={setViewMode} className="w-auto">
              <TabsList>
                <TabsTrigger value="standard">Standard View</TabsTrigger>
                <TabsTrigger value="compact" className="flex items-center gap-1">
                  <Atom size={16} />
                  <span>Compact View</span>
                </TabsTrigger>
              </TabsList>
            </Tabs>
            
            <div className="bg-primary-foreground/10 p-1 rounded-md">
              <button 
                className={`px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${!threadsOnlyMode ? 'bg-primary-foreground text-primary' : 'text-primary-foreground/80 hover:bg-primary-foreground/20'}`}
                onClick={() => setThreadsOnlyMode(false)}
              >
                Single/Thread Posts
              </button>
              <button 
                className={`px-3 py-1.5 rounded-md text-sm font-medium transition-colors flex items-center gap-1 ${threadsOnlyMode ? 'bg-primary-foreground text-primary' : 'text-primary-foreground/80 hover:bg-primary-foreground/20'}`}
                onClick={() => {
                  setThreadsOnlyMode(true);
                  setIsThread(true);
                  updatePlatformsForThreadMode();
                }}
              >
                <StackSimple size={16} />
                Threads Only
              </button>
            </div>
          </div>
        </div>
      </header>
      
      <main className="container mx-auto max-w-6xl py-6 px-4 flex-1 overflow-y-auto">
        <AnimatePresence>
          {showSuccess && (
            <motion.div 
              initial={{ opacity: 0, y: -10 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0 }}
              className="bg-accent/10 text-accent-foreground border border-accent/30 rounded-lg p-4 mb-6 flex items-center"
            >
              <div className="bg-accent text-accent-foreground p-2 rounded-full mr-3">
                <Rocket size={20} weight="fill" />
              </div>
              <div>
                <h3 className="font-medium">Post published successfully!</h3>
                <p className="text-sm opacity-80">Your content is now live on your selected platforms.
                  {(isThread || threadsOnlyMode) && threadPosts.length > 0 ? ` ${threadPosts.length + 1} posts published as a thread.` : ''}
                  {media.length > 0 ? ` ${media.length} media file${media.length > 1 ? 's' : ''} uploaded.` : ''}
                </p>
              </div>
            </motion.div>
          )}
        </AnimatePresence>
        
        {viewMode === 'standard' ? (
          <div className="grid gap-6 grid-cols-1">
            <section className="bg-card rounded-xl shadow-sm border p-6">
              <h2 className="text-lg font-semibold mb-5">Target Platforms</h2>
              <PlatformSelectors 
                platforms={threadsOnlyMode ? PLATFORMS.filter(p => p.threadSupport) : PLATFORMS} 
                selectedPlatforms={selectedPlatforms}
                onPlatformsChange={handlePlatformsChange}
                accounts={accounts}
                selectedAccounts={selectedAccounts}
              />
            </section>
            
            <section className="bg-card rounded-xl shadow-sm border p-5">
              <PostEditor 
                content={content}
                onContentChange={setContent}
                selectedPlatforms={selectedPlatforms}
                hashtags={hashtags}
                onHashtagsChange={setHashtags}
                promoMode={promoMode}
                onPromoModeChange={setPromoMode}
                onPost={handlePost}
                media={media}
                onMediaChange={setMedia}
                isThread={threadsOnlyMode || isThread}
                onThreadChange={threadsOnlyMode ? () => {} : setIsThread}
                threadPosts={threadPosts}
                onThreadPostsChange={setThreadPosts}
                threadsOnlyMode={threadsOnlyMode}
              />
            </section>
          </div>
        ) : (
          <section className="bg-card rounded-xl shadow-sm border p-5">
            <CompactView
              content={content}
              onContentChange={setContent}
              selectedPlatforms={selectedPlatforms}
              onPlatformsChange={handlePlatformsChange}
              hashtags={hashtags}
              onHashtagsChange={setHashtags}
              promoMode={promoMode}
              onPromoModeChange={setPromoMode}
              onPost={handlePost}
              media={media}
              onMediaChange={setMedia}
              isThread={threadsOnlyMode || isThread}
              onThreadChange={threadsOnlyMode ? () => {} : setIsThread}
              threadPosts={threadPosts}
              onThreadPostsChange={setThreadPosts}
              threadsOnlyMode={threadsOnlyMode}
            />
          </section>
        )}
      </main>
      
      <footer className="bg-muted/30 py-4 px-4 border-t">
        <div className="container mx-auto max-w-6xl flex justify-between items-center text-sm text-muted-foreground">
          <p>Social Media Management Hub — A unified platform for all your social media needs</p>
          <div className="flex items-center gap-2">
            <TooltipProvider>
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button variant="ghost" size="icon" className="h-8 w-8">
                    <Question size={16} />
                  </Button>
                </TooltipTrigger>
                <TooltipContent side="top">
                  <p>Help & Support</p>
                </TooltipContent>
              </Tooltip>
            </TooltipProvider>
          </div>
        </div>
      </footer>
      
      <Toaster position="top-right" richColors closeButton />
    </div>
  );
}

export default App;

