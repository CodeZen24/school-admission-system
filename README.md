# School Admission System

This repository contains the take-home School Admission System for the Assistant Software Engineer role at OnnoRokom Projukti Limited.

The application will use ASP.NET Core, PostgreSQL, and React. The backend foundation is in place; admission features, persistence, the frontend, and Docker setup will be added in the next milestones.

## Run the API locally

You need a .NET 8 SDK.

```powershell
dotnet restore backend/SchoolAdmission.sln --locked-mode
dotnet build backend/SchoolAdmission.sln --configuration Release --no-restore
dotnet test backend/SchoolAdmission.sln --configuration Release --no-build
dotnet run --project backend/src/SchoolAdmission.Api
```

The development profile listens on `http://localhost:8080`.

| URL | Purpose |
| --- | --- |
| `http://localhost:8080/health` | Basic API health check |
| `http://localhost:8080/swagger` | OpenAPI UI in Development |

## Configuration

| Setting | Default | Description |
| --- | --- | --- |
| `HOLD_DURATION_SECONDS` | `600` | Seat-hold lifetime in seconds. Values must be between 1 and 86400. |

`HOLD_DURATION_SECONDS` overrides `Admission:HoldDurationSeconds` from `appsettings.json`. Invalid values stop the API during startup.

## Checks

Run these before committing backend changes:

```powershell
dotnet format backend/SchoolAdmission.sln --verify-no-changes --no-restore
dotnet build backend/SchoolAdmission.sln --configuration Release --no-restore
dotnet test backend/SchoolAdmission.sln --configuration Release --no-build
```

Package versions are managed in `Directory.Packages.props`, and NuGet lock files are committed for repeatable restores.

Docker-only startup instructions will replace the local setup section once the Compose stack is available.
