# Feature: Member portal

Status: **Done**

## Purpose

Give a registered `Member` a safe landing page after login that shows their own profile and bookings instead of staff analytics.

## Actors and authorization

| Actor | Access |
| --- | --- |
| `Member` | `/Member` only |
| `Admin` | Staff pages and analytics home |
| `SuperAdmin` | Dashboard plus everything |
| Anonymous | Login/register only |

`MemberPortalController` uses `[Authorize(Roles = "Member")]`.

## Scope

* Explicit route `Member/{action=Index}/{id?}`
* `HomeController` redirects `Member` users to `/Member`
* Profile card: name, email, phone, date of birth, gender, address, health-record status
* Bookings table: session, category, coach, start time, attendance
* Helpful notice when a `Member` account has no linked `Member` record
* Staff navigation links hidden from members; `My Profile` shown instead

## Business rules

* The current user is resolved from `UserManager.GetUserId(User)`.
* The only booking query is correlated to `Members.ApplicationUserId == currentUserId`; no other member's bookings can appear.
* Health records remain staff-managed; members see only `Recorded` / `Not recorded`.
* The portal is read-only.

## Non-goals

* No self-service profile editing
* No self-service booking or cancellation
* No membership purchase or payment
* No document/health-record download

## Verification evidence

* `dotnet build GymManagementSolution.slnx --nologo`: `0 Warning(s), 0 Error(s)`
* `dotnet test GymManagementSolution.slnx` with `GYM_TEST_CONNECTION_STRING`: `107 Passed / 0 Failed / 0 Skipped`
* E2E coverage: login redirect to `/Member`, own booking visible, other member's booking hidden, unlinked-account notice

## Risks and follow-ups

* A user who has both `Member` and another role lands in the portal unless they are `SuperAdmin`; account creation currently assigns a single role, so this is not expected in normal use.
