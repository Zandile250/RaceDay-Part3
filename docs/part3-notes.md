# Part 3 notes: how the MVC app, Blob Storage and Docker fit together

## Architecture rules from the brief, and how they are met

| Brief requirement | How |
|---|---|
| MVC consumes the API for **all** data | Every call goes through one class, `Services/RaceDayApi.cs` (typed `HttpClient`). No EF, no connection string, no project reference to `RaceDay.Api`. |
| No duplicated business logic | Ownership, capacity, duplicate enrolment, role checks and conflict rules stay in the API. The MVC app only shows what the API answers. Form attributes (required, length, range) give instant feedback; the API's own answer is always shown when it disagrees. |
| Session-based authentication, role kept on every page | The MVC app has its own server-side session (`.RaceDay.Web.Session`, 30 min idle) holding the user's id, name, role and picture. |
| Protected pages | `[RaceDayAuthorize]` / `[RaceDayAuthorize(Roles = "Organiser")]` on every controller except public pages (landing, login, register, images). Not signed in -> login (with return URL). Wrong role -> access-denied page. |
| End session and redirect | `POST /account/logout` ends the API session, clears the MVC session and redirects to the login page with a message. |
| MVC never talks to Azure | Images are uploaded **to the API**; the API stores them in Blob Storage. To show an image, the browser requests `/media/...` from the MVC app, which fetches it from `GET /api/files/...` on the API. |

### How the two sessions line up

The API authenticates with its own cookie session (Part 2). When the user logs in, `RaceDayApi` captures the API's
`.AspNetCore.Session` cookie from the login response and stores it in the user's MVC session. Every later API call
replays that cookie, so the API sees the same logged-in user. The HTTP handler has `UseCookies = false`, so
cookies from different users can never be mixed up. If the API forgets the user (its session expires, or it
restarts), the next call returns 401, the MVC session is cleared and the user is sent to the login page with an
explanation. If the API cannot be reached at all, a friendly "can't reach RaceDay" page is shown.

## Changes made to the Part 2 API

* `Azure.Storage.Blobs` package, `Services/IFileStorage` + `BlobFileStorage`, registered in `Program.cs`.
* `Controllers/FilesController` (new controller, so existing controllers and their tests are untouched):
  * `POST /api/events/{id}/banner` (Organiser, owner only)
  * `POST /api/users/me/picture` (any signed-in user)
  * `GET /api/files/{blobName}` (public, serves the image)
  * Uploads accept JPG/PNG/WebP/GIF up to 5 MB, and the first bytes of the file are checked (a renamed `.exe` is rejected).
    Only names the API generated can be downloaded (no path tricks). Replacing an image deletes the old blob.
* `Users.ProfilePictureUrl` column and `UserResponse.ProfilePictureUrl` (optional last parameter, so existing code still compiles). **Needs an EF migration** (`AddProfilePicture`), see README. `database/part3-profile-picture.sql` is only for a hand-managed database.
* `appsettings.json`: `AzureStorage` section (Azurite by default).
* `RaceDay.Tests/FilesControllerTests.cs`: 12 test cases using an in-memory fake storage.

## Page map

| Role | Menu | Pages |
|---|---|---|
| Visitor | Log in, Register | Landing, Login, Register |
| Participant | Upcoming events, My enrolments, My results, Profile | Events with filters (text, type, dates, distance); event detail with categories and Enrol; enrolments with status colours and cancel; results history; profile with picture |
| Organiser | Dashboard, Events, Categories, Enrolments, Results, Profile | Dashboard (events, enrolments per event, upcoming dates); event create/edit/delete with banner; categories; roster; results entry |

## Assumptions to be aware of

* **"47th out of 312 finishers"**: the total is the number of results with status *Finished* in the whole event
  (the API stores one position per enrolment and does not say whether positions are per category). If your
  organisers rank per category, change the count in `MyResultsController` to also match the category name.
* **Dates/currency**: the entry fee is shown with the symbol in `Ui:CurrencySymbol` (default `R`).
* **Past events**: the UI hides the Enrol button for events that have already started. The API itself does not
  forbid it; if you want that enforced, add the rule to `EnrolmentsController` in the API.
* **Enrolment status**: the API only ever creates `Confirmed` enrolments. The UI already colour-codes
  `Pending` (amber) and `Cancelled` (red), so it will show them if the API starts producing them.
