import { useState, useEffect } from 'react';

const STORAGE_KEY = 'admission_student_id';
const DEFAULT_STUDENT_ID = '10000000000000001';

export function validateStudentId(id: string): { isValid: boolean; error?: string } {
  if (!id) {
    return { isValid: false, error: 'Student ID is required.' };
  }
  if (!/^\d+$/.test(id)) {
    return { isValid: false, error: 'Student ID must contain ASCII digits only.' };
  }
  if (id.length !== 17) {
    return {
      isValid: false,
      error: `Student ID must be exactly 17 digits (currently ${id.length}).`,
    };
  }
  return { isValid: true };
}

export function useStudentIdentity() {
  const [studentId, setStudentIdState] = useState<string>(() => {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      return stored && stored.trim().length > 0 ? stored.trim() : DEFAULT_STUDENT_ID;
    } catch {
      return DEFAULT_STUDENT_ID;
    }
  });

  const validation = validateStudentId(studentId);

  const setStudentId = (newId: string) => {
    const trimmed = newId.trim();
    setStudentIdState(trimmed);
    try {
      localStorage.setItem(STORAGE_KEY, trimmed);
    } catch {
    }
  };

  useEffect(() => {
    try {
      localStorage.setItem(STORAGE_KEY, studentId);
    } catch {
    }
  }, [studentId]);

  return {
    studentId,
    setStudentId,
    isValid: validation.isValid,
    validationError: validation.error,
  };
}
