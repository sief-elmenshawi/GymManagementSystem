# 🏋️ Gym Management System

An ASP.NET Core MVC application for managing gym members, trainers, plans, memberships, sessions, and bookings. It uses a layered architecture, EF Core with SQL Server, ASP.NET Core Identity, AutoMapper, and Razor views.

---

# 🚀 Features

## 👤 Authentication and account security

* ASP.NET Core Identity with unique emails and account lockout
* Seeded `SuperAdmin`, `Admin`, and `Member` roles (user accounts only when a seed password is configured)
* Global antiforgery validation for unsafe requests
* HTTPS redirection only when an HTTPS URL/port is configured
* Existing seeded user passwords are **not** reset on later startups

## 📝 Public member registration

* `/Account/Register` is public and linked from the login page
* Creates an Identity account with **only** the `Member` role; role fields from the request are ignored
* Creates a linked `Member` record with contact and address details
* Health records are not collected during registration and can be added later by staff
* Duplicate Identity/Member emails and member phone numbers are rejected

## 🧭 Super Admin dashboard

* `/Dashboard` is restricted to `SuperAdmin`
* Shows analytics summary cards and quick links to every management page
* `Home` redirects `SuperAdmin` users to the dashboard; other authenticated users keep the analytics home page
* Users without `SuperAdmin` are redirected to `AccessDenied`
* Includes a shortcut to `/Users` for account management

## 👤 User management

* `/Users` is restricted to `SuperAdmin`
* List accounts with their assigned roles
* Create users with a chosen role and password
* Edit account details, role, and optionally reset the password
* Delete users, with guards against self-deletion and demoting/deleting the last `SuperAdmin`

---

## 👥 Member management

* Create, edit, delete, and list members
* Store contact details, address, date of birth, gender, and optional photo
* View member health records
* Member list is paged and uses a lightweight projection (the photo payload is not loaded)

## 🏋️ Trainer management

* Create, edit, delete, and list trainers
* Maintain specialty and contact details
* Assign trainers to sessions

## 📅 Session management

* Create, edit, delete, and list sessions
* Enforce capacity and schedule rules
* Show available slots from booking counts in a single SQL query
* Session list is ordered and paged in SQL

## 💳 Membership management

* Create and manage plans
* Create and cancel member memberships
* List active memberships with member and plan names via a flat SQL projection
* Membership list is ordered and paged in SQL

## 📌 Booking system

* Book members into sessions
* Prevent bookings that violate business rules (membership, capacity, timing, duplicates)
* Track attendance
* Anti-join query for available members instead of loading all bookings into memory

## 📊 Analytics

* Dashboard counts for upcoming, ongoing, and completed sessions
* Member, trainer, and active-membership totals

---

# 🏗️ Architecture

```text
GymManagementSolution
│
├── GymManagement              Presentation layer (ASP.NET Core MVC)
│   ├── Controllers
│   ├── Views
│   ├── Program.cs / ProgramExtensions.cs
│   └── Identity, antiforgery, HTTPS, and configuration helpers
│
├── GymManagement.BLL          Business logic layer
│   ├── Services / Interfaces
│   ├── ViewModels
│   ├── AutoMapper profile
│   └── Common (Result, PagedResult)
│
├── GymManagement.DAL          Data access layer
│   ├── GymDbContext
│   ├── Entities / Configurations
│   ├── Repositories / Unit of Work
│   ├── Migrations / Seeders
│   └── List projection DTOs
│
├── GymManagement.Tests        Unit, SQL performance, and HTTP E2E tests
│
└── GymManagementSolution.slnx
```

Dependency direction: `GymManagement` → `GymManagement.BLL` → `GymManagement.DAL`. Views primarily consume view models and `PagedResult<T>` from the BLL; a few forms also reference DAL enums directly.

---

# 🛠️ Technologies

| Area | Stack |
| --- | --- |
| Backend | C#, .NET 9, ASP.NET Core MVC |
| Data | SQL Server, EF Core 9, Code First migrations, Fluent API |
| Mapping | AutoMapper 16.3.0 |
| Identity | ASP.NET Core Identity with `IdentityRole` |
| Frontend | Razor, HTML5, CSS, Bootstrap, JavaScript, jQuery |
| Assets | Frontend libraries vendored under `wwwroot/lib` for offline use |
| Testing | xUnit, Moq, EF Core SQL Server integration tests |

Patterns used: Repository, Unit of Work, Dependency Injection, and layered services.

---

# ⚙️ Installation and setup

## 1. Prerequisites

* [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
* SQL Server (LocalDB, Express, Developer, or remote)
* EF Core CLI tools, only for manual migration commands:

```bash
dotnet tool install --global dotnet-ef --version 9.0.16
```

A local `dotnet-tools.json` may be used instead, but it is environment-specific and must not be committed.

## 2. Clone

```bash
git clone https://github.com/sief-elmenshawi/GymManagementSystem.git
cd GymManagementSystem
```

## 3. Configure secrets

The committed `GymManagement/appsettings.json` contains no connection string, seed password, or license key. Supply them through environment variables or a secrets manager. User Secrets are convenient for local Development only; ASP.NET Core does not load them in other environments.

```bash
dotnet user-secrets init --project GymManagement
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your SQL Server connection string>" --project GymManagement
dotnet user-secrets set "SeedUsers:Password" "<local seed password>" --project GymManagement
```

Environment variables:

| Variable | Purpose |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | SQL Server database |
| `GYM_SEED_PASSWORD` | Password for seeded admin users; takes precedence over `SeedUsers:Password` |
| `AUTOMAPPER_LICENSE_KEY` | AutoMapper key required outside Development |
| `LUCKYPENNY_LICENSE_KEY` | Shared fallback for the AutoMapper key |
| `GYM_TEST_CONNECTION_STRING` | Enables SQL Server integration/E2E/performance tests |

> Never commit real connection strings, passwords, or license keys.

## 4. Database and seeding

Pending migrations are applied automatically at startup, followed by data and identity seeding.

Manual migration command:

```bash
dotnet ef database update --project GymManagement.DAL --startup-project GymManagement
```

In Package Manager Console, set **Default project** to `GymManagement.DAL` and run `Update-Database`.

Seeding creates missing roles/users only. If no password is configured, identity seeding is skipped with a warning. Changing the configured password later does not reset existing users.

## 5. Run

```bash
dotnet run --project GymManagement
```

In Development, no AutoMapper license key is needed. In non-development environments the app fails fast at startup when no valid key is found.

---

# ✅ Verification

```bash
dotnet build GymManagementSolution.slnx --nologo
dotnet test GymManagementSolution.slnx --nologo
dotnet list package --vulnerable --include-transitive
```

SQL-dependent tests are skipped unless `GYM_TEST_CONNECTION_STRING` is set. With it, tests create and drop disposable databases named `GymManagement_E2E_*` and `GymManagement_PerfTest_*`.

EF tooling can also be checked without starting the web app:

```bash
dotnet ef migrations list --project GymManagement.DAL --startup-project GymManagement --configuration Production
```

Last full verification: build `0 Warning(s), 0 Error(s)`, tests `99 Passed / 0 Failed / 0 Skipped`, no vulnerable packages, EF tooling OK.

---

# ⚡ Performance notes

List pages were changed to avoid N+1 queries and unbounded result sets:

| Page | Before | Now |
| --- | --- | --- |
| Sessions | 2001 commands, full list | 2 commands: SQL-ordered page + count |
| Booking | 961 commands, full list | 2 commands: filtered SQL page + count |
| Members | All entities including photo payload | 20-row `MemberListInfo` projection + count |
| Memberships | `Include(Member)` + `Include(Plan)` over all rows | 20-row `MembershipListInfo` projection + count |

Indexes were added by migration `20261127005950_AddPerformanceIndexes` for session dates, ongoing sessions, membership end dates, and membership/plan foreign keys. Analytics queries were measured and left unchanged because alternatives were not clearly faster.

---

# 🔐 Roles

| Role | Created by seeder | Notes |
| --- | --- | --- |
| `SuperAdmin` | Yes | Required for member management pages |
| `Admin` | Yes | Authenticated account for general management |
| `Trainer` | No | Not seeded by `IdentityDataSeeding` |
| `Member` | No | Not seeded by `IdentityDataSeeding` |

`HomeController` requires authentication only. Management controllers (`Sessions`, `Trainers`, `Plans`, `Membership`, `Booking`) require `SuperAdmin` or `Admin`. `DashboardController`, `UsersController`, and `MembersController` require `SuperAdmin`. Public registration and login are anonymous.

---

# 🧭 Future improvements

* Add a RESTful Web API version
* Add payment integration
* Add notification system using SignalR
* Expand role-based authorization to more controllers
* Add browser-driven UI tests on top of the existing HTTP E2E tests

---

# 👨‍💻 Author

**Sief Ibrahim Ezzat**

.NET Developer

GitHub: https://github.com/sief-elmenshawi

LinkedIn: https://linkedin.com/in/sief-elmenshawi
