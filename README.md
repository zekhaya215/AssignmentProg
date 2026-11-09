 RaceDay: Part 3 (ASP.NET Core MVC Front End)

RaceDay is a platform for running, walking and cycling events. This repository holds **Part 3**: an ASP.NET Core MVC website that consumes the RESTful API built in **Part 2 (AssignmentProg)**.

| Item | Link |
|---|---|
| Part 2 API repository | <PASTE PART 2 REPO LINK> |
| Part 3 website (this repo) | <PASTE PART 3 REPO LINK> |
| Demo video | <PASTE VIDEO LINK> |
| CI status | see the screenshot below |

- **Part 2 project / namespace:** `AssignmentProg`
- **Part 3 project:** `RaceDay.Web`, namespace `AssignmentProg.Web`

---

1. Architecture

```
Browser -> RaceDay.Web (MVC, session auth) -> AssignmentProg API (JWT) -> SQL Server
                                                      |
                                                      +-> Azure Blob Storage (images)
```

- The MVC website has **no database access and no Azure credentials**. Every read and write goes through the API using a typed `HttpClient` (`ApiClient`).
- Login calls `api/auth/login`. The API returns a JWT, user id, role and name. These are stored in **session state**, and the token is sent as a `Bearer` header on every API call.
- Images are uploaded from the website **to the API**, which stores them in Azure Blob Storage. The website never talks to Azure directly.

---

## 2. Features

 Everyone
- Browse upcoming events, with search by name or location and filters by type and date range
- View event details, banner image and categories
- Register and log in as an **Organiser** or a **Participant**

 Organiser
- Dashboard with event and enrolment totals
- Create, edit and delete events, with banner image upload
- Add categories (Age or Distance) to an event
- View enrolments for an event
- Capture results (finish times) once the event has concluded

Participant
- Enrol in an event and choose a category
- My Events, with status: Confirmed, Pending or Completed
- My Results, showing finish time and position, for example "47 out of 312"
- Profile page with phone number and profile picture upload

 Role-based navigation
The navigation bar changes with the signed-in role. A custom `SessionAuth` filter blocks anonymous users and sends the wrong role back to the home page, so a Participant cannot open Organiser pages and the reverse.

---

 3. Azure Blob Storage

Images are stored in **Azure Blob Storage**. Only the Part 2 API touches Azure.

Resources
| Item | Value |
|---|---|
| Resource group | `FinalPOE-RG` |
| Storage account | `stroremvcimages` |
| Container | `event-banners` |
| Container access level | Blob (anonymous read for blobs only, no listing) |

 What is stored
| Image | Blob path in the container |
|---|---|
| Event banner | `banners/<guid>.<ext>` |
| Profile picture | `profiles/<guid>.<ext>` |

Each file gets a new GUID name, so uploads never overwrite each other and the original file name is never exposed.

### Upload flow
```
1. Organiser picks a banner in the Create/Edit Event form (RaceDay.Web)
2. RaceDay.Web sends the file to the API as multipart/form-data
   (POST api/events or PUT api/events/{id}, Bearer token attached)
3. API validates the file and uploads it with BlobStorageService
4. API saves the blob's public URL in Event.BannerImageUrl (SQL Server)
5. The website reads the URL from the API and shows it in an <img> tag
```
Profile pictures follow the same flow through `PUT api/profile`.

 Validation (done in the API, in `BlobStorageService`)
- Allowed types: `.jpg`, `.jpeg`, `.png`, `.webp`
- Maximum size: 5 MB
- Empty files are rejected
- The container is created automatically if it does not exist (`CreateIfNotExists`, public access type Blob)

Configuration
The connection string is a secret and is **never committed**. It is set on the **API** only (User Secrets locally, environment variables in Docker or CI):

```
dotnet user-secrets set "AzureBlob:ConnectionString" "<storage account connection string>"
dotnet user-secrets set "AzureBlob:Container" "event-banners"
```

Where to find the connection string: Azure portal > `stroremvcimages` > Security + networking > **Access keys** > Show > Connection string.

As environment variables (Docker): `AzureBlob__ConnectionString` and `AzureBlob__Container`.

 How to verify
1. Create an event with a banner in the website.
2. In the Azure portal, open `stroremvcimages` > Containers > `event-banners` > `banners`. The new file is there.
3. Open the blob URL in a browser. The image loads, because the container allows anonymous blob read.

 Rubric mapping
- Upload of event banners and profile pictures: yes
- Files stored in Azure Blob Storage: yes
- Image URL saved in the database: yes (`Event.BannerImageUrl`, profile image path)
- Images displayed in the website: event cards, event details and profile page
- Credentials kept out of source control: yes

---

## 4. Tech stack
- .NET 8, ASP.NET Core MVC, Razor views
- Session state and a typed `HttpClient` (`ApiClient`)
- Bootstrap 5 with a custom theme (`wwwroot/css/site.css`)
- Docker (multi-stage build) and GitHub Actions CI

## 5. Project structure

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

---

 6. Running locally

 Step 1: start the API (Part 2)
1. Open the `AssignmentProg` solution in Visual Studio.
2. Set `DefaultConnection` in `appsettings.json` to a SQL Server you can reach, for example:
   `Server=.\\SQLEXPRESS;Database=AssignmentProgDatabase;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true`
3. Set the API's User Secrets (never commit these):
   ```
   dotnet user-secrets set "Jwt:Key" "<random string, 32+ characters>"
   dotnet user-secrets set "Jwt:Issuer" "RaceDayApi"
   dotnet user-secrets set "Jwt:Audience" "RaceDayWeb"
   dotnet user-secrets set "AzureBlob:ConnectionString" "<storage account connection string>"
   dotnet user-secrets set "AzureBlob:Container" "event-banners"
   dotnet user-secrets set "Seed:AdminPassword" "<strong password>"
   ```
4. Run with the **https** profile. The API listens on `https://localhost:7206`. Check `https://localhost:7206/api/events`.
5. On startup the API applies all migrations and seeds the roles (Admin, Organiser, Participant) and an admin account.
Step 2: start the website (Part 3)
1. Open the `RaceDay` solution (project `RaceDay.Web`) in a second Visual Studio window.
2. Check `appsettings.json`:
   ```json
   "Api": { "BaseUrl": "https://localhost:7206/" }
   ```
3. Set `RaceDay.Web` as the startup project and press **F5**.

 Step 3: try it
1. Register an **Organiser**, log in, create an event with a banner, then add categories.
2. Register a **Participant** in a private window, log in and enrol.
3. As the Organiser, edit the event date to a past date, then capture results.
4. As the Participant, open **My Results**.



 7. API endpoints used by the website

| Method | Endpoint | Used for |
|---|---|---|
| POST | `api/auth/register`, `api/auth/login` | Register and log in |
| GET | `api/events`, `api/events/{id}` | Browse and view events |
| GET | `api/events/mine` | Organiser's own events |
| POST/PUT/DELETE | `api/events`, `api/events/{id}` | Manage events, banner upload |
| GET/POST | `api/events/{id}/categories` | List and add categories |
| GET | `api/events/{id}/enrolments` | Organiser's enrolment list |
| POST | `api/events/{id}/results` | Capture results |
| POST/GET | `api/enrolments`, `api/enrolments/mine` | Enrol, my events |
| GET | `api/results/mine` | My results with position and finisher count |
| GET/PUT | `api/profile` | Profile and picture upload |



 8. Docker

Build and run the website image:

docker build -t raceday-web .
docker run -p 8080:8080 -e Api__BaseUrl=http://host.docker.internal:<API-HTTP-PORT>/ raceday-web


Open `http://localhost:8080`. Use the API's **http** port from its `launchSettings.json`. Inside a container a `https://localhost` address does not work, because the container does not trust the development certificate.

Or with Compose:

docker compose up --build
```

If the API also runs in Docker, pass its secrets as environment variables: `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, `AzureBlob__ConnectionString`, `AzureBlob__Container`, `Seed__AdminPassword`.



 9. Continuous integration

`.github/workflows/ci.yml` restores, builds and builds the Docker image on every push and pull request.

![CI build](docs/ci-green.png)



 10. Configuration summary

| Setting | Where | Purpose |
|---|---|---|
| `Api:BaseUrl` | `RaceDay.Web/appsettings.json` or `Api__BaseUrl` | Address of the Part 2 API |
| `Jwt:*` | API User Secrets or env vars | Token signing and validation |
| `AzureBlob:*` | API User Secrets or env vars | Blob Storage access |
| `Seed:AdminPassword` | API User Secrets or env vars | Seeded admin password |

11. Security notes
- No secrets are stored in this repository.
- The website has no direct access to the database or to Azure.
- Role checks happen in the website (`SessionAuth`) and again in the API (JWT roles).
- Sessions are HTTP-only and expire after 8 hours of inactivity.
- Uploads are limited by type and size, and stored under random names.
- A previously hardcoded admin password in an early Part 2 commit is exposed in the Git history. That password must be changed.

 12. Troubleshooting

| Problem | Fix |
|---|---|
| "Could not reach the API" | Start the API, and check `Api:BaseUrl` has the right port |
| "Session has not been configured" | `Program.cs` needs `AddSession` and `UseSession()` |
| Banner not showing | Check the container access level is **Blob**, and the blob URL opens in a browser |
| Upload returns an error | File must be JPG, PNG or WEBP and under 5 MB |
| API crashes on startup with a SQL error | `DefaultConnection` points at a server you cannot reach |
| `Invalid column name 'BannerImageUrl'` | Run `Update-Database -Context ApplicationDbContext` in the API project |

 13. CI screenshot
Save a screenshot of the green GitHub Actions run as `docs/ci-green.png`.

 Author
<ZEKHAYA MANXUSA>, <S10478227>, <PROG6212>
