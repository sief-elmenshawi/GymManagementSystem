# Feature: SuperAdmin User Management

Status: **Done**

## Purpose

Let a `SuperAdmin` manage application accounts from `/Users` without editing the database directly.

## Actors and authorization

| Actor | Access |
| --- | --- |
| `SuperAdmin` | Full access to `/Users` |
| Other authenticated users | Redirected to `/Account/AccessDenied` |
| Anonymous users | Login page |

`UsersController` uses `[Authorize(Roles = "SuperAdmin")]`.

## Scope

* `Index`: single-query list of users with roles from `AspNetUserRoles`/`AspNetRoles`
* `Create`: create a confirmed account, assign one existing role, set initial password
* `Edit`: update names, user name, email, phone, role, optionally reset password, and keep the email confirmed in one EF transaction
* `Delete`: POST-only deletion with antiforgery protection
* Explicit route `Users/{action=Index}/{id?}`
* Dashboard card and layout nav link for `SuperAdmin`

## Business rules

* Roles must exist in `RoleManager` before assignment.
* Email and user name must be unique.
* A user cannot delete their own account.
* The last `SuperAdmin` cannot be demoted or deleted.
* Password changes use Identity reset tokens, not direct hash edits.
* Admin-created and admin-edited accounts are marked `EmailConfirmed` because sign-in requires confirmation globally.
* A failed password or role step rolls back the profile and role changes; the change tracker is cleared before the form is redisplayed.

## Non-goals

* Public registration lives at `/Account/Register` and always assigns `Member`
* No self-service profile editing here; members edit their own linked profile in `/Member/EditProfile`
* No bulk user import/export
* No new EF migration (uses existing ASP.NET Core Identity tables)
* Self-service password recovery lives in `AccountController`, not here

## User flow

1. `SuperAdmin` opens `/Dashboard` and selects **Users & Roles**.
2. The user list shows accounts and role badges.
3. The admin creates or edits an account through an antiforgery-protected form.
4. Invalid operations show a validation message and keep the user unchanged.

## Verification evidence

* `dotnet build GymManagementSolution.slnx --nologo`: `0 Warning(s), 0 Error(s)`
* `dotnet test GymManagementSolution.slnx` with `GYM_TEST_CONNECTION_STRING`: `119 Passed / 0 Failed / 0 Skipped`
* E2E coverage: seeded user list, create with role assignment and confirmed email, admin forbidden, last-SuperAdmin demotion guard, invalid-password edit rollback

## Risks and follow-ups

* **Resolved:** `Edit` now wraps profile, password reset, and role assignment in one `GymDbContext` transaction; a later Identity failure rolls back the profile and role changes.
* **Accepted by design:** public registration remains a separate anonymous flow in `AccountController` and still requires email confirmation.
