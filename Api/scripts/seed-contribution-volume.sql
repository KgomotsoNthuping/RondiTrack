-- Creates 100 test users, memberships and contributions.
-- The script uses an existing contribution cycle so that it does not
-- change the normal application startup or create a second test database.

DO $$
DECLARE
    v_stokvel_id uuid;
    v_cycle_id uuid;
BEGIN
    -- Pick an existing contribution cycle and its stokvel.
    SELECT
        "StokvelId",
        "Id"
    INTO
        v_stokvel_id,
        v_cycle_id
    FROM "ContributionCycles"
    ORDER BY "CycleNumber"
    LIMIT 1;

    IF v_cycle_id IS NULL THEN
        RAISE EXCEPTION
            'No contribution cycle exists. Create a stokvel and cycle first.';
    END IF;

    RAISE NOTICE
        'Using stokvel % and cycle %',
        v_stokvel_id,
        v_cycle_id;

    -- Create 100 performance-test users.
    INSERT INTO "Users"
    (
        "Id",
        "FullName",
        "Email",
        "PhoneNumber"
    )
    SELECT
        gen_random_uuid(),
        'Performance Member ' ||
            LPAD(number::text, 3, '0'),
        'performance_5_3_' ||
            LPAD(number::text, 3, '0') ||
            '@ronditrack.test',
        '071' ||
            LPAD(number::text, 7, '0')
    FROM generate_series(1, 100) AS number
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM "Users" existing_user
        WHERE existing_user."Email" =
            'performance_5_3_' ||
            LPAD(number::text, 3, '0') ||
            '@ronditrack.test'
    );

    -- Add the test users as members of the selected stokvel.
    INSERT INTO "StokvelMembers"
    (
        "UserId",
        "StokvelId",
        "Role",
        "JoinedAtUtc"
    )
    SELECT
        user_record."Id",
        v_stokvel_id,
        'Member',
        NOW()
    FROM "Users" user_record
    WHERE user_record."Email"
        LIKE 'performance_5_3_%@ronditrack.test'
    AND NOT EXISTS
    (
        SELECT 1
        FROM "StokvelMembers" existing_member
        WHERE existing_member."UserId" =
            user_record."Id"
        AND existing_member."StokvelId" =
            v_stokvel_id
    );

    -- Give every test member one contribution in the selected cycle.
    INSERT INTO "Contributions"
    (
        "Id",
        "StokvelId",
        "UserId",
        "ContributionCycleId",
        "Amount"
    )
    SELECT
        gen_random_uuid(),
        v_stokvel_id,
        user_record."Id",
        v_cycle_id,
        500 +
        ROW_NUMBER() OVER (
            ORDER BY user_record."Email"
        )
    FROM "Users" user_record
    WHERE user_record."Email"
        LIKE 'performance_5_3_%@ronditrack.test'
    AND NOT EXISTS
    (
        SELECT 1
        FROM "Contributions" existing_contribution
        WHERE existing_contribution."StokvelId" =
            v_stokvel_id
        AND existing_contribution."UserId" =
            user_record."Id"
        AND existing_contribution."ContributionCycleId" =
            v_cycle_id
    );

    RAISE NOTICE
        'Performance seed completed.';
END $$;