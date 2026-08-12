-- =====================================================================
-- Phase 2 CUTOVER (data side). Makes the dynamic twins the live letters.
--   A. create twins for any enum templates added since Phase 1 (idempotent)
--   B. un-gate: restore each twin's IsPublished from its enum source
--   C. copy Offer/Admission email templates onto the dynamic definitions
--      so definition-based auto-emails keep working after cutover
-- Additive/idempotent. Run in a transaction; caller ROLLBACK or COMMIT.
-- The code change (ReleaseAsync -> twin) must deploy AFTER this commits,
-- so releases never hit an unpublished twin in the gap.
-- =====================================================================

-- A. Any enum template with no twin yet -> create one (published per source).
INSERT INTO "LetterTemplates"
 ("LetterTemplateId","ProgrammeId","LetterType","BodyHtml","CertificateBackgroundPath",
  "CertificateLayoutJson","UpdatedAt","UpdatedByUserId","DeletedAt","IsPublished","PartnerId",
  "Language","LetterTypeDefinitionId")
SELECT gen_random_uuid(), t."ProgrammeId", t."LetterType", t."BodyHtml", t."CertificateBackgroundPath",
       t."CertificateLayoutJson", now(), t."UpdatedByUserId", NULL, t."IsPublished", t."PartnerId",
       t."Language", m.def_id
FROM "LetterTemplates" t
JOIN (VALUES
  ('OfferLetter','22222222-2222-2222-2222-def000000001'::uuid),
  ('AdmissionLetter','22222222-2222-2222-2222-def000000002'::uuid),
  ('Transcript','22222222-2222-2222-2222-def000000003'::uuid),
  ('Certificate','22222222-2222-2222-2222-def000000004'::uuid),
  ('ProvisionalCertificate','22222222-2222-2222-2222-def000000005'::uuid),
  ('PrintableTranscript','22222222-2222-2222-2222-def000000006'::uuid),
  ('StudentIdCard','22222222-2222-2222-2222-def000000007'::uuid),
  ('FinalProposalApproval','22222222-2222-2222-2222-def000000008'::uuid),
  ('FinalProjectApproval','22222222-2222-2222-2222-def000000009'::uuid)
) AS m(letter_type, def_id) ON m.letter_type = t."LetterType"
WHERE t."DeletedAt" IS NULL AND t."LetterTypeDefinitionId" IS NULL
  AND NOT EXISTS (SELECT 1 FROM "LetterTemplates" x
    WHERE x."ProgrammeId"=t."ProgrammeId" AND x."PartnerId"=t."PartnerId"
      AND x."LetterTypeDefinitionId"=m.def_id
      AND x."Language" IS NOT DISTINCT FROM t."Language" AND x."DeletedAt" IS NULL);

-- B. Un-gate: each twin's publish state = its enum source's.
UPDATE "LetterTemplates" tw
SET "IsPublished" = src."IsPublished", "UpdatedAt" = now()
FROM "LetterTemplates" src,
  (VALUES
    ('22222222-2222-2222-2222-def000000001'::uuid,'OfferLetter'),
    ('22222222-2222-2222-2222-def000000002'::uuid,'AdmissionLetter'),
    ('22222222-2222-2222-2222-def000000003'::uuid,'Transcript'),
    ('22222222-2222-2222-2222-def000000004'::uuid,'Certificate'),
    ('22222222-2222-2222-2222-def000000005'::uuid,'ProvisionalCertificate'),
    ('22222222-2222-2222-2222-def000000006'::uuid,'PrintableTranscript'),
    ('22222222-2222-2222-2222-def000000007'::uuid,'StudentIdCard'),
    ('22222222-2222-2222-2222-def000000008'::uuid,'FinalProposalApproval'),
    ('22222222-2222-2222-2222-def000000009'::uuid,'FinalProjectApproval')
  ) AS m(def_id, letter_type)
WHERE tw."LetterTypeDefinitionId" = m.def_id
  AND src."LetterType" = m.letter_type
  AND src."DeletedAt" IS NULL AND src."LetterTypeDefinitionId" IS NULL
  AND src."ProgrammeId"=tw."ProgrammeId" AND src."PartnerId"=tw."PartnerId"
  AND src."Language" IS NOT DISTINCT FROM tw."Language"
  AND tw."DeletedAt" IS NULL AND tw."IsPublished" IS DISTINCT FROM src."IsPublished";

-- C. Copy Offer/Admission email templates onto the dynamic definitions.
INSERT INTO "LetterEmailTemplates"
 ("LetterEmailTemplateId","ProgrammeId","LetterType","IsEmailEnabled","Subject","BodyHtml",
  "CcRecipientsJson","BccRecipientsJson","UpdatedAt","UpdatedByUserId","DeletedAt","PartnerId","LetterTypeDefinitionId")
SELECT gen_random_uuid(), e."ProgrammeId", e."LetterType", e."IsEmailEnabled", e."Subject", e."BodyHtml",
       e."CcRecipientsJson", e."BccRecipientsJson", now(), e."UpdatedByUserId", NULL, e."PartnerId", m.def_id
FROM "LetterEmailTemplates" e
JOIN (VALUES
  ('OfferLetter','22222222-2222-2222-2222-def000000001'::uuid),
  ('AdmissionLetter','22222222-2222-2222-2222-def000000002'::uuid)
) AS m(letter_type, def_id) ON m.letter_type = e."LetterType"
WHERE e."DeletedAt" IS NULL AND e."LetterTypeDefinitionId" IS NULL
  AND NOT EXISTS (SELECT 1 FROM "LetterEmailTemplates" x
    WHERE x."ProgrammeId"=e."ProgrammeId" AND x."PartnerId"=e."PartnerId"
      AND x."LetterTypeDefinitionId"=m.def_id AND x."DeletedAt" IS NULL);

\echo '--- twins published vs enum source published (must match per type) ---'
SELECT m.letter_type,
  (SELECT count(*) FROM "LetterTemplates" s WHERE s."DeletedAt" IS NULL AND s."LetterTypeDefinitionId" IS NULL AND s."LetterType"=m.letter_type AND s."IsPublished") AS enum_pub,
  (SELECT count(*) FROM "LetterTemplates" tw WHERE tw."DeletedAt" IS NULL AND tw."LetterTypeDefinitionId"=m.def_id AND tw."IsPublished") AS twin_pub
FROM (VALUES
  ('22222222-2222-2222-2222-def000000001'::uuid,'OfferLetter'),
  ('22222222-2222-2222-2222-def000000002'::uuid,'AdmissionLetter'),
  ('22222222-2222-2222-2222-def000000003'::uuid,'Transcript'),
  ('22222222-2222-2222-2222-def000000004'::uuid,'Certificate'),
  ('22222222-2222-2222-2222-def000000005'::uuid,'ProvisionalCertificate'),
  ('22222222-2222-2222-2222-def000000006'::uuid,'PrintableTranscript'),
  ('22222222-2222-2222-2222-def000000007'::uuid,'StudentIdCard'),
  ('22222222-2222-2222-2222-def000000008'::uuid,'FinalProposalApproval'),
  ('22222222-2222-2222-2222-def000000009'::uuid,'FinalProjectApproval')
) AS m(def_id, letter_type) ORDER BY m.letter_type;

\echo '--- email templates: enum offer/admission vs dynamic twins (must match) ---'
SELECT
 (SELECT count(*) FROM "LetterEmailTemplates" WHERE "DeletedAt" IS NULL AND "LetterTypeDefinitionId" IS NULL AND "LetterType" IN ('OfferLetter','AdmissionLetter')) AS enum_email,
 (SELECT count(*) FROM "LetterEmailTemplates" WHERE "DeletedAt" IS NULL AND "LetterTypeDefinitionId" IN ('22222222-2222-2222-2222-def000000001','22222222-2222-2222-2222-def000000002')) AS twin_email;

\echo '--- content still exact: twins differing from enum source (must be 0) ---'
SELECT count(*) AS mismatched FROM "LetterTemplates" tw
JOIN (VALUES
  ('22222222-2222-2222-2222-def000000001'::uuid,'OfferLetter'),('22222222-2222-2222-2222-def000000002'::uuid,'AdmissionLetter'),
  ('22222222-2222-2222-2222-def000000003'::uuid,'Transcript'),('22222222-2222-2222-2222-def000000004'::uuid,'Certificate'),
  ('22222222-2222-2222-2222-def000000005'::uuid,'ProvisionalCertificate'),('22222222-2222-2222-2222-def000000006'::uuid,'PrintableTranscript'),
  ('22222222-2222-2222-2222-def000000007'::uuid,'StudentIdCard'),('22222222-2222-2222-2222-def000000008'::uuid,'FinalProposalApproval'),
  ('22222222-2222-2222-2222-def000000009'::uuid,'FinalProjectApproval')
) AS m(def_id, letter_type) ON m.def_id = tw."LetterTypeDefinitionId"
WHERE tw."DeletedAt" IS NULL AND NOT EXISTS (
  SELECT 1 FROM "LetterTemplates" src WHERE src."DeletedAt" IS NULL AND src."LetterTypeDefinitionId" IS NULL
    AND src."ProgrammeId"=tw."ProgrammeId" AND src."PartnerId"=tw."PartnerId" AND src."LetterType"=m.letter_type
    AND src."Language" IS NOT DISTINCT FROM tw."Language"
    AND src."BodyHtml" IS NOT DISTINCT FROM tw."BodyHtml"
    AND src."CertificateBackgroundPath" IS NOT DISTINCT FROM tw."CertificateBackgroundPath"
    AND src."CertificateLayoutJson" IS NOT DISTINCT FROM tw."CertificateLayoutJson");

-- REVERSAL of the un-gate (re-gate twins):
--   UPDATE "LetterTemplates" SET "IsPublished"=false WHERE "LetterTypeDefinitionId"::text LIKE '22222222-2222-2222-2222-def%';
