# Feature: Public member registration

Status: **Done**

## Purpose

Allow a visitor to create a gym member account without administrator intervention, while guaranteeing that the new account can never receive an administrative role.

## Actors and authorization

| Actor | Access |
| --- | --- |
| Anonymous visitor | `/Account/Register`, `/Account/Login` |
| `Member` | `/Member` portal after email confirmation; staff management pages are forbidden |
| `Admin` | Staff management pages, except SuperAdmin-only pages |
| `SuperAdmin` | Everything |

`AccountController.Register` uses `[AllowAnonymous]`. Management controllers use `SuperAdmin,Admin` role requirements; `Dashboard`, `Users`, and `Members` remain `SuperAdmin`-only.

## Scope

* Public `/Account/Register` GET/POST with antiforgery protection
* Registration link on the login page
* Account creation through `UserManager`
* Hard-coded `Member` role assignment; no role input is accepted from the request
* `Member` record creation linked by `ApplicationUserId`
* Duplicate email and phone validation across Identity and `Members`
* Confirmation email with a signed Identity token; login is blocked until confirmation

## Business rules

* The `Member` role is seeded alongside `SuperAdmin` and `Admin`.
* Identity `UserName` is the email address; login continues to work by email.
* `SignIn.RequireConfirmedAccount` and `RequireConfirmedEmail` are enabled globally; seeded and SuperAdmin-created accounts are auto-confirmed.
* User creation, `Member` role assignment, and linked `Member` record creation commit in one EF transaction; any failure rolls the whole registration back.
* Health data is not collected at registration. `Member.HealthRecord` is optional until staff add/edit it from `/Members/HealthRecordCreate/{id}`.
* `Photo` is stored as an empty string until a staff member uploads one.
* Deleting the Identity user clears the link (`FK ... ON DELETE SET NULL`) instead of deleting gym history.

## Data model

* `Member.ApplicationUserId` — nullable `nvarchar(450)` FK to `AspNetUsers.Id`
* Unique filtered index on `ApplicationUserId` (one user links to at most one member)
* `ApplicationUser.Member` — optional navigation
* Migration: `20261127102651_AddMemberUserLink`

## Non-goals

* No self-service profile editing in this anonymous flow; members edit their linked profile at `/Member/EditProfile`
* No membership purchase or payment
* No photo upload at registration
* No health questionnaire at registration

## User flow

1. Visitor opens `/Account/Login` and selects **Create an account**.
2. Visitor submits name, email, phone, date of birth, gender, address, and password.
3. The app creates the Identity user, assigns only `Member`, creates the linked `Member` record, and emails a confirmation link.
4. The visitor is redirected to login with instructions to check their email.
5. Login remains blocked until `/Account/ConfirmEmail` is opened with a valid token.
6. After confirmation, `Home` redirects the `Member` to `/Member` for their profile and bookings.

## Verification evidence

* `dotnet build GymManagementSolution.slnx --nologo`: `0 Warning(s), 0 Error(s)`
* `dotnet test GymManagementSolution.slnx` with `GYM_TEST_CONNECTION_STRING`: `119 Passed / 0 Failed / 0 Skipped`
* E2E coverage: role and link creation, duplicate email rejection, ignored admin-role field, blocked pre-confirmation login, login after confirmation, member forbidden from staff booking pages, missing-role rollback
* EF migration `AddMemberUserLink` applied by the test host during startup

## Risks and follow-ups

* **Resolved:** Identity and gym data share one `GymDbContext`, so registration uses one explicit EF transaction instead of compensating deletes.
* **Resolved:** the member portal now supports self-service profile editing and self-service booking/cancellation; see `FEATURE_MEMBER_PORTAL.md`.
