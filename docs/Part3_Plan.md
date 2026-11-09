# RaceDay — Part 3 Plan

## Goal
Add an MVC web application (`RaceDay.Web`) that uses the Part 2 API for everything, store event banners and profile pictures in Azure Blob Storage **through the API**, run the MVC app in Docker, and extend CI to build it.

## Architecture
- `RaceDay.Web` has **no** EF Core, SQL or Azure packages. It calls the API with one typed `HttpClient` (`ApiClient`).
- The API authenticates with a server-side session cookie. `ApiSessionHandler` stores each user's API cookie in their own MVC session and sends it on every call (HttpClient cookie jar disabled so sessions can't mix).
- The MVC session holds user id, role, name and picture URL; `[RequireLogin("Role")]` redirects anonymous users to login and wrong-role users to an access-denied page. The API re-checks every call.
- Images: browser → MVC (size check) → API (`ImageRules`: size, content type, magic bytes) → `BlobStorageService` → Azure → URL saved on `Events.BannerImageUrl` / `Users.ProfilePictureUrl` → MVC `<img src>`.

## API changes needed by the UI
| Need | Change |
|---|---|
| Show banners / pictures | `BannerImageUrl`, `ProfilePictureUrl` columns + DTO fields |
| Organiser dashboard | `GET /api/events/mine` (events + enrolment and category counts) |
| Upload endpoints | `POST /api/events/{id}/banner`, `POST /api/users/me/picture` |
| My events | enrolment DTO carries event date and location |
| My results ("47th out of 312") | result DTO carries event name, date and total finishers |

## Risks and mitigations
See `README.md` (design notes) — session cookie leakage, expired sessions, comma decimal separators, Azure anonymous access, secrets in git, Docker reaching the host API, CI YAML quoting.
