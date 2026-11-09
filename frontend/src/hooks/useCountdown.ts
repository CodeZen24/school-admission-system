import { useState, useEffect } from 'react';

export interface CountdownState {
  remainingSeconds: number;
  formatted: string;
  isExpired: boolean;
}

function calculateRemainingSeconds(expiresAtIso: string): number {
  if (!expiresAtIso) {
    return 0;
  }
  const expiry = new Date(expiresAtIso).getTime();
  const now = Date.now();
  const diffMs = expiry - now;
  return Math.max(0, Math.floor(diffMs / 1000));
}

function formatDuration(totalSeconds: number): string {
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  const paddedMinutes = String(minutes).padStart(2, '0');
  const paddedSeconds = String(seconds).padStart(2, '0');
  return `${paddedMinutes}:${paddedSeconds}`;
}

export function useCountdown(expiresAtIso: string): CountdownState {
  const [remainingSeconds, setRemainingSeconds] = useState<number>(() =>
    calculateRemainingSeconds(expiresAtIso),
  );

  useEffect(() => {
    setRemainingSeconds(calculateRemainingSeconds(expiresAtIso));

    const interval = setInterval(() => {
      const remaining = calculateRemainingSeconds(expiresAtIso);
      setRemainingSeconds(remaining);
      if (remaining <= 0) {
        clearInterval(interval);
      }
    }, 1000);

    return () => clearInterval(interval);
  }, [expiresAtIso]);

  return {
    remainingSeconds,
    formatted: formatDuration(remainingSeconds),
    isExpired: remainingSeconds <= 0,
  };
}
