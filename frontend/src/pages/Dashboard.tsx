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
  const { studentId, setStudentId, isValid, validationError } = useStudentIdentity();

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

  // Fetch classes
  const refreshClasses = useCallback(async () => {
    try {
      const data = await apiClient.getClasses();
      setClasses(data);
    } catch (err) {
      // Don't override notification during background poll if user is reading something
      console.error('Failed to load classes:', err);
    } finally {
      setIsLoadingClasses(false);
    }
  }, []);

  // Fetch current student holds
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

  // Combined refresh
  const refreshAll = useCallback(async () => {
    await Promise.allSettled([refreshClasses(), refreshHolds()]);
  }, [refreshClasses, refreshHolds]);

  // Run on mount and when student ID changes
  useEffect(() => {
    void refreshAll();
  }, [refreshAll]);

  // Periodic visibility-aware background polling every 4 seconds
  usePolling(refreshAll, 4000, true);

  // Class created callback
  const handleClassCreated = useCallback((newClass: AdmissionClass) => {
    setClasses(prev => [newClass, ...prev]);
    void refreshClasses();
  }, [refreshClasses]);

  // Apply for seat hold
  const handleApply = async (classId: string) => {
    if (!isValid) {
      setNotification({
        type: 'warning',
        title: 'Invalid Student ID',
        message: validationError || 'Please provide a valid 17-digit student ID above.',
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
        message: 'Your 10-minute reservation hold is active. Confirm simulated payment below to finalize enrollment.',
      });
      await refreshAll();
    } catch (err) {
      if (err instanceof ApiError) {
        setNotification({
          type: 'error',
          title: err.status === 409 ? 'Reservation Conflict' : 'Application Failed',
          message: err.message,
        });
      } else {
        setNotification({
          type: 'error',
          title: 'Error',
          message: 'An unexpected error occurred while reserving your seat.',
        });
      }
      // Refresh to get latest real seats
      void refreshClasses();
    } finally {
      setApplyingClassId(null);
    }
  };

  // Confirm hold & simulate fee payment
  const handleConfirm = async (holdId: string) => {
    setNotification(null);
    try {
      await apiClient.confirmHold(holdId, studentId);
      setNotification({
        type: 'success',
        title: 'Admission Confirmed!',
        message: 'Your fee payment was simulated and your seat is now permanently confirmed.',
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

  // Cancel hold
  const handleCancel = async (holdId: string) => {
    setNotification(null);
    try {
      await apiClient.cancelHold(holdId, studentId);
      setNotification({
        type: 'info',
        title: 'Reservation Cancelled',
        message: 'Your seat hold has been released and the capacity is available for other students.',
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
          message: 'An unexpected error occurred while cancelling the reservation.',
        });
      }
      void refreshAll();
    }
  };

  return (
    <div className="max-w-6xl mx-auto px-4 py-8 sm:px-6 lg:px-8">
      {/* Top Banner & Title */}
      <div className="mb-6 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight sm:text-3xl">
            School Admission Portal
          </h1>
          <p className="text-sm text-slate-500 mt-0.5">
            Real-time seat reservation, concurrency-safe holds, and admission workflow
          </p>
        </div>
        <div className="flex items-center space-x-2 text-xs text-slate-400">
          <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
          <span>Live sync active</span>
        </div>
      </div>

      {/* Global Toast / Notification */}
      {notification && (
        <div className="mb-6 transition-all">
          <Alert
            type={notification.type}
            title={notification.title}
            onClose={() => setNotification(null)}
          >
            {notification.message}
          </Alert>
        </div>
      )}

      {/* Student Identity Bar */}
      <StudentIdentityBar
        studentId={studentId}
        onStudentIdChange={setStudentId}
        isValid={isValid}
        validationError={validationError}
      />

      {/* Class Creation Form (Collapsible) & Classes Grid */}
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

      {/* Student's Applications History & Actions */}
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
