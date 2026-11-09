# RaceDay — Event Management System

## Overview

RaceDay is a full-stack, web-based event management system built for South Africa's road running, walking, and cycling community. It replaces the paper-based registration, spreadsheets, and disconnected communication that many local race organisers currently rely on.

The platform allows **Event Organisers** to create and manage events, define entry categories, and capture participant results. **Participants** can browse upcoming events, enter events by selecting a category, track their personal performance history, and prepare for race day using live weather and route information.

This repository contains **Part 1** of the RaceDay Portfolio of Evidence: system planning, including the database design (ERD), the API endpoint plan, and the SQL database script. No application code is written in this part — Parts 2 (RESTful API) and 3 (MVC web application) build on top of this foundation.

## User Roles

RaceDay supports two distinct user roles:

- **Organiser** — can create, edit, and delete events; manage event categories; capture participant results; and view all enrolments for their own events.
- **Participant** — can create an account, browse events, enter an event by selecting a category, view their own enrolments, and track their own results.

Role-based access will be enforced at the API level in Part 2 and reflected consistently in the MVC interface in Part 3.

## Repository Structure

```
├── .github/
│   └── workflows/
│       └── validate-repo-structure.yml   # CI/CD: validates required files exist
├── docs/
│   ├── RaceDay_ERD.png                   # Entity Relationship Diagram
│   ├── RaceDay_API_Endpoint_Plan.md      # Full API endpoint plan
│   └── RaceDay_Database.sql              # Database creation + seed script
└── README.md
```

## Part 1 Deliverables

### 1. Entity Relationship Diagram
Located at `docs/RaceDay_ERD.png`. Models 6 entities: `Roles`, `Users`, `Events`, `Categories`, `Enrolments`, and `Results`, with primary keys, foreign keys, and cardinality shown for every relationship.

A few deliberate design decisions worth noting:
- `Users` is a single table with a `RoleId` foreign key (rather than separate Organiser/Participant tables), since both roles share identical attributes — the difference in permissions is enforced at the application layer, not the schema layer.
- `Events` includes `Latitude`/`Longitude` (to support the live weather feature) and `RouteMapUrl` (to support Azure Blob Storage integration in Part 3), even though those features aren't built until later parts — this avoids schema changes further into the project.
- `Categories` supports **both** age-based (e.g. "Under 20", "Senior") and distance-based (e.g. "10km", "21km") categories via a `CategoryType` field, with `Distance` or `MinAge`/`MaxAge` populated accordingly. This is enforced with a `CHECK` constraint in the SQL script.
- `Enrolments` enforces one enrolment per participant per category via a `UNIQUE(ParticipantId, CategoryId)` constraint.
- `Results` has a `Status` field (Finished/DNF/DNS/DQ) and nullable `FinishTime`/`Position`, since not every participant who enrols finishes the race.

### 2. API Endpoint Plan
Located at `docs/RaceDay_API_Endpoint_Plan.md`. Covers all required areas — Authentication, User Profile, Events, Categories, Event Enrolments, and Results — with HTTP method, route, description, required role, request body, and expected response (including failure cases) specified for every endpoint.

### 3. SQL Database Script
Located at `docs/RaceDay_Database.sql`.

## Setup Instructions — Running the Database Script

1. Open **SQL Server Management Studio (SSMS)** and connect to your local or remote SQL Server instance.
2. Open a new query window.
3. Open `docs/RaceDay_Database.sql` and execute it.
4. The script will:
   - Create a new `RaceDay` database (if one doesn't already exist)
   - Create all 6 tables with primary keys, foreign keys, and constraints
   - Seed the database with sample data: 2 Organisers, 2 Participants, 3 Events, 6 Categories, 4 Enrolments, and 3 Results
5. To verify, expand **Databases → RaceDay → Tables** in Object Explorer, or run:
   ```sql
   SELECT * FROM RaceDay.dbo.Events;
   ```

No additional setup (no application code, no dependencies) is required for Part 1, since this part is planning and database design only.

## CI/CD

A GitHub Actions workflow (`.github/workflows/validate-repo-structure.yml`) runs automatically on every push to `main`. It validates that the `/docs` folder exists and contains the ERD, API endpoint plan, and SQL script, and that a README is present.

**Successful build:**

<!-- Paste your screenshot of the green GitHub Actions build here, e.g.: -->
<!-- ![CI/CD Passing](docs/cicd-success.png) -->

## Video Walkthrough

An unlisted YouTube video walking through the planning documents, ERD design decisions, endpoint plan choices, and a live run of the SQL script in SSMS:

<!-- Paste your unlisted YouTube link here -->
**[Video link here]**

---

## Part 2 — RESTful API

### Repository Structure (updated)

```
├── .github/
│   └── workflows/
│       ├── validate-repo-structure.yml   # Part 1 CI: validates /docs files
│       └── build-and-test.yml            # Part 2 CI: builds API + runs unit tests
├── docs/
│   ├── RaceDay_ERD.png
│   ├── RaceDay_API_Endpoint_Plan.md
│   └── RaceDay_Database.sql
├── RaceDay.Api/                          # ASP.NET Core Web API (Part 2)
│   ├── Controllers/                      # Auth, Users, Events, Categories, Enrolments, Results
│   ├── Models/                           # EF Core entities (match the Part 1 ERD exactly)
│   ├── Data/                             # DbContext, Code-First configuration, seed data
│   ├── DTOs/                             # Request/response shapes per the Part 1 endpoint plan
│   ├── Auth/                             # Session-based authentication + role enforcement
│   └── Services/                         # Weather service (Open-Meteo integration)
├── RaceDay.Api.Tests/                    # xUnit test project
├── RaceDay.sln
└── README.md
```

### What Part 2 Implements

A RESTful API built with ASP.NET Core Web API (C#), following the endpoint plan approved in Part 1 exactly — every route, role, and failure case from `docs/RaceDay_API_Endpoint_Plan.md` is implemented here.

**Database:** Entity Framework Core, Code-First. The model classes in `RaceDay.Api/Models/` and the Fluent API configuration in `RaceDay.Api/Data/RaceDayDbContext.cs` reproduce the Part 1 ERD and SQL script field-for-field — including the `CHECK` constraints (event type, category type/age/distance combination) and `UNIQUE` constraints (one email per user, one enrolment per participant per category, one result per enrolment) from Part 1's Section C.

**Authentication:** Session-based, not token-based. On login, the server stores `UserId` and `Role` in a server-side session (`RaceDay.Api/Auth/RequireSessionAttribute.cs`), and every subsequent request is authenticated and authorized from that session — matching the brief's explicit requirement for "session management to maintain the user's authenticated state and role." Passwords are hashed with BCrypt and never stored in plain text.

**Role enforcement:** A custom `[RequireSession]` authorization attribute enforces both "must be logged in" (401 if not) and "must be the right role" (403 if wrong role) on every protected endpoint — for example, only the Organiser who created an event can edit or delete it; only a Participant can enrol in a category.

**Weather integration:** `GET /api/events/{id}/weather` calls the free Open-Meteo API using the event's `Latitude`/`Longitude` from the Part 1 ERD, fulfilling the background brief's "live weather information" feature a full part ahead of when it was strictly due.

### Setup Instructions

**Prerequisites:** .NET 8 SDK, SQL Server (or SQL Server Express/LocalDB), and the EF Core CLI tool.

1. Clone the repository and restore dependencies:
   ```
   dotnet restore RaceDay.sln
   ```

2. Update the connection string in `RaceDay.Api/appsettings.json` if your SQL Server instance isn't on `localhost` with Windows/Trusted authentication.

3. **Generate the initial migration** (not committed to source control, since it must be generated against your own environment the first time):
   ```
   cd RaceDay.Api
   dotnet tool install --global dotnet-ef   (if not already installed)
   dotnet ef migrations add InitialCreate
   ```

4. Run the API:
   ```
   dotnet run --project RaceDay.Api
   ```
   On first run, the database is created automatically (via `Database.Migrate()`), and seed data is inserted — matching Part 1's Section C: 2 Organisers, 2 Participants, 3 Events, 6 Categories, 4 Enrolments, 3 Results.

5. Open Swagger UI at `https://localhost:5001/swagger` (or `http://localhost:5000/swagger`) to view and test every endpoint directly in the browser.

   **Seeded test accounts** (password for all: `Password123!`):
   - Organiser: `thabo.nkosi@raceday.co.za`
   - Participant: `sarah.vdm@example.com`

### Running the Tests

```
dotnet test RaceDay.Api.Tests/RaceDay.Api.Tests.csproj
```

22 tests across 4 test classes, covering both expected success and expected failure scenarios for every required area from the Part 2 brief:
- `AuthTests` — registration and login (valid/invalid roles, duplicate email, wrong password, protected routes without a session)
- `EventManagementTests` — Organiser create/update/delete, ownership enforcement, invalid event types
- `RoleEnforcementTests` — Participants rejected from Organiser-only actions and vice versa, anonymous requests rejected
- `EnrolmentTests` — correct Participant+Category+Event linkage, duplicate enrolment rejection, full-category rejection, Organiser visibility into their event's enrolments

Tests run against a real HTTP pipeline (via `WebApplicationFactory`) backed by an isolated EF Core in-memory database per test class, so they exercise the actual controllers, routing, and session middleware — not mocks.

### CI/CD

A second GitHub Actions workflow (`.github/workflows/build-and-test.yml`) runs on every push to `main`: it restores, builds the full solution, and runs all 22 unit tests. A failing test fails the build.

**Successful build:**

<!-- Paste your screenshot of the green Part 2 GitHub Actions build here, e.g.: -->
<!-- ![Part 2 CI/CD Passing](docs/cicd-success-part2.png) -->

### Video Walkthrough

An unlisted YouTube video demonstrating the running API, explaining the code structure, endpoints, authentication, and unit tests:

<!-- Paste your unlisted YouTube link here -->
**[Video link here]**
