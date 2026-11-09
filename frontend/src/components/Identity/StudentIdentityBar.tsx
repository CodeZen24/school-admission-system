import type { ChangeEvent } from 'react';

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
  const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
    onStudentIdChange(e.target.value);
  };

  return (
    <header className="bg-white rounded-xl border border-slate-200 p-5 shadow-sm mb-8">
      <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-5">
        <div className="flex-1">
          <div className="flex items-center space-x-2 mb-2">
            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-indigo-100 text-indigo-800">
              Active Student Identity
            </span>
            <span className="text-xs text-slate-500">
              Attached to requests via <code className="bg-slate-100 px-1 py-0.5 rounded text-slate-700 font-mono">X-Student-Id</code>
            </span>
          </div>

          <div className="relative max-w-md">
            <input
              id="student-id-input"
              type="text"
              className={`w-full font-mono text-sm px-3.5 py-2 rounded-lg border outline-none transition-all pr-16 ${
                isValid
                  ? 'border-emerald-300 focus:border-emerald-500 focus:ring-2 focus:ring-emerald-100 bg-emerald-50/20'
                  : 'border-amber-300 focus:border-amber-500 focus:ring-2 focus:ring-amber-100 bg-amber-50/20'
              }`}
              value={studentId}
              onChange={handleChange}
              maxLength={17}
              placeholder="17 ASCII digits"
              aria-describedby="student-id-feedback"
              aria-invalid={!isValid}
            />
            <span
              className={`absolute right-3 top-1/2 -translate-y-1/2 text-xs font-mono font-medium px-1.5 py-0.5 rounded ${
                studentId.length === 17
                  ? 'bg-emerald-100 text-emerald-800'
                  : 'bg-amber-100 text-amber-800'
              }`}
            >
              {studentId.length}/17
            </span>
          </div>

          <div id="student-id-feedback" className="mt-1.5 text-xs">
            {validationError ? (
              <span className="text-amber-700 font-medium flex items-center gap-1">
                ⚠️ {validationError}
              </span>
            ) : (
              <span className="text-emerald-700 font-medium flex items-center gap-1">
                ✓ Valid 17-digit student identity ready for seat reservations
              </span>
            )}
          </div>
        </div>

        <div className="flex flex-col sm:flex-row sm:items-center gap-2 pt-2 lg:pt-0 border-t lg:border-t-0 border-slate-100">
          <span className="text-xs font-medium text-slate-500">Switch Demo Student:</span>
          <div className="flex items-center gap-2">
            {PRESET_STUDENTS.map(preset => {
              const isSelected = studentId === preset.id;
              return (
                <button
                  key={preset.id}
                  type="button"
                  className={`text-xs font-medium px-3 py-1.5 rounded-lg border transition-all ${
                    isSelected
                      ? 'bg-indigo-600 text-white border-indigo-600 shadow-sm'
                      : 'bg-white text-slate-700 border-slate-300 hover:bg-slate-50'
                  }`}
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
    </header>
  );
}
