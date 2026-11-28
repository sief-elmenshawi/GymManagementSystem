# Comprehensive hardening plan

This document tracks the remaining production hardening work. It is intentionally implementation-focused: each task is small, independently verifiable, and preserves the existing MVC routes, form field names, redirect behavior, and exact user-facing strings.

## Status summary

- OPS-1: Done — CI secret confirmed and failed run is green.
- OPS-2: Done for the local staging database — the unique-index migration is already applied and the duplicate check returned zero rows. No user-supplied env var was available; the local `skip-worktree` connection string was used without printing or committing it.
- OPS-3: Blocked — SMTP values were not supplied; missing SMTP is logged and skipped as designed.
- A1–A16: Done and verified by build/tests.
- The batch is committed and pushed to `master`; no secrets are included.

## Constraints

- Do not commit secrets or connection strings.
- `GymManagement/appsettings.json` stays `skip-worktree`; its committed form contains no connection string.
- `GymManagement/dotnet-tools.json` remains locally excluded.
- Keep `RequireConfirmedEmail = true`, Development-only mailbox, confirmed generated users, and SMTP failure logging without rollback.
- Keep existing antiforgery tokens, form field names, links, routes, redirect targets, and exact E2E assertion strings.
- Endpoint rate limits require `UseRateLimiter()` after `UseRouting()`.
- Email delivery stays outside database transactions and never rolls them back.
- Browser tests remain opt-in via `GYM_RUN_BROWSER_TESTS=1`.
- Database-backed tests require `GYM_TEST_CONNECTION_STRING`.
- Prefer UTC (`DateTime.UtcNow` / `TimeProvider`) for app timestamps and test fixtures.

## Tasks

| ID | Action | Expected files/components | Depends on | Acceptance criteria | Verification | Status |
| --- | --- | --- | --- | --- | --- | --- |
| OPS-1 | Verify CI secret and rerun failed CI | `.github/workflows/ci.yml`, GitHub secret | — | `GYM_CI_SA_PASSWORD` exists; rerun of `46135896202` green | `gh run watch 46135896202 --exit-status` | Done |
| OPS-2 | Verify staging migration state and duplicate bookings | local staging DB, EF CLI, preflight SQL | Available local connection | Duplicate check returns zero; latest migration applied; no secret printed/committed | Duplicate query + migration history | Done (local staging) |
| OPS-3 | Configure SMTP using user-supplied values | env vars, email sender, docs | User input | Mail sends in target environment; failures are logged without rolling back DB writes | Send a test confirmation email | Blocked |
| A1 | Add per-IP/per-user rate limits to account POSTs | `AuthenticationRateLimiting`, controllers, options, Program | — | Login/register/forgot/reset/edit-profile are limited; `Retry-After` is present on 429 | `AuthenticationRateLimitTests` | Done |
| A2 | Harden forwarded headers and host filtering | `HostingConfiguration`, Program, config docs | — | Forwarded headers only from configured proxies; unexpected hosts are rejected | Build + configuration review | Done |
| A3 | Prevent duplicate bookings at the database level | Booking unique index, migration, service handling, preflight SQL | OPS-2 | Existing DB checked; duplicate save produces a stable user message | SQL preflight + migration history + focused tests | Done (applied on local staging DB) |
| A4 | Audit health-record creation from member-create | `MemberService`, audit model, E2E assertion | — | Initial health record writes the same audit row as later edits, with actor email | `MemberAndBookingFlowTests` | Done |
| A5 | Classify and log unexpected database failures | `DatabaseErrors`, services, controllers, tests | — | Duplicate violations stay user-friendly; other DB failures are logged and return a generic message | Focused tests + logs | Done |
| A6 | Make user management transactional and bounded | `UsersController`, repository paging | — | Role/user updates commit atomically; list endpoints have bounded page sizes | Build + focused tests | Done |
| A7 | Opt into demo user seeding outside Development | `SeedCredentials`, Program, tests | — | Production does not seed users unless explicitly enabled | `SeedCredentialsTests` | Done |
| A8 | Standardize app clocks on UTC | Services, fixtures, tests | — | Time comparisons use UTC consistently; no full-suite failures from local time | Full test suite | Done |
| A9 | Align validation messages and model metadata | ViewModels, tests | — | Validation is consistent between client/server and tests | Focused tests | Done |
| A10 | Harden attachment uploads | `AttachmentService`, options, views, tests | — | Extension allowlist is case-insensitive and configurable; content signatures and size limits are enforced | `AttachmentServiceTests` | Done |
| A11 | Propagate cancellation tokens and normalize `ResultKind` | services, controllers, tests | — | Async paths honor cancellation; result kinds are consistent without changing HTTP redirects/strings | Build + focused tests | Done |
| A12 | Harden CI workflow | `.github/workflows/ci.yml` | OPS-1 | CI builds/tests with SQL Server and no secrets in logs | GitHub run `46135896202` | Done |
| A13 | Fix accessibility/test-audit findings | views, tests | — | Rendered-page tests pass; no obvious accessibility regressions | Focused suite | Done |
| A14 | Cover new hardening behavior with tests | unit + E2E tests | A1–A13 | New and updated tests pass with SQL backing | Focused suite: 37/37 passed | Done |
| A15 | Clean up misspelled names and duplicate helpers | repository folders, services, view models, controllers | A1–A14 | Misspelled identifiers removed; shared helpers consolidated; build has no warnings/errors | Build: 0 warnings, 0 errors | Done |
| A16 | Final verification and documentation refresh | full suite, browser suite, EF model check, plan | A15 | Full suite green; browser suite green; no pending model changes; statuses current | See evidence log | Done |

## Evidence log

- `2026-11-28` — OPS-1: `gh run watch 46135896202 --exit-status` → green.
- `2026-11-28` — A15: `dotnet build GymManagementSolution.slnx --nologo --no-restore -p:OutDir=... -p:UseAppHost=false` → `0 Warning(s), 0 Error(s)`.
- `2026-11-28` — A14 focused suite: `Failed: 0, Passed: 37, Skipped: 0, Total: 37`.
- `2026-11-28` — Full default suite after A15: `Failed: 0, Passed: 143, Skipped: 4, Total: 147`.
- `2026-11-28` — Browser suite (`GYM_RUN_BROWSER_TESTS=1`) after A15: `Failed: 0, Passed: 4, Skipped: 0, Total: 4`.
- `2026-11-28` — `dotnet ef migrations has-pending-model-changes` → `No changes have been made to the model since the last migration.`
- `2026-11-28` — OPS-2 staging check: duplicate booking groups `0`; all 8 migrations applied, last `20261128102032_BookingUniqueIndex`.
- `2026-11-28` — Staging smoke run: app started with `ASPNETCORE_ENVIRONMENT=Staging`, explicit `AllowedHosts`, AutoMapper license, and deliberate seed opt-in. Public pages returned 200; protected pages redirected to login; seeded users confirmed with `SuperAdmin`/`Admin` roles.
- `2026-11-28` — A12 workflow was hardened after the green `46135896202` run. First pushed run `46145816401` failed only at the EF model check because the design-time DbContext needed `ConnectionStrings__DefaultConnection`; the step now passes it from `GYM_TEST_CONNECTION_STRING`.

## Remaining work

1. OPS-3: the user must supply SMTP values or set the `Email__*` environment variables. Until then, delivery is logged and skipped without rolling back database writes.
2. Confirm the CI run triggered by the push; no other local work is pending.

## Deployment notes for the user

- Before applying `20261128102032_BookingUniqueIndex` to an existing database, run `scripts/sql/check-booking-duplicates.sql` and resolve any rows it returns.
- Never put the production connection string or SMTP password into `appsettings.json` or source control. Supply them via environment variables / secret manager in the target environment.
- Expected SMTP environment variable names: `Email__From`, `Email__Smtp__Host`, `Email__Smtp__Port`, `Email__Smtp__UseSsl`, `Email__Smtp__User`, `Email__Smtp__Password`. These map to the app's `EmailOptions` section; do not commit their values.
