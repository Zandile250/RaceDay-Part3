Race Day for South African resident

RaceDay, a full stack, web based, event management system, has been designed for the South African road running, walking and cycling community. It enables Event Organisers to create and manage events, categories and results; Participants can browse events, enter into categories and view their own results and registration history.

This is an individual's Portfolio of Evidence (POE) that is developed over three sections:

Contents

Quick start
Why RaceDay?
Part 1: Design and database
Part 2: REST API
Part 3: Web application, Blob Storage and Docker
Project layout
Tests and CI
Requirement map (Part 3)

Quick start

Requires Docker Desktop. From the repository root:

docker compose up --build

The first run downloads images and takes a few minutes. Allow about a minute and a half after the containers start, because the API waits for the database.

What	                                               Link
The website (start here)	                           http://localhost:8081
API documentation                                    (Swagger)	http://localhost:8080/swagger
API base address	                                   http://localhost:8080
Blob Storage emulator (Azurite)	                     http://localhost:10000
Repository (Part 3 branch)	                         https://github.com/Zandile250/RaceDay2/tree/part-3
Build pipeline (GitHub Actions)	                     https://github.com/Zandile250/RaceDay2/actions

Try it

Open http://localhost:8081 and click Register.
Register an Organiser. The invite code is RACEDAY-ORG-2026.
Create an event with a banner image, then add one or more categories.
In a private browser window, register a Participant, filter the events, open one and click Enrol.
As the Organiser, open Enrolments, then Results, and publish a finish time and position.
As the Participant, open My results to see it, and My profile to upload a profile picture.


Useful Docker commands:

docker compose up -d           # start in the background
docker compose ps              # check that all four containers are running
docker compose logs api        # read the API's log
docker compose down            # stop and remove the containers
docker compose down -v         # stop AND delete all data (fresh start)

Why RaceDay?

Community races are often run with entries by email or WhatsApp, a spreadsheet for the start list, and results posted as a PDF. RaceDay replaces that with one system:

Both sides in one place. Organisers manage events, enrolments and results; participants discover events and enrol.
Results flow back to the runner. Once an organiser publishes a time and position, it appears in the participant's personal race history.
A different experience for each role. Each role has its own menu and pages, and cannot open the other's.
API-first. All data and rules live in a REST API, and the website only calls it.
Easy to run. One command starts the database, image storage, API and website.

Part 1: Design and database

Part 1 is the design of the platform, before any code was written. It covers:

Document	                                                  Purpose
erd.pdf	                                                    Entity relationship diagram of the database
data-dictionary.md	                                        Every table and column, with types and meanings
design-decisions.md	                                        The key decisions behind the ERD and why they were made
api-endpoint-plan.md	                                      The planned REST endpoints
raceday-database.sql	                                      SQL script for the database design

The database has six tables: Roles, Users, Events, Categories, Enrolments and Results. An event belongs to one organiser, has many categories, a participant enrols in a category, and an enrolment can have one result.

Part 2: REST API

Part 2 turns the design into a working ASP.NET Core 8 Web API (RaceDay.Api).

What it uses: Entity Framework Core (code-first migrations), SQL Server, session-based authentication, Swagger documentation, BCrypt password hashing, and xUnit tests (RaceDay.Tests).

How it works

Logging in creates a session on the API, identified by a cookie. Each protected endpoint checks the session and the user's role.
Organisers register with an invite code (Organiser:InviteCode, default RACEDAY-ORG-2026), so nobody can simply make themselves an organiser.
Organisers can only change their own events, categories and enrolment lists. Participants can only change their own enrolments.
The database schema is created by EF Core migrations when the API starts.

Main endpoints (full detail in Swagger)

Area	                                                              Endpoints
Authentication	                                                    POST /api/auth/register, POST /api/auth/login, POST /api/auth/logout
Profile	                                                            GET and PUT /api/users/me
Events	                                                            GET /api/events (optional ?upcoming=true), GET /api/events/{id}, POST /api/events, PUT and DELETE /api/events/{id}
Categories	                                                        POST /api/events/{id}/categories, PUT and DELETE /api/categories/{id}
Enrolments	                                                        POST /api/enrolments, GET /api/enrolments/me, GET /api/events/{id}/enrolments, DELETE /api/enrolments/{id}
Results	                                                            POST /api/results, PUT /api/results/{id}, GET /api/results/me, GET /api/events/{id}/results

An exported Swagger definition is in docs/. Notes on the Part 2 design are in docs/part2-notes.md.

Part 3: Web application, Blob Storage and Docker

Part 3 adds the MVC website, image storage and containers.

Browser ──► RaceDay.Web (MVC, :8081) ──► RaceDay.Api (REST, :8080) ──► SQL Server
                                                │
                                                └──────────────────► Azure Blob Storage
                                                                      (Azurite when running locally)
The MVC website (RaceDay.Web) has no database access and no business rules. Everything goes through the API.
Only the API talks to Azure Blob Storage. The website uploads images to the API and fetches them back from it.
When a user logs in, the website keeps the API's session cookie in its own session and sends it with each request, so the API sees the same signed-in user.
An exported Swagger definition is in docs/. Notes on the Part 2 design are in docs/part2-notes.md

Features

Everyone

Sign up and log in. Sessions retain the sign-in and role per page.
Pages are protected: if they have not signed in, visitors are redirected to sign-in page, and users are not allowed to access the pages of other roles.
A sign out will terminate the session and bring an individual back to the sign-in screen.

Organiser

Events created, enrolments per event, upcoming dates: Dashboard.
Add, modify and remove events (Name, Description, Date, Location, Distance, Type) and add a banner image to the events.
Organize categories for each event.
Display all the participants in an event along with their category and status of enrolment in the event.
Enter and publish finish times and finishing position for an event.

Participant

Home page of upcoming events with filters (search, type, dates, distance).
The full event page with banner and categories to choose from.
Enrol in a category (recorded immediately), and cancel an enrolment.
My enrolments, including status of each enrolment.
Results I get: event, date, category, finish time and finish position (e.g. "47th of 312 finishers").
Profile: Edit information and add a profile picture.

Docker

The whole platform runs from the repository root with docker compose up --build (see the quick start). It starts four containers:

Container	                                                Role
web	                                                  The MVC website (port 8081)
api	                                                  The REST API (port 8080)
db	                                                  SQL Server 2022
azurite	                                              Local stand-in for Azure Blob Storage

Run only the website container, when the API is already running on port 8080 of your machine:

docker build -t raceday-web ./RaceDay.Web
docker run --rm -p 8081:8080 -e Api__BaseUrl=http://host.docker.internal:8080/ raceday-web

The website's Dockerfile is multi-stage (SDK to build, a small runtime image to run), restores packages in a cached layer, and runs as a non-root user.

Run without Docker (development)

Needs the .NET 8 SDK, SQL Server (LocalDB works) and Azurite.

# terminal 1: the API (https profile: https://localhost:53644)
dotnet run --project RaceDay.Api --launch-profile https

# terminal 2: the website (http://localhost:5080)
$env:Api__BaseUrl="https://localhost:53644/"
dotnet run --project RaceDay.Web --launch-profile http

Trust the development certificate once with dotnet dev-certs https --trust. The website reads the API address from Api:BaseUrl, the same setting Docker uses, so it behaves the same in both.

Using a real Azure Storage account

Set AzureStorage__ConnectionString on the API (an environment variable or user secrets). The container raceday-images is created on first use and stays private; images are served through the API.

Project layout

RaceDay.Api/            REST API: controllers, EF Core models, migrations, Blob Storage service
RaceDay.Web/            MVC website: controllers, Razor views, styles, API client, Dockerfile
RaceDay.Tests/          xUnit tests for the API
docs/                   Design documents and notes for each part
docker-compose.yml      Runs database, storage emulator, API and website together
.github/workflows/      CI pipeline

Technology: 
ASP.NET Core 8 (MVC and Web API), Entity Framework Core, SQL Server, Azure Blob Storage (Azurite locally), Bootstrap 5, Docker, GitHub Actions, xUnit.

Tests and CI
dotnet test RaceDay.Tests/RaceDay.Tests.csproj
<img width="461" height="364" alt="image" src="https://github.com/user-attachments/assets/aeb0f8d0-d20f-4277-be1f-5ad0e5ec6906" />


Requirement map (Part 3)
Brief section	                                                                                                                           Where to look
1. MVC consumes the API, no database access	                                                                     RaceDay.Web/Services/RaceDayApi.cs is the only class that talks to the API. The MVC project has no Entity Framework and no connection string.
2. Authentication, session, role-based navigation, protected pages, logout	                                     AccountController, Filters/RaceDayAuthorizeAttribute.cs, Views/Shared/_Layout.cshtml (menu per role)
3. Organiser interface	                                                                                         Organiser*Controller.cs and Views/Organiser*
4. Participant interface	                                                                                       HomeController, EventsController, EnrolmentsController, MyResultsController, ProfileController
5. Azure Blob Storage in the API only	                                                                           RaceDay.Api/Services/BlobFileStorage.cs, RaceDay.Api/Controllers/FilesController.cs; the website fetches images through MediaController
6. Docker	                                                                                                       RaceDay.Web/Dockerfile, docker-compose.yml, the run command at the top of this file
7. UI design	                                                                                                   RaceDay.Web/wwwroot/css/site.css (one theme), Bootstrap 5, colour-coded status pills, friendly error pages
8. GitHub and CI/CD	                                                                                             Branch part-3, 20+ commits, .github/workflows/ci.yml builds the API tests and the MVC project


The GitHub Actions workflow (.github/workflows/ci.yml) runs on every push. It runs the API tests, builds the MVC project and builds the MVC Docker image.
<img width="874" height="356" alt="image" src="https://github.com/user-attachments/assets/6a3a273e-e3d4-4af9-8a4c-4c6919564c22" />


https://youtu.be/Yw7k_FVcmtk

