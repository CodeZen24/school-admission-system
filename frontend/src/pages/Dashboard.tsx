import { useState, useCallback, useEffect } from 'react';
import { CalendarClock, GraduationCap, UsersRound } from 'lucide-react';
import { apiClient, ApiError } from '../api/apiClient';
import type { AdmissionClass, StudentHoldResponse } from '../api/types';
import { useStudentIdentity } from '../hooks/useStudentIdentity';
import { usePolling } from '../hooks/usePolling';
import { Alert } from '../components/Common/Alert';
import { StudentIdentityBar } from '../components/Identity/StudentIdentityBar';
import { CreateClassForm } from '../components/Classes/CreateClassForm';
import { ClassList } from '../components/Classes/ClassList';
import { MyApplicationsList } from '../components/Holds/MyApplicationsList';

export function Dashboard() {
  const { studentId, setStudentId, isValid, validationError } =
    useStudentIdentity();

  const [classes, setClasses] = useState<AdmissionClass[]>([]);
  const [studentHolds, setStudentHolds] = useState<StudentHoldResponse[]>([]);
  const [isLoadingClasses, setIsLoadingClasses] = useState(true);
  const [isLoadingHolds, setIsLoadingHolds] = useState(true);
  const [applyingClassId, setApplyingClassId] = useState<string | null>(null);

  const [classesError, setClassesError] = useState<string | null>(null);
  const [holdsError, setHoldsError] = useState<string | null>(null);
  const [hasClassesSyncError, setHasClassesSyncError] = useState(false);
  const [hasHoldsSyncError, setHasHoldsSyncError] = useState(false);

  const [notification, setNotification] = useState<{
    type: 'success' | 'error' | 'info' | 'warning';
    title?: string;
    message: string;
  } | null>(null);

  const refreshClasses = useCallback(async () => {
    try {
      const data = await apiClient.getClasses();
      setClasses(data);
      setClassesError(null);
      setHasClassesSyncError(false);
    } catch (err) {
      console.error('Failed to load classes:', err);
      const message =
        err instanceof ApiError
          ? err.message
          : 'Unable to reach the server to load classes.';
      setClasses((prev) => {
        if (prev.length === 0) {
          setClassesError(message);
        } else {
          setHasClassesSyncError(true);
        }
        return prev;
      });
    } finally {
      setIsLoadingClasses(false);
    }
  }, []);

  const refreshHolds = useCallback(async () => {
    if (!isValid || !studentId) {
      setStudentHolds([]);
      setHoldsError(null);
      setHasHoldsSyncError(false);
      setIsLoadingHolds(false);
      return;
    }

    try {
      const data = await apiClient.getMyHolds(studentId);
      setStudentHolds(data);
      setHoldsError(null);
      setHasHoldsSyncError(false);
    } catch (err) {
      console.error('Failed to load student applications:', err);
      const message =
        err instanceof ApiError
          ? err.message
          : 'Unable to reach the server to load your applications.';
      setStudentHolds((prev) => {
        if (prev.length === 0) {
          setHoldsError(message);
        } else {
          setHasHoldsSyncError(true);
        }
        return prev;
      });
    } finally {
      setIsLoadingHolds(false);
    }
  }, [isValid, studentId]);

  const refreshAll = useCallback(async () => {
    await Promise.allSettled([refreshClasses(), refreshHolds()]);
  }, [refreshClasses, refreshHolds]);

  useEffect(() => {
    void refreshAll();
  }, [refreshAll]);

  usePolling(refreshAll, 4000, true);

  const handleClassCreated = useCallback(
    (newClass: AdmissionClass) => {
      setClasses((prev) => [newClass, ...prev]);
      void refreshClasses();
    },
    [refreshClasses],
  );

  const handleApply = async (classId: string) => {
    if (!isValid) {
      setNotification({
        type: 'warning',
        title: 'Invalid Student ID',
        message:
          validationError ||
          'Please provide a valid 17-digit student ID above.',
      });
      return;
    }

    setApplyingClassId(classId);
    setNotification(null);

    try {
      await apiClient.createHold(classId, studentId);
      setNotification({
        type: 'success',
        title: 'Seat Reserved!',
        message:
          'Your reservation hold is active. Confirm simulated payment before it expires to finalize enrollment.',
      });
      await refreshAll();
    } catch (err) {
      if (err instanceof ApiError) {
        setNotification({
          type: 'error',
          title:
            err.status === 409 ? 'Reservation Conflict' : 'Application Failed',
          message: err.message,
        });
      } else {
        setNotification({
          type: 'error',
          title: 'Error',
          message: 'An unexpected error occurred while reserving your seat.',
        });
      }
      void refreshClasses();
    } finally {
      setApplyingClassId(null);
    }
  };

  const handleConfirm = async (holdId: string) => {
    setNotification(null);
    try {
      await apiClient.confirmHold(holdId, studentId);
      setNotification({
        type: 'success',
        title: 'Admission Confirmed!',
        message:
          'Your fee payment was simulated and your seat is now permanently confirmed.',
      });
      await refreshAll();
    } catch (err) {
      if (err instanceof ApiError) {
        setNotification({
          type: 'error',
          title: err.status === 409 ? 'Cannot Confirm' : 'Confirmation Error',
          message: err.message,
        });
      } else {
        setNotification({
          type: 'error',
          title: 'Error',
          message: 'An unexpected error occurred while confirming your hold.',
        });
      }
      void refreshAll();
    }
  };

  const handleCancel = async (holdId: string) => {
    setNotification(null);
    try {
      await apiClient.cancelHold(holdId, studentId);
      setNotification({
        type: 'info',
        title: 'Reservation Cancelled',
        message:
          'Your seat hold has been released and the capacity is available for other students.',
      });
      await refreshAll();
    } catch (err) {
      if (err instanceof ApiError) {
        setNotification({
          type: 'error',
          title: 'Cancellation Error',
          message: err.message,
        });
      } else {
        setNotification({
          type: 'error',
          title: 'Error',
          message:
            'An unexpected error occurred while cancelling the reservation.',
        });
      }
      void refreshAll();
    }
  };

  const openClasses = classes.filter(
    (classItem) => classItem.availableSeats > 0,
  ).length;
  const availableSeats = classes.reduce(
    (total, classItem) => total + classItem.availableSeats,
    0,
  );
  const activeHolds = studentHolds.filter(
    (hold) => hold.status === 'HELD',
  ).length;

  const hasSyncWarning =
    (hasClassesSyncError || hasHoldsSyncError) &&
    (classes.length > 0 || studentHolds.length > 0);

  return (
    <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="mb-2 text-[10px] font-extrabold uppercase tracking-[0.1em] text-[#0f766e]">
            Admission registry
          </p>
          <h1 className="text-3xl font-extrabold tracking-tight text-[#17201e] sm:text-4xl">
            Admission &amp; seat allocation
          </h1>
          <p className="mt-2 max-w-2xl text-sm font-medium leading-6 text-[#68766f]">
            Reserve a place in an available class, then confirm the simulated
            fee before the hold expires.
          </p>
        </div>
        <div className="flex items-center gap-3 rounded-2xl bg-white px-4 py-3 shadow-sm ring-1 ring-[#e2e9e5]">
          <CalendarClock
            size={18}
            className="text-[#0f766e]"
            aria-hidden="true"
          />
          <div>
            <p className="text-[10px] font-extrabold uppercase tracking-[0.08em] text-[#75817b]">
              Registry status
            </p>
            <p className="text-xs font-extrabold text-[#24302b]">
              Auto-syncing active
            </p>
          </div>
        </div>
      </div>

      {!isLoadingClasses && classes.length > 0 && (
        <section
          className="mb-8 grid grid-cols-1 gap-3 sm:grid-cols-3"
          aria-label="Admission summary"
        >
          <div className="flex items-center gap-3 rounded-xl border border-[#dfe7e3] bg-white px-4 py-3.5">
            <span className="grid h-9 w-9 place-items-center rounded-lg bg-[#e7f5f1] text-[#0f766e]">
              <GraduationCap size={18} aria-hidden="true" />
            </span>
            <div>
              <p className="text-[10px] font-extrabold uppercase tracking-[0.08em] text-[#78847e]">
                Open classes
              </p>
              <p className="mt-0.5 text-lg font-extrabold text-[#24302b]">
                {openClasses}
              </p>
            </div>
          </div>
          <div className="flex items-center gap-3 rounded-xl border border-[#dfe7e3] bg-white px-4 py-3.5">
            <span className="grid h-9 w-9 place-items-center rounded-lg bg-[#e7f5f1] text-[#0f766e]">
              <UsersRound size={18} aria-hidden="true" />
            </span>
            <div>
              <p className="text-[10px] font-extrabold uppercase tracking-[0.08em] text-[#78847e]">
                Seats available
              </p>
              <p className="mt-0.5 text-lg font-extrabold text-[#24302b]">
                {availableSeats}
              </p>
            </div>
          </div>
          <div className="flex items-center gap-3 rounded-xl border border-[#dfe7e3] bg-white px-4 py-3.5">
            <span className="grid h-9 w-9 place-items-center rounded-lg bg-[#fff0d8] text-[#94600f]">
              <CalendarClock size={18} aria-hidden="true" />
            </span>
            <div>
              <p className="text-[10px] font-extrabold uppercase tracking-[0.08em] text-[#78847e]">
                Your active holds
              </p>
              <p className="mt-0.5 text-lg font-extrabold text-[#24302b]">
                {activeHolds}
              </p>
            </div>
          </div>
        </section>
      )}

      {hasSyncWarning && (
        <div className="mb-6">
          <Alert
            type="warning"
            title="Live Connection Notice"
            onClose={() => {
              setHasClassesSyncError(false);
              setHasHoldsSyncError(false);
            }}
          >
            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
              <span>
                Live synchronization is temporarily interrupted. Displayed seat
                counts or application history may be delayed.
              </span>
              <button
                type="button"
                onClick={() => void refreshAll()}
                className="shrink-0 font-extrabold underline hover:opacity-80 text-amber-950"
              >
                Retry sync
              </button>
            </div>
          </Alert>
        </div>
      )}

      {notification && (
        <div className="mb-6">
          <Alert
            type={notification.type}
            title={notification.title}
            onClose={() => setNotification(null)}
          >
            {notification.message}
          </Alert>
        </div>
      )}

      <StudentIdentityBar
        studentId={studentId}
        onStudentIdChange={setStudentId}
        isValid={isValid}
        validationError={validationError}
      />

      <CreateClassForm onClassCreated={handleClassCreated} />

      {classesError && classes.length === 0 && !isLoadingClasses ? (
        <div className="mb-10">
          <Alert type="error" title="Unable to Load Admission Classes">
            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mt-1">
              <span>{classesError}</span>
              <button
                type="button"
                onClick={() => {
                  setIsLoadingClasses(true);
                  setClassesError(null);
                  void refreshClasses();
                }}
                className="shrink-0 rounded-lg bg-rose-700 px-3.5 py-1.5 text-xs font-bold text-white shadow-sm hover:bg-rose-800 transition"
              >
                Retry
              </button>
            </div>
          </Alert>
        </div>
      ) : (
        <ClassList
          classes={classes}
          studentHolds={studentHolds}
          isLoading={isLoadingClasses}
          applyingClassId={applyingClassId}
          isStudentIdValid={isValid}
          onApply={handleApply}
          onHoldExpired={refreshAll}
        />
      )}

      {holdsError && studentHolds.length === 0 && !isLoadingHolds ? (
        <div className="mb-8">
          <Alert type="error" title="Unable to Load Application History">
            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mt-1">
              <span>{holdsError}</span>
              <button
                type="button"
                onClick={() => {
                  setIsLoadingHolds(true);
                  setHoldsError(null);
                  void refreshHolds();
                }}
                className="shrink-0 rounded-lg bg-rose-700 px-3.5 py-1.5 text-xs font-bold text-white shadow-sm hover:bg-rose-800 transition"
              >
                Retry
              </button>
            </div>
          </Alert>
        </div>
      ) : (
        <MyApplicationsList
          holds={studentHolds}
          isLoading={isLoadingHolds}
          onConfirm={handleConfirm}
          onCancel={handleCancel}
          onHoldExpired={refreshAll}
        />
      )}
    </div>
  );
}
