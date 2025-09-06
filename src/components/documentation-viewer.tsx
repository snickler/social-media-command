import React, { useState, useEffect } from 'react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { ScrollArea } from "@/components/ui/scroll-area";
import { 
  DOCUMENTATION_FILES, 
  DOCUMENTATION_CATEGORIES, 
  loadAndParseDocumentation,
  getDocumentationByCategory 
} from '@/lib/documentation';

interface DocumentationViewerProps {
  defaultTab?: string;
  onTabChange?: (tab: string) => void;
  activeTab?: string;
}

export function DocumentationViewer({ 
  defaultTab = 'user-guide', 
  onTabChange, 
  activeTab 
}: DocumentationViewerProps) {
  const [currentTab, setCurrentTab] = useState(activeTab || defaultTab);
  const [documentContent, setDocumentContent] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState<Record<string, boolean>>({});

  // Load documentation content when tab changes
  useEffect(() => {
    const loadDocumentation = async (docId: string) => {
      if (documentContent[docId]) {
        return; // Already loaded
      }

      setLoading(prev => ({ ...prev, [docId]: true }));
      
      try {
        const content = await loadAndParseDocumentation(docId);
        setDocumentContent(prev => ({ ...prev, [docId]: content }));
      } catch (error) {
        console.error('Failed to load documentation:', error);
        setDocumentContent(prev => ({ 
          ...prev, 
          [docId]: '<div class="error">Failed to load documentation</div>' 
        }));
      } finally {
        setLoading(prev => ({ ...prev, [docId]: false }));
      }
    };

    // Load current tab content
    loadDocumentation(currentTab);
  }, [currentTab, documentContent]);

  const handleTabChange = (tab: string) => {
    setCurrentTab(tab);
    onTabChange?.(tab);
  };

  const renderCategoryTabs = () => {
    const categorizedDocs = DOCUMENTATION_CATEGORIES.map(category => ({
      category,
      docs: getDocumentationByCategory(category)
    })).filter(item => item.docs.length > 0);

    return (
      <Tabs defaultValue={categorizedDocs[0]?.category} className="w-full">
        <TabsList className="grid w-full grid-cols-2 lg:grid-cols-3">
          {categorizedDocs.slice(0, 3).map(({ category }) => (
            <TabsTrigger 
              key={category} 
              value={category}
              className="text-xs"
            >
              {category}
            </TabsTrigger>
          ))}
        </TabsList>
        
        {categorizedDocs.map(({ category, docs }) => (
          <TabsContent key={category} value={category} className="mt-4">
            <Tabs 
              value={currentTab} 
              onValueChange={handleTabChange} 
              className="w-full"
            >
              <TabsList className="grid w-full" style={{ gridTemplateColumns: `repeat(${Math.min(docs.length, 4)}, 1fr)` }}>
                {docs.slice(0, 4).map(doc => (
                  <TabsTrigger 
                    key={doc.id} 
                    value={doc.id}
                    className="text-xs"
                  >
                    {doc.title.length > 15 ? `${doc.title.substring(0, 15)}...` : doc.title}
                  </TabsTrigger>
                ))}
              </TabsList>
              
              {docs.map(doc => (
                <TabsContent key={doc.id} value={doc.id} className="mt-4">
                  <ScrollArea className="h-[500px] w-full">
                    <div className="pr-4">
                      {loading[doc.id] ? (
                        <div className="flex items-center justify-center py-8">
                          <div className="text-sm text-muted-foreground">Loading documentation...</div>
                        </div>
                      ) : (
                        <div 
                          className="prose prose-sm max-w-none prose-headings:text-foreground prose-p:text-foreground prose-li:text-foreground prose-strong:text-foreground"
                          dangerouslySetInnerHTML={{ 
                            __html: documentContent[doc.id] || '<div>No content available</div>' 
                          }}
                        />
                      )}
                    </div>
                  </ScrollArea>
                </TabsContent>
              ))}
            </Tabs>
          </TabsContent>
        ))}
      </Tabs>
    );
  };

  return renderCategoryTabs();
}