-- =====================================================================
-- Adopt each already-issued OLD letter as version 1 in the new letter
-- version history. ADDITIVE ONLY: inserts one StudentDocumentVersion per
-- qualifying StudentDocument, pointing at the EXACT existing PDF (no
-- re-render). The live StudentDocument row is untouched.
--
-- Qualifies: a non-deleted StudentDocument under one of the 9 built-in
-- letter DocumentTypes, that has a stored PDF and no version rows yet.
-- Idempotent (NOT EXISTS guard) and reversible (Trigger = 'Migrated').
--
-- Run inside a transaction; caller decides ROLLBACK (dry run) or COMMIT.
-- =====================================================================

INSERT INTO "StudentDocumentVersions"
 ("StudentDocumentVersionId","StudentDocumentId","VersionNumber","FileName",
  "StoragePath","Trigger","GeneratedByName","GeneratedByUserId","Language","CreatedAt")
SELECT gen_random_uuid(), d."StudentDocumentId", 1, d."FileName",
       d."StoragePath", 'Migrated', 'Migrated from built-in letter', NULL, NULL,
       d."UploadedAt"   -- preserve the original released date
FROM "StudentDocuments" d
WHERE d."DeletedAt" IS NULL
  AND d."StoragePath" IS NOT NULL
  AND d."DocumentTypeId" IN (
    '22222222-2222-2222-2222-100000000001',  -- Offer Letter
    '22222222-2222-2222-2222-100000000002',  -- Admission Letter
    '22222222-2222-2222-2222-100000000003',  -- Transcript
    '22222222-2222-2222-2222-100000000004',  -- Certificate
    '22222222-2222-2222-2222-100000000005',  -- Provisional Certificate
    '22222222-2222-2222-2222-100000000006',  -- Printable Transcript
    '22222222-2222-2222-2222-100000000007',  -- Student ID Card
    '22222222-2222-2222-2222-100000000009',  -- Final Proposal Approval
    '22222222-2222-2222-2222-100000000010'   -- Final Project Approval
  )
  AND NOT EXISTS (
    SELECT 1 FROM "StudentDocumentVersions" v
    WHERE v."StudentDocumentId" = d."StudentDocumentId"
  );

\echo '--- seeded v1 rows by trigger (Migrated = this run) ---'
SELECT "Trigger", count(*) FROM "StudentDocumentVersions" GROUP BY "Trigger" ORDER BY 1;

\echo '--- coverage: issued old letters vs those now having >=1 version (should match) ---'
SELECT
  count(*) FILTER (WHERE has_pdf) AS issued_old_letters,
  count(*) FILTER (WHERE has_pdf AND has_ver) AS with_version
FROM (
  SELECT d."StudentDocumentId",
         (d."StoragePath" IS NOT NULL) AS has_pdf,
         EXISTS (SELECT 1 FROM "StudentDocumentVersions" v WHERE v."StudentDocumentId"=d."StudentDocumentId") AS has_ver
  FROM "StudentDocuments" d
  WHERE d."DeletedAt" IS NULL
    AND d."DocumentTypeId" IN (
      '22222222-2222-2222-2222-100000000001','22222222-2222-2222-2222-100000000002',
      '22222222-2222-2222-2222-100000000003','22222222-2222-2222-2222-100000000004',
      '22222222-2222-2222-2222-100000000005','22222222-2222-2222-2222-100000000006',
      '22222222-2222-2222-2222-100000000007','22222222-2222-2222-2222-100000000009',
      '22222222-2222-2222-2222-100000000010')
) s;

\echo '--- sanity: every seeded v1 points at its document''s exact current file ---'
SELECT count(*) AS mismatched_file
FROM "StudentDocumentVersions" v
JOIN "StudentDocuments" d ON d."StudentDocumentId" = v."StudentDocumentId"
WHERE v."Trigger" = 'Migrated'
  AND (v."StoragePath" IS DISTINCT FROM d."StoragePath" OR v."FileName" IS DISTINCT FROM d."FileName");

-- REVERSAL (if ever needed):  DELETE FROM "StudentDocumentVersions" WHERE "Trigger" = 'Migrated';
