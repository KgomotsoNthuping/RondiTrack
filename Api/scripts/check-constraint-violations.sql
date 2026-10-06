-- One contribution per member per cycle.

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


-- One payout per stokvel contribution cycle.

SELECT
    "StokvelId",
    "ContributionCycleId",
    COUNT(*) AS "DuplicateCount"
FROM "Payouts"
GROUP BY
    "StokvelId",
    "ContributionCycleId"
HAVING COUNT(*) > 1;