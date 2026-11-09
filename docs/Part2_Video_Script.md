# RaceDay — Part 2 Video Script (~3–4 minutes)

Per the rubric's top band, this needs to cover: the running API, code
structure, all endpoints, authentication, Swagger, and unit tests. That's
more ground than Part 1's video, so budget more time.

## [0:00–0:20] Intro
"Hi, I'm [your name], this is my Part 2 submission for RaceDay — the
RESTful API that implements the endpoint plan I built in Part 1. I'll show
the running API in Swagger, walk through the code structure, demonstrate
authentication and role-based access, and run the unit tests."

## [0:20–0:50] Code structure (screen: Visual Studio / editor, Solution Explorer open)
"My project follows a standard layered structure. Models holds the six
EF Core entities, matching my Part 1 ERD exactly. Data holds the DbContext
— I used the Fluent API to set up the same constraints from my Part 1 SQL
script, like the check constraint that stops a category being both age and
distance-based. DTOs keep the request and response shapes separate from my
database models. Auth holds a custom RequireSession attribute that enforces
both login and role checks. And Controllers has one file per resource:
Auth, Users, Events, Categories, Enrolments, and Results — matching my
Part 1 endpoint plan one-to-one."

## [0:50–1:20] Authentication (screen: Swagger, or Postman/browser)
"Authentication is session-based, not token-based, per the brief. When I
log in [demonstrate POST /api/auth/login with a seeded account], the
server stores my user ID and role in a server-side session. Passwords are
hashed with BCrypt — never stored in plain text [show the Users table or
the hashed value briefly]. From here, every request I make is tied to that
session."

## [1:20–2:10] Role enforcement (screen: Swagger, two browser sessions or Postman)
"Now I'll show role enforcement actually working. Logged in as an
Organiser, I can create an event [POST /api/events — show 201]. If I try
the same request without being logged in at all, I get a 401 [demonstrate].
And if I log in as a Participant and try to create an event, I get a 403
Forbidden [demonstrate] — because event creation is Organiser-only. I also
enforce ownership: [log in as a second Organiser, try to edit the first
Organiser's event] — also 403, because you can only edit events you
created yourself."

## [2:10–2:40] A full flow: enrolment (screen: Swagger)
"Here's a full participant flow. I'll browse events [GET /api/events,
public, no login needed], view categories for one [GET
/api/events/{id}/categories], then as a logged-in Participant, enrol in a
category [POST /api/categories/{id}/enrolments — show 201]. If I try to
enrol in the same category again, I get a 409 Conflict, because my
database has a unique constraint stopping duplicate enrolments [demonstrate]."

## [2:40–3:00] Swagger (screen: Swagger UI, scroll through all groups)
"All 24 endpoints from my Part 1 plan are visible and testable here in
Swagger, grouped by controller, each with a description of what it does,
the role required, and its expected responses — this is a live mirror of
the endpoint plan table I submitted in Part 1."

## [3:00–3:40] Unit tests (screen: terminal or Test Explorer)
"Finally, my unit tests. I have 22 tests across four classes, covering
registration and login, event management, role enforcement, and
enrolments — each with both success and failure cases." [Run `dotnet test`
live, let it finish]
"And they all pass — 22 succeeded, 0 failed. These run against a real
in-memory version of the API on every push, through my GitHub Actions
workflow, which is how I get the green build I've linked in my README."

## [3:40–4:00] Close
"That's my Part 2 submission — the full API, session-based authentication,
role enforcement, Swagger documentation, and a passing test suite, all
matching the plan I approved in Part 1. Thanks for watching."
