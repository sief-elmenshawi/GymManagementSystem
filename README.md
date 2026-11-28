# 🏋️ Gym Management System

An ASP.NET Core MVC application for managing gym members, trainers, plans, memberships, sessions, and bookings. It uses a layered architecture, EF Core with SQL Server, ASP.NET Core Identity, AutoMapper, and Razor views.

## 🆕 Latest update — 28 November 2026

- Explicit role-matrix coverage for anonymous, `SuperAdmin`, `Admin`, and `Member` routes.
- Shared account layout plus a stricter responsive/accessibility rendering audit; mobile overflow on member/home pages fixed.
- Booking creation now runs in one EF transaction with a unique `(MemberId, SessionId)` index from migration `20261128102032_BookingUniqueIndex`.
- SQL-backed paging for plan and trainer lists; documentation and verification numbers refreshed.
- Verified: build `0 Warning(s), 0 Error(s)`, default tests `133 Passed / 4 Skipped / 137 Total`, browser-enabled tests `137 Passed / 137 Total`.

---

# 🚀 Features

## 👤 Authentication and account security

* ASP.NET Core Identity with unique emails and account lockout
* Email confirmation is required before any account can sign in
* `/Account/ForgotPassword` and `/Account/ResetPassword` send single-use reset links
* Seeded `SuperAdmin`, `Admin`, and `Member` roles (user accounts only when a seed password is configured)
* Seeded and SuperAdmin-created accounts are auto-confirmed
* Global antiforgery validation for unsafe requests
* HTTPS redirection only when an HTTPS URL/port is configured
* Existing seeded user passwords are **not** reset on later startups

## 📝 Public member registration

* `/Account/Register` is public and linked from the login page
* Creates an Identity account with **only** the `Member` role; role fields from the request are ignored
* Creates a linked `Member` record with contact and address details
* Sends a confirmation email; the account cannot sign in until the link is opened
* Health records are not collected during registration; staff can add/edit them from the member page
* Duplicate Identity/Member emails and member phone numbers are rejected

## 🧭 Super Admin dashboard

* `/Dashboard` is restricted to `SuperAdmin`
* Shows analytics summary cards and quick links to every management page
* `Home` redirects `SuperAdmin` users to the dashboard and `Member` users to `/Member`; admins keep the analytics home page
* Users without `SuperAdmin` are redirected to `AccessDenied`
* Includes a shortcut to `/Users` for account management

## 👤 User management

* `/Users` is restricted to `SuperAdmin`
* List accounts with their assigned roles
* Create users with a chosen role and password; admin-created accounts are confirmed automatically
* Edit account details, role, and optionally reset the password
* Delete users, with guards against self-deletion and demoting/deleting the last `SuperAdmin`

---

## 👥 Member management

* Create, edit, delete, and list members
* Store contact details, address, date of birth, gender, and optional photo
* View, create, and edit member health records from the member details page
* Member list is paged and uses a lightweight projection (the photo payload is not loaded)

## 🧍 Member portal

* `/Member` is restricted to the `Member` role
* Shows the signed-in member's profile, health-record status, and **only their own bookings**
* Accounts with the `Member` role but no linked profile get a clear contact-admin notice
* Staff navigation links are hidden from members

## 🏋️ Trainer management

* Create, edit, delete, and list trainers
* Trainer list is ordered and paged in SQL
* Maintain specialty and contact details
* Assign trainers to sessions

## 📅 Session management

* Create, edit, delete, and list sessions
* Enforce capacity and schedule rules
* Show available slots from booking counts in a single SQL query
* Session list is ordered and paged in SQL

## 💳 Membership management

* Create and manage plans
* Plan list is ordered and paged in SQL
* Create and cancel member memberships
* List active memberships with member and plan names via a flat SQL projection
* Membership list is ordered and paged in SQL

## 📌 Booking system

* Book members into sessions
* Prevent bookings that violate business rules (membership, capacity, timing, duplicates)
* Track attendance
* Anti-join query for available members instead of loading all bookings into memory
* Booking creation runs in one transaction; a unique `(MemberId, SessionId)` index stops concurrent duplicate bookings

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
├── GymManagement.Tests        Unit, SQL performance, HTTP E2E, and opt-in browser UI tests
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
| Testing | xUnit, Moq, EF Core SQL Server integration tests, Playwright/Chromium UI tests |

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
| `GYM_RUN_BROWSER_TESTS` | Set to `1` to enable Chromium-driven UI tests |
| `AuthenticationRateLimit__PermitLimit` | Login/reset requests per client IP per window (default `8`) |
| `AuthenticationRateLimit__WindowSeconds` | Rate-limit window in seconds (default `60`) |
| `Email__From` | Sender address for account emails (default `no-reply@gymmanagement.local`) |
| `Email__Smtp__Host` | SMTP host; when empty, Development uses the local mailbox |
| `Email__Smtp__Port` | SMTP port (default `587`) |
| `Email__Smtp__UseSsl` | Enable SSL/STARTTLS (default `true`) |
| `Email__Smtp__User` | SMTP username |
| `Email__Smtp__Password` | SMTP password/secret |

In Development, confirmation and reset messages are captured in memory and can be read from `GET /dev-mailbox/{email}` (`DELETE /dev-mailbox` clears them). These endpoints are never mapped outside Development. In other environments, configure SMTP; otherwise email delivery is skipped and logged as an error. Failures also increment the `email_delivery_failures` counter; see [email delivery monitoring](docs/deployment/PRODUCTION_CONFIGURATION.md#email-delivery-monitoring).

Anonymous login, resend-confirmation, forgot-password and reset-password POSTs are rate limited per client IP and endpoint (`AuthenticationRateLimit__*` settings above). Exceeding the limit returns `429` with `Retry-After`.

> Never commit real connection strings, passwords, or license keys.

For Production, use the [deployment configuration checklist](docs/deployment/PRODUCTION_CONFIGURATION.md), copy [production.env.example](docs/deployment/production.env.example) to a secure local file, and validate it with `pwsh ./scripts/check-production-config.ps1` (or `powershell` on Windows PowerShell 5.1) before starting the app. The checker reports only missing names and never prints values.

## 4. Database and seeding

Pending migrations are applied automatically at startup, followed by data and identity seeding.

Manual migration command:

```bash
dotnet ef database update --project GymManagement.DAL --startup-project GymManagement
```

In Package Manager Console, set **Default project** to `GymManagement.DAL` and run `Update-Database`.

Seeding creates missing roles/users only. If no password is configured, identity seeding is skipped with a warning. Changing the configured password later does not reset existing users.

The development database is up to date through `20261128102032_BookingUniqueIndex` (no pending model changes). When upgrading an existing database, run `scripts/sql/check-booking-duplicates.sql` first and resolve any duplicate bookings before that migration applies.

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
pwsh ./scripts/check-production-config.ps1 -EnvFile ./production.env
```

The production checker is optional for local runs; it validates names for a deployed environment and never prints values. See [Production configuration](docs/deployment/PRODUCTION_CONFIGURATION.md).

SQL-dependent tests are skipped unless `GYM_TEST_CONNECTION_STRING` is set. With it, tests create and drop disposable databases named `GymManagement_E2E_*` and `GymManagement_PerfTest_*`.

### Browser-driven UI tests

`BrowserUiFlowTests` and `UiRenderingSmokeTests` use Playwright with headless Chromium against a real Kestrel process. They are skipped by default; the skip message lists any missing prerequisite. To run them locally:

```powershell
# Once per machine / after a Playwright version upgrade
powershell -ExecutionPolicy Bypass .\GymManagement.Tests\bin\Debug\net9.0\playwright.ps1 install chromium

$conn = (Get-Content GymManagement/appsettings.json -Raw | ConvertFrom-Json).ConnectionStrings.DefaultConnection
$env:GYM_TEST_CONNECTION_STRING = $conn
$env:GYM_RUN_BROWSER_TESTS = '1'
dotnet test GymManagementSolution.slnx --nologo --filter "FullyQualifiedName~BrowserUiFlowTests|FullyQualifiedName~UiRenderingSmokeTests"
```

The fixture starts the app on a free localhost port with a disposable `GymManagement_E2E_Browser_*` database and Development mailbox, then tears both down. Never set `GYM_RUN_BROWSER_TESTS` in Production.

`BrowserUiFlowTests` also guard the login error regression: an unconfirmed account must show the confirmation message exactly once. `UiRenderingSmokeTests` walks every linked public, staff, and member page at desktop width plus representative pages at a 390px mobile width, checking broken images, duplicate ids, missing accessible names, overflow, and console errors.

### Continuous integration

`.github/workflows/ci.yml` runs the build and the full test suite (including browser tests) on push and pull requests. It expects one repository secret, `GYM_CI_SA_PASSWORD`, used only by the disposable CI SQL Server container. No password or connection string is committed.

EF tooling can also be checked without starting the web app:

```bash
dotnet ef migrations list --project GymManagement.DAL --startup-project GymManagement --configuration Production
```

Last full verification: build `0 Warning(s), 0 Error(s)`, full default suite `143 Passed / 0 Failed / 4 Skipped / 147 Total` (browser tests opt-in), opt-in browser suite `4 Passed / 0 Failed / 0 Skipped / 4 Total`, and `dotnet ef migrations has-pending-model-changes` reported no pending changes.

---

# ⚡ Performance notes

List pages were changed to avoid N+1 queries and unbounded result sets:

| Page | Before | Now |
| --- | --- | --- |
| Sessions | 2001 commands, full list | 2 commands: SQL-ordered page + count |
| Booking | 961 commands, full list | 2 commands: filtered SQL page + count |
| Members | All entities including photo payload | 20-row `MemberListInfo` projection + count |
| Memberships | `Include(Member)` + `Include(Plan)` over all rows | 20-row `MembershipListInfo` projection + count |
| Plans | Full table load | 12-row SQL-ordered page + count |
| Trainers | Full table load | 12-row SQL-ordered page + count |

Indexes were added by migration `20261127005950_AddPerformanceIndexes` for session dates, ongoing sessions, membership end dates, and membership/plan foreign keys. Migration `20261128102032_BookingUniqueIndex` adds a surrogate booking key and the unique `(MemberId, SessionId)` index used as the final duplicate-booking guard. Analytics queries were measured and left unchanged because alternatives were not clearly faster.

---

# 🔐 Roles

| Role | Created by seeder | Notes |
| --- | --- | --- |
| `SuperAdmin` | Yes | Required for member management pages |
| `Admin` | Yes | Authenticated account for general management |
| `Trainer` | No | Not seeded by `IdentityDataSeeding` |
| `Member` | No | Not seeded by `IdentityDataSeeding` |

`HomeController` requires authentication only and redirects by role. `MemberPortalController` requires `Member`. Management controllers (`Sessions`, `Trainers`, `Plans`, `Membership`, `Booking`) require `SuperAdmin` or `Admin`. `DashboardController`, `UsersController`, and `MembersController` require `SuperAdmin`. Public account actions are explicitly anonymous. `AuthorizationMatrixTests` covers every combination, including denied cross-role access. The development mailbox endpoints are mapped only in Development.

---

# 🧭 Future improvements

* Add a RESTful Web API version
* Add payment integration
* Add notification system using SignalR

---

# 👨‍💻 Author

**Sief Ibrahim Ezzat**

.NET Developer

GitHub: https://github.com/sief-elmenshawi

LinkedIn: https://linkedin.com/in/sief-elmenshawi
