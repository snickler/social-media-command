import { useRef, useState } from 'react';
import { Textarea } from '@/components/ui/textarea';
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { HashtagInput } from '@/components/platform-controls';
import { SocialPlatform, formatPostForPlatform, getPlatformById } from '@/lib/platform-utils';
import { Copy, PaperPlaneRight } from '@phosphor-icons/react';

interface PostEditorProps {
  content: string;
  onContentChange: (content: string) => void;
  selectedPlatforms: SocialPlatform[];
  hashtags: string[];
  onHashtagsChange: (hashtags: string[]) => void;
  promoMode: boolean;
  onPromoModeChange: (enabled: boolean) => void;
  onPost: () => void;
}

export function PostEditor({
  content,
  onContentChange,
  selectedPlatforms,
  hashtags,
  onHashtagsChange,
  promoMode,
  onPromoModeChange,
  onPost
}: PostEditorProps) {
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const [activeTab, setActiveTab] = useState<string>("editor");
  
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
  
  return (
    <Card className="w-full shadow-sm">
      <CardHeader className="pb-2">
        <div className="flex items-center justify-between">
          <CardTitle className="text-xl font-semibold text-primary">Compose Post</CardTitle>
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
      </CardHeader>
      
      <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
        <div className="px-6">
          <TabsList className="w-full grid grid-cols-2">
            <TabsTrigger value="editor">Editor</TabsTrigger>
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
                promoMode
              }, platform);
              
              return (
                <div key={platformId} className="border rounded-md p-3">
                  <div className="flex items-center justify-between mb-2">
                    <h3 className="font-medium" style={{ color: platform.color }}>
                      {platform.name} Preview
                    </h3>
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
                  </div>
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
          </div>
          <Button 
            onClick={onPost} 
            disabled={!content.trim() || !platformsSelected}
            className="bg-accent hover:bg-accent/90 text-accent-foreground"
          >
            <PaperPlaneRight size={18} weight="bold" className="mr-2" />
            Post Now
          </Button>
        </div>
      </CardFooter>
    </Card>
  );
}