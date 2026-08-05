// In-app changelog shown on the SuperAdmin "Changelog" tab.
// STANDING RULE: prepend a new entry here with EVERY deploy.
// Newest first. { id, date, title, summary, details } — details is an array
// of plain-text bullet lines.
export const CHANGELOG = [
  {
    id: 'mail-lead-history-sales',
    date: '2026-08-05',
    title: 'Full mail history on leads & students + Mail for Sales',
    summary: 'Leads and converted students inherit the complete email history; Sales get their own Mail page.',
    details: [
      'Creating a lead (from the CRM, from a mail, or by setting its email) now links the ENTIRE archived mail history with that address — incoming and outgoing — not just future mail.',
      'When a lead converts to a student, the student inherits the lead\'s complete mail history automatically.',
      'Sales users now see the Mail tab, scoped to the mail accounts a SuperAdministrator grants them (e.g. their own sales address) — with the same webmail, linking and reply features.',
      'Sales can also open the Mail tab on a partner\'s manage page to view linked mail and send new mail from their granted account.',
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
    id: 'student-ids-changelog',
    date: '2026-08-05',
    title: 'Multiple Student IDs + this changelog',
    summary: 'Students can carry several Student IDs; CSV import matches by email only.',
    details: [
      'Students now hold multiple Student IDs: one primary (printed on lists and letters) plus any number of aliases with optional labels. Admission manages them from the student drawer (add, edit, make primary, remove); partners and students see the list read-only.',
      'Student IDs are globally unique — no two students can share an ID.',
      'CSV import matches rows by EMAIL ONLY. Existing students get only NEW programmes (programmes they already have are skipped) enrolled under the importing partner, and unknown Student IDs on their rows are attached as aliases labelled "CSV import". Rows whose ID belongs to a different student are rejected.',
      'The students overview search also finds alias IDs, and the export gains an "All student IDs" column.',
      'This changelog tab (SuperAdmin only) — updated with every deploy from now on.',
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
    id: 'aug4-catchup',
    date: '2026-08-04',
    title: 'Payments, contacts and partner access rework',
    summary: 'Part-payments, partner contact book, per-spec programme grants.',
    details: [
      'Part-payment records per installment/invoice with notes; paid state derives automatically once covered; Amount paid columns and partner-visible history.',
      'Partner contact book: typed contacts (Owner/Admission/Marketing/Finance + custom) with any number of contact methods (18 seeded channels, Email/Phone/WhatsApp enabled) and notes; Owner contacts are admission-only.',
      'Core programme access became per-specialization: unticking one spec no longer revokes the whole programme, and the signup wizard respects partner toggles.',
      'Partner “+ Add programme” on own students; specialization-level programme approval with per-spec review and cloning.',
    ],
  },
]
