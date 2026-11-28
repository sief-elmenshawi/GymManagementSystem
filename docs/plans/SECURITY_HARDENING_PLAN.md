# Security hardening plan

Status: **Done**

## Outcome

Implement the four previously proposed hardening items 1–4 without changing the agreed product decisions (`RequireConfirmedAccount = true`, seeded users auto-confirmed, Development-only mailbox, SMTP failures logged and skipped, one EF transaction for Identity + gym data).

## Scope

Included:

- G1: rate limiting on anonymous account endpoints.
- G2: proof of current password plus email re-confirmation when a member changes sign-in email.
- G3: observable SMTP failure monitoring (structured logs + metrics + deployment guidance).
- G4: opt-in browser-driven UI tests on top of the existing HTTP E2E tests.

Non-goals:

- README item 5 (expand role-based authorization to more controllers).
- REST API, payments, SignalR.
- Changing lockout settings or the SMTP "log-and-skip" decision.
- Adding a real alerting service or webhook secret.

## Constraints and assumptions

- Endpoint-specific rate limits require `UseRateLimiter()` **after** `UseRouting()`.
- `Microsoft.AspNetCore.RateLimiting` ships with the ASP.NET Core shared framework; no new production package is needed.
- Identity and gym data share one scoped `GymDbContext`; the profile edit stays in one transaction.
- Email delivery happens outside the database transaction; it must never roll back a profile change.
- Browser tests are opt-in (`GYM_RUN_BROWSER_TESTS=1`) because browser binaries are not committed and are not installed by `dotnet test`.

## Tasks

| ID | Action | Expected files/components | Depends on | Acceptance criteria | Verification | Status |
| --- | --- | --- | --- | --- | --- | --- |
| G1 | Add configurable per-IP rate limiting to account POST endpoints | `Program.cs`, new rate-limit options, `AccountController`, tests, deployment docs | — | Login, resend-confirmation, forgot-password and reset-password POSTs return `429` after the configured window; other endpoints unaffected; values are configurable via environment variables | Focused rate-limit tests + full build | Done |
| G2 | Protect member email changes | `MemberPortalController`, `EditProfile.cshtml`, `MemberToUpdateViewModel`, E2E tests, feature docs | — | Current password is verified on every profile save; changed email becomes unconfirmed and receives a fresh confirmation link; unchanged email keeps `EmailConfirmed`; failed password leaves data untouched | Focused E2E self-service tests | Done |
| G3 | Make SMTP failures observable | `AppEmailSender`, new monitor abstraction/implementation, deployment docs, unit tests | — | Missing SMTP config and SMTP exceptions increment a failure metric and emit structured logs; success path is countable; no secret/PII in metric tags; log-and-skip unchanged | Focused unit tests + docs review | Done |
| G4 | Add browser-driven UI tests | test csproj, Playwright fixture, browser tests, README, plan | G2 | Chromium tests exercise real rendered pages; skip with a clear reason when SQL test DB, opt-in flag or browser binaries are absent | Opt-in browser run + normal full test suite (skips cleanly) | Done |
| G5 | Final verification and delivery | this plan, README, git | G1–G4 | Build clean, full suite green with SQL env var, no vulnerable packages, no pending model changes, `master` pushed | Full verification command set + `git status -sb` | Done |

## Checkpoints

1. After G1: build and focused rate-limit tests before touching the portal.
2. After G2: focused self-service E2E tests, then review the confirmation flow end to end.
3. After G3: focused email sender tests and deployment doc review.
4. After G4: run normal suite to confirm clean skips, then opt-in browser run if a browser is available.
5. After G5: commit and push `master`.

## Evidence log

- G1: `dotnet test ... --filter FullyQualifiedName~AuthenticationRateLimitTests` with `GYM_TEST_CONNECTION_STRING` -> `Passed: 2, Failed: 0, Skipped: 0`.
- G2: `dotnet test ... --filter FullyQualifiedName~MemberSelfServiceFlowTests` -> `Passed: 6, Failed: 0, Skipped: 0` (fresh confirmation, wrong-password rollback, unchanged-email case).
- G3: `dotnet test ... --filter FullyQualifiedName~AppEmailSenderTests` -> `Passed: 3, Failed: 0, Skipped: 0`.
- G4: default run -> `Skipped: 4` with prerequisite reasons; opt-in run with `GYM_TEST_CONNECTION_STRING` + `GYM_RUN_BROWSER_TESTS=1` -> `Passed: 4, Failed: 0, Skipped: 0`.
- G5 verification: `dotnet build GymManagementSolution.slnx --nologo` -> `0 Warning(s), 0 Error(s)`; full suite with `GYM_TEST_CONNECTION_STRING` -> `Passed: 133, Failed: 0, Skipped: 4`; with `GYM_RUN_BROWSER_TESTS=1` -> `Passed: 137, Failed: 0, Skipped: 0`; `dotnet list ... package --vulnerable --include-transitive` -> no vulnerable packages; `dotnet ef migrations list` -> latest `20261128102032_BookingUniqueIndex`; `dotnet ef migrations has-pending-model-changes` -> no changes.
