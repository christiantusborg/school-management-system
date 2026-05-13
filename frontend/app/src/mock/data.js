// ── Mock employee accounts ────────────────────────────────────────────────────
export const mockEmployees = [
  { username: 'adm', password: 'adm', role: 'employee', displayName: 'Admin' },
]

// ── Enrollment status options (admin-managed, partner view-only) ──────────────
export const ENROLLMENT_STATUSES = [
  'Active',
  'Active (final project)',
  'Applicant withdraw',
  'Cancelled',
  'Deferred',
  'Dismissed',
  'Drop out',
  'Graduated',
  'Potential applicant',
  'Potential applicant paid',
  'Transferred',
]

// ── ID counters ───────────────────────────────────────────────────────────────
let _nextId = 6
export function getNextId() { return _nextId++ }

let _enrollId = 107
export const nextEnrollId = () => _enrollId++

// ── Student records (shared, mutated in-memory) ───────────────────────────────
// Personal info stays at student level; programme-specific data is in enrollments[].
export const students = [
  {
    id: 1,
    studentId: 'IBSS.MBA.23110102',
    firstName: 'ABC', lastName: 'Sample',
    passportId: '800203-10-5685',
    address: 'No 70, Jalan PP 2/11, Taman Putra Prima, 47130 Puchong, Selangor, Malaysia',
    partner: 'Partner A',
    email: 'abc.sample@email.com', dateOfBirth: '1985-06-20',
    highestDegree: 'Bachelor', languageResult: 'IELTS 6.5', yearsWorkExperience: 5,
    docPassport: null, docDegree: null, docLanguage: null, docCV: null,
    docsVerified: { passport: false, degree: false, language: false, cv: false },
    enrollments: [
      {
        id: 100,
        programme: 'Master of Business Administration',
        specialization: 'Business Administration',
        commencementDate: '2023-11-04',
        modeOfStudy: 'Distance/Online self-study',
        durationOfStudy: '18 months',
        selectedPathway: null, offerType: null,
        paymentDone: false, admissionConfirmed: false,
        missingDocsSubmitted: false, certReleased: false,
        // Admin-managed status & progress
        enrollmentStatus: 'Active',
        coursesCompleted: 0, coursesRequired: 8,
        finalProjectStatus: 'not applicable',
        transcriptReleased: false,
        tuitionFeeStatus: 'unpaid',
        otherFeesStatus: 'not applicable',
        changeNotes: [],
      },
    ],
  },
  {
    id: 2,
    studentId: 'IBSS.MBA.23110105',
    firstName: 'Jane', lastName: 'Doe',
    passportId: 'A12345678',
    address: '123 Example Street, Kuala Lumpur, Malaysia',
    partner: 'Partner B',
    email: 'jane.doe@email.com', dateOfBirth: '1990-03-15',
    highestDegree: 'Bachelor', languageResult: 'IELTS 7.0', yearsWorkExperience: 3,
    docPassport: null, docDegree: null, docLanguage: null, docCV: null,
    docsVerified: { passport: false, degree: false, language: false, cv: false },
    enrollments: [
      {
        id: 101,
        programme: 'Master of Business Administration',
        specialization: 'Finance',
        commencementDate: '2023-11-04',
        modeOfStudy: 'Blended learning',
        durationOfStudy: '18 months',
        selectedPathway: null, offerType: null,
        paymentDone: false, admissionConfirmed: false,
        missingDocsSubmitted: false, certReleased: false,
        enrollmentStatus: 'Active',
        coursesCompleted: 2, coursesRequired: 8,
        finalProjectStatus: 'not applicable',
        transcriptReleased: false,
        tuitionFeeStatus: 'partially paid',
        otherFeesStatus: 'fully paid',
        changeNotes: [],
      },
      {
        id: 105,
        programme: 'Master of Finance',
        specialization: 'Finance',
        commencementDate: '2024-03-01',
        modeOfStudy: 'Distance/Online self-study',
        durationOfStudy: '18 months',
        selectedPathway: null, offerType: 'offer',
        paymentDone: false, admissionConfirmed: false,
        missingDocsSubmitted: false, certReleased: false,
        enrollmentStatus: 'Potential applicant paid',
        coursesCompleted: 0, coursesRequired: 8,
        finalProjectStatus: 'not applicable',
        transcriptReleased: false,
        tuitionFeeStatus: 'unpaid', otherFeesStatus: 'not applicable',
        changeNotes: [],
      },
    ],
  },
  {
    id: 3,
    studentId: 'IBSS.BBA.23120301',
    firstName: 'Ahmad', lastName: 'Razak',
    passportId: 'B98765432',
    address: '45 Jalan Ampang, Kuala Lumpur, Malaysia',
    partner: 'Partner A',
    email: 'ahmad.razak@email.com', dateOfBirth: '1995-08-22',
    highestDegree: '', languageResult: '', yearsWorkExperience: 0,
    docPassport: null, docDegree: null, docLanguage: null, docCV: null,
    docsVerified: { passport: false, degree: false, language: false, cv: false },
    enrollments: [
      {
        id: 102,
        programme: 'Bachelor of Business Administration',
        specialization: 'Marketing',
        commencementDate: '2023-12-01',
        modeOfStudy: 'Blended learning',
        durationOfStudy: '',
        selectedPathway: null, offerType: null,
        paymentDone: false, admissionConfirmed: false,
        missingDocsSubmitted: false, certReleased: false,
        enrollmentStatus: 'Active',
        coursesCompleted: 0, coursesRequired: 10,
        finalProjectStatus: 'not done',
        transcriptReleased: false,
        tuitionFeeStatus: 'unpaid',
        otherFeesStatus: 'not applicable',
        changeNotes: [],
      },
      {
        id: 106,
        programme: 'Master of Marketing',
        specialization: 'Marketing',
        commencementDate: '2024-06-01',
        modeOfStudy: 'Distance/Online self-study',
        durationOfStudy: '18 months',
        selectedPathway: null, offerType: 'offer',
        paymentDone: false, admissionConfirmed: false,
        missingDocsSubmitted: false, certReleased: false,
        enrollmentStatus: 'Active',
        coursesCompleted: 1, coursesRequired: 8,
        finalProjectStatus: 'not applicable',
        transcriptReleased: false,
        tuitionFeeStatus: 'unpaid', otherFeesStatus: 'not applicable',
        changeNotes: [],
      },
    ],
  },
  {
    id: 4,
    studentId: 'IBSS.MF.24010401',
    firstName: 'Siti', lastName: 'Nurhayati',
    passportId: 'C11223344',
    address: '77 Jalan Bukit Bintang, Kuala Lumpur, Malaysia',
    partner: 'Partner C',
    email: 'siti.nurhayati@email.com', dateOfBirth: '1988-11-05',
    highestDegree: '', languageResult: '', yearsWorkExperience: 0,
    docPassport: null, docDegree: null, docLanguage: null, docCV: null,
    docsVerified: { passport: false, degree: false, language: false, cv: false },
    enrollments: [
      {
        id: 103,
        programme: 'Master of Finance',
        specialization: 'Finance',
        commencementDate: '2024-01-04',
        modeOfStudy: 'Distance/Online self-study',
        durationOfStudy: '',
        selectedPathway: null, offerType: null,
        paymentDone: false, admissionConfirmed: false,
        missingDocsSubmitted: false, certReleased: false,
        enrollmentStatus: 'Active',
        coursesCompleted: 0, coursesRequired: 8,
        finalProjectStatus: 'not applicable',
        transcriptReleased: false,
        tuitionFeeStatus: 'unpaid',
        otherFeesStatus: 'not applicable',
        changeNotes: [],
      },
    ],
  },
  {
    id: 5,
    studentId: 'IBSS.BCS.24020501',
    firstName: 'Lee', lastName: 'Wei Ming',
    passportId: 'D55667788',
    address: '12 Taman Desa, Kuala Lumpur, Malaysia',
    partner: 'Partner B',
    email: 'lee.weiming@email.com', dateOfBirth: '1997-02-28',
    highestDegree: '', languageResult: '', yearsWorkExperience: 0,
    docPassport: null, docDegree: null, docLanguage: null, docCV: null,
    docsVerified: { passport: false, degree: false, language: false, cv: false },
    enrollments: [
      {
        id: 104,
        programme: 'Bachelor of Computer Science',
        specialization: 'Computer Science',
        commencementDate: '2024-02-05',
        modeOfStudy: 'Blended learning',
        durationOfStudy: '',
        selectedPathway: null, offerType: null,
        paymentDone: false, admissionConfirmed: false,
        missingDocsSubmitted: false, certReleased: false,
        enrollmentStatus: 'Active',
        coursesCompleted: 0, coursesRequired: 12,
        finalProjectStatus: 'not done',
        transcriptReleased: false,
        tuitionFeeStatus: 'unpaid',
        otherFeesStatus: 'not applicable',
        changeNotes: [],
      },
    ],
  },
]

// ── Partner review wizard seeding ─────────────────────────────────────────────
export function makePartnerReview() {
  return {
    passport:  { status: 'pending', reason: '' },
    degree:    { status: 'pending', reason: '' },
    language:  { status: 'pending', reason: '' },
    cv:        { status: 'pending', reason: '' },
    programme: { status: 'pending', reason: '' },
    completedAt: null,
    partnerName: '',
  }
}

for (const s of students) {
  if (!s.partnerReview) s.partnerReview = makePartnerReview()
  for (const e of s.enrollments) {
    if (e.durationMonths === undefined) e.durationMonths = null
    if (e.paymentPlan === undefined) e.paymentPlan = null
  }
}

// Seed sample doc filenames on the first two students so previews render in the review wizard.
if (students[0]) Object.assign(students[0], {
  docPassport: 'passport.jpg', docDegree: 'degree.pdf', docLanguage: 'ielts.pdf', docCV: 'cv.pdf',
})
if (students[1]) Object.assign(students[1], {
  docPassport: 'id-card.png', docDegree: 'degree-cert.pdf', docLanguage: 'toefl.pdf', docCV: 'resume.docx',
})

// ── In-memory questions ───────────────────────────────────────────────────────
export const mockQuestions = []
