import { useEffect } from 'react';
import { useCountdown } from '../../hooks/useCountdown';

interface HoldCountdownBadgeProps {
  expiresAt: string;
  onExpired?: () => void;
}

export function HoldCountdownBadge({ expiresAt, onExpired }: HoldCountdownBadgeProps) {
  const { remainingSeconds, formatted, isExpired } = useCountdown(expiresAt);

  useEffect(() => {
    if (isExpired && onExpired) {
      onExpired();
    }
  }, [isExpired, onExpired]);

  if (isExpired) {
    return (
      <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold bg-rose-100 text-rose-800 border border-rose-200">
        Expired
      </span>
    );
  }

  const isUrgent = remainingSeconds < 120; // under 2 minutes

  return (
    <span
      className={`inline-flex items-center px-2.5 py-0.5 rounded text-xs font-mono font-medium border ${
        isUrgent
          ? 'bg-rose-50 text-rose-700 border-rose-300 animate-pulse'
          : 'bg-amber-50 text-amber-800 border-amber-300'
      }`}
      title={`Reservation expires at ${new Date(expiresAt).toLocaleTimeString()}`}
    >
      ⏱ {formatted} remaining
    </span>
  );
}
