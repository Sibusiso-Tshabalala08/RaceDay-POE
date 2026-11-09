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
