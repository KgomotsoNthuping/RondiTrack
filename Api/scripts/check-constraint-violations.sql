-- =====================================================
-- Contribution uniqueness
-- One member should contribute only once per cycle.
-- =====================================================

SELECT
    "StokvelId",
    "UserId",
    "ContributionCycleId",
    COUNT(*) AS "DuplicateCount"
FROM "Contributions"
GROUP BY
    "StokvelId",
    "UserId",
    "ContributionCycleId"
HAVING COUNT(*) > 1;


-- =====================================================
-- Contribution-cycle uniqueness
-- A stokvel should not have the same cycle number twice.
-- =====================================================

SELECT
    "StokvelId",
    "CycleNumber",
    COUNT(*) AS "DuplicateCount"
FROM "ContributionCycles"
GROUP BY
    "StokvelId",
    "CycleNumber"
HAVING COUNT(*) > 1;


-- =====================================================
-- Payout uniqueness
-- A contribution cycle should not be paid out twice.
-- =====================================================

SELECT
    "ContributionCycleId",
    COUNT(*) AS "DuplicateCount"
FROM "Payouts"
GROUP BY
    "ContributionCycleId"
HAVING COUNT(*) > 1;


-- =====================================================
-- Membership check
-- This is already protected by the composite primary key
-- from the previous relationship work.
-- =====================================================

SELECT
    "UserId",
    "StokvelId",
    COUNT(*) AS "DuplicateCount"
FROM "StokvelMembers"
GROUP BY
    "UserId",
    "StokvelId"
HAVING COUNT(*) > 1;