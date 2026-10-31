# RaceDay (Parts 2 and 3)

RaceDay is a platform for organising and entering running, walking and cycling events.

| Part | What | Where |
|---|---|---|
| 2 | ASP.NET Core 8 Web API, EF Core, SQL Server, session authentication, Swagger, xUnit | `RaceDay.Api`, `RaceDay.Tests` |
| 3 | ASP.NET Core 8 **MVC web app** that consumes the API, **Azure Blob Storage** for images, **Docker** | `RaceDay.Web`, plus additions to `RaceDay.Api` |

```
Browser ──► RaceDay.Web (MVC, :8081) ──► RaceDay.Api (REST, :8080) ──► SQL Server
                                               └────────────────────► Azure Blob Storage (Azurite locally)
```

The MVC app has **no database access and no business rules**: every read and write goes through the API, and only the
API talks to Azure. Details and design decisions are in [docs/part3-notes.md](docs/part3-notes.md).

## Run everything with one command (Docker)

Requires Docker Desktop.

```
docker compose up --build
```

| | |
|---|---|
| **Web app** | http://localhost:8081 |
| API + Swagger | http://localhost:8080/swagger |

This starts SQL Server, the Azurite Blob Storage emulator, the API and the MVC app. The database and storage
container are created automatically on first start. Stop with `docker compose down` (add `-v` to wipe the data).

**Run only the MVC container** (when the API is already running on port 8080 of your machine):

```
docker build -t raceday-web ./RaceDay.Web
docker run --rm -p 8081:8080 -e Api__BaseUrl=http://host.docker.internal:8080/ raceday-web
```

(On Linux add `--add-host=host.docker.internal:host-gateway`.)

## Try it

1. Open http://localhost:8081 and **Register** as an **Organiser**. The invite code is `RACEDAY-ORG-2026`
   (configurable through `Organiser__InviteCode`).
2. Create an event with a banner image, then add categories (for example 5 km and 10 km).
3. In another browser or a private window, register as a **Participant**, filter the events, open one and **Enrol**.
4. As the Organiser, open **Enrolments**, then **Results**, enter finish times and positions and publish them.
5. As the Participant, open **My results**.

## Run without Docker (development)

Needs the .NET 8 SDK, SQL Server (LocalDB works) and Azurite (`npm i -g azurite && azurite-blob`, or the Visual Studio
Azurite).

```
# terminal 1 - API (https profile: https://localhost:53644)
dotnet run --project RaceDay.Api --launch-profile https

# terminal 2 - MVC app (http://localhost:5080)
Api__BaseUrl=https://localhost:53644/ dotnet run --project RaceDay.Web --launch-profile http
```

PowerShell: `$env:Api__BaseUrl="https://localhost:53644/"; dotnet run --project RaceDay.Web --launch-profile http`.
Trust the development certificate once with `dotnet dev-certs https --trust`.

The same configuration key (`Api:BaseUrl`) is used in Docker, so the app behaves identically in both.

### Database migration for Part 3

The database is created by EF Core migrations when the API starts (also in Docker). Part 3 adds one column,
`Users.ProfilePictureUrl`, so create a migration once and commit it:

```
dotnet ef migrations add AddProfilePicture --project RaceDay.Api
```

The API applies it on the next start. (Only if you already have a database you manage by hand, run
`database/part3-profile-picture.sql` instead.)

### Using a real Azure Storage account

Set `AzureStorage__ConnectionString` (user secrets or an environment variable) on the **API**. The container
(`raceday-images`) is created on first use and stays private; images are served through the API.

## Tests and CI

```
dotnet test RaceDay.Tests/RaceDay.Tests.csproj
```

GitHub Actions (`.github/workflows/ci.yml`) runs the API tests, builds the MVC project and builds the MVC Docker image
on every push to `main`, `part-2` and `part-3`.

## CI build status

(Add the screenshot of the successful green GitHub Actions run here.)
