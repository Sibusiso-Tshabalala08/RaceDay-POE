# RaceDay — Part 3 Video Script (aim for 6–8 minutes, unlisted on YouTube)

The brief says the video must show: the full running app, the MVC structure, Azure Blob integration and the Docker setup. Record in this order. Have BOTH browser windows ready (one normal, one private/incognito) so you can show two roles at once.

## Before you press record
- API running: `dotnet run --project RaceDay.Api --urls http://0.0.0.0:5000`
- MVC running **in Docker** at http://localhost:8080 (so the demo proves Docker works)
- Azure portal open on your storage account → Containers → raceday-images (empty or with a few files)
- A small JPG/PNG ready for a banner and one for a profile picture
- Visual Studio open on the solution with `RaceDay.Web` expanded

## [0:00–0:30] Intro
"Hi, I'm [name]. This is my RaceDay Part 3 submission: an MVC web app that consumes my Part 2 API, stores images in Azure Blob Storage, and runs in Docker. I'll demo both roles, then show the code, Azure and the Docker setup."

## [0:30–1:15] Docker first (terminal)
- Show `docker build -f RaceDay.Web/Dockerfile -t raceday-web .` already built, then run `docker run --rm -p 8080:8080 -e ApiBaseUrl=http://host.docker.internal:5000/ raceday-web`.
- Say: "The container only contains the MVC app. It reaches my API on the host through host.docker.internal — the ApiBaseUrl setting is the only difference from running locally."
- Open Docker Desktop → Containers to show it running.

## [1:15–2:45] Participant journey (normal window, http://localhost:8080)
1. Landing page → **Join RaceDay** → register a new Participant (show a validation error first: mismatched passwords).
2. Home page: upcoming events, use the type and distance filters.
3. Open an event: banner, categories, live weather → **Enrol**. Show the green *Confirmed* message.
4. **My events** → green Confirmed badge. **My results** → "47th out of 312 finishers" style row (use the seeded Sarah account if the new user has no result).
5. **Profile** → change name → upload a profile picture → it appears in the navbar.

## [2:45–4:45] Organiser journey (private window)
1. Log in as `thabo.nkosi@raceday.co.za` / `Password123!`. Point out the different navigation and the **Dashboard**: totals, enrolments per event, upcoming dates.
2. **Events → New event**: fill the form, choose a banner image, create it.
3. Show the **banner now displayed** on the events list and the event page.
4. **Categories**: add one Distance category and one Age category; show the friendly error when a field is wrong.
5. Switch to the participant window, enrol in the new event, then back to the organiser: **Enrolments** page shows the participant with a green status.
6. **Results**: enter a finish time and position, save; switch to the participant → **My results** shows it.
7. Role protection: as the participant, type `/Organiser/Events` in the address bar → access-denied page. Log out, type `/Dashboard` → redirected to login.

## [4:45–5:30] Azure Blob Storage
- Azure portal → storage account → container **raceday-images**: show `event-banners/…` and `profile-pictures/…` blobs that you just uploaded.
- Show the blob URL, then show in SQL Server (or the browser's image address) that the same URL is stored in `Events.BannerImageUrl`.
- Say: "The MVC app never talks to Azure. It posts the file to the API; the API validates it, uploads it with `BlobStorageService`, and saves the URL."

## [5:30–7:15] Code structure (Visual Studio)
- `RaceDay.Web`: Controllers (one per page group), Views, Models, `Services/ApiClient.cs` ("every API call lives here"), `ApiSessionHandler.cs` ("forwards each user's session cookie"), `Infrastructure/RequireLoginAttribute.cs` (role guard), `Program.cs` (HttpClient setup, `UseCookies = false`).
- Show the `.csproj`: "No Entity Framework, no Azure package — the MVC project cannot touch the database or Azure."
- API: `Services/BlobStorageService.cs` and `ImageRules.cs` (size, type, magic-byte checks), `EventsController.UploadBanner`.
- `Dockerfile`: build stage then runtime stage, port 8080, non-root user.

## [7:15–8:00] CI and wrap-up
- GitHub → Actions: green run showing build-and-test (includes `RaceDay.Web` build and the Docker image build) and the structure check.
- GitHub → commits: scroll the 20+ commit history on the part-3 branch / main.
- "Thanks for watching — repo link and README are in my submission."
