import { reactive } from 'vue'

let _seq = 500
export const uid = () => `id_${_seq++}`

// ── IBSS Core Programmes (admin-editable) ─────────────────────────────────────
export const corePrograms = reactive([
  {
    id: 'prog_mba', name: 'Master of Business Administration', code: 'MBA',
    specializations: [
      { id: 'maj_mba_ba',  name: 'Business Administration', subjects: [
        { id: 'subj_1', code: 'MBA501', name: 'Strategic Management',          ects: 15 },
        { id: 'subj_2', code: 'MBA502', name: 'International Marketing',       ects: 15 },
        { id: 'subj_3', code: 'MBA503', name: 'Strategic Business Management', ects: 15 },
        { id: 'subj_4', code: 'MBA504', name: 'Financial Management',          ects: 15 },
      ]},
      { id: 'maj_mba_fin', name: 'Finance', subjects: [
        { id: 'subj_5', code: 'MBA511', name: 'Corporate Finance',             ects: 15 },
        { id: 'subj_6', code: 'MBA512', name: 'Investment Analysis',           ects: 15 },
        { id: 'subj_7', code: 'MBA513', name: 'Financial Risk Management',     ects: 15 },
        { id: 'subj_8', code: 'MBA514', name: 'International Finance',         ects: 15 },
      ]},
    ],
  },
  {
    id: 'prog_bba', name: 'Bachelor of Business Administration', code: 'BBA',
    specializations: [
      { id: 'maj_bba_ba',  name: 'Business Administration', subjects: [
        { id: 'subj_9',  code: 'BBA301', name: 'Business Fundamentals',      ects: 15 },
        { id: 'subj_10', code: 'BBA302', name: 'Principles of Marketing',    ects: 15 },
        { id: 'subj_11', code: 'BBA303', name: 'Introduction to Finance',    ects: 15 },
        { id: 'subj_12', code: 'BBA304', name: 'Organisational Behaviour',   ects: 15 },
      ]},
      { id: 'maj_bba_mkt', name: 'Marketing', subjects: [
        { id: 'subj_13', code: 'BBA311', name: 'Marketing Strategy',         ects: 15 },
        { id: 'subj_14', code: 'BBA312', name: 'Consumer Behaviour',         ects: 15 },
        { id: 'subj_15', code: 'BBA313', name: 'Digital Marketing',          ects: 15 },
        { id: 'subj_16', code: 'BBA314', name: 'Brand Management',           ects: 15 },
      ]},
    ],
  },
  {
    id: 'prog_mf', name: 'Master of Finance', code: 'MF',
    specializations: [
      { id: 'maj_mf_fin', name: 'Finance', subjects: [
        { id: 'subj_17', code: 'MF501', name: 'Advanced Corporate Finance',  ects: 15 },
        { id: 'subj_18', code: 'MF502', name: 'Portfolio Management',        ects: 15 },
        { id: 'subj_19', code: 'MF503', name: 'Risk & Derivatives',          ects: 15 },
        { id: 'subj_20', code: 'MF504', name: 'International Finance',       ects: 15 },
      ]},
    ],
  },
  {
    id: 'prog_bcs', name: 'Bachelor of Computer Science', code: 'BCS',
    specializations: [
      { id: 'maj_bcs_cs', name: 'Computer Science', subjects: [
        { id: 'subj_21', code: 'BCS301', name: 'Data Structures & Algorithms', ects: 15 },
        { id: 'subj_22', code: 'BCS302', name: 'Database Systems',             ects: 15 },
        { id: 'subj_23', code: 'BCS303', name: 'Software Engineering',         ects: 15 },
        { id: 'subj_24', code: 'BCS304', name: 'Computer Networks',            ects: 15 },
      ]},
    ],
  },
])

// ── Helpers ───────────────────────────────────────────────────────────────────
export const getAllCoreAccessKeys = () => {
  const keys = []
  for (const p of corePrograms) for (const m of p.specializations) keys.push(`${p.id}__${m.id}`)
  return keys
}
export const findCoreProgram = id => corePrograms.find(p => p.id === id) ?? null
export const findCoreSpecialization   = (progId, majId) => findCoreProgram(progId)?.specializations.find(m => m.id === majId) ?? null

// ── Partner records ───────────────────────────────────────────────────────────
// coreAccess:       string[]  'progId__majId' — enabled core programme+specialization pairs
// clones:           Clone[]
//   Clone (specialization):  { id, type:'specialization',     srcProgId, srcMajId, name, subjects[] }
//   Clone (prog):   { id, type:'programme', srcProgId, name, code,
//                     specializations: [{ id, srcMajId, name, subjects[] }] }
export const partnerRecords = reactive([
  { id:'pa', name:'Partner A', username:'partner_a', password:'partner_a', role:'partner', coreAccess: getAllCoreAccessKeys(), clones: [
    {
      id: 'clone_pa_mba', type: 'programme', srcProgId: 'prog_mba',
      name: 'Master of Business Administration (Partner A)',
      code: 'MBA',
      status: 'approved',
      rejectionReason: null,
      specializations: [
        { id: 'clone_pa_mba_maj1', srcMajId: 'maj_mba_ba', name: 'Business Administration',
          subjects: [
            { id: 'clone_pa_s1', code: 'MBA501', name: 'Strategic Management',          ects: 15 },
            { id: 'clone_pa_s2', code: 'MBA502', name: 'International Marketing',       ects: 15 },
            { id: 'clone_pa_s3', code: 'MBA503', name: 'Strategic Business Management', ects: 15 },
            { id: 'clone_pa_s4', code: 'MBA504', name: 'Financial Management',          ects: 15 },
          ]},
        { id: 'clone_pa_mba_maj2', srcMajId: 'maj_mba_fin', name: 'Finance',
          subjects: [
            { id: 'clone_pa_s5', code: 'MBA511', name: 'Corporate Finance',             ects: 15 },
            { id: 'clone_pa_s6', code: 'MBA512', name: 'Investment Analysis',           ects: 15 },
            { id: 'clone_pa_s7', code: 'MBA513', name: 'Financial Risk Management',     ects: 15 },
            { id: 'clone_pa_s8', code: 'MBA514', name: 'International Finance',         ects: 15 },
          ]},
      ],
    },
    {
      id: 'clone_pa_bba', type: 'programme', srcProgId: 'prog_bba',
      name: 'Bachelor of Business Administration (Partner A)',
      code: 'BBA',
      status: 'rejected',
      rejectionReason: 'Programme structure does not meet IBSS accreditation requirements. Please revise the subject credit allocation and resubmit.',
      specializations: [
        { id: 'clone_pa_bba_maj1', srcMajId: 'maj_bba_ba', name: 'Business Administration',
          subjects: [
            { id: 'clone_pa_s9',  code: 'BBA301', name: 'Business Fundamentals',    ects: 15 },
            { id: 'clone_pa_s10', code: 'BBA302', name: 'Principles of Marketing',  ects: 15 },
            { id: 'clone_pa_s11', code: 'BBA303', name: 'Introduction to Finance',  ects: 15 },
            { id: 'clone_pa_s12', code: 'BBA304', name: 'Organisational Behaviour', ects: 15 },
          ]},
        { id: 'clone_pa_bba_maj2', srcMajId: 'maj_bba_mkt', name: 'Marketing',
          subjects: [
            { id: 'clone_pa_s13', code: 'BBA311', name: 'Marketing Strategy',  ects: 15 },
            { id: 'clone_pa_s14', code: 'BBA312', name: 'Consumer Behaviour',  ects: 15 },
            { id: 'clone_pa_s15', code: 'BBA313', name: 'Digital Marketing',   ects: 15 },
            { id: 'clone_pa_s16', code: 'BBA314', name: 'Brand Management',    ects: 15 },
          ]},
      ],
    },
  ] },
  { id:'pb', name:'Partner B', username:'partner_b', password:'partner_b', role:'partner', coreAccess: getAllCoreAccessKeys(), clones: [] },
  { id:'pc', name:'Partner C', username:'partner_c', password:'partner_c', role:'partner', coreAccess: getAllCoreAccessKeys(), clones: [] },
])

// ── Clone helpers ─────────────────────────────────────────────────────────────
export function getSpecializationClone(partner, srcProgId, srcMajId) {
  return partner?.clones.find(c => c.type === 'specialization' && c.srcProgId === srcProgId && c.srcMajId === srcMajId) ?? null
}
export function getProgrammeClone(partner, srcProgId) {
  return partner?.clones.find(c => c.type === 'programme' && c.srcProgId === srcProgId) ?? null
}

export function cloneSpecializationFn(partner, srcProgId, srcMajId) {
  const maj = findCoreSpecialization(srcProgId, srcMajId)
  if (!maj) return
  const existingCount = partner.clones.filter(
    c => c.type === 'specialization' && c.srcProgId === srcProgId && c.srcMajId === srcMajId
  ).length
  const name = existingCount === 0 ? `${maj.name} (Clone)` : `${maj.name} (Clone ${existingCount + 1})`
  partner.clones.push({
    id: uid(), type: 'specialization', srcProgId, srcMajId, name,
    subjects: maj.subjects.map(s => ({ id: uid(), code: s.code ?? '', name: s.name, ects: s.ects ?? 15 })),
  })
}

export function createCustomSpecializationFn(partner, srcProgId, name) {
  if (!name.trim()) return
  partner.clones.push({
    id: uid(), type: 'custom', srcProgId,
    name: name.trim(),
    subjects: [],
  })
}

// Deep-clone an entire core programme into the partner's clones[] with status:'draft'
export function cloneProgramFn(partner, srcProgId) {
  const prog = findCoreProgram(srcProgId)
  if (!prog) return
  partner.clones.push({
    id: uid(), type: 'programme', srcProgId,
    name: `${prog.name} (${partner.name})`,
    code: prog.code,
    status: 'draft',
    rejectionReason: null,
    specializations: prog.specializations.map(m => ({
      id: uid(), srcMajId: m.id, name: m.name,
      subjects: m.subjects.map(s => ({ id: uid(), code: s.code ?? '', name: s.name, ects: s.ects ?? 15 })),
    })),
  })
}

// Partner submits their programme clone for IBSS approval
export function submitForApprovalFn(partner, cloneId) {
  const clone = partner.clones.find(c => c.id === cloneId)
  if (clone && clone.type === 'programme') {
    clone.status = 'pending'
    clone.rejectionReason = null
  }
}

// Admin approves a partner programme clone
export function approveProgramFn(partner, cloneId) {
  const clone = partner.clones.find(c => c.id === cloneId)
  if (clone && clone.type === 'programme') clone.status = 'approved'
}

// Admin rejects a partner programme clone with a reason
export function rejectProgramFn(partner, cloneId, reason) {
  const clone = partner.clones.find(c => c.id === cloneId)
  if (clone && clone.type === 'programme') {
    clone.status = 'rejected'
    clone.rejectionReason = reason ?? ''
  }
}

// Resolve subjects for grade entry: specialization clone (first) → custom specialization → core
export function resolveSubjects(partnerName, programmeName, specializationName) {
  const partner = partnerRecords.find(p => p.name === partnerName)
  if (!partner) return []
  const cProg = corePrograms.find(p => p.name === programmeName)
  const cMaj  = cProg?.specializations.find(m => m.name === specializationName)

  // Specialization clone (take first clone of this specialization)
  if (cProg && cMaj) {
    const mClone = partner.clones.find(
      c => c.type === 'specialization' && c.srcProgId === cProg.id && c.srcMajId === cMaj.id
    )
    if (mClone) return mClone.subjects
  }
  // Custom specialization by name under this programme
  if (cProg) {
    const custom = partner.clones.find(
      c => c.type === 'custom' && c.srcProgId === cProg.id && c.name === specializationName
    )
    if (custom) return custom.subjects
  }
  // Core fallback
  return cMaj?.subjects ?? []
}

// ── Derived dropdown lists ────────────────────────────────────────────────────
export const getProgrammeNames = () => corePrograms.map(p => p.name)
export const getSpecializationNames     = () => { const s = new Set(); for (const p of corePrograms) for (const m of p.specializations) s.add(m.name); return [...s] }
export const getPartnerNames   = () => partnerRecords.map(p => p.name)

// ── Programme level & admission pathways ──────────────────────────────────────
export function getProgramLevel(programmeName) {
  const n = programmeName ?? ''
  if (/Doctor|DBA/i.test(n))           return 'doctor'
  if (/Master/i.test(n))               return 'master'
  if (/Advanced Diploma/i.test(n))     return 'advanced_diploma'
  if (/Diploma/i.test(n))              return 'diploma'
  if (/Bachelor/i.test(n))             return 'bachelor'
  return 'master'
}

export const PATHWAYS = {
  master: [
    { id: 'p1', label: "Pathway 1: Direct Entry via Bachelor's Degree",    minYears: 0 },
    { id: 'p2', label: 'Pathway 2: Advanced Diploma + 3 Years Work Exp',   minYears: 3 },
    { id: 'p3', label: 'Pathway 3: Diploma + 5 Years Work Exp',            minYears: 5 },
    { id: 'p4', label: 'Pathway 4: High School + 8 Years Work Exp',        minYears: 8 },
  ],
  doctor: [
    { id: 'p1', label: "Pathway 1: Master's Degree (Preferred Entry)",     minYears: 0  },
    { id: 'p2', label: "Pathway 2: Bachelor's Degree + 5 Years Work Exp",  minYears: 5  },
    { id: 'p3', label: 'Pathway 3: Advanced Diploma + 7 Years Work Exp',   minYears: 7  },
    { id: 'p4', label: 'Pathway 4: Diploma + 9 Years Work Exp',            minYears: 9  },
    { id: 'p5', label: 'Pathway 5: High School + 12 Years Work Exp',       minYears: 12 },
  ],
  diploma: [
    { id: 'p1', label: 'Open Entry', minYears: 0 },
  ],
  advanced_diploma: [
    { id: 'p1', label: 'Pathway 1: Diploma or Associate Degree',                      minYears: 0 },
    { id: 'p2', label: 'Pathway 2: High School Certificate + 3 Years Work Exp',       minYears: 3 },
  ],
  bachelor: [
    { id: 'p1', label: 'Pathway 1: Advanced Diploma',                                 minYears: 0 },
    { id: 'p2', label: 'Pathway 2: Diploma + 2 Years Work Exp',                       minYears: 2 },
    { id: 'p3', label: 'Pathway 3: High School Certificate + 5 Years Work Exp',       minYears: 5 },
  ],
}

// ── Grade standard lookup ─────────────────────────────────────────────────────
export function getGradeInfo(score) {
  const s = Number(score)
  if (score === '' || score === null || score === undefined || isNaN(s)) return null
  if (s >= 75) return { ukGrade:'A+', ectsGrade:'A',  ectsPoints:7.0, remark:'Excellent – outstanding performance with only minor shortcomings' }
  if (s >= 70) return { ukGrade:'A',  ectsGrade:'A',  ectsPoints:6.0, remark:'Very good – above the average standard, but with some shortcomings' }
  if (s >= 63) return { ukGrade:'A-', ectsGrade:'B',  ectsPoints:5.0, remark:'Good – generally sound work with a number of notable shortcomings' }
  if (s >= 55) return { ukGrade:'B',  ectsGrade:'C',  ectsPoints:4.0, remark:'Satisfactory – but with significant shortcomings' }
  if (s >= 50) return { ukGrade:'C',  ectsGrade:'D',  ectsPoints:3.0, remark:'Sufficient – performance meets the minimum criteria' }
  if (s >= 45) return { ukGrade:'D',  ectsGrade:'E',  ectsPoints:2.5, remark:'Sufficient – performance meets the minimum criteria' }
  if (s >= 41) return { ukGrade:'E',  ectsGrade:'FX', ectsPoints:2.0, remark:'Fail – some more work required before the credit can be awarded' }
  if (s >= 35) return { ukGrade:'FX', ectsGrade:'F',  ectsPoints:1.5, remark:'Fail' }
  if (s >= 30) return { ukGrade:'F',  ectsGrade:'F',  ectsPoints:1.0, remark:'Fail' }
  return         { ukGrade:'0',  ectsGrade:'F',  ectsPoints:0.0, remark:'Fail – no/little visible achievement' }
}
