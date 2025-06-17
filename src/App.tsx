import { useState } from 'react';
import { useKV } from '@github/spark/hooks';
import { AnimatePresence, motion } from 'framer-motion';
import { toast, Toaster } from 'sonner';
import { PlatformSelectors } from '@/components/platform-controls';
import { PostEditor } from '@/components/post-editor';
import { DEFAULT_HASHTAGS, PLATFORMS, SocialPlatform } from '@/lib/platform-utils';
import { Rocket } from '@phosphor-icons/react';

function App() {
  // State management with persistence
  const [content, setContent] = useKV('post-content', '');
  const [selectedPlatforms, setSelectedPlatforms] = useKV<SocialPlatform[]>('selected-platforms', []);
  const [hashtags, setHashtags] = useKV('hashtags', DEFAULT_HASHTAGS.slice(0, 5));
  const [promoMode, setPromoMode] = useKV('promo-mode', false);
  const [isPosting, setIsPosting] = useState(false);
  const [showSuccess, setShowSuccess] = useState(false);

  const handlePost = () => {
    if (!content.trim() || selectedPlatforms.length === 0) return;
    
    setIsPosting(true);
    
    // Simulate posting with a delay
    setTimeout(() => {
      setIsPosting(false);
      setShowSuccess(true);
      
      // Reset form
      setContent('');
      
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
                <p className="text-sm opacity-80">Your content is now live on your selected platforms.</p>
              </div>
            </motion.div>
          )}
        </AnimatePresence>
        
        <div className="grid gap-8 grid-cols-1">
          <section>
            <h2 className="text-xl font-semibold mb-4">Target Platforms</h2>
            <PlatformSelectors 
              platforms={PLATFORMS} 
              selectedPlatforms={selectedPlatforms}
              onPlatformsChange={setSelectedPlatforms}
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