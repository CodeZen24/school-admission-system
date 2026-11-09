import { useEffect, useRef, useCallback } from 'react';

/**
 * Periodically invokes the callback while the browser tab is active/visible.
 *
 * @param callback The async function to call on each tick.
 * @param intervalMs The polling interval in milliseconds.
 * @param enabled Whether polling is active.
 */
export function usePolling(
  callback: () => void | Promise<void>,
  intervalMs = 4000,
  enabled = true,
) {
  const savedCallback = useRef(callback);

  useEffect(() => {
    savedCallback.current = callback;
  }, [callback]);

  const pollNow = useCallback(() => {
    try {
      void savedCallback.current();
    } catch {
    }
  }, []);

  useEffect(() => {
    if (!enabled) {
      return;
    }

    const tick = () => {
      if (typeof document !== 'undefined' && document.visibilityState === 'hidden') {
        return;
      }
      pollNow();
    };

    const timer = setInterval(tick, intervalMs);

    const handleVisibilityChange = () => {
      if (document.visibilityState === 'visible') {
        tick();
      }
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);

    return () => {
      clearInterval(timer);
      document.removeEventListener('visibilitychange', handleVisibilityChange);
    };
  }, [intervalMs, enabled, pollNow]);

  return { pollNow };
}
