export type HoldStatus = 'HELD' | 'CONFIRMED' | 'CANCELLED' | 'EXPIRED';

export interface AdmissionClass {
  id: string;
  name: string;
  totalSeats: number;
  heldSeats: number;
  confirmedSeats: number;
  availableSeats: number;
  createdAt?: string;
}

export interface CreateClassRequest {
  name: string;
  totalSeats: number;
}

export interface CreateHoldResponse {
  holdId: string;
  classId: string;
  expiresAt: string;
}

export interface HoldDetailsResponse {
  holdId: string;
  classId: string;
  studentId: string;
  status: HoldStatus;
  expiresAt: string;
  createdAt: string;
}

export interface ConfirmHoldResponse {
  holdId: string;
  status: 'CONFIRMED';
  confirmedAt: string;
}

export interface StudentHoldResponse {
  holdId: string;
  classId: string;
  className: string;
  status: HoldStatus;
  expiresAt: string;
  createdAt: string;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}
