-- =====================================================
-- Contribution paging query plan
-- =====================================================
--
-- 1. Run the paged contributions endpoint.
--
-- Example:
--
-- GET
-- /api/stokvels/{stokvelId}/cycles/{cycleId}/contributions
--     ?pageSize=20
--     &sort=userId
--
-- 2. Copy the actual contribution SELECT from the
--    EF Core Database.Command log.
--
-- 3. Paste the SQL directly underneath:
--
--    EXPLAIN (ANALYZE, BUFFERS)
--
-- Do not manually rewrite the EF query.
--
-- Keep the BEFORE INDEX plan/result in the README.
--
-- After the EF Core index migration is applied,
-- run exactly the same query again and record the
-- AFTER INDEX plan.
--
-- Record:
--
-- Scan type
-- Execution Time
-- Estimated rows
-- Actual rows
-- Rows Removed by Filter
-- Sort node, if present
--
-- =====================================================


EXPLAIN (ANALYZE, BUFFERS)

-- PASTE THE ACTUAL SQL FROM THE EF CORE LOG HERE
;