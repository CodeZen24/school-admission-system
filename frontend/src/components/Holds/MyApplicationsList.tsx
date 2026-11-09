import { useEffect, useState } from 'react';
import type { StudentHoldResponse } from '../../api/types';
import { useCountdown } from '../../hooks/useCountdown';

interface MyApplicationsListProps {
  holds: StudentHoldResponse[];
  isLoading: boolean;
  onConfirm: (holdId: string) => Promise<void>;
  onCancel: (holdId: string) => Promise<void>;
  onHoldExpired?: () => void;
}
interface ApplicationRowProps {
  hold: StudentHoldResponse;
  onConfirm: (holdId: string) => Promise<void>;
  onCancel: (holdId: string) => Promise<void>;
  onHoldExpired?: () => void;
}

function formatAppliedDate(dateString?: string): string | null {
  if (!dateString) return null;
  const parsed = new Date(dateString);
  return Number.isNaN(parsed.getTime()) ? null : parsed.toLocaleString();
}

function ApplicationRow({
  hold,
  onConfirm,
  onCancel,
  onHoldExpired,
}: ApplicationRowProps) {
  const [confirming, setConfirming] = useState(false);
  const [cancelling, setCancelling] = useState(false);
  const appliedDate = formatAppliedDate(hold.createdAt);
  const { remainingSeconds, formatted, isExpired } = useCountdown(
    hold.expiresAt,
  );
  const held = hold.status === 'HELD';
  const status = held && isExpired ? 'EXPIRED' : hold.status;
  const actionable = held && !isExpired && remainingSeconds > 0;
  useEffect(() => {
    if (held && isExpired) onHoldExpired?.();
  }, [held, isExpired, onHoldExpired]);
  const badge = {
    HELD: 'bg-[#fff0d8] text-[#94600f]',
    CONFIRMED: 'bg-[#e1f2e7] text-[#28764d]',
    CANCELLED: 'bg-[#edf0ee] text-[#65716c]',
    EXPIRED: 'bg-[#fbe7e5] text-[#a64b45]',
  }[status];
  const symbol = { HELD: '○', CONFIRMED: '✓', CANCELLED: '–', EXPIRED: '!' }[
    status
  ];
  const confirm = async () => {
    if (!actionable || confirming) return;
    setConfirming(true);
    try {
      await onConfirm(hold.holdId);
    } finally {
      setConfirming(false);
    }
  };
  const cancel = async () => {
    if (!actionable || cancelling) return;
    setCancelling(true);
    try {
      await onCancel(hold.holdId);
    } finally {
      setCancelling(false);
    }
  };
  return (
    <li className="flex flex-col gap-4 px-5 py-5 transition hover:bg-[#f8fbf9] sm:flex-row sm:items-center sm:justify-between">
      <div className="flex min-w-0 items-start gap-3">
        <span
          className={`grid h-10 w-10 shrink-0 place-items-center rounded-xl text-sm font-extrabold ${badge}`}
        >
          {symbol}
        </span>
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="font-extrabold text-[#24302b]">{hold.className}</h3>
            <span
              className={`rounded-full px-2 py-1 text-[9px] font-extrabold tracking-[0.06em] ${badge}`}
            >
              {status}
            </span>
          </div>
          <p
            className="mt-1 text-xs font-mono text-[#75817b]"
            title={hold.holdId}
          >
            Reservation {hold.holdId.substring(0, 8)}…
            {appliedDate ? ` · Applied ${appliedDate}` : ''}
          </p>
          {status === 'HELD' && (
            <p
              className={`mt-2 text-xs font-bold ${remainingSeconds < 120 ? 'text-[#b34a3f]' : 'text-[#94600f]'}`}
            >
              Time to confirm: {formatted}
            </p>
          )}
          {status === 'CONFIRMED' && (
            <p className="mt-2 text-xs font-semibold text-[#28764d]">
              Your seat is confirmed.
            </p>
          )}
          {status === 'EXPIRED' && (
            <p className="mt-2 text-xs font-semibold text-[#a64b45]">
              The hold expired and the seat was released.
            </p>
          )}
        </div>
      </div>
      {actionable && (
        <div className="flex shrink-0 gap-2 sm:pl-4">
          <button
            type="button"
            className="rounded-xl bg-[#0f766e] px-3.5 py-2.5 text-xs font-extrabold text-white shadow-sm transition hover:bg-[#0b625b] disabled:opacity-60"
            onClick={confirm}
            disabled={confirming || cancelling}
          >
            {confirming ? 'Confirming…' : 'Confirm payment'}
          </button>
          <button
            type="button"
            className="rounded-xl bg-[#f3f6f4] px-3.5 py-2.5 text-xs font-extrabold text-[#68756f] transition hover:bg-[#e5ebe7] disabled:opacity-60"
            onClick={cancel}
            disabled={confirming || cancelling}
          >
            {cancelling ? 'Cancelling…' : 'Cancel'}
          </button>
        </div>
      )}
    </li>
  );
}

export function MyApplicationsList({
  holds,
  isLoading,
  onConfirm,
  onCancel,
  onHoldExpired,
}: MyApplicationsListProps) {
  return (
    <section className="mb-8" aria-labelledby="applications-heading">
      <div className="mb-4 flex items-end justify-between gap-4">
        <div>
          <p className="mb-1 text-[10px] font-extrabold uppercase tracking-[0.08em] text-[#0f766e]">
            Application history
          </p>
          <h2
            id="applications-heading"
            className="text-2xl font-extrabold tracking-tight text-[#17201e]"
          >
            My applications &amp; reservations
          </h2>
          <p className="mt-1 text-xs font-medium text-[#718078]">
            The records below belong to the active student identity.
          </p>
        </div>
        {holds.length > 0 && (
          <span className="hidden rounded-full bg-white px-3 py-1.5 text-[10px] font-bold text-[#65716c] ring-1 ring-[#e2e9e5] sm:block">
            {holds.length} record{holds.length === 1 ? '' : 's'}
          </span>
        )}
      </div>
      {isLoading && holds.length === 0 ? (
        <div className="rounded-2xl bg-white p-8 text-center text-sm font-semibold text-[#718078] shadow-sm ring-1 ring-[#e2e9e5]">
          Loading applications…
        </div>
      ) : holds.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-[#b9cbc3] bg-white/70 p-10 text-center">
          <div className="mx-auto mb-3 grid h-11 w-11 place-items-center rounded-xl bg-[#e7f5f1] text-sm font-extrabold text-[#0f766e]">
            +
          </div>
          <h3 className="font-extrabold text-[#24302b]">No applications yet</h3>
          <p className="mt-1 text-sm text-[#718078]">
            Choose an available class above to reserve a seat.
          </p>
        </div>
      ) : (
        <ul className="divide-y divide-[#e7ece9] overflow-hidden rounded-2xl bg-white shadow-[0_2px_16px_rgba(28,44,38,0.06)] ring-1 ring-[#e2e9e5]">
          {holds.map((hold) => (
            <ApplicationRow
              key={hold.holdId}
              hold={hold}
              onConfirm={onConfirm}
              onCancel={onCancel}
              onHoldExpired={onHoldExpired}
            />
          ))}
        </ul>
      )}
    </section>
  );
}
