import { useState } from 'react';
import { Check, X } from '@phosphor-icons/react';
import { cn } from '@/lib/utils';
import { SocialPlatform, SocialPlatformConfig, Account } from '@/lib/platform-utils';
import { SelectedAccounts } from '@/components/selected-accounts';

interface PlatformSelectorProps {
  platform: SocialPlatformConfig;
  selected: boolean;
  onToggle: (id: SocialPlatform) => void;
  accounts?: Account[];
  selectedAccountIds?: string[];
}

export function PlatformSelector({ 
  platform, 
  selected, 
  onToggle, 
  accounts = [], 
  selectedAccountIds = []
}: PlatformSelectorProps) {
  return (
    <button
      onClick={() => onToggle(platform.id)}
      className={cn(
        "flex items-center gap-2 px-4 py-3 rounded-md transition-all",
        "border-2 hover:shadow-md",
        selected ? "border-primary bg-primary/10" : "border-border"
      )}
      aria-pressed={selected}
    >
      <div 
        className="w-4 h-4 rounded-full flex items-center justify-center"
        style={{ backgroundColor: platform.color }}
      >
        {selected && <Check size={12} weight="bold" className="text-white" />}
      </div>
      <span className="font-medium">{platform.name}</span>
      
      {selected && accounts.length > 0 && (
        <div className="ml-auto mr-2">
          <SelectedAccounts 
            platformId={platform.id}
            accounts={accounts}
            selectedAccountIds={selectedAccountIds}
          />
        </div>
      )}
      
      {platform.characterLimit && (
        <span className="ml-auto text-xs text-muted-foreground">
          {platform.characterLimit} chars
        </span>
      )}
    </button>
  );
}

interface PlatformSelectorsProps {
  selectedPlatforms: SocialPlatform[];
  onPlatformsChange: (platforms: SocialPlatform[]) => void;
  platforms: SocialPlatformConfig[];
  accounts?: Account[];
  selectedAccounts?: Record<SocialPlatform, string[]>;
}

export function PlatformSelectors({ 
  selectedPlatforms, 
  onPlatformsChange, 
  platforms,
  accounts = [],
  selectedAccounts = {}
}: PlatformSelectorsProps) {
  const togglePlatform = (id: SocialPlatform) => {
    if (selectedPlatforms.includes(id)) {
      onPlatformsChange(selectedPlatforms.filter(p => p !== id));
    } else {
      onPlatformsChange([...selectedPlatforms, id]);
    }
  };

  const getPlatformAccounts = (platformId: SocialPlatform) => {
    return accounts.filter(account => account.platformId === platformId);
  };

  const getPlatformSelectedAccountIds = (platformId: SocialPlatform) => {
    return selectedAccounts[platformId] || [];
  };

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-2">
      {platforms.map(platform => (
        <PlatformSelector
          key={platform.id}
          platform={platform}
          selected={selectedPlatforms.includes(platform.id)}
          onToggle={togglePlatform}
          accounts={getPlatformAccounts(platform.id)}
          selectedAccountIds={getPlatformSelectedAccountIds(platform.id)}
        />
      ))}
    </div>
  );
}

interface HashtagBadgeProps {
  tag: string;
  onRemove?: (tag: string) => void;
}

export function HashtagBadge({ tag, onRemove }: HashtagBadgeProps) {
  return (
    <div className="inline-flex items-center gap-1 bg-secondary/30 px-2 py-1 rounded-full text-sm">
      #{tag}
      {onRemove && (
        <button 
          onClick={() => onRemove(tag)}
          className="hover:bg-secondary/50 rounded-full p-0.5"
          aria-label={`Remove ${tag} hashtag`}
        >
          <X size={14} weight="bold" />
        </button>
      )}
    </div>
  );
}

interface HashtagInputProps {
  hashtags: string[];
  onHashtagsChange: (hashtags: string[]) => void;
}

export function HashtagInput({ hashtags, onHashtagsChange }: HashtagInputProps) {
  const [inputValue, setInputValue] = useState('');

  const addHashtag = () => {
    const tag = inputValue.trim().replace(/^#/, '').toLowerCase();
    if (tag && !hashtags.includes(tag)) {
      onHashtagsChange([...hashtags, tag]);
      setInputValue('');
    }
  };

  const removeHashtag = (tag: string) => {
    onHashtagsChange(hashtags.filter(t => t !== tag));
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      addHashtag();
    } else if (e.key === 'Backspace' && inputValue === '' && hashtags.length > 0) {
      onHashtagsChange(hashtags.slice(0, -1));
    }
  };

  return (
    <div className="space-y-2">
      <div className="flex items-center gap-2 flex-wrap mb-1">
        {hashtags.map(tag => (
          <HashtagBadge key={tag} tag={tag} onRemove={removeHashtag} />
        ))}
      </div>
      
      <div className="flex gap-2">
        <input
          type="text"
          value={inputValue}
          onChange={(e) => setInputValue(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Add hashtag..."
          className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
        />
        <button
          onClick={addHashtag}
          disabled={!inputValue.trim()}
          className="inline-flex items-center justify-center whitespace-nowrap rounded-md text-sm font-medium ring-offset-background transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50 bg-primary text-primary-foreground hover:bg-primary/90 h-10 px-4 py-2"
        >
          Add
        </button>
      </div>
    </div>
  );
}