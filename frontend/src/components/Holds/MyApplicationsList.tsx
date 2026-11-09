import { useState, useEffect } from 'react';
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

function ApplicationRow({
  hold,
  onConfirm,
  onCancel,
  onHoldExpired,
}: ApplicationRowProps) {
  const [isConfirming, setIsConfirming] = useState(false);
  const [isCancelling, setIsCancelling] = useState(false);
  const isHeld = hold.status === 'HELD';
  const { remainingSeconds, formatted, isExpired } = useCountdown(hold.expiresAt);

  useEffect(() => {
    if (isHeld && isExpired && onHoldExpired) {
      onHoldExpired();
    }
  }, [isHeld, isExpired, onHoldExpired]);

  const effectiveStatus = isHeld && isExpired ? 'EXPIRED' : hold.status;
  const canAct = isHeld && !isExpired && remainingSeconds > 0;

  const handleConfirm = async () => {
    if (!canAct || isConfirming) return;
    setIsConfirming(true);
    try {
      await onConfirm(hold.holdId);
    } finally {
      setIsConfirming(false);
    }
  };

  const handleCancel = async () => {
    if (!canAct || isCancelling) return;
    setIsCancelling(true);
    try {
      await onCancel(hold.holdId);
    } finally {
      setIsCancelling(false);
    }
  };

  const statusBadge = {
    HELD: 'bg-amber-100 text-amber-800 border-amber-200',
    CONFIRMED: 'bg-emerald-100 text-emerald-800 border-emerald-200',
    CANCELLED: 'bg-slate-100 text-slate-600 border-slate-200',
    EXPIRED: 'bg-rose-100 text-rose-800 border-rose-200',
  }[effectiveStatus] || 'bg-slate-100 text-slate-700';

  return (
    <tr className="border-b border-slate-100 hover:bg-slate-50/50 transition-colors">
      <td className="py-3 px-4">
        <div className="font-semibold text-slate-900">{hold.className}</div>
        <div className="text-xs font-mono text-slate-400" title={hold.holdId}>
          {hold.holdId.substring(0, 8)}...
        </div>
      </td>
      <td className="py-3 px-4">
        <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold border ${statusBadge}`}>
          {effectiveStatus}
        </span>
      </td>
      <td className="py-3 px-4">
        {isHeld ? (
          isExpired ? (
            <span className="text-xs text-rose-600 font-medium">Expired</span>
          ) : (
            <span
              className={`text-xs font-mono font-medium ${
                remainingSeconds < 120 ? 'text-rose-600 font-bold animate-pulse' : 'text-amber-800'
              }`}
            >
              ⏱ {formatted}
            </span>
          )
        ) : (
          <span className="text-xs text-slate-400">—</span>
        )}
      </td>
      <td className="py-3 px-4 text-xs text-slate-500">
        {new Date(hold.createdAt).toLocaleTimeString([], {
          hour: '2-digit',
          minute: '2-digit',
          second: '2-digit',
        })}
      </td>
      <td className="py-3 px-4 text-right">
        {canAct ? (
          <div className="flex items-center justify-end space-x-2">
            <button
              type="button"
              className="text-xs font-medium px-3 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white transition-colors shadow-sm disabled:opacity-50"
              onClick={handleConfirm}
              disabled={isConfirming || isCancelling}
              title="Confirm admission and simulate fee payment"
            >
              {isConfirming ? 'Confirming...' : '💳 Confirm / Pay Fee'}
            </button>
            <button
              type="button"
              className="text-xs font-medium px-2.5 py-1.5 rounded-lg border border-rose-200 text-rose-700 hover:bg-rose-50 transition-colors disabled:opacity-50"
              onClick={handleCancel}
              disabled={isConfirming || isCancelling}
              title="Cancel seat reservation"
            >
              {isCancelling ? 'Cancelling...' : 'Cancel'}
            </button>
          </div>
        ) : (
          <span className="text-xs font-medium text-slate-400">
            {effectiveStatus === 'CONFIRMED' && '✓ Enrolled'}
            {effectiveStatus === 'CANCELLED' && 'Cancelled'}
            {effectiveStatus === 'EXPIRED' && 'Expired'}
          </span>
        )}
      </td>
    </tr>
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
    <section className="mb-12">
      <div className="mb-4">
        <h2 className="text-xl font-bold text-slate-900">My Seat Applications & Reservations</h2>
        <p className="text-xs text-slate-500">
          History for active student identity (synced via <code>X-Student-Id</code>)
        </p>
      </div>

      {isLoading && holds.length === 0 ? (
        <div className="bg-white rounded-xl border border-slate-200 p-8 text-center shadow-sm">
          <p className="text-sm text-slate-500">Loading student applications...</p>
        </div>
      ) : holds.length === 0 ? (
        <div className="bg-white rounded-xl border border-dashed border-slate-300 p-8 text-center shadow-sm">
          <div className="text-2xl mb-1">📋</div>
          <h3 className="text-sm font-semibold text-slate-800 mb-1">No Applications Yet</h3>
          <p className="text-xs text-slate-500">
            Select an available class above and click &quot;Apply for Seat&quot; to begin.
          </p>
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-slate-50 border-b border-slate-200 text-xs font-semibold text-slate-600 uppercase tracking-wider">
                <tr>
                  <th className="py-3 px-4">Class</th>
                  <th className="py-3 px-4">Status</th>
                  <th className="py-3 px-4">Countdown</th>
                  <th className="py-3 px-4">Applied At</th>
                  <th className="py-3 px-4 text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {holds.map(hold => (
                  <ApplicationRow
                    key={hold.holdId}
                    hold={hold}
                    onConfirm={onConfirm}
                    onCancel={onCancel}
                    onHoldExpired={onHoldExpired}
                  />
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </section>
  );
}
