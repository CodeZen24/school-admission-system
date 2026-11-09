import type { AdmissionClass, StudentHoldResponse } from '../../api/types';
import { ClassCard } from './ClassCard';

interface ClassListProps {
  classes: AdmissionClass[];
  studentHolds: StudentHoldResponse[];
  isLoading: boolean;
  applyingClassId: string | null;
  isStudentIdValid: boolean;
  onApply: (classId: string) => void;
  onHoldExpired?: () => void;
}

export function ClassList({
  classes,
  studentHolds,
  isLoading,
  applyingClassId,
  isStudentIdValid,
  onApply,
  onHoldExpired,
}: ClassListProps) {
  if (isLoading && classes.length === 0)
    return (
      <div className="rounded-2xl bg-white p-12 text-center shadow-[0_2px_16px_rgba(28,44,38,0.06)] ring-1 ring-[#e2e9e5]">
        <div className="mx-auto mb-3 h-8 w-8 animate-spin rounded-full border-4 border-[#0f766e] border-t-transparent" />
        <p className="text-sm font-bold text-[#617069]">
          Loading admission classes...
        </p>
      </div>
    );
  if (classes.length === 0)
    return (
      <div className="rounded-2xl border border-dashed border-[#b9cbc3] bg-white/70 p-12 text-center">
        <div className="mx-auto mb-3 grid h-11 w-11 place-items-center rounded-xl bg-[#e7f5f1] text-sm font-extrabold text-[#0f766e]">
          +
        </div>
        <h3 className="mb-1 text-base font-extrabold text-[#24302b]">
          No classes created yet
        </h3>
        <p className="text-sm text-[#718078]">
          Use the “Create new class” button above to add the first admission
          class.
        </p>
      </div>
    );
  return (
    <div className="mb-10 grid grid-cols-1 gap-5 md:grid-cols-2 xl:grid-cols-3">
      {classes.map((classItem) => (
        <ClassCard
          key={classItem.id}
          classItem={classItem}
          studentHold={studentHolds.find(
            (hold) => hold.classId === classItem.id,
          )}
          isApplying={applyingClassId === classItem.id}
          isStudentIdValid={isStudentIdValid}
          onApply={onApply}
          onHoldExpired={onHoldExpired}
        />
      ))}
    </div>
  );
}
