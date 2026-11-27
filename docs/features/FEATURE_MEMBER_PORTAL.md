# Feature: Member portal

Status: **Done**

## Purpose

Give a registered `Member` a safe landing page after login that shows their own profile and bookings instead of staff analytics.

## Actors and authorization

| Actor | Access |
| --- | --- |
| `Member`-only account | `/Member` profile, self-edit, sessions, booking and cancellation |
| `Admin` | Staff pages and analytics home |
| `SuperAdmin` | Dashboard plus everything |
| Anonymous | Login/register only |

`MemberPortalController` uses `[Authorize(Roles = "Member")]`.

## Scope

* Explicit route `Member/{action=Index}/{id?}`
* `HomeController` redirects `Member` users to `/Member`
* Profile card: name, email, phone, date of birth, gender, address, health-record status
* `GET/POST /Member/EditProfile` for the signed-in member's linked profile
* `GET /Member/Sessions`, `POST /Member/Book`, and `POST /Member/Cancel`
* Bookings table: session, category, coach, start time, attendance, and cancel action
* Helpful notice/AccessDenied redirect when a `Member` account has no linked `Member` record
* Staff navigation links hidden from members; `My Profile`, `Edit Profile`, and `Book a Session` shown instead

## Business rules

* The current user is resolved from `UserManager.GetUserId(User)`; every profile/booking action derives `MemberId` from the linked `Member.ApplicationUserId`. No route or form value can select another member.
* All booking views and mutations reuse `IBookingService`, so capacity, duplicate, started-session, and active-membership rules stay identical to staff booking.
* Profile editing reuses `IMemberService` and updates the linked Identity email/user name/phone in the same EF transaction; a failure rolls both back.
* Health records remain staff-managed; members see only `Recorded` / `Not recorded`.
* `Member` + another role follows the staff home policy (`SuperAdmin` -> dashboard, otherwise analytics); only a member-only account lands in `/Member`.

## Non-goals

* No membership purchase or payment
* No document/health-record download
* No `MarkAttendedAsync` or member-list access for members; attendance remains staff-managed

## Verification evidence

* `dotnet build GymManagementSolution.slnx --nologo`: `0 Warning(s), 0 Error(s)`
* `dotnet test GymManagementSolution.slnx` with `GYM_TEST_CONNECTION_STRING`: `119 Passed / 0 Failed / 0 Skipped`
* E2E coverage: login redirect to `/Member`, own booking visible, other member's booking hidden, unlinked-account notice and AccessDenied redirect, self-edit with Identity sync, self-book/self-cancel, started-session and missing-membership rejection

## Risks and follow-ups

* **Resolved:** `HomeController` now sends `SuperAdmin` to the dashboard, other staff roles to analytics, and only member-only accounts to `/Member`, so multi-role accounts never land in the portal by accident.
