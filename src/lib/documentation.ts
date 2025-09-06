import { marked } from 'marked';

export interface DocumentationFile {
  id: string;
  title: string;
  category: string;
  path: string;
  content?: string;
}

export const DOCUMENTATION_FILES: DocumentationFile[] = [
  // User Guides
  {
    id: 'user-guide',
    title: 'User Guide',
    category: 'User Guides',
    path: '/docs/user-guides/user-guide.md'
  },
  {
    id: 'getting-started',
    title: 'Getting Started',
    category: 'User Guides', 
    path: '/docs/user-guides/getting-started.md'
  },
  
  // Technical Documentation
  {
    id: 'technical-documentation',
    title: 'Technical Documentation',
    category: 'Technical',
    path: '/docs/technical/technical-documentation.md'
  },
  {
    id: 'implementation-summary',
    title: 'Implementation Summary',
    category: 'Technical',
    path: '/docs/technical/implementation-summary.md'
  },
  {
    id: 'performance-optimizations',
    title: 'Performance Optimizations',
    category: 'Technical',
    path: '/docs/technical/performance-optimizations.md'
  },
  
  // Security Documentation
  {
    id: 'security-overview',
    title: 'Security Overview',
    category: 'Security',
    path: '/docs/security/security-overview.md'
  },
  {
    id: 'secure-storage-implementation',
    title: 'Secure Storage Implementation',
    category: 'Security',
    path: '/docs/security/secure-storage-implementation.md'
  },
  
  // Development Documentation
  {
    id: 'enhanced-features',
    title: 'Enhanced Features',
    category: 'Development',
    path: '/docs/development/enhanced-features.md'
  },
  {
    id: 'enhanced-features-final',
    title: 'Enhanced Features Final',
    category: 'Development',
    path: '/docs/development/enhanced-features-final.md'
  },
  {
    id: 'oauth-config-demo',
    title: 'OAuth Configuration Demo',
    category: 'Development',
    path: '/docs/development/oauth-config-demo.md'
  },
  {
    id: 'prd',
    title: 'Product Requirements Document',
    category: 'Development',
    path: '/docs/development/prd.md'
  },
  
  // Operations Documentation
  {
    id: 'log-analysis-fixes',
    title: 'Log Analysis Fixes',
    category: 'Operations',
    path: '/docs/operations/log-analysis-fixes.md'
  },
  {
    id: 'logging-implementation',
    title: 'Logging Implementation',
    category: 'Operations',
    path: '/docs/operations/logging-implementation.md'
  }
];

export const DOCUMENTATION_CATEGORIES = [
  'User Guides',
  'Technical',
  'Security',
  'Development',
  'Operations'
];

/**
 * Load markdown content from a file path
 */
export async function loadMarkdownFile(path: string): Promise<string> {
  try {
    const response = await fetch(path);
    if (!response.ok) {
      throw new Error(`Failed to load ${path}: ${response.statusText}`);
    }
    return await response.text();
  } catch (error) {
    console.error('Error loading markdown file:', error);
    return `# Error Loading Documentation\n\nFailed to load documentation from ${path}.\n\nPlease check that the file exists and is accessible.`;
  }
}

/**
 * Parse markdown content to HTML
 */
export function parseMarkdown(content: string): string {
  try {
    return marked(content);
  } catch (error) {
    console.error('Error parsing markdown:', error);
    return `<div class="error">Error parsing markdown content</div>`;
  }
}

/**
 * Get documentation file by ID
 */
export function getDocumentationFile(id: string): DocumentationFile | undefined {
  return DOCUMENTATION_FILES.find(file => file.id === id);
}

/**
 * Get documentation files by category
 */
export function getDocumentationByCategory(category: string): DocumentationFile[] {
  return DOCUMENTATION_FILES.filter(file => file.category === category);
}

/**
 * Load and parse documentation file
 */
export async function loadAndParseDocumentation(id: string): Promise<string> {
  const file = getDocumentationFile(id);
  if (!file) {
    return `<div class="error">Documentation file not found: ${id}</div>`;
  }
  
  const content = await loadMarkdownFile(file.path);
  return parseMarkdown(content);
}