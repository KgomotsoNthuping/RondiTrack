-- Removes only data created by seed-contribution-volume.sql.

DELETE FROM "Contributions"
WHERE "UserId" IN
(
    SELECT "Id"
    FROM "Users"
    WHERE "Email"
        LIKE 'performance_5_3_%@ronditrack.test'
);

DELETE FROM "StokvelMembers"
WHERE "UserId" IN
(
    SELECT "Id"
    FROM "Users"
    WHERE "Email"
        LIKE 'performance_5_3_%@ronditrack.test'
);

DELETE FROM "Users"
WHERE "Email"
    LIKE 'performance_5_3_%@ronditrack.test';