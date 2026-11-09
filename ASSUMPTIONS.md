# Assumptions and Decisions

This file records the choices I made where the assignment did not define an exact implementation detail.

## IDs and classes

- I use UUIDs for class and hold IDs. The identifiers in the assignment examples are treated as examples rather than a required format.
- A class represents one admission unit, such as `Class 6 - Morning Shift`. I did not add a separate shift entity because the required API already treats the unit as a class.
- A class name is required, trimmed, and limited to 200 characters. Duplicate names are allowed because separate sections may reasonably share a name.
- Class capacity cannot be changed after creation. Updating capacity was not part of the assignment.

## Student identity

- `X-Student-Id` must contain exactly 17 ASCII digits. I use it to determine who owns a hold.
- This header is not authentication. The assignment scope does not require accounts, passwords, sessions, or JWTs, so the application treats the supplied value as a trusted identifier.

## Seats and concurrent requests

- PostgreSQL is responsible for admission correctness. When a student applies, the API locks the relevant class row with `SELECT ... FOR UPDATE` before it checks capacity and creates a hold.
- Seat counts are calculated from holds instead of being stored as counters. A live `HELD` row and every `CONFIRMED` row occupy a seat; `CANCELLED` and `EXPIRED` rows do not.
- This design guarantees that occupied seats never exceed the class capacity across concurrent requests and API instances. It does not claim strict wall-clock ordering between requests; PostgreSQL lock scheduling decides which competing transaction proceeds first.
- The database clock is used for hold expiry and transitions so that the API and PostgreSQL do not disagree because of machine clock differences.

## Holds

- A student may have only one `HELD` or `CONFIRMED` application for the same class. PostgreSQL enforces this with a partial unique index.
- A hold can be `HELD`, `CONFIRMED`, `CANCELLED`, or `EXPIRED`.
- Confirmation is idempotent: confirming an already confirmed hold returns success without changing seat counts again.
- Cancellation is idempotent for cancelled and expired holds. A confirmed hold cannot be cancelled.
- A background worker marks expired holds without needing another API request. Seat queries also treat an overdue hold as unavailable immediately, so a delayed worker does not block a seat.

## Database validation

- The database validates positive capacity, non-empty class names, student ID format, valid status values, timestamp ordering, and the timestamps required by confirmed and cancelled states.
- These checks are intentionally in PostgreSQL as well as in the API. They protect the data if another process writes to the database or if an application bug bypasses request validation.

## Frontend and Docker

- The dashboard saves the last student ID in browser `localStorage`. Applications remain in PostgreSQL and are fetched again after refresh or browser restart.
- The dashboard polls while the tab is visible and shows a countdown for active holds. The server remains the authority if the browser countdown and server state differ.
- Docker Compose uses a named PostgreSQL volume. `docker compose down` keeps data, while `docker compose down -v` removes it deliberately.
- The frontend API URL is a Vite build-time setting. When `VITE_API_BASE_URL` changes, the frontend image must be rebuilt.
