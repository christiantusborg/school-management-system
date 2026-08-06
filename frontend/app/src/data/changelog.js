// In-app changelog shown on the SuperAdmin "Changelog" tab.
// STANDING RULES: prepend a new entry here with EVERY deploy, and keep ONE
// function per entry — only closely related changes share an entry.
// Newest first. { id, date, title, summary, details } — details is an array
// of plain-text bullet lines.
export const CHANGELOG = [
  {
    id: 'final-project-upload-fields',
    date: '2026-08-06',
    title: 'Final project details: Project name, Supervisor, Word count',
    summary: 'Per-student project details on final-project assignment uploads, printable in the approval letters.',
    details: [
      'WHERE: Module Cohorts → open a "Final Project / Dissertation" cohort → Uploaded Assignments tab → open the student\'s fold-out. The three fields sit on each uploaded file and on the upload form.',
      'HOW: fill in Final project name, Supervisor and Word count when uploading the student\'s project file, or on an existing upload and press "Save details". Admission, partner staff and teachers can edit; the student sees the values read-only.',
      'USE IN LETTERS: the letter designer (Partners → Programmes → Letters row) offers [supervisor] and [word count] tags; [project title] prints the upload\'s Final project name and falls back to the grade-modal Project Title.',
      'The Proposal Approval letter reads uploads in the student\'s Dissertation Proposal cohort first, the Project Approval letter the Final Project cohort; missing values fall back to the other cohort, newest upload first.',
      'After changing values, use the letter\'s Generate button on the student drawer\'s Letters tab to re-render the PDF.',
    ],
  },
  {
    id: 'approval-letters-any-admin',
    date: '2026-08-06',
    title: 'Approval letters: Generate open to all Admission staff',
    summary: 'Every non-read-only admin level can now generate the Proposal/Project Approval letters.',
    details: [
      'The Generate button for the "Proposal Approval Letter" and "Project Approval Letter" is now visible and usable for Manager and Editor levels too, not only Administrator and SuperAdministrator.',
      'Viewer and Sales stay read-only. All other letters keep the Administrator+ restriction.',
    ],
  },
  {
    id: 'drawer-header-partner',
    date: '2026-08-06',
    title: 'Drawer header no longer shows the signup partner',
    summary: 'The student drawer header shows only the email; partners are on the programme cards.',
    details: [
      'The admin student drawer header used to append the partner the student ORIGINALLY signed up with, which is misleading for multi-partner students.',
      'It now shows only the email; the correct partner for each application stays on the 🤝 chip of every programme card.',
    ],
  },
  {
    id: 'approval-letters',
    date: '2026-08-06',
    title: 'Final project approval letters',
    summary: 'Proposal/Project approval letters auto-release when the cohort mark reaches the pass mark.',
    details: [
      'Two new letters on the Letters tab: "Proposal Approval Letter" and "Project Approval Letter", each with its own designer template per programme + partner and manual Generate / Send buttons.',
      'They release AUTOMATICALLY when a mark saved in a "Dissertation Proposal" (proposal letter) or "Final Project / Dissertation" (project letter) cohort reaches the programme pass mark.',
      'The pass mark is configurable per programme on the Letters tab (🎯 input), default 40.',
      'Already-released letters are never duplicated, and a missing template never blocks a grade save.',
    ],
  },
  {
    id: 'spec-code-edit',
    date: '2026-08-06',
    title: 'Admission can edit specialization codes',
    summary: 'Specialization codes on custom programmes are editable from the Programmes panel.',
    details: [
      'A ✎ button next to each specialization code in the partner Programmes panel lets the Admission Office correct the code on custom programmes — whether created by the partner or by admission.',
    ],
  },
  {
    id: 'partner-prog-duration',
    date: '2026-08-06',
    title: 'Duration of study on partner programme editor',
    summary: 'My Programs edit form gains the min–max duration months fields.',
    details: [
      'The partner portal programme editor was missing the Duration of study range: min and max months are now editable next to ECTS (and shown read-only on locked programmes).',
      'The range is what approved per-student durations are validated against.',
    ],
  },
  {
    id: 'mail-lead-history',
    date: '2026-08-05',
    title: 'Full mail history on leads and converted students',
    summary: 'Leads inherit the whole email archive with their address; conversion carries it to the student.',
    details: [
      'Creating a lead (from the CRM, from a mail, or by setting its email) links the ENTIRE archived mail history with that address — incoming and outgoing — not just future mail.',
      'When a lead converts to a student, the student inherits the lead’s complete mail history automatically, keeping the log unbroken from first contact to enrolment.',
    ],
  },
  {
    id: 'sales-mail-access',
    date: '2026-08-05',
    title: 'Mail for Sales users',
    summary: 'Sales get the Mail tab, scoped to the accounts they are granted.',
    details: [
      'Sales users now see the Mail tab, limited to the mail accounts a SuperAdministrator grants them (e.g. their own sales address) — with the same webmail, linking and reply features.',
      'Sales can also open the Mail tab on a partner’s manage page to view linked mail and send new mail from their granted account.',
    ],
  },
  {
    id: 'mail-entity-panels',
    date: '2026-08-05',
    title: 'Mail on student & partner pages + named links',
    summary: 'Admission Mail tab on the student drawer and partner view; mail chips show names and jump to the record.',
    details: [
      'The admin student drawer and the partner manage view each gained a Mail tab: the full email log linked to that student/partner (incoming and outgoing, with account colour labels) plus a compose box to send a new mail directly from there — the To address is prefilled.',
      'Webmail link chips now show the actual student name, lead name or partner name instead of a generic label, and clicking a chip jumps straight to that student drawer, CRM lead or partner view.',
      'Reminder: every synced email is stored permanently in the hub database (bodies + attachments up to 10 MB), so the per-student and per-partner logs survive even if mail is deleted from the mailbox.',
    ],
  },
  {
    id: 'student-ids',
    date: '2026-08-05',
    title: 'Multiple Student IDs',
    summary: 'Students carry several IDs (one primary + aliases); CSV import matches by email only.',
    details: [
      'Students now hold multiple Student IDs: one primary (printed on lists and letters) plus any number of aliases with optional labels. Admission manages them from the student drawer (add, edit, make primary, remove); partners and students see the list read-only.',
      'Student IDs are globally unique — no two students can share an ID.',
      'CSV import matches rows by EMAIL ONLY. Existing students get only NEW programmes (programmes they already have are skipped) enrolled under the importing partner, and unknown Student IDs on their rows are attached as aliases labelled "CSV import". Rows whose ID belongs to a different student are rejected.',
      'The students overview search also finds alias IDs, and the export gains an "All student IDs" column.',
    ],
  },
  {
    id: 'changelog-tab',
    date: '2026-08-05',
    title: 'This changelog',
    summary: 'SuperAdmin changelog tab, updated with every deploy.',
    details: [
      'New Changelog tab (SuperAdministrator only): vertical entry list, newest first, each with a short title and summary; click for the detailed change list.',
      'Every future deploy adds its entry here before shipping.',
    ],
  },
  {
    id: 'admin-addprog-partner-first',
    date: '2026-08-05',
    title: 'Admin “+ Add programme” picks the school first',
    summary: 'Programme list limited to the chosen partner’s real access.',
    details: [
      'The admin student drawer’s “+ Add programme” now starts with a School/partner selector (defaulting to the student’s current partner).',
      'The programme list shows only that partner’s granted core specializations and approved custom programmes.',
      'The created enrolment belongs to the chosen partner and appears in that partner’s portal.',
    ],
  },
  {
    id: 'multi-partner-students',
    date: '2026-08-05',
    title: 'Multi-partner students + existing-email signup',
    summary: 'Signing an existing email attaches a new application instead of failing.',
    details: [
      'All signup wizards (partner, admission, public link, CRM convert) recognise an existing student by email: staff see “Student found” and continue straight to programme selection; public signups verify a 6-digit code emailed to the address first.',
      'The new application enrols under the signing partner and enters the normal review pipeline; a same-partner + same-programme duplicate is blocked.',
      'Same partner re-applications carry their earlier documents over with verified status kept; a different partner uploads fresh documents.',
      'Partners see only their own enrolments, documents, payments and reviews on shared students; Admission sees everything with partner + programme labels on every file.',
    ],
  },
  {
    id: 'mail-hub',
    date: '2026-08-05',
    title: 'Webmail hub',
    summary: 'Multi-account IMAP webmail with auto-linking and SMTP replies.',
    details: [
      'New Mail tab (admission): sync any number of IMAP mailboxes (all folders, 90 days back, then continuous + auto-sync every 5 minutes) with a clear colour label per account.',
      'Every mail links automatically to matching students, CRM leads and partners; unknown senders get a “Create lead” button.',
      'Reply / reply-all / new mail sends through the originating account’s real SMTP server and is archived.',
      'SuperAdmin gear config adds accounts and grants per-user mailbox access. Partners and students get read-only Mail views of messages linked to them; lead emails show in the CRM drawer.',
    ],
  },
  {
    id: 'crm-launch',
    date: '2026-08-05',
    title: 'Sales CRM',
    summary: 'Lead kanban with scoring, follow-ups, My Day and convert-to-student.',
    details: [
      'New CRM tab: multi-pipeline lead board with drag-to-stage, per-stage SLA “rotting” warnings and automatic lead scoring (hot at 70+).',
      'Activity timeline per lead (calls, emails, WhatsApp, meetings, notes) with due follow-ups, a Next Actions queue and My Day attention buckets.',
      'Convert to student opens the signup wizard prefilled and links the lead to the created student automatically.',
      'CSV lead import with dry-run, source ROI report, round-robin auto-assignment. Sales users see their own leads; admission sees all.',
    ],
  },
  {
    id: 'part-payments',
    date: '2026-08-04',
    title: 'Part-payments on installments and invoices',
    summary: 'Multiple dated payments with notes; paid state derives automatically.',
    details: [
      'Every tuition installment and additional invoice can carry multiple payment records (amount, date, note such as where it was paid).',
      'An item flips to Paid automatically once its payments cover the amount (paid date = latest payment) and back if a record is removed.',
      'Amount-paid columns and totals count part-payments; partners see the payment history read-only.',
    ],
  },
  {
    id: 'partner-contacts',
    date: '2026-08-04',
    title: 'Partner contact book',
    summary: 'Typed multi-contact book with configurable contact methods.',
    details: [
      'Any number of named contacts per partner, typed by role (Owner / Admission / Marketing / Finance + custom), each with any number of contact methods and a free-text note.',
      'System Config → Contact Methods manages the channel list (18 seeded worldwide; Email, Phone and WhatsApp enabled by default) and the contact types.',
      'Owner contacts can only be changed by the Admission Office; partners manage all other types from their portal Contacts tab.',
    ],
  },
  {
    id: 'per-spec-grants',
    date: '2026-08-04',
    title: 'Per-specialization core programme access',
    summary: 'Grants and partner toggles work per specialization; signup respects them.',
    details: [
      'Core programme access is stored per specialization: unticking one spec no longer revokes the whole programme, and the partner’s own disable toggle keeps the grant.',
      'The signup wizard lists a core programme only when at least one specialization is granted and enabled, showing only those specializations.',
    ],
  },
  {
    id: 'spec-approval',
    date: '2026-08-04',
    title: 'Specialization-level programme approval + cloning',
    summary: 'Custom programmes are approved per specialization; specs can be cloned.',
    details: [
      'Each specialization of a custom programme has its own Draft → Pending → Approved/Rejected workflow; the programme goes live once one spec is approved.',
      'Specializations can be cloned from the same programme or any core programme of the same award level; partner clones need review, admission clones are approved instantly.',
      'Partners also gained “+ Add programme” on their own students, limited to their real access.',
    ],
  },
  {
    id: 'aug3-duration',
    date: '2026-08-03',
    title: 'Durations stored as value + unit',
    summary: 'Study durations keep exactly what was entered — whole months or days.',
    details: [
      'Approved study durations are stored as the exact value + unit entered: months stay whole months, days stay days, and letters print precisely what was typed.',
      'The Month|Day toggle clears the field on switch instead of converting.',
      'All existing student durations were migrated and verified 1:1 against the pre-change backup.',
    ],
  },
  {
    id: 'signup-granted-only',
    date: '2026-08-03',
    title: 'Signup wizard shows only granted programmes',
    summary: 'Partners’ public signup lists their granted and own programmes only.',
    details: [
      'The public signup wizard lists ONLY programmes granted to the partner (plus their own live custom programmes) instead of every core programme.',
    ],
  },
  {
    id: 'date-columns',
    date: '2026-08-03',
    title: 'Commencement + Graduation date columns',
    summary: 'Both students overviews show the enrolment timeline dates.',
    details: [
      'Commencement date and Graduation date columns were added to the students overview in both the admission and partner portals, aligned per enrolment.',
    ],
  },
  {
    id: 'statistics-suite',
    date: '2026-08-02',
    title: 'Statistics tabs with exports',
    summary: 'Outcomes, Grades, Teachers, Demographics, Operations, Finance, Trends — all exportable.',
    details: [
      'Statistics split into tabs: Outcomes, Grades (module difficulty + rubric criterion analysis), Teachers (deviation vs module average), Demographics (gender/age/nationality/industry/position/disability), Operations & QA, Finance and Trends with a month/quarter toggle.',
      'Per-tab CSV/PDF export plus a full-report PDF and a real multi-sheet Excel workbook, all downloaded with normal header authentication.',
    ],
  },
  {
    id: 'sales-role',
    date: '2026-08-02',
    title: 'Sales role, leaderboard and configurable views',
    summary: 'Sales staff scoping, signups leaderboard, referral links, custom column views.',
    details: [
      'New Sales admin level: sees only own students and assigned partners (assignable from the partner profile and Admin Users), with a trimmed portal.',
      'Signups leaderboard with metric switcher (signup / paid / status), amounts and per-event details, per-staff and per-school views, CSV/PDF export.',
      'Referral links (?ref=…) and actor attribution give every signup an “Added by”; “Handled by (Sales)” credits payments and status changes.',
      'Per-user configurable column views on the students overview: show/hide and reorder columns, unlimited named views stored server-side.',
    ],
  },
  {
    id: 'combined-invoices',
    date: '2026-08-02',
    title: 'Combined invoices',
    summary: 'One numbered invoice bundling items across a partner’s students.',
    details: [
      'Admission picks unpaid installments and additional invoices across a partner’s students and generates ONE numbered invoice (INV-{PARTNER}-{seq}); partners download read-only.',
      'Mark paid updates the underlying items; SuperAdmin can revert; deletion allowed within 1 hour (SuperAdmin anytime, never while Paid).',
      'Each partner can have its own invoice design template authored in the letter designer.',
    ],
  },
  {
    id: 'rubrics-grading',
    date: '2026-08-02',
    title: 'Rubrics grading',
    summary: 'Weighted rubric templates for module grading.',
    details: [
      'Shared rubric templates in System Config plus per-module custom rubrics; criteria scored 1–100 and weighted by Max % (must total 100).',
      'Grading happens on the cohort Grades tab with one save; the final grade is always calculated; rubric breakdowns appear on student grade sheets for admission and partners.',
    ],
  },
  {
    id: 'cards-intake',
    date: '2026-08-02',
    title: 'Student ID cards + intake questionnaires',
    summary: 'Designable digital student cards and versioned evaluation forms.',
    details: [
      'Digital student ID cards with a designer template, generated at offer acceptance, with a per-student Student Card ID override that re-renders released cards.',
      'Versioned intake questionnaires with targeting, anonymous statistics and six ready-made evaluation forms; cohort questionnaires gate grade visibility until filled.',
    ],
  },
  {
    id: 'overview-rework',
    date: '2026-08-02',
    title: 'Students overview + editors rework',
    summary: 'Payment-stage filter, split columns, email editing, fonts, letter templates.',
    details: [
      'Students overview: payment-stage dropdown (Unpaid / Paid partially / Paid full / No plan), Enrolments / Specialization / Status split into columns, simplified status labels with tooltips.',
      'Student email editable by admission and partner (login follows, back to unverified).',
      'Font selector with the top-25 common fonts on all document designers, mapped to real server fonts.',
      'Letter and email templates per (programme, partner); immutable student log notes with per-note visibility; per-module start/end dates with programme-level defaults; PartnerNumber + CSV student import.',
    ],
  },
]
