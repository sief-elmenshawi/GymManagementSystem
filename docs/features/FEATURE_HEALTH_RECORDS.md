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
* `GET /Members/HealthRecordDetails/{memberId}` — existing details view, now with an Edit link
* Link from `MemberDetails` to add/edit the record

## Business rules

* Route `id` is the **member** id, not the health-record id.
* Each member has at most one health record (`HealthRecords.MemberId` is unique).
* Saving validates height, weight, blood type, and decimal precision through `HealthRecordViewModel`.
* New records set `CreatedAt`; edits set `UpdatedAt`.
* The service returns `NotFound` when the member does not exist.
* No EF migration was needed; the health-record table already existed.

## Verification evidence

* `dotnet build GymManagementSolution.slnx --nologo`: `0 Warning(s), 0 Error(s)`
* `dotnet test GymManagementSolution.slnx` with `GYM_TEST_CONNECTION_STRING`: `107 Passed / 0 Failed / 0 Skipped`
* E2E coverage: create, details, edit, and invalid blood-type rejection

## Risks and follow-ups

* Health data is sensitive; a future hardening step should add an audit trail of who changed it.
