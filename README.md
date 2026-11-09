# School Admission System

A production-grade, concurrency-safe School Admission & Seat Reservation System built for the Assistant Software Engineer (.NET) assignment at OnnoRokom Projukti Limited.

The system guarantees the core business invariant across multiple concurrent requests and API instances:
$$\text{heldSeats} + \text{confirmedSeats} \le \text{totalSeats}$$

Seat allocation is strictly first-come, first-served (FCFS), backed by PostgreSQL row-level locks and database-driven seat derivation. It includes an automated background expiration worker and a modern, responsive React + Tailwind CSS dashboard with real-time countdown timers.

---

## Architecture Overview

```
                                      ┌─────────────────────────────────────────┐
                                      │   React 18 + TypeScript + Tailwind CSS  │
                                      │          (Port 3000 / Nginx)            │
                                      └────────────────────┬────────────────────┘
                                                           │ HTTP / X-Student-Id
                                                           ▼
                                      ┌─────────────────────────────────────────┐
                                      │     ASP.NET Core (.NET 8) Web API       │
                                      │               (Port 8080)               │
                                      │  ┌───────────────────────────────────┐  │
                                      │  │ HoldExpirationWorker (Background) │  │
                                      │  └───────────────────────────────────┘  │
                                      └────────────────────┬────────────────────┘
                                                           │ EF Core 8 / Npgsql
                                                           ▼
                                      ┌─────────────────────────────────────────┐
                                      │             PostgreSQL 16               │
                                      │   - SELECT ... FOR UPDATE Row Locks     │
                                      │   - Partial Unique Indices              │
                                      │   - Check Constraints & DB Time now()   │
                                      │   - Named Persistent Docker Volume      │
                                      └─────────────────────────────────────────┘
```

- **Backend:** ASP.NET Core (.NET 8), Entity Framework Core 8, Npgsql.
- **Concurrency & Invariants:** Serialized seat allocation using PostgreSQL row-level locking (`SELECT ... FOR UPDATE` at `READ COMMITTED`). Dynamic single-query seat calculation using database time (`now()`), eliminating counter drift, race conditions, and N+1 queries.
- **Hold Lifecycle:** Background worker (`HoldExpirationWorker`) automatically materializes expired holds in PostgreSQL without requiring API traffic.
- **Frontend:** React 18, TypeScript, Vite 5, Tailwind CSS v3, visibility-aware polling, live countdown timers, and RFC 7807 Problem Details error mapping.
- **Deployment:** Multi-stage Dockerfiles and root `docker-compose.yml` with health checks and persistent storage.

---

## Quick Start (Docker Compose)

The entire stack runs with a single command on any machine with Docker installed.

### 1. Start the stack

```bash
docker compose up --build
```

This starts:
1. `db`: PostgreSQL 16 on port `5432` with health checking via `pg_isready`.
2. `migrate`: One-shot container that applies EF Core migrations and exits cleanly.
3. `backend`: ASP.NET Core API on port `8080` (starts only after migration completes successfully).
4. `frontend`: React SPA served by Nginx on port `3000` (starts after backend is healthy).

### 2. Access the Application

| Service | URL | Notes |
| --- | --- | --- |
| **Frontend Dashboard** | `http://localhost:3000` | Full admission workflow, student switcher, countdown timers |
| **Backend API** | `http://localhost:8080` | Base URL for all REST endpoints |
| **OpenAPI / Swagger** | `http://localhost:8080/swagger` | Interactive API documentation |
| **API Health Check** | `http://localhost:8080/health` | Returns `200 Healthy` |

### 3. Data Persistence & Cleanup

- **Retain Data:** Stop containers with `docker compose down`. All created classes, holds, and student enrollments are safely preserved in the named Docker volume `school_admission_postgres_data`.
- **Clean Reset:** Stop containers and wipe the database volume:
  ```bash
  docker compose down -v
  ```

---

## Configuration

All parameters can be customized via environment variables or a `.env` file (see `.env.example`):

| Variable | Default | Purpose |
| --- | --- | --- |
| `HOLD_DURATION_SECONDS` | `600` | Lifetime of a seat hold in seconds (1–86400). Validated on startup. |
| `BACKEND_PORT` | `8080` | Host port for the ASP.NET Core API. |
| `FRONTEND_PORT` | `3000` | Host port for the React dashboard. |
| `POSTGRES_DB` | `school_admission` | PostgreSQL database name. |
| `POSTGRES_USER` | `postgres` | PostgreSQL username. |
| `POSTGRES_PASSWORD` | `postgres` | PostgreSQL password. |
| `POSTGRES_PORT` | `5432` | Host port for PostgreSQL. |
| `CORS_ORIGINS` | `http://localhost:3000,http://127.0.0.1:3000` | Allowed origins for cross-origin browser requests. |
| `VITE_API_BASE_URL` | `http://localhost:8080` | Backend URL used by the frontend client. |

---

## API Reference & cURL Examples

All endpoints accept and return `application/json`. Error responses follow RFC 7807 Problem Details.

### 1. Create a Class
`POST /classes`
```bash
curl -i -X POST http://localhost:8080/classes \
  -H "Content-Type: application/json" \
  -d '{"name": "Class 10 - Science", "totalSeats": 30}'
```
**Response:** `201 Created` with `Location: /classes/{classId}`

### 2. List All Classes
`GET /classes`
```bash
curl -i http://localhost:8080/classes
```
**Response:** `200 OK`
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Class 10 - Science",
    "totalSeats": 30,
    "heldSeats": 2,
    "confirmedSeats": 5,
    "availableSeats": 23
  }
]
```

### 3. Get Class Details
`GET /classes/{classId}`
```bash
curl -i http://localhost:8080/classes/3fa85f64-5717-4562-b3fc-2c963f66afa6
```
**Response:** `200 OK` (or `404 Not Found` if unknown, `400 Bad Request` if malformed GUID).

### 4. Apply for a Seat Hold
`POST /classes/{classId}/holds`
Requires a 17-digit student identity header:
```bash
curl -i -X POST http://localhost:8080/classes/3fa85f64-5717-4562-b3fc-2c963f66afa6/holds \
  -H "X-Student-Id: 10000000000000001"
```
**Response:** `201 Created`
```json
{
  "holdId": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
  "classId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "expiresAt": "2026-11-09T14:00:00Z"
}
```
*Note: Returns `409 Conflict` if the class has zero available seats or if the student already has an active hold or confirmed seat in this class.*

### 5. Get Hold Details
`GET /holds/{holdId}`
```bash
curl -i http://localhost:8080/holds/a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d
```
**Response:** `200 OK`
```json
{
  "holdId": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
  "classId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "HELD",
  "expiresAt": "2026-11-09T14:00:00Z"
}
```

### 6. Confirm Hold & Simulate Fee Payment
`POST /holds/{holdId}/confirm`
```bash
curl -i -X POST http://localhost:8080/holds/a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d/confirm \
  -H "X-Student-Id: 10000000000000001"
```
**Response:** `200 OK`
```json
{
  "holdId": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
  "status": "CONFIRMED"
}
```
*Note: Only the owner student can confirm. Confirming an already confirmed hold is idempotent (`200 OK`). Confirming an expired or cancelled hold returns `409 Conflict`.*

### 7. Cancel a Hold
`DELETE /holds/{holdId}`
```bash
curl -i -X DELETE http://localhost:8080/holds/a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d \
  -H "X-Student-Id: 10000000000000001"
```
**Response:** `204 No Content` (empty body)
*Note: Only the owner student can cancel. Repeated cancellation is idempotent (`204 No Content`). Confirmed holds cannot be cancelled (`409 Conflict`).*

### 8. List My Applications
`GET /me/holds`
```bash
curl -i http://localhost:8080/me/holds \
  -H "X-Student-Id: 10000000000000001"
```
**Response:** `200 OK` (ordered newest first)
```json
[
  {
    "holdId": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
    "classId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "className": "Class 10 - Science",
    "status": "CONFIRMED",
    "expiresAt": "2026-11-09T14:00:00Z"
  }
]
```

---

## Local Development & Testing

### Running Tests

Automated integration tests run against real PostgreSQL instances using Testcontainers:

```powershell
# Restore dependencies in locked mode
dotnet restore backend/SchoolAdmission.sln --locked-mode

# Verify zero code-style or formatting issues
dotnet format backend/SchoolAdmission.sln --verify-no-changes --no-restore

# Build with warnings as errors
dotnet build backend/SchoolAdmission.sln --configuration Release --no-restore

# Execute the 68 integration tests
dotnet test backend/SchoolAdmission.sln --configuration Release --no-build
```

### Running Frontend Locally

```powershell
cd frontend
npm install
npm run dev
```

Dashboard starts at `http://localhost:3000`.

---

## Concurrency Verification Evidence

The test suite includes a high-concurrency reservation load test (`HoldApplicationTests.ApplyConcurrentlyFiftyRequestsForFiveSeatsAllocatesExactlyCapacityWithoutOverselling`):
- 50 concurrent tasks compete for a class with only 5 seats.
- Exactly 5 holds succeed with `201 Created`.
- Remaining 45 requests receive `409 Conflict`.
- Database invariant `heldSeats + confirmedSeats <= totalSeats` is asserted directly in PostgreSQL.
