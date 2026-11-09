# Assumptions and Architectural Decisions

This document details the architectural decisions and domain assumptions made during the implementation of the School Admission System, distinguishing the core assignment specifications from design choices where requirements were open-ended.

---

## 1. Identifiers
- **Format:** All entity identifiers (`classId`, `holdId`) are standard UUID v4 strings (e.g. `3fa85f64-5717-4562-b3fc-2c963f66afa6`).
- **Assignment Examples:** The assignment examples (`c_123`, `h_789`) are treated as illustrative placeholders. Standard UUIDs prevent ID enumeration and coordinate cleanly across distributed services.

## 2. Class Definitions & Capacity
- **Name:** Class names are trimmed, required, non-empty, and constrained to a maximum of 200 characters.
- **Uniqueness:** Duplicate class names are permitted because schools frequently have multiple sections of the same grade (e.g., separate shifts or rooms).
- **Capacity:** `totalSeats` must be a positive 32-bit integer (`totalSeats > 0`), enforced both at API validation and by a PostgreSQL `CHECK` constraint (`ck_admission_classes_total_seats_positive`).
- **Dynamic Allocation:** Capacity editing after creation was not specified in the assignment and is deliberately excluded.

## 3. Student Identity & Security Boundaries
- **Format:** The `X-Student-Id` header must contain exactly 17 ASCII digits (e.g., `10000000000000001`), mirroring the Bangladesh national 17-digit birth-registration number.
- **Header Trust:** As specified in the developer assignment, the `X-Student-Id` header represents trusted identity for the scope of this project. It is not cryptographic authentication (no JWT/passwords/sessions).
- **Ownership Enforcement:** Every mutating hold operation (`POST /holds/{holdId}/confirm`, `DELETE /holds/{holdId}`) strictly verifies that the requesting student ID matches the hold's original student ID, returning `403 Forbidden` on mismatch.

## 4. Concurrency & Seat Calculation
- **Capacity Invariant:** At all times, the system guarantees $\text{heldSeats} + \text{confirmedSeats} \le \text{totalSeats}$.
- **Row-Level Locking:** Seat allocation is serialized at the class row level in PostgreSQL using `SELECT total_seats FROM admission_classes WHERE id = @id FOR UPDATE` at the `READ COMMITTED` isolation level. This guarantees zero overselling across concurrent threads, containers, or API instances.
- **No Cached Counters:** `heldSeats`, `confirmedSeats`, and `availableSeats` are calculated dynamically via conditional aggregation in a single query:
  - `heldSeats`: rows with `status = 'HELD' AND expires_at > now()`
  - `confirmedSeats`: rows with `status = 'CONFIRMED'`
  - `availableSeats`: $\max(\text{totalSeats} - \text{heldSeats} - \text{confirmedSeats}, 0)$
  This eliminates counter drift, desynchronization, and race conditions.
- **Database Time:** All expiration boundaries and timestamps are evaluated against PostgreSQL UTC time (`now()`), eliminating container clock skew.

## 5. Duplicate Applications
- **Rule:** A student may not hold more than one active reservation (`HELD`) or confirmed seat (`CONFIRMED`) in the same class simultaneously.
- **Enforcement:** Enforced at the database level by a partial unique index:
  `CREATE UNIQUE INDEX ix_admission_holds_class_id_student_id_active ON admission_holds (class_id, student_id) WHERE status IN ('HELD', 'CONFIRMED');`
- **Re-application:** If a previous hold expires or is cancelled, the partial index frees the student to apply again.

## 6. Hold State Transitions & Idempotency
- **Lifecycle States:** Four explicit states: `HELD`, `CONFIRMED`, `CANCELLED`, `EXPIRED`.
- **Idempotent Confirmation:** Calling `POST /holds/{holdId}/confirm` on an already confirmed hold returns `200 OK` without double-counting seats.
- **Idempotent Cancellation:** Calling `DELETE /holds/{holdId}` on an already cancelled or expired hold returns `204 No Content` without throwing errors.
- **Illegal Transitions:**
  - Confirming an `EXPIRED` or `CANCELLED` hold returns `409 Conflict`.
  - Cancelling a `CONFIRMED` hold returns `409 Conflict`.

## 7. Automatic Background Expiration
- **Autonomous Release:** A background worker (`HoldExpirationWorker`) runs periodically every 2 seconds to materialize expired holds in PostgreSQL (`UPDATE admission_holds SET status = 'EXPIRED' WHERE status = 'HELD' AND expires_at <= now()`).
- **Zero-Traffic Reclaim:** Even if the background worker has not yet completed a tick, seat calculation queries immediately treat past-due holds as available, ensuring zero capacity starvation.

## 8. Frontend Experience & Polling
- **Styling:** Styled with Tailwind CSS v3 using an accessible, responsive card-and-table layout.
- **Visibility-Aware Polling:** Dashboard polls the backend every 4 seconds while the browser tab is visible (`document.visibilityState === 'visible'`). Polling suspends when the tab is hidden to conserve bandwidth.
- **Live Countdown:** Holds display a real-time countdown timer. The "Confirm / Pay Fee" button disables dynamically the exact second remaining time reaches 00:00 without requiring a page reload.
- **Persistence:** The active student identity is stored in `localStorage` under `admission_student_id`, surviving page refreshes and browser restarts.

## 9. Error Format
- **RFC 7807 Problem Details:** All API errors return a standardized `application/problem+json` payload containing `title`, `status`, `detail`, `instance`, and a correlation `traceId`.

## 10. Persistence & Docker
- **Named Volume:** PostgreSQL data is persisted in the named Docker volume `school_admission_postgres_data`.
- **Graceful Shutdown:** `docker compose down` stops containers while preserving all data. `docker compose down -v` is the documented command for an intentional, clean reset.
