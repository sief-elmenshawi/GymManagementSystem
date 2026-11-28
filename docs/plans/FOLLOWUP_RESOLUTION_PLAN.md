# Follow-up resolution plan

Status: **Done**

## Outcome

Resolve every `Risks and follow-ups` item documented under `docs/features/*.md` without changing the already-agreed product decisions.

## Scope

Included:

- Resend-email-confirmation endpoint and safer multi-role home redirect.
- Atomic registration and atomic SuperAdmin user edit.
- Queryable audit trail for health-record changes.
- Member self-service profile edit and self-service booking/cancel.
- Documentation updates that close or explicitly accept the remaining operational notes.

Non-goals:

- README `Future improvements` (REST API, payments, SignalR).
- Changing `RequireConfirmedAccount = true`, seeded-user auto-confirmation, or SMTP log-and-skip behavior.
- Adding a new rate-limiting/notification infrastructure.

## Constraints and assumptions

- Identity and gym data share one scoped `GymDbContext`; one explicit EF transaction can cover both.
- No retrying execution strategy is configured, so `BeginTransactionAsync` is valid. If one is added later, wrap transactions in `CreateExecutionStrategy().ExecuteAsync(...)`.
- Billing/email delivery cannot be part of a DB transaction; SMTP failures remain logged, as already decided.
- The dev mailbox is intentionally in-memory and Development-only.
- Migrations through `20261128102032_BookingUniqueIndex` must be applied to the development database before final EF verification.

## Tasks

| ID | Action | Expected files/components | Depends on | Acceptance criteria | Verification | Status |
| --- | --- | --- | --- | --- | --- | --- |
| F1 | Add resend-confirmation flow and fix multi-role redirect | `AccountController`, new VM/views, `HomeController`, `_Layout`, login/confirm views, tests | — | Anonymous POST resends only for existing unconfirmed account with generic response; `Member`+other role no longer lands in `/Member`; `SuperAdmin` still wins | `dotnet test` filtered to account/home + focused build | Done |
| F2 | Make registration atomic | `AccountController.Register` | — | User, role, and member commit together; role/member failure leaves no orphan user | E2E role-failure test asserts zero users/members; duplicate-email regression | Done |
| F3 | Make SuperAdmin user edit atomic | `UsersController.Edit` | — | Invalid password/role step rolls back profile, password, and role changes | E2E invalid-password test asserts unchanged profile | Done |
| F4 | Add health-record audit trail | `HealthRecordAudit` entity/config/DbSet, migration, `MemberService`, `MembersController`, details view, tests | — | Created/Updated/Deleted actions recorded with actor and snapshot; migration applied | Focused tests + `dotnet ef migrations list` | Done |
| F5 | Add member self-profile edit | `MemberPortalController`, `EditProfile.cshtml`, `IMemberService`, layout, tests | F1 | Members can edit only their own linked profile; identity email/phone stay in sync | E2E self-edit + unlinked-account denial | Done |
| F6 | Add member self-booking/cancel | `MemberPortalController`, portal VMs/views, `IBookingService` reuse, tests | F5 | Members book/cancel only their own bookings; existing business rules still enforced | E2E self-book/self-cancel + started-session and membership denials | Done |
| F7 | Close docs, full verification, commit and push | feature docs, plan, README if needed | F1–F6 | All risk notes resolved or explicitly accepted; clean status; master pushed | Build, tests with `GYM_TEST_CONNECTION_STRING`, vulnerability check, EF check, `git status` | Done |

## Checkpoints

1. After F1–F3: build and focused tests before touching the portal.
2. After F4: review generated migration and apply it to the development DB.
3. After F6: full suite, then commit/push.

## Evidence log

- F1/F2: `dotnet build GymManagementSolution.slnx --nologo` -> `0 Warning(s), 0 Error(s)`; focused tests with `GYM_TEST_CONNECTION_STRING` -> `Passed: 11, Failed: 0, Skipped: 0`.
- F3: `dotnet test ... --filter FullyQualifiedName~UserManagementFlowTests` -> `Passed: 5, Failed: 0, Skipped: 0` (final full-suite rerun still pending at F7).
- F4: `dotnet test ... --filter "FullyQualifiedName~HealthRecordManagementFlowTests|FullyQualifiedName~MemberPhotoTests"` -> `Passed: 5, Failed: 0, Skipped: 0`; `dotnet ef database update` applied `20261127201350_AddHealthRecordAudit`.
- F5/F6: `dotnet test ... --filter "FullyQualifiedName~MemberSelfServiceFlowTests|FullyQualifiedName~MemberPortalFlowTests"` -> `Passed: 6, Failed: 0, Skipped: 0`.
- F7 verification: `dotnet build GymManagementSolution.slnx --nologo` -> `0 Warning(s), 0 Error(s)`; full suite with `GYM_TEST_CONNECTION_STRING` -> `Passed: 133, Failed: 0, Skipped: 4`; with `GYM_RUN_BROWSER_TESTS=1` -> `Passed: 137, Failed: 0, Skipped: 0`; `dotnet list ... package --vulnerable --include-transitive` -> no vulnerable packages; `dotnet ef migrations list` is current through `20261128102032_BookingUniqueIndex`; `dotnet ef migrations has-pending-model-changes` -> no changes.
