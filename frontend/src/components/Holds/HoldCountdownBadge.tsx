import { useEffect } from 'react';
import { useCountdown } from '../../hooks/useCountdown';

interface HoldCountdownBadgeProps {
  expiresAt: string;
  onExpired?: () => void;
}

export function HoldCountdownBadge({
  expiresAt,
  onExpired,
}: HoldCountdownBadgeProps) {
  const { remainingSeconds, formatted, isExpired } = useCountdown(expiresAt);
  useEffect(() => {
    if (isExpired) onExpired?.();
  }, [isExpired, onExpired]);
  if (isExpired)
    return (
      <span className="inline-flex rounded-full bg-[#fbe7e5] px-2.5 py-1 text-[10px] font-extrabold text-[#a64b45]">
        Expired
      </span>
    );
  return (
    <span
      className={`inline-flex rounded-full px-2.5 py-1 text-[10px] font-extrabold ${remainingSeconds < 120 ? 'animate-pulse bg-[#fbe7e5] text-[#a64b45]' : 'bg-[#fff0d8] text-[#94600f]'}`}
      title={`Reservation expires at ${new Date(expiresAt).toLocaleTimeString()}`}
    >
      {formatted} remaining
    </span>
  );
}
