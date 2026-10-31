# Part 3 commit plan (24 commits)

Work on a clearly named branch, then run these in order. Each step is a meaningful, self-contained change, and every
step builds. Run `git status` before each commit to check you are adding what you expect.

```
git checkout -b part-3
```

| # | Commit message | Files to add |
|---|---|---|
| 1 | Add Azure Blob Storage package and file storage abstraction | `git add RaceDay.Api/RaceDay.Api.csproj RaceDay.Api/Services/IFileStorage.cs` |
| 2 | Implement BlobFileStorage with lazy private container | `git add RaceDay.Api/Services/BlobFileStorage.cs` |
| 3 | Add ProfilePictureUrl to User and UserResponse | `git add RaceDay.Api/Models/Entities.cs RaceDay.Api/Dtos/Dtos.cs RaceDay.Api/Controllers/AuthController.cs` |
| 4 | Add FilesController for banners, profile pictures and downloads | `git add RaceDay.Api/Controllers/FilesController.cs` |
| 5 | Register storage in Program and add AzureStorage settings | `git add RaceDay.Api/Program.cs RaceDay.Api/appsettings.json` |
| 6 | Add EF migration for profile picture | `git add RaceDay.Api/Migrations` (add `database/part3-profile-picture.sql` too if you keep it) |
| 7 | Add FilesController unit tests with fake storage | `git add RaceDay.Tests/FilesControllerTests.cs` |
| 8 | Scaffold RaceDay.Web MVC project and add to solution | `git add RaceDay.Web/RaceDay.Web.csproj RaceDay.Web/Program.cs RaceDay.Web/appsettings.json RaceDay.Web/Properties RaceDay.sln` |
| 9 | Add API DTOs and result types for the MVC client | `git add RaceDay.Web/Models/ApiModels.cs RaceDay.Web/Services/ApiResult.cs RaceDay.Web/Services/IRaceDayApi.cs` |
| 10 | Implement RaceDayApi client with session cookie forwarding | `git add RaceDay.Web/Services/RaceDayApi.cs RaceDay.Web/Services/SessionUser.cs RaceDay.Web/Services/ModelStateExtensions.cs` |
| 11 | Add role-based authorize and API exception filters | `git add RaceDay.Web/Filters` |
| 12 | Add RaceDay theme: Bootstrap, fonts, icons, CSS and JS | `git add RaceDay.Web/wwwroot` |
| 13 | Add shared layout, role-aware navigation and partials | `git add RaceDay.Web/Views/_ViewImports.cshtml RaceDay.Web/Views/_ViewStart.cshtml RaceDay.Web/Views/Shared RaceDay.Web/Services/UiHelpers.cs RaceDay.Web/Models/ViewModels.cs` |
| 14 | Add register, login, logout and access denied pages | `git add RaceDay.Web/Controllers/RaceDayController.cs RaceDay.Web/Controllers/AccountController.cs RaceDay.Web/Views/Account` |
| 15 | Add landing page, participant home with filters and error pages | `git add RaceDay.Web/Controllers/HomeController.cs RaceDay.Web/Views/Home` |
| 16 | Add event detail page and enrolment flow | `git add RaceDay.Web/Controllers/EventsController.cs RaceDay.Web/Controllers/EnrolmentsController.cs RaceDay.Web/Views/Events RaceDay.Web/Views/Enrolments` |
| 17 | Add My Results page | `git add RaceDay.Web/Controllers/MyResultsController.cs RaceDay.Web/Views/MyResults` |
| 18 | Add profile page with picture upload and media proxy | `git add RaceDay.Web/Controllers/ProfileController.cs RaceDay.Web/Controllers/MediaController.cs RaceDay.Web/Views/Profile` |
| 19 | Add organiser dashboard | `git add RaceDay.Web/Controllers/OrganiserController.cs RaceDay.Web/Views/Organiser` |
| 20 | Add organiser event management with banner upload | `git add RaceDay.Web/Controllers/OrganiserEventsController.cs RaceDay.Web/Views/OrganiserEvents` |
| 21 | Add organiser category management | `git add RaceDay.Web/Controllers/OrganiserCategoriesController.cs RaceDay.Web/Views/OrganiserCategories` |
| 22 | Add organiser enrolments and results entry pages | `git add RaceDay.Web/Controllers/OrganiserEnrolmentsController.cs RaceDay.Web/Controllers/OrganiserResultsController.cs RaceDay.Web/Views/OrganiserEnrolments RaceDay.Web/Views/OrganiserResults` |
| 23 | Containerise MVC app and add Azurite and web to compose | `git add RaceDay.Web/Dockerfile RaceDay.Web/.dockerignore docker-compose.yml` |
| 24 | Update CI to build the MVC project and image; document Part 3 | `git add .github/workflows/ci.yml README.md docs .gitignore` |

Then push and open the Actions tab to take the green-run screenshot for the README:

```
git push -u origin part-3
```

Tip: if a commit step shows "nothing to commit", you probably staged those files earlier. Skip it.
