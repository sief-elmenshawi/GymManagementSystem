# 🏋️ Gym Management System

A complete **Gym Management System** built using **ASP.NET Core MVC** to automate and manage gym operations including members, trainers, sessions, memberships, plans, and bookings.

The project follows a clean layered architecture to achieve **scalability, maintainability, and separation of concerns**.

---

# 🚀 Features

## 👤 Authentication & Authorization

* User authentication using **ASP.NET Core Identity**
* Role-based authorization
* Secure password hashing
* User and role management

---

## 👥 Member Management

* Add, update, delete, and view members
* Manage member information
* Track memberships and subscriptions
* Handle member attachments

---

## 🏋️ Trainer Management

* Manage trainers
* Assign trainers to sessions
* Maintain trainer information

---

## 📅 Session Management

* Create and manage gym sessions
* Define session capacity
* Assign trainers
* Track session schedules

---

## 💳 Membership Management

* Create membership plans
* Manage subscriptions
* Track member memberships

---

## 📌 Booking System

* Book sessions
* Manage booking operations
* Prevent invalid bookings based on business rules

---

# 🏗️ Architecture

The project is designed using a layered architecture:

```text
GymManagementSystem

│
├── Presentation Layer
│   └── ASP.NET Core MVC
│       ├── Controllers
│       ├── Views
│       └── ViewModels
│
├── Business Logic Layer
│   └── Services
│       ├── Business Rules
│       ├── Application Logic
│       └── DTOs
│
└── Data Access Layer
    └── Entity Framework Core
        ├── DbContext
        ├── Entities
        ├── Configurations
        └── Migrations
```

---

# 🛠️ Technologies Used

## Backend

* C#
* ASP.NET Core MVC
* .NET 9
* Entity Framework Core
* LINQ

## Database

* SQL Server
* Entity Framework Core Code First
* Fluent API
* Migrations

## Frontend

* Razor Views
* HTML5
* CSS3
* Bootstrap
* JavaScript
* jQuery

## Security

* ASP.NET Core Identity
* Role-Based Authorization

## Tools

* Visual Studio 2022
* Git & GitHub
* Swagger
* Postman

---

# 🧩 Design Patterns & Principles

The project applies:

* Repository Pattern
* Unit of Work Pattern
* Specification Pattern
* Dependency Injection
* SOLID Principles
* Object-Oriented Programming (OOP)

---

# 📂 Project Structure

```text
GymManagementSolution

│
├── GymManagement              # ASP.NET Core MVC presentation layer
│
├── GymManagement.BLL
│
├── GymManagement.DAL
│
├── GymManagement.Tests
│
└── GymManagementSolution.slnx
```

---

# ⚙️ Installation & Setup

## 1. Prerequisites

* [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
* SQL Server (LocalDB, Express, Developer, or a remote instance)
* EF Core CLI tools (only needed to manage migrations manually):

```bash
dotnet tool install --global dotnet-ef --version 9.0.16
```

---

## 2. Clone Repository

```bash
git clone https://github.com/sief-elmenshawi/GymManagementSystem.git
cd GymManagementSystem
```

---

## 3. Configure Database Connection

The committed `GymManagement/appsettings.json` intentionally contains **no connection string**. A local copy with a connection string (and a local `dotnet-tools.json`) is kept uncommitted on the development machine and is never pushed.

For a fresh clone, provide the connection string through **User Secrets** or an environment variable instead of committing it:

```bash
dotnet user-secrets init --project GymManagement
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=GymManagement;Trusted_Connection=True;TrustServerCertificate=True;" --project GymManagement
```

PowerShell alternative:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=.;Database=GymManagement;Trusted_Connection=True;TrustServerCertificate=True;'
```

---

## 4. Database Migrations and Seeding

Pending migrations are applied automatically when the application starts, followed by identity/data seeding. Manual migration commands are still available:

```bash
dotnet ef database update --project GymManagement.DAL --startup-project GymManagement
```

In Package Manager Console, set **Default project** to `GymManagement.DAL` and run:

```powershell
Update-Database
```

Default seed users are created only when a seed password is configured:

* Development: `GymManagement/appsettings.Development.json` (`SeedUsers:Password`)
* Any environment: `GYM_SEED_PASSWORD` environment variable or `SeedUsers:Password` in configuration

No seed password is stored in source code. If no password is found in a non-development environment, the host logs a warning and skips identity seeding.

---

## 5. Run Application

```bash
dotnet run --project GymManagement
```

---

## 6. Run Automated Tests

```bash
dotnet test GymManagementSolution.slnx
```

The `GymManagement.Tests` project contains unit tests for mapping, services, seed-password resolution, and AutoMapper license resolution.

SQL Server end-to-end tests (login, member create/edit/delete, booking create/cancel) are skipped unless a test connection string is supplied. They create and drop a disposable `GymManagement_E2ETests` database:

```powershell
$env:GYM_TEST_CONNECTION_STRING = 'Server=.;Database=GymManagement_E2ETests;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet test GymManagementSolution.slnx
```

---

## 7. Production Configuration

* **AutoMapper license:** set `AUTOMAPPER_LICENSE_KEY` (or the shared `LUCKYPENNY_LICENSE_KEY`) in the hosting environment, or `AutoMapper:LicenseKey` through a configuration provider. ASP.NET Core loads User Secrets only in Development, so use them for local development only. No key is required in Development. Non-development environments fail fast at startup with a clear error when no key is found.
* **Seed admin password:** set `GYM_SEED_PASSWORD` or `SeedUsers:Password` in configuration before first startup in Production. Seeding is skipped (with a warning) when no password is configured.
* **Connection string:** provide `ConnectionStrings__DefaultConnection` through the hosting environment or a secrets manager. Do not commit environment-specific files.

---

# 🔐 Default Roles

| Role    | Permissions              |
| ------- | ------------------------ |
| Admin   | Full system management   |
| Trainer | Manage assigned sessions |
| Member  | View and book sessions   |

---

# 🎯 Future Improvements

* Build RESTful Web API version
* Add payment integration
* Add notification system using SignalR
* Add dashboard analytics
* Add browser-driven UI tests (Playwright/Selenium)

---

# 👨‍💻 Author

**Sief Ibrahim Ezzat**

.NET Developer

GitHub:
https://github.com/sief-elmenshawi

LinkedIn:
https://linkedin.com/in/sief-elmenshawi
