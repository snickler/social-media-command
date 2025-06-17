import { useCallback, useEffect, useMemo, useRef, useState } from 'react';

// Performance monitoring hook following Microsoft's telemetry patterns
export function usePerformance() {
  const performanceMarks = useRef<Map<string, number>>(new Map());
  
  const startTiming = useCallback((label: string) => {
    performanceMarks.current.set(label, performance.now());
    performance.mark(`${label}-start`);
  }, []);
  
  const endTiming = useCallback((label: string) => {
    const startTime = performanceMarks.current.get(label);
    if (startTime) {
      const duration = performance.now() - startTime;
      performance.mark(`${label}-end`);
      performance.measure(label, `${label}-start`, `${label}-end`);
      
      // Log performance metrics in development
      if (import.meta.env.DEV) {
        console.log(`Performance: ${label} took ${duration.toFixed(2)}ms`);
      }
      
      performanceMarks.current.delete(label);
      return duration;
    }
    return 0;
  }, []);
  
  const measureComponent = useCallback((componentName: string) => {
    startTiming(`component-${componentName}`);
    return () => endTiming(`component-${componentName}`);
  }, [startTiming, endTiming]);
  
  return { startTiming, endTiming, measureComponent };
}

// Debounced callback hook for performance optimization
export function useDebounce<T extends (...args: any[]) => any>(
  callback: T,
  delay: number
): T {
  const timeoutRef = useRef<NodeJS.Timeout | null>(null);
  
  return useCallback((...args: Parameters<T>) => {
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
    }
    
    timeoutRef.current = setTimeout(() => {
      callback(...args);
    }, delay);
  }, [callback, delay]) as T;
}

// Throttled callback hook for high-frequency events
export function useThrottle<T extends (...args: any[]) => any>(
  callback: T,
  delay: number
): T {
  const lastCallRef = useRef<number>(0);
  
  return useCallback((...args: Parameters<T>) => {
    const now = Date.now();
    if (now - lastCallRef.current >= delay) {
      lastCallRef.current = now;
      callback(...args);
    }
  }, [callback, delay]) as T;
}

// Memory-efficient array operations
export function useArrayOperations<T>() {
  return useMemo(() => ({
    addItem: (array: T[], item: T) => [...array, item],
    removeItem: (array: T[], index: number) => [
      ...array.slice(0, index),
      ...array.slice(index + 1)
    ],
    updateItem: (array: T[], index: number, updater: (item: T) => T) => [
      ...array.slice(0, index),
      updater(array[index]),
      ...array.slice(index + 1)
    ],
    moveItem: (array: T[], fromIndex: number, toIndex: number) => {
      const result = [...array];
      const [removed] = result.splice(fromIndex, 1);
      result.splice(toIndex, 0, removed);
      return result;
    }
  }), []);
}

// Virtual scrolling hook for large lists
export function useVirtualScroll<T>(
  items: T[],
  containerHeight: number,
  itemHeight: number
) {
  const [scrollTop, setScrollTop] = useState(0);
  
  const visibleRange = useMemo(() => {
    const start = Math.floor(scrollTop / itemHeight);
    const end = Math.min(
      start + Math.ceil(containerHeight / itemHeight) + 1,
      items.length
    );
    return { start, end };
  }, [scrollTop, itemHeight, containerHeight, items.length]);
  
  const visibleItems = useMemo(() => 
    items.slice(visibleRange.start, visibleRange.end)
  , [items, visibleRange]);
  
  const totalHeight = items.length * itemHeight;
  const offsetY = visibleRange.start * itemHeight;
  
  return {
    visibleItems,
    totalHeight,
    offsetY,
    setScrollTop,
    visibleRange
  };
} 