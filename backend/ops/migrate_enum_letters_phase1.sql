-- =====================================================================
-- Phase 1: copy every built-in (enum) letter template into an EXACT
-- dynamic twin. ADDITIVE ONLY: no existing row is updated or deleted.
-- Idempotent (safe to re-run) and reversible (twins are the rows whose
-- LetterTypeDefinitionId is one of the 9 fixed def ids below).
--
-- Run inside a transaction; the caller decides ROLLBACK (dry run) or
-- COMMIT (execute). See migrate_enum_letters_run.sh.
-- =====================================================================

-- 1) The 9 letter-type definitions, one per enum LetterType. Each points
--    at the SAME existing system DocumentType and carries the SAME
--    reference prefix, so generated letters keep the same category and
--    reference code (MGW-OL-..., MGW-TR-..., ...). Idempotent on id.
INSERT INTO "LetterTypeDefinitions"
 ("LetterTypeDefinitionId","Name","ReferencePrefix","DocumentTypeId","TriggerStatusId",
  "VisibleToStudent","VisibleToPartner","EmailOnRelease","AllowLegacyUpload","SortOrder","CreatedAt","DeletedAt")
SELECT v.id, v.name, v.prefix, v.doc, NULL, v.viss, v.visp, v.email, false, v.sort, now(), NULL
FROM (VALUES
  ('22222222-2222-2222-2222-def000000001'::uuid,'Offer Letter',          'OL',     '22222222-2222-2222-2222-100000000001'::uuid, true,  true,  true,  1),
  ('22222222-2222-2222-2222-def000000002'::uuid,'Admission Letter',      'AL',     '22222222-2222-2222-2222-100000000002'::uuid, true,  true,  true,  2),
  ('22222222-2222-2222-2222-def000000003'::uuid,'Digital Transcript',    'TR',     '22222222-2222-2222-2222-100000000003'::uuid, true,  true,  false, 3),
  ('22222222-2222-2222-2222-def000000004'::uuid,'Digital Certificate',   'CERT',   '22222222-2222-2222-2222-100000000004'::uuid, true,  true,  false, 4),
  ('22222222-2222-2222-2222-def000000005'::uuid,'Printable Certificate', 'PCERT',  '22222222-2222-2222-2222-100000000005'::uuid, true,  true,  false, 5),
  ('22222222-2222-2222-2222-def000000006'::uuid,'Printable Transcript',  'PTR',    '22222222-2222-2222-2222-100000000006'::uuid, false, false, false, 6),
  ('22222222-2222-2222-2222-def000000007'::uuid,'Student ID Card',       'IDCARD', '22222222-2222-2222-2222-100000000007'::uuid, true,  true,  false, 7),
  ('22222222-2222-2222-2222-def000000008'::uuid,'Proposal Approval',     'PROPAPP','22222222-2222-2222-2222-100000000009'::uuid, true,  true,  false, 8),
  ('22222222-2222-2222-2222-def000000009'::uuid,'Project Approval',      'PROJAPP','22222222-2222-2222-2222-100000000010'::uuid, true,  true,  false, 9)
) AS v(id,name,prefix,doc,viss,visp,email,sort)
WHERE NOT EXISTS (SELECT 1 FROM "LetterTypeDefinitions" d WHERE d."LetterTypeDefinitionId" = v.id);

-- 2) Copy every enum template into a dynamic twin. Exact content copy:
--    same ProgrammeId, PartnerId, Language, BodyHtml, background, layout
--    JSON and IsPublished. New row id; LetterTypeDefinitionId set to the
--    mapped definition; the enum LetterType column is preserved on the
--    twin purely as an origin marker (the enum unique index ignores rows
--    whose LetterTypeDefinitionId is NOT NULL). Idempotent: skips an enum
--    row that already has a twin for (programme, partner, definition, language).
INSERT INTO "LetterTemplates"
 ("LetterTemplateId","ProgrammeId","LetterType","BodyHtml","CertificateBackgroundPath",
  "CertificateLayoutJson","UpdatedAt","UpdatedByUserId","DeletedAt","IsPublished","PartnerId",
  "Language","LetterTypeDefinitionId")
SELECT gen_random_uuid(), t."ProgrammeId", t."LetterType", t."BodyHtml", t."CertificateBackgroundPath",
       t."CertificateLayoutJson", now(), t."UpdatedByUserId", NULL, t."IsPublished", t."PartnerId",
       t."Language", m.def_id
FROM "LetterTemplates" t
JOIN (VALUES
  ('OfferLetter',            '22222222-2222-2222-2222-def000000001'::uuid),
  ('AdmissionLetter',        '22222222-2222-2222-2222-def000000002'::uuid),
  ('Transcript',             '22222222-2222-2222-2222-def000000003'::uuid),
  ('Certificate',            '22222222-2222-2222-2222-def000000004'::uuid),
  ('ProvisionalCertificate', '22222222-2222-2222-2222-def000000005'::uuid),
  ('PrintableTranscript',    '22222222-2222-2222-2222-def000000006'::uuid),
  ('StudentIdCard',          '22222222-2222-2222-2222-def000000007'::uuid),
  ('FinalProposalApproval',  '22222222-2222-2222-2222-def000000008'::uuid),
  ('FinalProjectApproval',   '22222222-2222-2222-2222-def000000009'::uuid)
) AS m(letter_type, def_id) ON m.letter_type = t."LetterType"
WHERE t."DeletedAt" IS NULL
  AND t."LetterTypeDefinitionId" IS NULL
  AND NOT EXISTS (
    SELECT 1 FROM "LetterTemplates" x
    WHERE x."ProgrammeId" = t."ProgrammeId"
      AND x."PartnerId"   = t."PartnerId"
      AND x."LetterTypeDefinitionId" = m.def_id
      AND x."Language" IS NOT DISTINCT FROM t."Language"
      AND x."DeletedAt" IS NULL
  );

-- 3) Verification (runs whether we ROLLBACK or COMMIT).
\echo '--- definitions now (should include the 9 fixed ids) ---'
SELECT "ReferencePrefix", "Name", "DocumentTypeId"
FROM "LetterTypeDefinitions"
WHERE "LetterTypeDefinitionId" IN (
  '22222222-2222-2222-2222-def000000001','22222222-2222-2222-2222-def000000002',
  '22222222-2222-2222-2222-def000000003','22222222-2222-2222-2222-def000000004',
  '22222222-2222-2222-2222-def000000005','22222222-2222-2222-2222-def000000006',
  '22222222-2222-2222-2222-def000000007','22222222-2222-2222-2222-def000000008',
  '22222222-2222-2222-2222-def000000009')
ORDER BY "SortOrder";

\echo '--- per type: enum originals vs dynamic twins (must match) ---'
SELECT src."LetterType",
       src.enum_count,
       COALESCE(tw.twin_count,0) AS twin_count,
       CASE WHEN src.enum_count = COALESCE(tw.twin_count,0) THEN 'OK' ELSE 'MISMATCH' END AS status
FROM (
  SELECT "LetterType", count(*) enum_count
  FROM "LetterTemplates"
  WHERE "DeletedAt" IS NULL AND "LetterTypeDefinitionId" IS NULL
    AND "LetterType" IN ('OfferLetter','AdmissionLetter','Transcript','Certificate',
      'ProvisionalCertificate','PrintableTranscript','StudentIdCard','FinalProposalApproval','FinalProjectApproval')
  GROUP BY "LetterType"
) src
LEFT JOIN (
  SELECT t."LetterType", count(*) twin_count
  FROM "LetterTemplates" t
  WHERE t."DeletedAt" IS NULL AND t."LetterTypeDefinitionId" IS NOT NULL
    AND t."LetterTypeDefinitionId" IN (
      '22222222-2222-2222-2222-def000000001','22222222-2222-2222-2222-def000000002',
      '22222222-2222-2222-2222-def000000003','22222222-2222-2222-2222-def000000004',
      '22222222-2222-2222-2222-def000000005','22222222-2222-2222-2222-def000000006',
      '22222222-2222-2222-2222-def000000007','22222222-2222-2222-2222-def000000008',
      '22222222-2222-2222-2222-def000000009')
  GROUP BY t."LetterType"
) tw ON tw."LetterType" = src."LetterType"
ORDER BY src."LetterType";

\echo '--- content exactness: twins whose content differs from their enum source (must be 0) ---'
SELECT count(*) AS mismatched_content
FROM "LetterTemplates" tw
JOIN (VALUES
  ('22222222-2222-2222-2222-def000000001'::uuid,'OfferLetter'),
  ('22222222-2222-2222-2222-def000000002'::uuid,'AdmissionLetter'),
  ('22222222-2222-2222-2222-def000000003'::uuid,'Transcript'),
  ('22222222-2222-2222-2222-def000000004'::uuid,'Certificate'),
  ('22222222-2222-2222-2222-def000000005'::uuid,'ProvisionalCertificate'),
  ('22222222-2222-2222-2222-def000000006'::uuid,'PrintableTranscript'),
  ('22222222-2222-2222-2222-def000000007'::uuid,'StudentIdCard'),
  ('22222222-2222-2222-2222-def000000008'::uuid,'FinalProposalApproval'),
  ('22222222-2222-2222-2222-def000000009'::uuid,'FinalProjectApproval')
) AS m(def_id, letter_type) ON m.def_id = tw."LetterTypeDefinitionId"
WHERE tw."DeletedAt" IS NULL
  AND NOT EXISTS (
    SELECT 1 FROM "LetterTemplates" src
    WHERE src."DeletedAt" IS NULL AND src."LetterTypeDefinitionId" IS NULL
      AND src."ProgrammeId" = tw."ProgrammeId"
      AND src."PartnerId"   = tw."PartnerId"
      AND src."LetterType"  = m.letter_type
      AND src."Language" IS NOT DISTINCT FROM tw."Language"
      AND src."BodyHtml" IS NOT DISTINCT FROM tw."BodyHtml"
      AND src."CertificateBackgroundPath" IS NOT DISTINCT FROM tw."CertificateBackgroundPath"
      AND src."CertificateLayoutJson" IS NOT DISTINCT FROM tw."CertificateLayoutJson"
      AND src."IsPublished" = tw."IsPublished"
  );

-- =====================================================================
-- INTERIM SAFETY GATE (applied after commit, auto-commit statement)
-- The twins share the enum letters' DocumentType and the dynamic renderer
-- is not yet fixed, so a manual dynamic Generate could overwrite a real
-- letter with a blank. Unpublish the twins so ReleaseDynamicAsync (which
-- requires IsPublished) can never fire until Phase 2 cutover.
--
--   UPDATE "LetterTemplates" SET "IsPublished" = false, "UpdatedAt" = now()
--   WHERE "DeletedAt" IS NULL
--     AND "LetterTypeDefinitionId"::text LIKE '22222222-2222-2222-2222-def%'
--     AND "IsPublished" = true;
--
-- PHASE 2 CUTOVER — restore each twin's publish state from its enum source
-- (deterministic; enum originals are left untouched by Phase 1):
--
--   UPDATE "LetterTemplates" tw
--   SET "IsPublished" = src."IsPublished"
--   FROM "LetterTemplates" src
--   JOIN (VALUES
--     ('22222222-2222-2222-2222-def000000001'::uuid,'OfferLetter'),
--     ('22222222-2222-2222-2222-def000000002'::uuid,'AdmissionLetter'),
--     ('22222222-2222-2222-2222-def000000003'::uuid,'Transcript'),
--     ('22222222-2222-2222-2222-def000000004'::uuid,'Certificate'),
--     ('22222222-2222-2222-2222-def000000005'::uuid,'ProvisionalCertificate'),
--     ('22222222-2222-2222-2222-def000000006'::uuid,'PrintableTranscript'),
--     ('22222222-2222-2222-2222-def000000007'::uuid,'StudentIdCard'),
--     ('22222222-2222-2222-2222-def000000008'::uuid,'FinalProposalApproval'),
--     ('22222222-2222-2222-2222-def000000009'::uuid,'FinalProjectApproval')
--   ) AS m(def_id, letter_type) ON m.def_id = tw."LetterTypeDefinitionId"
--   WHERE src."DeletedAt" IS NULL AND src."LetterTypeDefinitionId" IS NULL
--     AND src."ProgrammeId" = tw."ProgrammeId" AND src."PartnerId" = tw."PartnerId"
--     AND src."LetterType" = m.letter_type
--     AND src."Language" IS NOT DISTINCT FROM tw."Language"
--     AND tw."DeletedAt" IS NULL;
--
-- FULL REVERSAL of Phase 1 (if ever needed) — removes twins then definitions:
--   DELETE FROM "LetterTemplates"       WHERE "LetterTypeDefinitionId"::text LIKE '22222222-2222-2222-2222-def%';
--   DELETE FROM "LetterTypeDefinitions" WHERE "LetterTypeDefinitionId"::text LIKE '22222222-2222-2222-2222-def%';
-- =====================================================================
