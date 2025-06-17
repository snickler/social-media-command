import { useState } from 'react';
import { useKV } from '@github/spark/hooks';
import { AnimatePresence, motion } from 'framer-motion';
import { toast, Toaster } from 'sonner';
import { PlatformSelectors } from '@/components/platform-controls';
import { PostEditor } from '@/components/post-editor';
import { DEFAULT_HASHTAGS, PLATFORMS, SocialPlatform, Media, ThreadPost, getPlatformById } from '@/lib/platform-utils';
import { Rocket, StackSimple } from '@phosphor-icons/react';

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
  const [isPosting, setIsPosting] = useState(false);
  const [showSuccess, setShowSuccess] = useState(false);

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
    if (threadsOnlyMode && threadPosts.length === 0) {
      toast.error('Thread required', {
        description: 'You must add at least one thread post in threads-only mode'
      });
      return;
    }
    
    if (!content.trim() || selectedPlatforms.length === 0) return;
    
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
      toast.success('Post published successfully!', {
        description: `Published to ${selectedPlatforms.length} platform${selectedPlatforms.length > 1 ? 's' : ''}`
      });
      
      // Hide success message after a delay
      setTimeout(() => setShowSuccess(false), 3000);
    }, 1500);
  };

  return (
    <div className="min-h-screen bg-background font-sans">
      <header className="bg-primary text-primary-foreground py-6 px-4 shadow-md">
        <div className="container mx-auto max-w-6xl">
          <div className="flex items-center gap-3">
            <Rocket size={28} weight="fill" />
            <h1 className="text-2xl font-bold font-['Inter']">Social Media Management Hub</h1>
          </div>
          <p className="text-primary-foreground/80 mt-1">
            Create and manage posts across multiple platforms with ease
          </p>
        </div>
      </header>
      
      <main className="container mx-auto max-w-6xl py-8 px-4">
        <AnimatePresence>
          {showSuccess && (
            <motion.div 
              initial={{ opacity: 0, y: -10 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0 }}
              className="bg-accent/20 text-accent-foreground border border-accent rounded-lg p-4 mb-6 flex items-center"
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
        
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-xl font-semibold">Mode Selection</h2>
          <div className="flex items-center gap-2 bg-muted/30 p-2 rounded-md">
            <button 
              className={`px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${!threadsOnlyMode ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-muted/50'}`}
              onClick={() => setThreadsOnlyMode(false)}
            >
              Single/Thread Posts
            </button>
            <button 
              className={`px-3 py-1.5 rounded-md text-sm font-medium transition-colors flex items-center gap-1 ${threadsOnlyMode ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-muted/50'}`}
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
        
        <div className="grid gap-8 grid-cols-1">
          <section>
            <h2 className="text-xl font-semibold mb-4">Target Platforms</h2>
            <PlatformSelectors 
              platforms={threadsOnlyMode ? PLATFORMS.filter(p => p.threadSupport) : PLATFORMS} 
              selectedPlatforms={selectedPlatforms}
              onPlatformsChange={handlePlatformsChange}
            />
          </section>
          
          <section>
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
      </main>
      
      <footer className="bg-muted/30 py-6 px-4 border-t">
        <div className="container mx-auto max-w-6xl text-center text-sm text-muted-foreground">
          <p>Social Media Management Hub — A unified platform for all your social media needs</p>
        </div>
      </footer>
      
      <Toaster position="top-right" richColors />
    </div>
  );
}

export default App;
