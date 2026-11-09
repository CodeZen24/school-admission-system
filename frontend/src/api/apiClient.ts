import type {
  AdmissionClass,
  ConfirmHoldResponse,
  CreateClassRequest,
  CreateHoldResponse,
  HoldDetailsResponse,
  ProblemDetails,
  StudentHoldResponse,
} from './types';

const API_BASE_URL = (
  import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080'
).replace(/\/+$/, '');

export class ApiError extends Error {
  public readonly status: number;
  public readonly problemDetails?: ProblemDetails;

  constructor(status: number, message: string, problemDetails?: ProblemDetails) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.problemDetails = problemDetails;
  }
}

async function request<T>(
  path: string,
  options: {
    method?: 'GET' | 'POST' | 'DELETE';
    body?: unknown;
    studentId?: string;
  } = {},
): Promise<T> {
  const url = `${API_BASE_URL}${path}`;
  const headers: Record<string, string> = {};

  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  if (options.studentId) {
    headers['X-Student-Id'] = options.studentId;
  }

  let response: Response;
  try {
    response = await fetch(url, {
      method: options.method || 'GET',
      headers,
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
    });
  } catch (error) {
    const message =
      error instanceof Error && error.message
        ? `Network error: ${error.message}. Is the backend API running on ${API_BASE_URL}?`
        : `Unable to connect to backend on ${API_BASE_URL}`;
    throw new ApiError(0, message);
  }

  if (response.status === 204) {
    return undefined as unknown as T;
  }

  const contentType = response.headers.get('content-type') || '';
  const isJson = contentType.includes('application/json') || contentType.includes('application/problem+json');

  if (!response.ok) {
    if (isJson) {
      try {
        const problem = (await response.json()) as ProblemDetails;
        const message =
          problem.detail ||
          problem.title ||
          `Request failed with status ${response.status}`;
        throw new ApiError(response.status, message, problem);
      } catch (err) {
        if (err instanceof ApiError) {
          throw err;
        }
      }
    }

    const fallbackText = await response.text().catch(() => '');
    const message = fallbackText || `Request failed with HTTP ${response.status}`;
    throw new ApiError(response.status, message);
  }

  if (isJson) {
    return (await response.json()) as T;
  }

  return undefined as unknown as T;
}

export const apiClient = {
  getClasses(): Promise<AdmissionClass[]> {
    return request<AdmissionClass[]>('/classes');
  },

  getClass(classId: string): Promise<AdmissionClass> {
    return request<AdmissionClass>(`/classes/${encodeURIComponent(classId)}`);
  },

  createClass(payload: CreateClassRequest): Promise<AdmissionClass> {
    return request<AdmissionClass>('/classes', {
      method: 'POST',
      body: payload,
    });
  },

  createHold(classId: string, studentId: string): Promise<CreateHoldResponse> {
    return request<CreateHoldResponse>(`/classes/${encodeURIComponent(classId)}/holds`, {
      method: 'POST',
      studentId,
    });
  },

  getHold(holdId: string): Promise<HoldDetailsResponse> {
    return request<HoldDetailsResponse>(`/holds/${encodeURIComponent(holdId)}`);
  },

  confirmHold(holdId: string, studentId: string): Promise<ConfirmHoldResponse> {
    return request<ConfirmHoldResponse>(`/holds/${encodeURIComponent(holdId)}/confirm`, {
      method: 'POST',
      studentId,
    });
  },

  cancelHold(holdId: string, studentId: string): Promise<void> {
    return request<void>(`/holds/${encodeURIComponent(holdId)}`, {
      method: 'DELETE',
      studentId,
    });
  },

  getMyHolds(studentId: string): Promise<StudentHoldResponse[]> {
    return request<StudentHoldResponse[]>('/me/holds', {
      studentId,
    });
  },
};
