# RaceDay: Part 3 (ASP.NET Core MVC Front End)

RaceDay is a platform for running, walking and cycling events. This repository holds **Part 3**: an ASP.NET Core MVC website that consumes the RESTful API built in **Part 2 (AssignmentProg)**.

- **Part 2 (API):** `AssignmentProg`, repository: <PASTE PART 2 REPO LINK>
- **Part 3 (this project):** `RaceDay.Web`, namespace `AssignmentProg.Web`
- **Demo video:** <PASTE VIDEO LINK>
- **CI status:** see the screenshot in `docs/ci-green.png` (add after your first green build)

 Architecture

```
Browser  ->  RaceDay.Web (MVC, session auth)  ->  AssignmentProg API (JWT)  ->  SQL Server
                                                         |
                                                         +->  Azure Blob Storage (images)
```

- The MVC app holds **no database access and no Azure credentials**. Every read and write goes through the API using `HttpClient`.
- Login calls `api/auth/login`. The returned JWT, user id, role and name are kept in **session state**. The token is sent as a Bearer header on every API call.
- Images (event banners and profile pictures) are uploaded from the MVC app to the **API**, which stores them in Azure Blob Storage (container `event-banners`). The MVC app never talks to Azure directly.

Features

 Everyone
- Browse upcoming events, with search by name or location, and filters by type and date range
- View event details, banner and categories
- Register and log in as an **Organiser** or a **Participant**

Organiser
- Dashboard with event and enrolment totals
- Create, edit and delete events, with banner image upload
- Add categories (Age or Distance) to an event
- View enrolments for an event
- Capture results (finish times) once the event has concluded

 Participant
- Enrol in an event and choose a category
- My Events, showing status (Confirmed, Pending, Completed)
- My Results, showing finish time and position, for example "47 out of 312"
- Profile page with phone number and profile picture upload

 Role-based navigation
The navigation bar changes with the signed-in role. A custom `SessionAuth` filter blocks anonymous users and sends the wrong role back to the home page, so a Participant cannot open Organiser pages and the reverse.

 Tech stack
- .NET 8, ASP.NET Core MVC, Razor views
- Session state, `HttpClient` through a typed `ApiClient`
- Bootstrap 5 plus a custom theme (`wwwroot/css/site.css`)
- Docker (multi-stage build), GitHub Actions CI

 Project structure

```
RaceDay.Web/
  Controllers/   Account, Home, Organiser, Participant
  Filters/       SessionAuthAttribute.cs
  Models/        Dtos.cs, ViewModels.cs
  Services/      ApiClient.cs
  Views/         Account, Home, Organiser, Participant, Shared
  wwwroot/css/   site.css
  Program.cs
  appsettings.json
  Dockerfile
```

Running locally

 1. Start the API (Part 2)
1. Open the `AssignmentProg` solution in Visual Studio.
2. Make sure the connection string in `appsettings.json` points at a SQL Server you can reach, for example `Server=.\\SQLEXPRESS;Database=AssignmentProgDatabase;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true`.
3. Set the API's **User Secrets** (never commit these):
   ```
   dotnet user-secrets set "Jwt:Key" "<random string, 32+ characters>"
   dotnet user-secrets set "Jwt:Issuer" "RaceDayApi"
   dotnet user-secrets set "Jwt:Audience" "RaceDayWeb"
   dotnet user-secrets set "AzureBlob:ConnectionString" "<storage account connection string>"
   dotnet user-secrets set "AzureBlob:Container" "event-banners"
   dotnet user-secrets set "Seed:AdminPassword" "<strong password>"
   ```
4. Run the API with the **https** profile. It listens on `https://localhost:7206`. Check `https://localhost:7206/api/events`.

2. Start the website (Part 3)
1. Open the `RaceDay` solution (project `RaceDay.Web`) in a second Visual Studio window.
2. Check `appsettings.json`:
   ```json
   "Api": { "BaseUrl": "https://localhost:7206/" }
   ```
3. Set `RaceDay.Web` as the startup project and press **F5**.

 3. Try it
1. Register an **Organiser**, log in, create an event with a banner, then add categories.
2. Register a **Participant** in a private window, log in, and enrol.
3. As the Organiser, edit the event date to a past date, then capture results.
4. As the Participant, open **My Results**.

## Docker

Build and run the website image:

```
docker build -t raceday-web .
docker run -p 8080:8080 -e Api__BaseUrl=http://host.docker.internal:<API-HTTP-PORT>/ raceday-web
```

Then open `http://localhost:8080`. Use the API's **http** port from its `launchSettings.json`. Inside a container, a `https://localhost` address does not work, because the container cannot trust the development certificate.

Or with Compose:

```
docker compose up --build
```

Continuous integration

`.github/workflows/ci.yml` restores, builds and builds the Docker image on every push and pull request.

![CI build](docs/ci-green.png)

Configuration summary

| Setting | Where | Purpose |
|---|---|---|
| `Api:BaseUrl` | `RaceDay.Web/appsettings.json` or env var `Api__BaseUrl` | Address of the Part 2 API |
| `Jwt:*`, `AzureBlob:*`, `Seed:AdminPassword` | API User Secrets or environment variables | Secrets, never committed |

Security notes
- No secrets are stored in this repository.
- Image uploads are limited to JPG, PNG and WEBP, up to 5 MB, and are validated by the API.
- Sessions are HTTP-only and expire after 8 hours of inactivity.

 Troubleshooting
- **"Could not reach the API":** the API is not running, or `Api:BaseUrl` has the wrong port.
- **Login works but images do not show:** check the `event-banners` container allows public blob read.
- **"Session has not been configured":** the website's `Program.cs` is missing `AddSession` and `UseSession`.

 Author
ZEKHAYA MANXUSA


