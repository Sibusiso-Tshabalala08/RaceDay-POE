# RaceDay — API Endpoint Plan (Part 1, Section B)

Role Required options: **None** (public), **Any** (any logged-in user), **Organiser**, **Participant**.

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Creates a new user account as either an Organiser or a Participant. | None | { fullName, email, password, role } | 201 Created – new user (no password) returned. 400 Bad Request – invalid/missing fields. 409 Conflict – email already registered. |
| POST | /api/auth/login | Authenticates a user and returns a JWT for subsequent requests. | None | { email, password } | 200 OK – { token, user }. 401 Unauthorized – invalid credentials. |

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the profile of the currently logged-in user. | Any | None | 200 OK – user profile. 401 Unauthorized. |
| PUT | /api/users/me | Updates the logged-in user's own profile details. | Any | { fullName, email } | 200 OK – updated profile. 400 Bad Request. |
| GET | /api/users/{id} | Retrieves a specific user's public profile (e.g. an Organiser checking a participant). | Organiser | None | 200 OK – user profile. 404 Not Found. |

## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Lists all upcoming events, for browsing. | None | None | 200 OK – array of events. |
| GET | /api/events/{id} | Retrieves full details for a single event. | None | None | 200 OK – event details. 404 Not Found. |
| POST | /api/events | Creates a new event. | Organiser | { eventName, eventDate, location, latitude, longitude, distance, eventType, description } | 201 Created – new event. 400 Bad Request. |
| PUT | /api/events/{id} | Updates an event owned by the logged-in Organiser. | Organiser | { eventName, eventDate, location, latitude, longitude, distance, eventType, description } | 200 OK – updated event. 403 Forbidden – not the owner. 404 Not Found. |
| DELETE | /api/events/{id} | Deletes an event owned by the logged-in Organiser. | Organiser | None | 204 No Content. 403 Forbidden. 404 Not Found. |
| GET | /api/events/{id}/weather | Fetches live weather for the event's location/date, using its lat/long. | None | None | 200 OK – weather data. 404 Not Found – event doesn't exist. 502 Bad Gateway – weather provider unavailable. |
| POST | /api/events/{id}/route-image | Uploads a route map image to Azure Blob Storage and links it to the event (used in Part 3). | Organiser | multipart/form-data (image file) | 201 Created – { routeMapUrl }. 403 Forbidden. 404 Not Found. |

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{id}/categories | Lists all categories (e.g. 5km, 10km) for a given event. | None | None | 200 OK – array of categories. 404 Not Found. |
| POST | /api/events/{id}/categories | Adds a new category (age-based or distance-based) to an event owned by the logged-in Organiser. | Organiser | { categoryName, categoryType, distance, minAge, maxAge, maxParticipants, entryFee } | 201 Created – new category. 400 Bad Request – invalid combination of type/fields. 403 Forbidden. 404 Not Found. |
| PUT | /api/categories/{id} | Updates a category belonging to the Organiser's event. | Organiser | { categoryName, categoryType, distance, minAge, maxAge, maxParticipants, entryFee } | 200 OK – updated category. 403 Forbidden. 404 Not Found. |
| DELETE | /api/categories/{id} | Deletes a category, if it has no active enrolments. | Organiser | None | 204 No Content. 403 Forbidden. 404 Not Found. 409 Conflict – category has existing enrolments. |

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/categories/{id}/enrolments | Enrols the logged-in Participant into a category. | Participant | None | 201 Created – new enrolment. 400 Bad Request – category full. 404 Not Found. 409 Conflict – already enrolled in this category. |
| GET | /api/users/me/enrolments | Lists the logged-in Participant's own enrolments. | Participant | None | 200 OK – array of enrolments. |
| GET | /api/events/{id}/enrolments | Lists all enrolments across an event owned by the logged-in Organiser. | Organiser | None | 200 OK – array of enrolments. 403 Forbidden. 404 Not Found. |
| DELETE | /api/enrolments/{id} | Cancels the logged-in Participant's own enrolment. | Participant | None | 204 No Content. 403 Forbidden – not the owner. 404 Not Found. |

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{id}/results | Captures a result for a specific enrolment, in an event owned by the logged-in Organiser. | Organiser | { finishTime, position, status } | 201 Created – new result. 403 Forbidden. 404 Not Found. 409 Conflict – result already recorded. |
| PUT | /api/results/{id} | Updates a previously captured result. | Organiser | { finishTime, position, status } | 200 OK – updated result. 403 Forbidden. 404 Not Found. |
| GET | /api/users/me/results | Returns the logged-in Participant's personal performance history across all events. | Participant | None | 200 OK – array of results. |
| GET | /api/events/{id}/results | Returns the full results/leaderboard for an event. | None | None | 200 OK – array of results. 404 Not Found. |
