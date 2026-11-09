# AI Usage Disclosure

In compliance with the assignment guidelines, this document provides a comprehensive and transparent record of the artificial intelligence assistance utilized throughout the development of the School Admission System.

---

## 1. Tools and Models Used

- **Google Antigravity:** Gemini 2.5 Pro agentic coding assistant used for end-to-end pair programming, roadmap tracking, code generation, refactoring, and integration testing.
- **OpenAI Models (Codex / GPT-4o):** Consulted during initial requirements decomposition, contract review, and edge-case brainstorming.

---

## 2. Tasks Assisted by AI

AI tools assisted across the following development areas:

1. **Requirements Decomposition & Planning:**
   - Translating the assignment PDF (`Asst_Software_Engineer_Project_Assignment.pdf`) into an exhaustive requirement traceability matrix and phased milestone roadmap (`ROADMAP.md`).
   - Identifying critical concurrency edge cases: final-seat race conditions, idempotent re-confirmation, expired-hold capacity starvation, and duplicate active application backstops.

2. **Backend Architecture & Code Scaffolding:**
   - Structuring a modular ASP.NET Core (.NET 8) monolith with Entity Framework Core 8 and Npgsql.
   - Drafting EF Core entity configurations, PostgreSQL check constraints, and partial unique indexes.
   - Crafting the PostgreSQL `SELECT ... FOR UPDATE` row-locking transaction for seat allocations.
   - Designing the `HoldExpirationWorker` background service for autonomous expiration.

3. **Frontend Implementation:**
   - Scaffolding a Vite + React 18 + TypeScript application.
   - Implementing custom React hooks: `useStudentIdentity` (`localStorage` sync), `usePolling` (visibility-aware background sync), and `useCountdown` (tick-based timer).
   - Designing and restyling the UI using Tailwind CSS v3 utility classes for an accessible, responsive dashboard.

4. **Integration Testing & Testcontainers:**
   - Setting up `Testcontainers.PostgreSql` to run 68 automated integration tests against real PostgreSQL instances.
   - Implementing a 50-task concurrent seat reservation test to verify zero overselling under heavy contention.

5. **Docker & CI Automation:**
   - Authoring multi-stage Dockerfiles for backend (.NET 8 Alpine runtime) and frontend (Node 20 build + Nginx Alpine static serving).
   - Composing the root `docker-compose.yml` with health checks, migration sequencing, and persistent named volume.
   - Writing the GitHub Actions CI pipeline testing backend tests, frontend builds, and Docker Compose builds.

---

## 3. Critical Corrections & Decisions Made by the Developer

Every piece of AI-generated code and proposal was reviewed against the assignment requirements and project constraints. Key corrections and adjustments made:

1. **EF Core Package Version Alignment:**
   - *Problem:* AI initially pulled mismatched versions (`Microsoft.EntityFrameworkCore.Design` 8.0.33 vs `Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.11), triggering build and analyzer conflicts.
   - *Correction:* Manually pinned and aligned both packages to version `8.0.11` across the solution via `Directory.Packages.props`.

2. **Route Constraint Mapping (400 vs 404):**
   - *Problem:* ASP.NET Core route regex constraints (`{classId:guid}`) resulted in `404 Not Found` when callers supplied malformed GUIDs like `/classes/invalid-guid`.
   - *Correction:* Removed route constraints to allow model binding to capture the string, returning the required RFC 7807 `400 Bad Request` Problem Details.

3. **Database Time Synchronization:**
   - *Problem:* Initial hold availability logic compared hold timestamps against the host application clock (`DateTimeOffset.UtcNow`), making the system vulnerable to container clock skew.
   - *Correction:* Replaced application-side time with PostgreSQL database UTC time (`now()` / `transaction_timestamp()`) across all queries and seat calculations.

4. **PostgreSQL Check Constraint Test Isolation:**
   - *Problem:* When testing overlength student IDs, PostgreSQL rejected the 18th character with error `22001 StringDataRightTruncation` before the check constraint was evaluated.
   - *Correction:* Separated the format regex check constraint tests from the column length truncation test to accurately verify each layer of defense.

5. **Tailwind CSS Utility Adoption:**
   - *Problem:* Initial frontend layout used raw CSS stylesheets.
   - *Correction:* Replaced raw styles with Tailwind CSS v3 across all components, configuring PostCSS without native binary dependencies to ensure clean, cross-platform compilation.

6. **Docker Locked-Mode Restore Fix:**
   - *Problem:* `backend/Dockerfile` copied a non-existent `packages.lock.json` at the root of `backend/`, failing Docker builds.
   - *Correction:* Corrected the Dockerfile to copy project-specific `packages.lock.json` files for both the API and integration test projects, enabling deterministic locked restores inside containers.

---

## 4. Verification Methods

No AI suggestion was accepted without empirical verification:
- **Build Quality:** Enforced `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` and `<AnalysisLevel>latest-recommended</AnalysisLevel>` in `Directory.Build.props`.
- **Formatting:** Verified with `dotnet format --verify-no-changes`.
- **End-to-End CI Pipeline:** Verified via automated GitHub Actions CI runs on Ubuntu runners executing 68 integration tests against PostgreSQL, building the frontend with zero errors, and validating Docker Compose builds.
