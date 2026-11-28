-- Run this BEFORE applying migration 20261128102032_BookingUniqueIndex to an
-- existing database. The migration creates UX_Bookings_MemberId_SessionId and
-- will fail if duplicate (MemberId, SessionId) rows already exist.
--
-- Take a verified backup before running any DELETE below.

-- 1. Detection: any returned row blocks the migration.
SELECT
    MemberId,
    SessionId,
    COUNT(*) AS DuplicateCount
FROM dbo.Bookings
GROUP BY MemberId, SessionId
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC;

-- 2. Optional remediation (review the detection output first, then uncomment).
-- Keeps the lowest Id per (MemberId, SessionId) and removes the rest.
--
-- BEGIN TRAN;
--
-- DELETE d
-- FROM dbo.Bookings AS d
-- INNER JOIN (
--     SELECT MemberId, SessionId, MIN(Id) AS KeepId
--     FROM dbo.Bookings
--     GROUP BY MemberId, SessionId
--     HAVING COUNT(*) > 1
-- ) AS k
--     ON d.MemberId = k.MemberId
--    AND d.SessionId = k.SessionId
--    AND d.Id <> k.KeepId;
--
-- -- Re-run step 1 here; it must return zero rows before you COMMIT.
-- COMMIT;
