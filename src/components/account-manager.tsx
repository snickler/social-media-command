import { useState } from 'react';
import { useKV } from '@github/spark/hooks';
import { Plus, X, Pencil, UserCircle, Check } from '@phosphor-icons/react';
import { 
  Account, 
  SocialPlatform, 
  SocialPlatformConfig, 
  DEFAULT_ACCOUNTS,
  getPlatformById
} from '@/lib/platform-utils';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { 
  Dialog, 
  DialogContent, 
  DialogDescription, 
  DialogFooter, 
  DialogHeader, 
  DialogTitle,
  DialogClose
} from '@/components/ui/dialog';
import { 
  Drawer, 
  DrawerClose, 
  DrawerContent, 
  DrawerDescription, 
  DrawerFooter, 
  DrawerHeader, 
  DrawerTitle 
} from '@/components/ui/drawer';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { ScrollArea } from '@/components/ui/scroll-area';
import { useMediaQuery } from '@/hooks/use-media-query';

interface AccountFormProps {
  account: Partial<Account>;
  platformId: SocialPlatform;
  onSave: (account: Account) => void;
  onCancel: () => void;
}

function AccountForm({ account, platformId, onSave, onCancel }: AccountFormProps) {
  const [formState, setFormState] = useState({
    username: account.username || '',
    displayName: account.displayName || '',
    avatar: account.avatar || `https://api.dicebear.com/7.x/personas/svg?seed=${platformId}-${Date.now()}`
  });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormState(prev => ({ ...prev, [name]: value }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!formState.username.trim() || !formState.displayName.trim()) {
      return;
    }
    
    onSave({
      id: account.id || `${platformId}-${Date.now()}`,
      platformId,
      username: formState.username,
      displayName: formState.displayName,
      avatar: formState.avatar,
      isDefault: account.isDefault || false
    });
  };

  const platform = getPlatformById(platformId);

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="space-y-2">
        <div className="flex items-start gap-4">
          <div>
            <Avatar className="h-14 w-14">
              <AvatarImage src={formState.avatar} />
              <AvatarFallback style={{ backgroundColor: platform.color }}>
                {formState.displayName.charAt(0) || platform.name.charAt(0)}
              </AvatarFallback>
            </Avatar>
          </div>
          <div className="flex-1">
            <Label htmlFor="displayName">Display Name</Label>
            <Input
              id="displayName"
              name="displayName"
              value={formState.displayName}
              onChange={handleChange}
              placeholder={`Your ${platform.name} Display Name`}
              required
            />
          </div>
        </div>
      </div>
      
      <div className="space-y-2">
        <Label htmlFor="username">Username</Label>
        <div className="flex items-center">
          <span className="text-muted-foreground mr-1">@</span>
          <Input
            id="username"
            name="username"
            value={formState.username}
            onChange={handleChange}
            placeholder={`Your ${platform.name} Username`}
            required
          />
        </div>
      </div>
      
      <div className="pt-2 flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit">
          Save Account
        </Button>
      </div>
    </form>
  );
}

interface AccountItemProps {
  account: Account;
  isSelected: boolean;
  onSelect: (account: Account) => void;
  onEdit: (account: Account) => void;
  onDelete: (accountId: string) => void;
}

function AccountItem({ account, isSelected, onSelect, onEdit, onDelete }: AccountItemProps) {
  const platform = getPlatformById(account.platformId);
  
  return (
    <div 
      className={`flex items-center justify-between p-3 border rounded-md transition-colors ${
        isSelected ? 'border-primary bg-primary/5' : 'border-border hover:bg-muted/20'
      }`}
    >
      <div className="flex items-center gap-3 flex-1">
        <button 
          className={`w-5 h-5 rounded-full border flex items-center justify-center ${
            isSelected ? 'border-primary bg-primary text-primary-foreground' : 'border-muted-foreground'
          }`}
          onClick={() => onSelect(account)}
        >
          {isSelected && <Check size={12} weight="bold" />}
        </button>
        
        <Avatar>
          <AvatarImage src={account.avatar} />
          <AvatarFallback style={{ backgroundColor: platform.color }}>
            {account.displayName.charAt(0)}
          </AvatarFallback>
        </Avatar>
        
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <span className="font-medium truncate">{account.displayName}</span>
            {account.isDefault && (
              <Badge variant="outline" className="text-xs h-5">Default</Badge>
            )}
          </div>
          <div className="text-sm text-muted-foreground truncate">@{account.username}</div>
        </div>
      </div>
      
      <div className="flex items-center gap-1">
        <Button 
          variant="ghost" 
          size="icon" 
          className="h-8 w-8"
          onClick={() => onEdit(account)}
        >
          <Pencil size={16} />
        </Button>
        {!account.isDefault && (
          <Button 
            variant="ghost" 
            size="icon" 
            className="h-8 w-8 text-destructive hover:text-destructive/90"
            onClick={() => onDelete(account.id)}
          >
            <X size={16} />
          </Button>
        )}
      </div>
    </div>
  );
}

interface PlatformAccountsProps {
  platformId: SocialPlatform;
  accounts: Account[];
  selectedAccountIds: string[];
  onAccountsChange: (accountIds: string[]) => void;
  onAddAccount: (platformId: SocialPlatform) => void;
  onEditAccount: (account: Account) => void;
  onDeleteAccount: (accountId: string) => void;
}

function PlatformAccounts({ 
  platformId, 
  accounts, 
  selectedAccountIds,
  onAccountsChange,
  onAddAccount,
  onEditAccount,
  onDeleteAccount
}: PlatformAccountsProps) {
  const platform = getPlatformById(platformId);
  
  const toggleAccount = (account: Account) => {
    if (selectedAccountIds.includes(account.id)) {
      // If this is the only account or it's the default account, don't allow deselection
      if (selectedAccountIds.length === 1 || account.isDefault) {
        return;
      }
      onAccountsChange(selectedAccountIds.filter(id => id !== account.id));
    } else {
      onAccountsChange([...selectedAccountIds, account.id]);
    }
  };
  
  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <div 
            className="w-4 h-4 rounded-full" 
            style={{ backgroundColor: platform.color }}
          />
          <h3 className="font-medium">{platform.name}</h3>
        </div>
        <Button 
          variant="outline" 
          size="sm" 
          onClick={() => onAddAccount(platformId)}
          className="h-8"
        >
          <Plus size={14} className="mr-1" />
          Add Account
        </Button>
      </div>
      
      <div className="space-y-2">
        {accounts.length === 0 ? (
          <div className="text-center py-4 text-muted-foreground text-sm">
            No accounts configured
          </div>
        ) : (
          accounts.map(account => (
            <AccountItem 
              key={account.id}
              account={account}
              isSelected={selectedAccountIds.includes(account.id)}
              onSelect={toggleAccount}
              onEdit={onEditAccount}
              onDelete={onDeleteAccount}
            />
          ))
        )}
      </div>
    </div>
  );
}

interface AccountManagerProps {
  platforms: SocialPlatformConfig[];
  selectedPlatforms: SocialPlatform[];
  selectedAccounts: Record<SocialPlatform, string[]>;
  onSelectedAccountsChange: (accounts: Record<SocialPlatform, string[]>) => void;
}

export function AccountManager({ 
  platforms, 
  selectedPlatforms, 
  selectedAccounts,
  onSelectedAccountsChange
}: AccountManagerProps) {
  const [accounts, setAccounts] = useKV<Account[]>('platform-accounts', DEFAULT_ACCOUNTS);
  const [open, setOpen] = useState(false);
  const [editingAccount, setEditingAccount] = useState<Partial<Account> | null>(null);
  const [addingPlatformId, setAddingPlatformId] = useState<SocialPlatform | null>(null);
  const isDesktop = useMediaQuery("(min-width: 768px)");
  
  const getAccountsForPlatform = (platformId: SocialPlatform) => {
    return accounts.filter(account => account.platformId === platformId);
  };
  
  const getSelectedAccountsForPlatform = (platformId: SocialPlatform) => {
    return selectedAccounts[platformId] || 
      accounts
        .filter(account => account.platformId === platformId && account.isDefault)
        .map(account => account.id);
  };
  
  const handleSelectedAccountsChange = (platformId: SocialPlatform, accountIds: string[]) => {
    const newSelectedAccounts = { ...selectedAccounts };
    newSelectedAccounts[platformId] = accountIds;
    onSelectedAccountsChange(newSelectedAccounts);
  };
  
  const handleAddAccount = (platformId: SocialPlatform) => {
    setAddingPlatformId(platformId);
    setEditingAccount({});
  };
  
  const handleEditAccount = (account: Account) => {
    setAddingPlatformId(account.platformId);
    setEditingAccount(account);
  };
  
  const handleDeleteAccount = (accountId: string) => {
    // Remove the account
    setAccounts(accounts.filter(account => account.id !== accountId));
    
    // Remove the account from selected accounts
    const newSelectedAccounts = { ...selectedAccounts };
    Object.keys(newSelectedAccounts).forEach(platformId => {
      const platform = platformId as SocialPlatform;
      if (newSelectedAccounts[platform].includes(accountId)) {
        newSelectedAccounts[platform] = newSelectedAccounts[platform].filter(id => id !== accountId);
        
        // If we removed all accounts, add the default one
        if (newSelectedAccounts[platform].length === 0) {
          const defaultAccount = accounts.find(a => a.platformId === platform && a.isDefault);
          if (defaultAccount) {
            newSelectedAccounts[platform] = [defaultAccount.id];
          }
        }
      }
    });
    
    onSelectedAccountsChange(newSelectedAccounts);
  };
  
  const handleSaveAccount = (account: Account) => {
    // Check if we're updating or adding
    const isUpdating = accounts.some(a => a.id === account.id);
    
    if (isUpdating) {
      setAccounts(accounts.map(a => a.id === account.id ? account : a));
    } else {
      setAccounts([...accounts, account]);
      
      // Add this new account to the selected accounts for its platform
      const platformAccounts = selectedAccounts[account.platformId] || [];
      handleSelectedAccountsChange(account.platformId, [...platformAccounts, account.id]);
    }
    
    setEditingAccount(null);
    setAddingPlatformId(null);
  };
  
  const accountForm = (
    <AccountForm 
      account={editingAccount || {}}
      platformId={addingPlatformId!}
      onSave={handleSaveAccount}
      onCancel={() => {
        setEditingAccount(null);
        setAddingPlatformId(null);
      }}
    />
  );
  
  return (
    <>
      <Button 
        variant="outline" 
        onClick={() => setOpen(true)}
        className="flex items-center gap-2 text-base"
      >
        <UserCircle size={18} />
        Manage Accounts
      </Button>
      
      {isDesktop ? (
        <Dialog open={open} onOpenChange={setOpen}>
          <DialogContent className="max-w-2xl">
            <DialogHeader>
              <DialogTitle>Manage Social Media Accounts</DialogTitle>
              <DialogDescription>
                Configure multiple accounts for each platform. You can select which accounts to use for posting.
              </DialogDescription>
            </DialogHeader>
            
            {addingPlatformId ? (
              <div className="py-2">
                <h3 className="font-medium mb-4">
                  {editingAccount?.id ? 'Edit Account' : 'Add New Account'}
                </h3>
                {accountForm}
              </div>
            ) : (
              <ScrollArea className="h-[60vh] pr-4">
                <div className="space-y-6">
                  {platforms
                    .filter(platform => selectedPlatforms.includes(platform.id))
                    .map(platform => (
                      <PlatformAccounts
                        key={platform.id}
                        platformId={platform.id}
                        accounts={getAccountsForPlatform(platform.id)}
                        selectedAccountIds={getSelectedAccountsForPlatform(platform.id)}
                        onAccountsChange={(accountIds) => handleSelectedAccountsChange(platform.id, accountIds)}
                        onAddAccount={handleAddAccount}
                        onEditAccount={handleEditAccount}
                        onDeleteAccount={handleDeleteAccount}
                      />
                    ))}
                </div>
              </ScrollArea>
            )}
            
            {!addingPlatformId && (
              <DialogFooter>
                <DialogClose asChild>
                  <Button>Done</Button>
                </DialogClose>
              </DialogFooter>
            )}
          </DialogContent>
        </Dialog>
      ) : (
        <Drawer open={open} onOpenChange={setOpen}>
          <DrawerContent>
            <DrawerHeader>
              <DrawerTitle>Manage Social Media Accounts</DrawerTitle>
              <DrawerDescription>
                Configure multiple accounts for each platform
              </DrawerDescription>
            </DrawerHeader>
            
            {addingPlatformId ? (
              <div className="px-4 py-2">
                <h3 className="font-medium mb-4">
                  {editingAccount?.id ? 'Edit Account' : 'Add New Account'}
                </h3>
                {accountForm}
              </div>
            ) : (
              <div className="px-4 py-2 overflow-y-auto max-h-[70vh]">
                <div className="space-y-6">
                  {platforms
                    .filter(platform => selectedPlatforms.includes(platform.id))
                    .map(platform => (
                      <PlatformAccounts
                        key={platform.id}
                        platformId={platform.id}
                        accounts={getAccountsForPlatform(platform.id)}
                        selectedAccountIds={getSelectedAccountsForPlatform(platform.id)}
                        onAccountsChange={(accountIds) => handleSelectedAccountsChange(platform.id, accountIds)}
                        onAddAccount={handleAddAccount}
                        onEditAccount={handleEditAccount}
                        onDeleteAccount={handleDeleteAccount}
                      />
                    ))}
                </div>
              </div>
            )}
            
            {!addingPlatformId && (
              <DrawerFooter>
                <DrawerClose asChild>
                  <Button>Done</Button>
                </DrawerClose>
              </DrawerFooter>
            )}
          </DrawerContent>
        </Drawer>
      )}
    </>
  );
}