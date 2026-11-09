import { useState, useCallback, useEffect } from 'react';
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

  const [notification, setNotification] = useState<{
    type: 'success' | 'error' | 'info' | 'warning';
    title?: string;
    message: string;
  } | null>(null);

  const refreshClasses = useCallback(async () => {
    try {
      const data = await apiClient.getClasses();
      setClasses(data);
    } catch (err) {
      console.error('Failed to load classes:', err);
    } finally {
      setIsLoadingClasses(false);
    }
  }, []);

  const refreshHolds = useCallback(async () => {
    if (!isValid || !studentId) {
      setStudentHolds([]);
      setIsLoadingHolds(false);
      return;
    }

    try {
      const data = await apiClient.getMyHolds(studentId);
      setStudentHolds(data);
    } catch (err) {
      console.error('Failed to load student applications:', err);
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
          'Your 10-minute reservation hold is active. Confirm simulated payment below to finalize enrollment.',
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
          <span className="h-2.5 w-2.5 rounded-full bg-[#0f9b86]" />
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

      <ClassList
        classes={classes}
        studentHolds={studentHolds}
        isLoading={isLoadingClasses}
        applyingClassId={applyingClassId}
        isStudentIdValid={isValid}
        onApply={handleApply}
        onHoldExpired={refreshAll}
      />

      <MyApplicationsList
        holds={studentHolds}
        isLoading={isLoadingHolds}
        onConfirm={handleConfirm}
        onCancel={handleCancel}
        onHoldExpired={refreshAll}
      />
    </div>
  );
}
