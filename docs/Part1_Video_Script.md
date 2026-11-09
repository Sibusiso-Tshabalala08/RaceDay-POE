# RaceDay — Part 1 Video Script (~1:30–2:00)

## [0:00–0:15] Intro
"Hi, I'm [your name], and this is my Part 1 submission for RaceDay — an event
management system for South African running, walking, and cycling events.
In this video I'll walk through my ERD, my API endpoint plan, and then run
my SQL script live in SSMS."

## [0:15–0:45] ERD (screen: show RaceDay_ERD.png)
"My data model has six entities: Roles, Users, Events, Categories,
Enrolments, and Results. I used one Users table with a RoleId, rather than
separate Organiser and Participant tables, since both roles share the same
attributes — the difference in permissions is enforced in the API instead.

A couple of decisions I made deliberately: Events includes Latitude,
Longitude, and a RouteMapUrl — these aren't used until later parts, for live
weather and Azure Blob Storage, but I added them now so I wouldn't need to
change my schema later. And Categories supports both age-based categories,
like 'Under 20', and distance-based categories, like '10km', using a
CategoryType field — that was a direct requirement from the functional spec."

## [0:45–1:05] Endpoint Plan (screen: show RaceDay_API_Endpoint_Plan.md)
"My endpoint plan covers Authentication, User Profile, Events, Categories,
Enrolments, and Results, in that order — because each one depends on the
last existing first. Every endpoint has a required role, so permissions are
decided upfront, and every one lists failure cases, not just the happy
path — for example, enrolling in a full category returns a 400, and
enrolling twice returns a 409, which matches the unique constraint in my
database."

## [1:05–1:45] Live SQL run (screen: SSMS, open RaceDay_Database.sql)
"Now I'll run the SQL script live. This creates a dedicated RaceDay
database, then all six tables with their primary keys, foreign keys, and
constraints — for example, a check constraint that stops a category being
both age and distance-based at once."

[Click Execute — wait for it to finish]

"And it's run successfully — you can see the row counts for each insert:
2 roles, 4 users, 3 events, 6 categories, 4 enrolments, and 3 results."

[Expand Databases → RaceDay → Tables to show all 6 tables]

"Here are all six tables under the RaceDay database, matching my ERD
exactly."

[Optional: run a quick SELECT * FROM RaceDay.dbo.Events to show seed data]

## [1:45–2:00] Close
"That's my ERD, endpoint plan, and database script for Part 1. Thanks for
watching."
