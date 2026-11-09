# RunWay – ASP.NET Core MVC Event Management System

 is an ASP.NET Core MVC web application designed to manage events, users, and role-based access. The application uses Entity Framework Core, SQL Server, and ASP.NET Core Identity to provide authentication, authorisation, and user role management.

The project follows the Model-View-Controller (MVC) architecture, with a Services layer to separate business logic from controllers and views.

**Developer: Zekhaya Manxusa  
**Year:** 2026  
**Framework:** ASP.NET Core MVC (.NET 8)

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Features](#features)
3. [Technology Stack](#technology-stack)
4. [Project Structure](#project-structure)
5. [User Roles](#user-roles)
6. [Getting Started](#getting-started)
7. [Database Setup](#database-setup)
8. [Authentication and Registration](#authentication-and-registration)
9. [Adding New Features](#adding-new-features)
10. [Security Considerations](#security-considerations)
11. [Author](#author)

---

## 1. Project Overview

RunWay is a web-based event management system developed using ASP.NET Core MVC.

The application provides a structured approach to managing users and their permissions. It uses ASP.NET Core Identity for authentication and role-based authorisation to control access to administrative functionality.

The system is organised into models, views, controllers, data access components, and services to support maintainability and future development.

---

## 2. Features

The application includes the following features, subject to the functionality implemented in the project:

- **Authentication:** User registration, login, and account management using ASP.NET Core Identity.
- **Role-Based Authorisation:** Different access levels for administrators, organisers, and participants.
- **User Role Management:** Administrative functionality for viewing users and managing their roles.
- **MVC Architecture:** Separation of application logic, data models, and user interfaces.
- **Services Layer:** Business logic can be separated into reusable services.
- **Entity Framework Core:** Object-relational mapping for database operations.
- **SQL Server Integration:** Relational database support using SQL Server or LocalDB.
- **Database Migrations:** Support for managing database schema changes.
- **Responsive Interface:** Razor views and Bootstrap for the user interface.
- **Identity Pages:** Account-related pages organised under the `Areas/Identity` directory.

---

## 3. Technology Stack

| Technology | Purpose |
|---|---|
| C# | Application programming language |
| .NET 8 | Application framework |
| ASP.NET Core MVC | Web application architecture |
| Entity Framework Core | Database access and object-relational mapping |
| SQL Server / LocalDB | Relational database |
| ASP.NET Core Identity | Authentication and account management |
| Identity Roles | Role-based authorisation |
| Razor Views | Dynamic HTML rendering |
| Bootstrap | Responsive user interface |
| JavaScript and jQuery Validation | Client-side interaction and validation |
| Git and GitHub | Version control and source code hosting |

---

## 4. Project Structure

The project follows an organised ASP.NET Core MVC structure.

```text
RunWay/
│
├── Areas/
│   └── Identity/
│       └── Pages/
│           └── Account/
│               ├── Register.cshtml
│               ├── Register.cshtml.cs
│               ├── Login.cshtml
│               ├── Login.cshtml.cs
│               └── Manage/
│
├── Controllers/
│   ├── HomeController.cs
│   └── UserRolesController.cs
│
├── Models/
│   ├── ApplicationUser.cs
│   └── ViewModels/
│       ├── UserRolesViewModel.cs
│       └── ManageUserRolesViewModel.cs
│
├── Views/
│   ├── Home/
│   ├── UserRoles/
│   │   ├── Index.cshtml
│   │   └── Manage.cshtml
│   └── Shared/
│       └── _Layout.cshtml
│
├── Data/
│   ├── ApplicationDbContext.cs
│   └── Migrations/
│
├── Services/
│
├── wwwroot/
│
├── Program.cs
├── appsettings.json
├── README.md
└── AssignmentProg.csproj
```

**Note:** This is an illustrative structure. The actual files and folders depend on the implementation in your repository. Update this section if your project has additional folders or uses different filenames.

### Main Components

**Models**

Represent application data and define ViewModels used to transfer information between controllers and views.

**Controllers**

Handle HTTP requests, process user actions, and return views or redirect users to other application endpoints.

**Views**

Provide the user interface using Razor syntax, HTML, CSS, and JavaScript.

**Data**

Contains the Entity Framework Core database context and database migrations.

**Services**

Can contain reusable business logic and functionality shared across controllers.

**Areas/Identity**

Contains scaffolded ASP.NET Core Identity pages for registration, login, and account management.



Stores static assets such as CSS, JavaScript, and images.

---
5. User Roles

The application is designed to support three user roles.

| Role | Intended Responsibility |
|---|---|
| Admin | Manage users and control administrative access. |
| Organiser | Access event-related functionality permitted to organisers. |
| Participant | Register and access functionality permitted to participants. |

Access to protected pages and operations should be restricted using ASP.NET Core authorisation policies or role-based authorisation.

For example, an administrator-only controller action can use:

```csharp
[Authorize(Roles = "Admin")]
public IActionResult Index()
{
    return View();
}
```

Add the following namespace if it is not already present:

```csharp
using Microsoft.AspNetCore.Authorization;
```

The roles must exist in the Identity database before users can be assigned to them.



 Prerequisites

Before running the application, ensure that you have installed:

- .NET 8 SDK.
- Visual Studio with the ASP.NET and web development workload.
- SQL Server or SQL Server LocalDB.
- Git, if you want to clone the repository from GitHub.

 Step 1: Clone the Repository

Open a terminal and run:

```bash
git clone https://github.com/YOUR_USERNAME/RunWay.git
```

Navigate into the repository:

```bash
cd RunWay
```

Replace `YOUR_USERNAME` with your GitHub username.

### Step 2: Open the Project

Open the project in Visual Studio using the solution file, if one exists.

Alternatively, navigate to the directory containing the `.csproj` file and run:

```bash
dotnet restore
```

 Step 3: Configure the Database

Open `appsettings.json` and check the `DefaultConnection` connection string.

Configure it for your local SQL Server instance.

 Step 4: Apply Database Migrations

If Entity Framework Core migrations are included in the project, run:

```bash
dotnet ef database update
```

If the `dotnet ef` command is unavailable, install the Entity Framework Core command-line tool:

```bash
dotnet tool install --global dotnet-ef
```

Use a tool version compatible with your project's Entity Framework Core version.

 Step 5: Run the Application

Run:

```bash
dotnet run
```

Open the local URL displayed in the terminal.

The HTTPS address may differ depending on your project's launch settings.

 7. Database Setup

RunWay uses Entity Framework Core to communicate with its relational database.

The database context is responsible for configuring and accessing the application's data.

Database migrations help maintain the database schema as the application changes.

To create a new migration after modifying the data model, run:

```bash
dotnet ef migrations add MigrationName
```

Replace `MigrationName` with a meaningful description of the change.

Apply the migration using:

```bash
dotnet ef database update
```

Ensure that the correct startup project and database connection string are selected before applying migrations.

 8. Authentication and Registration

ASP.NET Core Identity provides the application's authentication and account management functionality.

Depending on the configuration, the registration process may include:

1. Entering registration details.
2. Validating the submitted information.
3. Creating the user account.
4. Assigning the appropriate role.
5. Requiring email confirmation, if enabled.
6. Allowing the user to sign in according to the application's authentication settings.

For example, assigning a newly registered user to the Participant role can be implemented using:

```csharp
var roleResult = await _userManager.AddToRoleAsync(
    user,
    "Participant"
);

if (!roleResult.Succeeded)
{
    foreach (var error in roleResult.Errors)
    {
        ModelState.AddModelError(string.Empty, error.Description);
    }

    return Page();
}
```

This example assumes that the user has already been created successfully and that the `Participant` role exists.

If email confirmation is enabled, the application must be configured to handle the confirmation process correctly.
9. Adding New Features

New functionality can be developed using the existing MVC architecture.

For example, adding event management functionality may involve the following steps:

1. Model:** Create or update the event entity.
2. Database:** Add the required entity configuration.
3. Migration:** Generate and apply a database migration if the schema changes.
4. Service:** Implement reusable business logic where appropriate.
5. ViewModel:** Define the data required by the user interface.
6. Controller:** Add actions to handle requests.
7. Views:** Create the required Razor views.
8. Testing:** Test the functionality and verify authorisation rules.

Example commands for a new migration:

```bash
dotnet ef migrations add AddEventManagement
dotnet ef database update
```

The commands should only be used when the corresponding model changes have been implemented.

10. Security Considerations

Security is an important part of an application that manages user accounts and roles.

The following practices are recommended:

- Never commit passwords, API keys, or production credentials.
- Use secure password policies for user accounts.
- Restrict administrative functionality using authorisation.
- Validate user input on the server.
- Keep .NET and NuGet dependencies updated.
- Store production secrets using an appropriate secrets-management solution.
- Use HTTPS when deploying the application.
- Verify role assignments before granting access to protected functionality.
- Avoid exposing sensitive user information in logs or error messages.
- Do not rely exclusively on client-side validation.

Before publishing the project, inspect the repository for sensitive configuration values.


11. Author

Developer:** Zekhaya Manxusa  
Project:** RunWay – ASP.NET Core MVC Event Management System  
Year:** 2026

