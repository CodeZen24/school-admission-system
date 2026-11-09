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

  const confirmedPct = totalSeats > 0 ? (confirmedSeats / totalSeats) * 100 : 0;
  const heldPct = totalSeats > 0 ? (heldSeats / totalSeats) * 100 : 0;

  const hasActiveHold =
    studentHold?.status === 'HELD' &&
    new Date(studentHold.expiresAt).getTime() > Date.now();
  const hasConfirmed = studentHold?.status === 'CONFIRMED';
  const isFull = availableSeats <= 0;

  let buttonText = 'Apply for Seat';
  let buttonDisabled = false;
  let buttonTitle = '';
  let buttonStyle = 'bg-indigo-600 hover:bg-indigo-700 text-white shadow-sm';

  if (hasConfirmed) {
    buttonText = '✓ Enrolled';
    buttonDisabled = true;
    buttonTitle = 'You are already enrolled in this class.';
    buttonStyle = 'bg-emerald-100 text-emerald-800 cursor-default';
  } else if (hasActiveHold) {
    buttonText = '⏱ Seat Reserved';
    buttonDisabled = true;
    buttonTitle = 'You already have an active hold in this class.';
    buttonStyle = 'bg-amber-100 text-amber-800 cursor-default';
  } else if (isFull) {
    buttonText = 'Class Full';
    buttonDisabled = true;
    buttonTitle = 'No seats available.';
    buttonStyle = 'bg-slate-100 text-slate-400 cursor-not-allowed';
  } else if (!isStudentIdValid) {
    buttonText = 'Invalid Student ID';
    buttonDisabled = true;
    buttonTitle = 'Enter a valid 17-digit student ID above to apply.';
    buttonStyle = 'bg-slate-100 text-slate-400 cursor-not-allowed';
  } else if (isApplying) {
    buttonText = 'Reserving...';
    buttonDisabled = true;
    buttonStyle = 'bg-indigo-400 text-white cursor-wait';
  }

  return (
    <article className="bg-white rounded-xl border border-slate-200 p-5 shadow-sm hover:shadow-md transition-shadow flex flex-col justify-between">
      <div>
        <div className="flex items-start justify-between gap-2 mb-3">
          <h3 className="text-base font-bold text-slate-900 leading-snug">{classItem.name}</h3>
          {hasConfirmed && (
            <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold bg-emerald-100 text-emerald-800 border border-emerald-200 shrink-0">
              Enrolled
            </span>
          )}
          {hasActiveHold && studentHold && (
            <div className="shrink-0">
              <HoldCountdownBadge
                expiresAt={studentHold.expiresAt}
                onExpired={onHoldExpired}
              />
            </div>
          )}
        </div>

        <div className="w-full bg-slate-100 rounded-full h-2.5 overflow-hidden flex mb-4">
          <div
            className="bg-emerald-500 h-full transition-all duration-300"
            style={{ width: `${confirmedPct}%` }}
            title={`Confirmed: ${confirmedSeats}`}
          />
          <div
            className="bg-amber-400 h-full transition-all duration-300"
            style={{ width: `${heldPct}%` }}
            title={`Held: ${heldSeats}`}
          />
        </div>

        <div className="grid grid-cols-4 gap-2 text-center mb-5">
          <div className="bg-slate-50 rounded-lg p-2 border border-slate-100">
            <div className="text-[10px] uppercase font-bold text-slate-500">Total</div>
            <div className="text-sm font-bold text-slate-800">{totalSeats}</div>
          </div>
          <div className="bg-amber-50/60 rounded-lg p-2 border border-amber-100">
            <div className="text-[10px] uppercase font-bold text-amber-700">Held</div>
            <div className="text-sm font-bold text-amber-800">{heldSeats}</div>
          </div>
          <div className="bg-emerald-50/60 rounded-lg p-2 border border-emerald-100">
            <div className="text-[10px] uppercase font-bold text-emerald-700">Confirmed</div>
            <div className="text-sm font-bold text-emerald-800">{confirmedSeats}</div>
          </div>
          <div
            className={`rounded-lg p-2 border ${
              availableSeats === 0
                ? 'bg-rose-50/60 border-rose-100 text-rose-800'
                : 'bg-indigo-50/60 border-indigo-100 text-indigo-800'
            }`}
          >
            <div className="text-[10px] uppercase font-bold">Available</div>
            <div className="text-sm font-bold">{availableSeats}</div>
          </div>
        </div>
      </div>

      <div>
        <button
          type="button"
          className={`w-full py-2.5 px-4 rounded-lg font-medium text-sm transition-all duration-150 ${buttonStyle}`}
          disabled={buttonDisabled}
          title={buttonTitle}
          onClick={() => onApply(classItem.id)}
        >
          {buttonText}
        </button>
      </div>
    </article>
  );
}
