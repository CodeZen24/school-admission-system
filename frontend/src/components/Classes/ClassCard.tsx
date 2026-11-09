import type { AdmissionClass, StudentHoldResponse } from '../../api/types';
import { HoldCountdownBadge } from '../Holds/HoldCountdownBadge';

interface ClassCardProps {
  classItem: AdmissionClass;
  studentHold?: StudentHoldResponse;
  isApplying: boolean;
  isStudentIdValid: boolean;
  onApply: (classId: string) => void;
  onHoldExpired?: () => void;
}

export function ClassCard({
  classItem,
  studentHold,
  isApplying,
  isStudentIdValid,
  onApply,
  onHoldExpired,
}: ClassCardProps) {
  const { totalSeats, heldSeats, confirmedSeats, availableSeats } = classItem;
  const confirmedPct = totalSeats ? (confirmedSeats / totalSeats) * 100 : 0;
  const heldPct = totalSeats ? (heldSeats / totalSeats) * 100 : 0;
  const hasActiveHold =
    studentHold?.status === 'HELD' &&
    new Date(studentHold.expiresAt).getTime() > Date.now();
  const hasConfirmed = studentHold?.status === 'CONFIRMED';
  const isFull = availableSeats <= 0;
  let label = 'Apply for seat';
  let disabled = false;
  let title = '';
  let buttonClass =
    'bg-[#0f766e] text-white shadow-[0_5px_14px_rgba(15,118,110,0.18)] hover:bg-[#0b625b]';
  if (hasConfirmed) {
    label = 'Admission confirmed';
    disabled = true;
    title = 'You are already enrolled in this class.';
    buttonClass = 'bg-[#e1f2e7] text-[#28764d]';
  } else if (hasActiveHold) {
    label = 'Seat reserved';
    disabled = true;
    title = 'You already have an active hold in this class.';
    buttonClass = 'bg-[#fff0d8] text-[#94600f]';
  } else if (isFull) {
    label = 'No seats available';
    disabled = true;
    title = 'No seats are currently available.';
    buttonClass = 'bg-[#edf0ee] text-[#85908b]';
  } else if (!isStudentIdValid) {
    label = 'Enter a valid student ID';
    disabled = true;
    title = 'Enter a valid 17-digit student ID above to apply.';
    buttonClass = 'bg-[#edf0ee] text-[#85908b]';
  } else if (isApplying) {
    label = 'Reserving seat…';
    disabled = true;
    buttonClass = 'bg-[#54a99f] text-white';
  }
  const status = hasConfirmed
    ? 'Confirmed'
    : hasActiveHold
      ? 'Hold active'
      : isFull
        ? 'Full'
        : 'Open';
  const statusClass = hasConfirmed
    ? 'bg-[#e1f2e7] text-[#28764d]'
    : hasActiveHold
      ? 'bg-[#fff0d8] text-[#94600f]'
      : isFull
        ? 'bg-[#edf0ee] text-[#69746f]'
        : 'bg-[#e7f5f1] text-[#0f766e]';

  return (
    <article className="flex min-h-[22rem] flex-col justify-between rounded-2xl bg-white p-5 shadow-[0_2px_16px_rgba(28,44,38,0.06)] ring-1 ring-[#e2e9e5] transition duration-200 hover:-translate-y-0.5 hover:shadow-[0_10px_26px_rgba(28,44,38,0.09)]">
      <div>
        <div className="mb-5 flex items-start justify-between gap-3">
          <span
            className={`rounded-full px-2.5 py-1 text-[10px] font-extrabold uppercase tracking-[0.06em] ${statusClass}`}
          >
            {status}
          </span>
          {hasActiveHold && studentHold ? (
            <HoldCountdownBadge
              expiresAt={studentHold.expiresAt}
              onExpired={onHoldExpired}
            />
          ) : (
            <span className="text-[11px] font-semibold text-[#7a8781]">
              {availableSeats} seat{availableSeats === 1 ? '' : 's'} free
            </span>
          )}
        </div>
        <h3 className="text-base font-extrabold leading-snug tracking-tight text-[#17201e]">
          {classItem.name}
        </h3>
        <p className="mt-1 text-xs font-medium text-[#718078]">
          Current admission capacity
        </p>
        <div className="mt-6">
          <div className="mb-2 flex items-center justify-between text-[11px] font-bold text-[#68756f]">
            <span>Seat allocation</span>
            <span>
              {Math.round((1 - availableSeats / totalSeats) * 100)}% occupied
            </span>
          </div>
          <div
            className="flex h-2.5 overflow-hidden rounded-full bg-[#e8eeeb]"
            aria-label={`${confirmedSeats} confirmed and ${heldSeats} held seats`}
          >
            <span
              className="bg-[#28764d] transition-all"
              style={{ width: `${confirmedPct}%` }}
              title={`${confirmedSeats} confirmed`}
            />
            <span
              className="bg-[#d3942c] transition-all"
              style={{ width: `${heldPct}%` }}
              title={`${heldSeats} held`}
            />
          </div>
        </div>
        <div className="mt-6 flex items-end gap-2">
          <strong
            className={`text-3xl tracking-tight ${availableSeats ? 'text-[#0f766e]' : 'text-[#7b8580]'}`}
          >
            {availableSeats}
          </strong>
          <span className="mb-1 text-xs font-bold text-[#53615b]">
            seats available
          </span>
        </div>
        <dl className="mt-4 grid grid-cols-3 divide-x divide-[#dfe7e3] rounded-xl bg-[#f3f6f4] py-3 text-center">
          <div>
            <dt className="text-[9px] font-extrabold uppercase tracking-[0.07em] text-[#7b8781]">
              Total
            </dt>
            <dd className="mt-1 text-sm font-extrabold text-[#293631]">
              {totalSeats}
            </dd>
          </div>
          <div>
            <dt className="text-[9px] font-extrabold uppercase tracking-[0.07em] text-[#a46d18]">
              Held
            </dt>
            <dd className="mt-1 text-sm font-extrabold text-[#94600f]">
              {heldSeats}
            </dd>
          </div>
          <div>
            <dt className="text-[9px] font-extrabold uppercase tracking-[0.07em] text-[#28764d]">
              Confirmed
            </dt>
            <dd className="mt-1 text-sm font-extrabold text-[#28764d]">
              {confirmedSeats}
            </dd>
          </div>
        </dl>
      </div>
      <button
        type="button"
        className={`mt-6 w-full rounded-xl px-4 py-3 text-sm font-extrabold transition disabled:cursor-not-allowed ${buttonClass}`}
        disabled={disabled}
        title={title}
        onClick={() => onApply(classItem.id)}
      >
        {label}
      </button>
    </article>
  );
}
