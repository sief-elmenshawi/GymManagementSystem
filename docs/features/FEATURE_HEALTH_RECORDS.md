# Feature: Member health records

Status: **Done**

## Purpose

Let staff add or update a member's health record after registration, without collecting medical data on the public form.

## Actors and authorization

| Actor | Access |
| --- | --- |
| `SuperAdmin` | Add/edit/view health records through `MembersController` |
| `Admin` | No access (existing `MembersController` rule) |
| `Member` | No direct access; can only see whether a record exists in the portal |

## Scope

* `GET /Members/HealthRecordCreate/{memberId}` — add/upsert form
* `POST /Members/HealthRecordCreate/{memberId}` — create or update record
* `GET /Members/HealthRecordEdit/{memberId}` — edit form
* `POST /Members/HealthRecordEdit/{memberId}` — update record
* `GET /Members/HealthRecordDetails/{memberId}` — details view with an Edit link and audit trail
* Link from `MemberDetails` to add/edit the record

## Business rules

* Route `id` is the **member** id, not the health-record id.
* Each member has at most one health record (`HealthRecords.MemberId` is unique).
* Saving validates height, weight, blood type, and decimal precision through `HealthRecordViewModel`.
* New records set `CreatedAt`; edits set `UpdatedAt`.
* The service returns `NotFound` when the member does not exist.
* Every create, update, and delete writes a `HealthRecordAudit` snapshot with the acting user and the values at that moment.
* Audit rows keep the actor email even if the account is later deleted; member/actor FK links are cleared explicitly before deletion because SQL Server rejects extra cascade paths.
* The health-record table already existed; migration `20261127201350_AddHealthRecordAudit` adds the audit table.

## Verification evidence

* `dotnet build GymManagementSolution.slnx --nologo`: `0 Warning(s), 0 Error(s)`
* `dotnet test GymManagementSolution.slnx` with `GYM_TEST_CONNECTION_STRING`: `133 Passed / 0 Failed / 4 Skipped / 137 Total` (browser tests opt-in)
* E2E coverage: create, details, edit, invalid blood-type rejection, audit entries for create/update/delete, audit preserved after member deletion
* `dotnet ef migrations list` includes `20261127201350_AddHealthRecordAudit`; `has-pending-model-changes` reports none

## Risks and follow-ups

* **Resolved:** the details view now exposes a queryable audit trail of who changed each health record and what values were stored; snapshots survive member/user deletion.
