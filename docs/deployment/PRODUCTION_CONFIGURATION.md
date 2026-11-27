# Production configuration

Status: **Recommended deployment checklist**

This document describes how to supply runtime secrets for the gym application without committing them. Source control contains only the committed safe `GymManagement/appsettings.json`, this template, and the checker script. Real connection strings, passwords, and license keys must come from the process environment, a systemd `EnvironmentFile`, Docker `--env-file`, or a secrets manager.

Related: [README](../../README.md), [account recovery feature](../features/FEATURE_ACCOUNT_RECOVERY.md), [production.env.example](production.env.example).

PowerShell examples use `pwsh`; on Windows PowerShell 5.1 run the same commands with `powershell` instead.

## Prerequisites

* .NET 9 runtime and the published application files
* SQL Server database reachable from the application host
* AutoMapper license key valid for non-Development use
* SMTP relay credentials if confirmation/reset emails must be delivered

`dotnet user-secrets` is for local Development only; ASP.NET Core does not load it in Production.

## Configuration keys

Environment-variable names use `__` for configuration sections (for example, `Email:Smtp:Host` becomes `Email__Smtp__Host`). The checker also accepts colon-style keys when reading an env file.

| Variable | Required? | Purpose |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Yes | Must be `Production`. `DOTNET_ENVIRONMENT` is also accepted by the checker. |
| `ConnectionStrings__DefaultConnection` | Yes | SQL Server connection string used by the app and startup migrations. |
| `AUTOMAPPER_LICENSE_KEY` | One source required | AutoMapper key required outside Development. |
| `LUCKYPENNY_LICENSE_KEY` | One source required | Documented AutoMapper fallback key. |
| `AutoMapper__LicenseKey` | One source required | Configuration equivalent of `AutoMapper:LicenseKey`. |
| `GYM_SEED_PASSWORD` | Bootstrap only | Password for seeded `SuperAdmin`/`Admin`/`Member` accounts. Takes precedence over `SeedUsers:Password`. |
| `SeedUsers__Password` | Bootstrap only | Alternative seed-password source. Existing passwords are never reset. |
| `Email__Smtp__Host` | Deployment checklist | Enables confirmation/reset emails. Without it, Production logs an error and skips delivery. |
| `Email__From` | Optional | Sender address. Runtime default: `no-reply@gymmanagement.local`. |
| `Email__Smtp__Port` | Optional | SMTP port. Runtime default: `587`. |
| `Email__Smtp__UseSsl` | Optional | SSL/STARTTLS. Runtime default: `true`. |
| `Email__Smtp__User` | Conditional | SMTP username when the relay requires authentication. |
| `Email__Smtp__Password` | Conditional | SMTP secret when the relay requires authentication. |
| `AuthenticationRateLimit__PermitLimit` | Optional | Requests per client IP and account endpoint per window. Runtime default: `8`. |
| `AuthenticationRateLimit__WindowSeconds` | Optional | Fixed-window length in seconds. Runtime default: `60`. |
| `GYM_TEST_CONNECTION_STRING` | Test runs only | Enables SQL integration/E2E/performance tests. Do not set in Production. |

Do not fill values in `production.env.example`. Copy it to a secure location and keep the copy out of Git.

## Start from the template

```powershell
Copy-Item .\docs\deployment\production.env.example .\production.env
# Edit .\production.env locally with real values, then verify it:
pwsh .\scripts\check-production-config.ps1 -EnvFile .\production.env
```

The local `production.env` is ignored by the repository `.gitignore`. Restrict access to the file (`chmod 600` on Linux) and delete it if it is no longer needed.

## Supply values to the application

### Current PowerShell process

Useful for a one-off local launch. Variables set this way disappear when the shell closes and may remain in shell history, so prefer a secret store for real deployments.

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Production'
$env:ConnectionStrings__DefaultConnection = '<connection string>'
$env:AUTOMAPPER_LICENSE_KEY = '<license key>'
$env:GYM_SEED_PASSWORD = '<bootstrap password>'
$env:Email__Smtp__Host = '<smtp host>'
$env:Email__Smtp__User = '<smtp user>'
$env:Email__Smtp__Password = '<smtp password>'

pwsh .\scripts\check-production-config.ps1
dotnet run --project GymManagement --configuration Release --no-launch-profile
```

`setx` can persist a variable for later interactive sessions, but it stores the value in the Windows registry and can expose it through command history. Prefer a managed secret store or a protected service configuration where available.

```powershell
setx ASPNETCORE_ENVIRONMENT Production
setx GYM_SEED_PASSWORD '<bootstrap password>'
```

### Linux/systemd

Create a root-owned env file and reference it from the service unit:

```bash
sudo install -m 600 /dev/null /etc/gymmanagement/production.env
sudo nano /etc/gymmanagement/production.env
```

```ini
[Service]
EnvironmentFile=/etc/gymmanagement/production.env
WorkingDirectory=/opt/gymmanagement
ExecStart=/usr/bin/dotnet /opt/gymmanagement/GymManagement.dll
```

```bash
sudo systemctl daemon-reload
sudo systemctl restart gymmanagement
pwsh ./scripts/check-production-config.ps1 -EnvFile /etc/gymmanagement/production.env
```

### Docker

Mount or pass an env file; never bake secrets into an image or build argument:

```bash
docker run --env-file /secure/path/production.env -p 8080:8080 gymmanagement:<tag>
docker exec <container> pwsh ./scripts/check-production-config.ps1
```

For orchestrators, populate the same names from the platform's secret mechanism and run the checker in the application's process context.

## Startup behavior to expect

| Situation | Behavior |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT=Production` | HSTS and the error page are enabled; `/dev-mailbox/*` is never mapped. |
| AutoMapper key missing or malformed | The app fails fast at startup with a message naming the accepted variables. A well-formed key has three JWT-shaped segments; AutoMapper validates the signature. |
| Seed password missing | A warning is logged and identity seeding is skipped. Set a password only to bootstrap missing default users; changing it later does not reset existing accounts. |
| SMTP host missing | Each attempted confirmation/reset email is logged as an error and skipped. Registration still succeeds, but the user cannot sign in until confirmed. |
| Pending EF migrations | Applied automatically at startup before seeding. |
| HTTPS redirection | Enabled only when an HTTPS URL/port is configured. |
| Too many login/reset requests from one IP | The endpoint returns `429` with `Retry-After`; other endpoints are unaffected. |

Because `SignIn.RequireConfirmedAccount` is `true`, Production must have working email delivery for self-service registration, password recovery, and member email changes.

## Email delivery monitoring

`AppEmailSender` reports every outcome through `IEmailDeliveryMonitor`. The default `EmailDeliveryMonitor` cannot throw on delivery failure, so the existing "log and skip" behavior is unchanged: registration and profile edits still commit when SMTP is unavailable.

| Signal | Name / template | Notes |
| --- | --- | --- |
| Meter | `GymManagement.Email` | Export with an OpenTelemetry metrics exporter. |
| Counter | `email_delivery_failures` | Tags: `reason` (`NotConfigured` / `SmtpError`), `smtp_host` (`none` when unset). No addresses in tags. |
| Counter | `email_delivery_successes` | Increments for SMTP sends and Development mailbox captures. |
| Log | `Email delivery is not configured... Failed for {Email} (subject: {Subject}).` | Log level `Error`; occurs once per attempted email when no SMTP host is set. |
| Log | `Failed to send account email to {Email} (subject: {Subject}, smtp host: {SmtpHost}).` | Log level `Error` with the SMTP exception attached. |

Recommended alerts:

* Page when `rate(email_delivery_failures[5m]) > 0` in Production — with `RequireConfirmedAccount = true`, users cannot sign in or confirm a changed email while delivery is broken.
* Warn when `email_delivery_successes` stays at zero while registrations or reset requests are occurring.
* A log-based alert on the two `Error` templates above is equivalent if metrics are not exported yet.

To add a webhook or ticket integration, replace the `IEmailDeliveryMonitor` singleton in the host composition with an implementation that forwards `RecordFailure` to that system; keep the counters/logs as the fallback signal. Do not put webhook secrets in `appsettings.json` — use the same environment/secret sources described above.

## Verify before and after deployment

```powershell
# Presence-only check; no values are printed.
pwsh .\scripts\check-production-config.ps1 -EnvFile .\production.env

# Require a bootstrap password source.
pwsh .\scripts\check-production-config.ps1 -EnvFile .\production.env -RequireSeedPassword

# Explicitly allow a deployment without SMTP (emails will be skipped and logged).
pwsh .\scripts\check-production-config.ps1 -EnvFile .\production.env -AllowNoSmtpHost
```

Expected successful output contains only names such as:

```text
PASS: Environment is Production (via ASPNETCORE_ENVIRONMENT)
PASS: SQL connection string (via ConnectionStrings__DefaultConnection)
PASS: AutoMapper license key (via AUTOMAPPER_LICENSE_KEY)
PASS: SMTP host (via Email__Smtp__Host)
PASS: Seed password (via GYM_SEED_PASSWORD)
Production configuration check passed. No values were printed.
```

Application checks:

```powershell
dotnet build GymManagementSolution.slnx --nologo
dotnet test GymManagementSolution.slnx --nologo
dotnet list package --vulnerable --include-transitive
git status --short
```

For EF tooling, pass the connection string at invocation time instead of committing it:

```powershell
$conn = (Get-Content -Raw .\GymManagement\appsettings.json | ConvertFrom-Json).ConnectionStrings.DefaultConnection
dotnet ef migrations list --project GymManagement.DAL --startup-project GymManagement --configuration Production --connection $conn
```

## Troubleshooting

| Symptom | Check |
| --- | --- |
| `FAIL: Environment must be Production` | Set `ASPNETCORE_ENVIRONMENT=Production` in the same process/service context. |
| `AutoMapper ... not a well-formed JWT` | Use a real three-segment license key from one of the supported variables. The checker does not print it. |
| Startup warning about seed admin password | Expected when no seed password is set; default users are not created. |
| `Email delivery is not configured` in logs / `email_delivery_failures{reason="NotConfigured"}` | Set `Email__Smtp__Host` and any provider-required credentials. Production has no mailbox fallback. |
| `Failed to send account email` / `email_delivery_failures{reason="SmtpError"}` | Check relay reachability, credentials, `Email__From`, port and SSL settings; the exception is attached to the log. |
| User secrets work locally but not on the server | User Secrets are Development-only; use one of the mechanisms above. |
| Script reports a missing key that you know is set | Run the checker in the same account/context as the service, or pass `-EnvFile` explicitly. |
