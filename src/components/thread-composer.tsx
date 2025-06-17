import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { 
  Textarea 
} from '@/components/ui/textarea';
import { Card, CardContent } from '@/components/ui/card';
import { MediaUploader } from '@/components/media-uploader';
import { ThreadPost, Media } from '@/lib/platform-utils';
import { 
  Plus, 
  X, 
  ArrowUp, 
  ArrowDown,
  Trash
} from '@phosphor-icons/react';
import { cn } from '@/lib/utils';

interface ThreadComposerProps {
  threadPosts: ThreadPost[];
  onThreadPostsChange: (posts: ThreadPost[]) => void;
  disabled?: boolean;
}

export function ThreadComposer({ 
  threadPosts, 
  onThreadPostsChange,
  disabled = false
}: ThreadComposerProps) {
  
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
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-base font-medium">Thread Posts</h3>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={addThreadPost}
          disabled={disabled}
        >
          <Plus size={16} className="mr-1" />
          Add Post
        </Button>
      </div>
      
      {threadPosts.length === 0 ? (
        <div className="text-center py-6 border border-dashed rounded-md text-muted-foreground">
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
              disabled={disabled}
            />
          ))}
        </div>
      )}
    </div>
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
  disabled?: boolean;
}

function ThreadPostCard({
  post,
  index,
  isFirst,
  isLast,
  onUpdate,
  onRemove,
  onMove,
  disabled = false
}: ThreadPostCardProps) {
  
  const handleContentChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    onUpdate({ content: e.target.value });
  };
  
  const handleMediaChange = (media: Media[]) => {
    onUpdate({ media });
  };
  
  return (
    <Card className={cn(
      "border shadow-sm transition-all",
      disabled && "opacity-70 pointer-events-none"
    )}>
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