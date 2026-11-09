# School Admission System

A small school-admission application for the Assistant Software Engineer (.NET) assignment. A class has a fixed number of seats. Students reserve a seat, confirm the simulated payment before the hold expires, or cancel the reservation.

PostgreSQL is the source of truth. Seat allocation locks the class row before counting active holds, so concurrent API requests cannot allocate more seats than a class has.

## What is included

- ASP.NET Core 8 API with PostgreSQL and EF Core migrations
- React 18, TypeScript, Vite, and Tailwind dashboard
- PostgreSQL-backed holds with confirmation, cancellation, and automatic expiry
- Real PostgreSQL integration tests, including a concurrent reservation test
- Docker Compose for the frontend, API, migration step, and database

## Run with Docker

Docker is the only requirement for the normal setup.

```bash
docker compose up --build
```

When startup finishes:

| Service | Address |
| --- | --- |
| Dashboard | `http://localhost:3000` |
| API | `http://localhost:8080` |
| API health check | `http://localhost:8080/health` |
| Swagger, in Development | `http://localhost:8080/swagger` |

The `migrate` service applies pending migrations before the API starts. PostgreSQL data is stored in the `school_admission_postgres_data` volume. Use `docker compose down` to stop the stack without deleting data. Use `docker compose down -v` only when a fresh database is wanted.

## Configuration

Copy `.env.example` to `.env` to override Compose defaults.

| Variable | Default | Purpose |
| --- | --- | --- |
| `HOLD_DURATION_SECONDS` | `600` | Hold length in seconds; must be between 1 and 86,400. |
| `BACKEND_PORT` | `8080` | Host port for the API. |
| `FRONTEND_PORT` | `3000` | Host port for the dashboard. |
| `POSTGRES_DB` | `school_admission` | Database name. |
| `POSTGRES_USER` | `postgres` | Database user. |
| `POSTGRES_PASSWORD` | `postgres` | Database password for local Compose use. |
| `POSTGRES_PORT` | `5432` | Host port for PostgreSQL. |
| `CORS_ORIGINS` | `http://localhost:3000,http://127.0.0.1:3000` | Comma-separated browser origins accepted by the API. |
| `VITE_API_BASE_URL` | `http://localhost:8080` | API URL compiled into the dashboard. |

## API

Errors use RFC 7807 Problem Details. Routes that operate on a student's holds require an `X-Student-Id` header containing exactly 17 ASCII digits.

| Method | Route | Success | Important failure responses |
| --- | --- | --- | --- |
| `POST` | `/classes` | `201 Created`; sends `id`, `name`, and `totalSeats`. | `400` invalid name or seat count. |
| `GET` | `/classes` | `200 OK`; includes total, held, confirmed, and available seats. | — |
| `GET` | `/classes/{classId}` | `200 OK`; one class with live seat counts. | `400` malformed ID; `404` unknown class. |
| `POST` | `/classes/{classId}/holds` | `201 Created`; requires `X-Student-Id`. | `400` invalid header or ID; `404` class; `409` full or duplicate application. |
| `GET` | `/holds/{holdId}` | `200 OK`; includes effective status and expiry. | `400` malformed ID; `404` unknown hold. |
| `POST` | `/holds/{holdId}/confirm` | `200 OK`; requires `X-Student-Id`; repeated confirmation is safe. | `400` invalid header or ID; `403` other student; `404` hold; `409` expired or cancelled. |
| `DELETE` | `/holds/{holdId}` | `204 No Content`; requires `X-Student-Id`; repeated cancellation is safe. | `400` invalid header or ID; `403` other student; `404` hold; `409` confirmed hold. |
| `GET` | `/me/holds` | `200 OK`; returns the current student's applications, newest first. | `400` invalid header. |

Example:

```bash
curl --request POST http://localhost:8080/classes \
  --header "Content-Type: application/json" \
  --data '{"name":"Class 10 - Science","totalSeats":30}'
```

## Local checks

Backend integration tests use Testcontainers and therefore need a working Docker engine.

```powershell
dotnet restore backend/SchoolAdmission.sln --locked-mode
dotnet format backend/SchoolAdmission.sln --verify-no-changes --no-restore
dotnet build backend/SchoolAdmission.sln --configuration Release --no-restore
dotnet test backend/SchoolAdmission.sln --configuration Release --no-build

npm ci --prefix frontend
npm run build --prefix frontend
```

The concurrency suite sends simultaneous reservation requests to real PostgreSQL instances across multiple load configurations (20 requests against 1 seat, 50 requests against 10 seats, and 50 requests against 25 seats). It verifies that successful holds never exceed class capacity, surplus requests receive HTTP 409 Conflict, and database state matches expected seat counts.

## Manual check before submission

After `docker compose up --build` finishes, open `http://localhost:3000` and work through this list:

1. Enter a valid 17-digit student ID, create a class, and confirm that it appears with the correct seat count.
2. Apply for a seat. Confirm that the held and available counts change, a countdown appears, and the application is listed below.
3. Click **Confirm / Pay Fee**, then click it again. The application should stay confirmed and the confirmed count should not increase twice.
4. Create a second class, apply, then cancel. Confirm that its seat becomes available again. Try cancelling a confirmed application and confirm that the UI shows a clear conflict message.
5. Change the student ID. The previous student's applications should disappear. Change back and confirm that the history returns.
6. Create a class with one seat. In another browser profile or a second browser, use a different student ID and try to apply at the same time. Only one request should succeed.
7. Set `HOLD_DURATION_SECONDS=10` in `.env`, rebuild with `docker compose up --build`, create a hold, wait for the countdown to finish, and confirm that the seat is released without refreshing the page.
8. Create a class or hold, run `docker compose down`, then run `docker compose up` again. Confirm that the data is still there. Use `docker compose down -v` only after this check if you want to reset the database.

## Troubleshooting

- If a port is already in use, change `BACKEND_PORT`, `FRONTEND_PORT`, or `POSTGRES_PORT` in `.env` and restart Compose.
- If migration startup fails after changing the database configuration, check `docker compose logs migrate` and `docker compose logs db`.
- If the frontend cannot reach the API, confirm `VITE_API_BASE_URL` and `CORS_ORIGINS`, then rebuild the frontend image because Vite reads its API URL at build time.
- To reset local data, stop the stack and run `docker compose down -v`.

See [ASSUMPTIONS.md](ASSUMPTIONS.md) for decisions that were outside the fixed assignment contract, and [AI_USAGE.md](AI_USAGE.md) for the AI disclosure.
