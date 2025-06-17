import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { Account, SocialPlatform, getPlatformById } from '@/lib/platform-utils';

interface SelectedAccountsProps {
  platformId: SocialPlatform;
  accounts: Account[];
  selectedAccountIds: string[];
}

export function SelectedAccounts({ platformId, accounts, selectedAccountIds }: SelectedAccountsProps) {
  const platform = getPlatformById(platformId);
  const selectedAccounts = accounts.filter(account => selectedAccountIds.includes(account.id));
  
  if (!selectedAccounts.length) {
    return null;
  }
  
  return (
    <div className="flex -space-x-2 overflow-hidden">
      <TooltipProvider>
        {selectedAccounts.map((account, index) => (
          <Tooltip key={account.id}>
            <TooltipTrigger asChild>
              <Avatar className={`h-6 w-6 ring-2 ring-background ${index > 0 ? 'ml-[-8px]' : ''}`}>
                <AvatarImage src={account.avatar} />
                <AvatarFallback 
                  className="text-[10px]"
                  style={{ backgroundColor: platform.color }}
                >
                  {account.displayName.charAt(0)}
                </AvatarFallback>
              </Avatar>
            </TooltipTrigger>
            <TooltipContent side="bottom" className="text-xs">
              <p>{account.displayName}</p>
              <p className="text-muted-foreground">@{account.username}</p>
            </TooltipContent>
          </Tooltip>
        ))}
        {selectedAccounts.length > 3 && (
          <Avatar className="h-6 w-6 ring-2 ring-background ml-[-8px]">
            <AvatarFallback className="text-[10px] bg-muted">
              +{selectedAccounts.length - 3}
            </AvatarFallback>
          </Avatar>
        )}
      </TooltipProvider>
    </div>
  );
}