import type { ChangeEvent } from 'react';
import { BadgeCheck, CheckCircle2, Fingerprint } from 'lucide-react';

interface StudentIdentityBarProps {
  studentId: string;
  onStudentIdChange: (id: string) => void;
  isValid: boolean;
  validationError?: string;
}

const PRESET_STUDENTS = [
  { label: 'Student 1', id: '10000000000000001' },
  { label: 'Student 2', id: '20000000000000002' },
  { label: 'Student 3', id: '30000000000000003' },
];

export function StudentIdentityBar({
  studentId,
  onStudentIdChange,
  isValid,
  validationError,
}: StudentIdentityBarProps) {
  const handleChange = (event: ChangeEvent<HTMLInputElement>) =>
    onStudentIdChange(event.target.value);

  return (
    <section
      className="mb-8 rounded-2xl bg-white p-5 shadow-[0_2px_16px_rgba(28,44,38,0.06)] ring-1 ring-[#e2e9e5] sm:p-6"
      aria-labelledby="identity-heading"
    >
      <div className="flex flex-col gap-6 lg:flex-row lg:items-center lg:justify-between">
        <div className="min-w-0 flex-1">
          <div className="mb-2 flex flex-wrap items-center gap-2">
            <h2
              id="identity-heading"
              className="text-[11px] font-extrabold uppercase tracking-[0.08em] text-[#44514c]"
            >
              Active student identity
            </h2>
            <span className="rounded-full bg-[#e7f5f1] px-2.5 py-1 text-[10px] font-bold text-[#0f766e]">
              Attached via X-Student-Id
            </span>
          </div>
          <div
            className={`flex max-w-xl items-center rounded-xl border px-3 transition ${isValid ? 'border-[#b8dfd4] bg-[#f8fcfa] focus-within:border-[#0f766e] focus-within:ring-2 focus-within:ring-[#b8dfd4]' : 'border-[#f0c98e] bg-[#fffaf2] focus-within:border-[#b97913] focus-within:ring-2 focus-within:ring-[#f0c98e]'}`}
          >
            <span
              className="mr-2 grid h-7 w-7 shrink-0 place-items-center rounded-lg bg-[#e7f5f1] text-[#0f766e]"
              aria-hidden="true"
            >
              <Fingerprint size={16} strokeWidth={2.2} />
            </span>
            <input
              id="student-id-input"
              type="text"
              inputMode="numeric"
              className="min-w-0 flex-1 bg-transparent py-3 font-mono text-sm tracking-wide text-[#17201e] placeholder:text-[#98a39e]"
              value={studentId}
              onChange={handleChange}
              maxLength={17}
              placeholder="17-digit student ID"
              aria-describedby="student-id-feedback"
              aria-invalid={!isValid}
            />
            <span
              className={`ml-2 shrink-0 rounded-full px-2 py-1 font-mono text-[10px] font-bold ${studentId.length === 17 ? 'bg-[#e1f2e7] text-[#28764d]' : 'bg-[#fff0d8] text-[#9a5d09]'}`}
            >
              {studentId.length}/17
            </span>
          </div>
          <p
            id="student-id-feedback"
            className={`mt-2 flex items-center gap-1.5 text-xs font-semibold ${validationError ? 'text-[#a86412]' : 'text-[#28764d]'}`}
          >
            {validationError ? (
              <BadgeCheck size={15} aria-hidden="true" />
            ) : (
              <CheckCircle2 size={15} aria-hidden="true" />
            )}
            {validationError ||
              'Valid 17-digit identity ready for seat reservations'}
          </p>
        </div>
        <div className="lg:text-right">
          <p className="mb-2 text-[10px] font-extrabold uppercase tracking-[0.08em] text-[#718078]">
            Switch demo profile
          </p>
          <div className="inline-flex rounded-xl bg-[#eef2ef] p-1">
            {PRESET_STUDENTS.map((preset) => {
              const selected = studentId === preset.id;
              return (
                <button
                  key={preset.id}
                  type="button"
                  className={`rounded-lg px-3 py-2 text-xs font-bold transition sm:px-4 ${selected ? 'bg-[#17201e] text-white shadow-sm' : 'text-[#66736d] hover:bg-white hover:text-[#17201e]'}`}
                  onClick={() => onStudentIdChange(preset.id)}
                  title={`Switch to ${preset.label} (${preset.id})`}
                >
                  {preset.label}
                </button>
              );
            })}
          </div>
        </div>
      </div>
    </section>
  );
}
