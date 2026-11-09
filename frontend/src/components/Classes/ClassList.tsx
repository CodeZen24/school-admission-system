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
  if (isLoading && classes.length === 0) {
    return (
      <div className="bg-white rounded-xl border border-slate-200 p-12 text-center shadow-sm">
        <div className="inline-block animate-spin rounded-full h-8 w-8 border-4 border-indigo-600 border-t-transparent mb-3" />
        <p className="text-sm text-slate-500 font-medium">Loading admission classes...</p>
      </div>
    );
  }

  if (classes.length === 0) {
    return (
      <div className="bg-white rounded-xl border border-dashed border-slate-300 p-12 text-center shadow-sm">
        <div className="text-3xl mb-2">📚</div>
        <h3 className="text-base font-semibold text-slate-800 mb-1">No Classes Created Yet</h3>
        <p className="text-sm text-slate-500">
          Use the &quot;+ Create New Class&quot; button above to create the first admission class.
        </p>
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5 mb-10">
      {classes.map(classItem => {
        const matchingHold = studentHolds.find(h => h.classId === classItem.id);

        return (
          <ClassCard
            key={classItem.id}
            classItem={classItem}
            studentHold={matchingHold}
            isApplying={applyingClassId === classItem.id}
            isStudentIdValid={isStudentIdValid}
            onApply={onApply}
            onHoldExpired={onHoldExpired}
          />
        );
      })}
    </div>
  );
}
