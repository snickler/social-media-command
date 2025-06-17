import { useRef, useState } from 'react';
import { Button } from '@/components/ui/button';
import { 
  Image as ImageIcon, 
  VideoCamera, 
  X, 
  FileX,
  UploadSimple
} from '@phosphor-icons/react';
import { Media } from '@/lib/platform-utils';
import { cn } from '@/lib/utils';

interface MediaUploaderProps {
  media: Media[];
  onMediaChange: (media: Media[]) => void;
  maxFiles?: number;
}

export function MediaUploader({ 
  media, 
  onMediaChange,
  maxFiles = 4
}: MediaUploaderProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isDragging, setIsDragging] = useState(false);
  
  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!e.target.files?.length) return;
    
    const selectedFiles = Array.from(e.target.files);
    const newMedia: Media[] = [];
    
    selectedFiles.forEach(file => {
      // Check if we've reached the maximum
      if (media.length + newMedia.length >= maxFiles) return;
      
      // Only allow images and videos
      if (!file.type.startsWith('image/') && !file.type.startsWith('video/')) {
        return;
      }
      
      const type = file.type.startsWith('image/') ? 'image' : 'video';
      const previewUrl = URL.createObjectURL(file);
      
      newMedia.push({
        id: `media-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
        file,
        previewUrl,
        type
      });
    });
    
    onMediaChange([...media, ...newMedia]);
    
    // Reset the input
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };
  
  const handleRemoveMedia = (id: string) => {
    const updatedMedia = media.filter(item => item.id !== id);
    
    // Revoke object URL to avoid memory leaks
    const removedItem = media.find(item => item.id === id);
    if (removedItem) {
      URL.revokeObjectURL(removedItem.previewUrl);
    }
    
    onMediaChange(updatedMedia);
  };
  
  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(true);
  };
  
  const handleDragLeave = () => {
    setIsDragging(false);
  };
  
  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    
    if (!e.dataTransfer.files?.length) return;
    
    // Create a fake event object to reuse our existing handler
    const event = {
      target: {
        files: e.dataTransfer.files
      }
    } as unknown as React.ChangeEvent<HTMLInputElement>;
    
    handleFileSelect(event);
  };
  
  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-sm font-medium">Media</h3>
          <p className="text-xs text-muted-foreground">
            Add up to {maxFiles} images or videos to your post
          </p>
        </div>
        
        <div className="flex items-center gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => fileInputRef.current?.click()}
            disabled={media.length >= maxFiles}
          >
            <UploadSimple size={16} className="mr-1" />
            Upload
          </Button>
        </div>
      </div>
      
      {/* Hidden file input */}
      <input
        ref={fileInputRef}
        type="file"
        accept="image/*,video/*"
        multiple
        className="hidden"
        onChange={handleFileSelect}
      />
      
      {/* Drag and drop area / Previews */}
      {media.length === 0 ? (
        <div 
          className={cn(
            "border-2 border-dashed rounded-md p-6 flex flex-col items-center justify-center gap-2",
            "text-muted-foreground transition-colors",
            isDragging ? "border-primary bg-primary/5" : "border-border"
          )}
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          onDrop={handleDrop}
          onClick={() => fileInputRef.current?.click()}
        >
          <div className="flex gap-2">
            <ImageIcon size={24} />
            <VideoCamera size={24} />
          </div>
          <p>Drag and drop media here or click to browse</p>
          <p className="text-xs opacity-70">Supports images and videos</p>
        </div>
      ) : (
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
          {media.map(item => (
            <div 
              key={item.id} 
              className="relative aspect-square rounded-md overflow-hidden border group"
            >
              {item.type === 'image' ? (
                <img 
                  src={item.previewUrl} 
                  alt="Media preview" 
                  className="w-full h-full object-cover"
                />
              ) : (
                <div className="w-full h-full bg-muted/50 flex items-center justify-center">
                  <video 
                    src={item.previewUrl} 
                    className="w-full h-full object-cover"
                    controls
                  />
                </div>
              )}
              
              <button
                type="button"
                onClick={() => handleRemoveMedia(item.id)}
                className="absolute top-1 right-1 bg-destructive text-destructive-foreground rounded-full p-1 opacity-0 group-hover:opacity-100 transition-opacity"
                aria-label="Remove media"
              >
                <X size={14} weight="bold" />
              </button>
              
              <div className="absolute bottom-1 left-1 bg-black/60 text-white text-xs rounded px-1.5 py-0.5 flex items-center gap-1">
                {item.type === 'image' ? (
                  <ImageIcon size={12} />
                ) : (
                  <VideoCamera size={12} />
                )}
                {item.file.name.split('.').pop()?.toUpperCase()}
              </div>
            </div>
          ))}
          
          {media.length < maxFiles && (
            <button
              type="button"
              onClick={() => fileInputRef.current?.click()}
              className="border-2 border-dashed rounded-md aspect-square flex flex-col items-center justify-center gap-1 text-muted-foreground hover:border-primary hover:text-primary transition-colors"
            >
              <UploadSimple size={24} />
              <span className="text-xs">Add more</span>
            </button>
          )}
        </div>
      )}
    </div>
  );
}