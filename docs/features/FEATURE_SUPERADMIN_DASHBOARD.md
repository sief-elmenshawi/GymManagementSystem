# Feature: Super Admin Dashboard

Status: **Done**

## Purpose

Give a `SuperAdmin` a single landing page with an at-a-glance summary and direct access to every management area. This is a navigation/overview feature; it does not duplicate the existing CRUD screens.

## Actors and authorization

| Actor | Access |
| --- | --- |
| `SuperAdmin` | `/Dashboard`, all management links, existing Home analytics |
| Other authenticated users | Home analytics only; `/Dashboard` redirects to `/Account/AccessDenied` |
| Anonymous users | Login page |

`DashboardController` uses `[Authorize(Roles = "SuperAdmin")]`.

## Scope

* New `DashboardController.Index` reusing `IAnalyticsService`
* New `Views/Dashboard/Index.cshtml` with six summary cards and six management cards
* Explicit route `Dashboard/{action=Index}/{id?}`
* `HomeController.Index` redirects `SuperAdmin` to `/Dashboard`
* Layout shows a `Dashboard` nav item only for `SuperAdmin`
* README updated with route, behavior, and role notes

## Non-goals

* User management is a separate feature at `/Users`; public registration is at `/Account/Register`
* No new CRUD endpoints
* No new database table, entity, or migration
* No change to analytics query shapes

## Data contracts

No new API/data contract. The view uses the existing `AnalyticsViewModel` from `GymManagement.BLL`.

## User flow

1. User signs in.
2. Login redirects to `/Home`.
3. If the user is `SuperAdmin`, Home redirects to `/Dashboard`; `Member` users go to `/Member`.
4. Dashboard shows analytics counts and links to Members, Trainers, Sessions, Plans, Memberships, and Session Schedule.
5. Non-`SuperAdmin` users requesting `/Dashboard` are redirected to `/Account/AccessDenied`.

## Verification evidence

* `dotnet build GymManagementSolution.slnx --nologo`: `0 Warning(s), 0 Error(s)`
* `dotnet test GymManagementSolution.slnx` with `GYM_TEST_CONNECTION_STRING`: `107 Passed / 0 Failed / 0 Skipped`
* Unit tests: `DashboardControllerTests`, `HomeControllerTests`
* E2E tests: `HomeRoute_RedirectsSuperAdminToDashboard`, `Dashboard_IsForbiddenForAdmin`

## Risks and follow-ups

* The dashboard currently links to existing screens; it does not embed or replace them.
* Management controllers are restricted to `SuperAdmin`/`Admin`; `Member` users land in the separate `/Member` portal.
