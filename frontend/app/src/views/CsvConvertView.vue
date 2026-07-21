<template>
  <div class="conv-page">
    <nav class="navbar">
      <span class="brand-text">MGW CSV Converter</span>
      <span class="brand-sub">Convert any spreadsheet CSV into the MGW import format — everything runs in your browser, nothing is uploaded.</span>
    </nav>

    <div class="container">
      <!-- Step 1: file + target -->
      <section class="card">
        <h2>1. Your file</h2>
        <div class="row">
          <input type="file" accept=".csv,text/csv" @change="onFile" />
          <label class="fmt">
            Convert to:
            <select v-model="target">
              <option value="students">Student import</option>
              <option value="grades">Grade import</option>
            </select>
          </label>
        </div>
        <p v-if="parseError" class="err-banner">{{ parseError }}</p>
        <p v-else-if="sourceHeaders.length" class="ok-note">
          Loaded <strong>{{ sourceRows.length }}</strong> rows with <strong>{{ sourceHeaders.length }}</strong> columns.
        </p>
      </section>

      <!-- Step 2: mapping -->
      <section v-if="sourceHeaders.length" class="card">
        <h2>2. Map your columns</h2>
        <p class="hint">
          For each MGW column, pick which column of your file fills it, or type a fixed value used
          for every row. Unmapped columns stay empty. Matching columns were pre-selected for you.
        </p>
        <div class="map-wrap">
          <table class="map-table">
            <thead>
              <tr><th>MGW column</th><th>From your file</th><th>Fixed value (every row)</th></tr>
            </thead>
            <tbody>
              <tr v-for="col in targetColumns" :key="col">
                <td class="mgw-col">{{ col }}<span v-if="requiredColumns.includes(col)" class="req">*</span></td>
                <td>
                  <select v-model="mapping[col]">
                    <option value="">— leave empty —</option>
                    <option v-for="h in sourceHeaders" :key="h" :value="h">{{ h }}</option>
                  </select>
                </td>
                <td>
                  <input v-model="fixed[col]" type="text" placeholder=""
                         :disabled="!!mapping[col]" />
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <p class="hint">* used by the import; leaving them empty will give row errors when the file is validated in MGW.</p>
      </section>

      <!-- Step 3: preview + download -->
      <section v-if="sourceHeaders.length" class="card">
        <h2>3. Preview &amp; download</h2>
        <div class="map-wrap">
          <table class="map-table preview-table">
            <thead>
              <tr><th v-for="col in activeColumns" :key="col">{{ col }}</th></tr>
            </thead>
            <tbody>
              <tr v-for="(row, i) in previewRows" :key="i">
                <td v-for="col in activeColumns" :key="col">{{ row[col] }}</td>
              </tr>
            </tbody>
          </table>
        </div>
        <p class="hint" v-if="sourceRows.length > previewRows.length">Showing the first {{ previewRows.length }} of {{ sourceRows.length }} rows.</p>
        <button class="btn-primary" @click="download">⤓ Download {{ target === 'students' ? 'student' : 'grade' }} import CSV</button>
      </section>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, watch } from 'vue'

const STUDENT_COLUMNS = [
  'StudentNumber', 'PartnerNumber', 'FirstName', 'LastName', 'Email',
  'ProgrammeCode', 'SpecializationCode', 'ModeOfStudy',
  'CommencementDate', 'DurationOfStudyMonths', 'InstructionLanguage',
  'DateOfBirth', 'PassportId', 'NationalityCode', 'Gender',
  'DisabilityDisclosure', 'DisabilitySupportNeeds',
  'AddressLine1', 'AddressLine2', 'City', 'StateRegion', 'PostalCode',
  'CountryCode', 'Phone',
  'HighestDegree', 'DegreeSpecialization', 'YearsWorkExperience',
  'PositionFunction', 'EmploymentIndustry',
  'MonthlySalaryAmount', 'MonthlySalaryCurrency',
  'WantsStudentCard',
]
const GRADE_COLUMNS = ['StudentNumber', 'ModuleCode', 'Grade']
const REQUIRED = {
  students: ['ProgrammeCode', 'SpecializationCode', 'FirstName', 'LastName', 'Email'],
  grades: ['StudentNumber', 'ModuleCode', 'Grade'],
}
// Synonyms for the auto-guess, keyed by normalized target name.
const SYNONYMS = {
  studentnumber: ['studentid', 'studentno', 'legacyid', 'id'],
  firstname: ['givenname', 'forename', 'first'],
  lastname: ['surname', 'familyname', 'last'],
  email: ['mail', 'emailaddress'],
  programmecode: ['programcode', 'programme', 'program'],
  specializationcode: ['specialisationcode', 'specialization', 'specialisation', 'speccode'],
  modeofstudy: ['mode', 'studymode'],
  commencementdate: ['startdate', 'commencement', 'start'],
  durationofstudymonths: ['duration', 'durationmonths', 'months'],
  instructionlanguage: ['language', 'teachinglanguage'],
  dateofbirth: ['dob', 'birthdate', 'birthday', 'born'],
  passportid: ['passport', 'passportno', 'nationalid', 'idnumber'],
  nationalitycode: ['nationality', 'country', 'citizenship'],
  gender: ['sex'],
  addressline1: ['address', 'street', 'address1'],
  addressline2: ['address2'],
  city: ['town'],
  stateregion: ['state', 'region', 'province'],
  postalcode: ['zip', 'zipcode', 'postcode'],
  countrycode: ['countryofresidence'],
  phone: ['mobile', 'phonenumber', 'telephone', 'tel'],
  highestdegree: ['degree', 'education', 'highesteducation'],
  degreespecialization: ['degreefield', 'fieldofstudy'],
  yearsworkexperience: ['workexperience', 'experienceyears', 'yearsofexperience'],
  positionfunction: ['position', 'jobtitle', 'function'],
  employmentindustry: ['industry'],
  monthlysalaryamount: ['salary', 'monthlysalary', 'income'],
  monthlysalarycurrency: ['currency', 'salarycurrency'],
  wantsstudentcard: ['studentcard'],
  modulecode: ['module', 'subjectcode', 'subject', 'course', 'coursecode'],
  grade: ['score', 'mark', 'result', 'points'],
}
const DATE_COLUMNS = ['DateOfBirth', 'CommencementDate']

const target = ref('students')
const sourceHeaders = ref([])
const sourceRows = ref([])
const parseError = ref('')
const mapping = reactive({})
const fixed = reactive({})

const targetColumns = computed(() => (target.value === 'students' ? STUDENT_COLUMNS : GRADE_COLUMNS))
const requiredColumns = computed(() => REQUIRED[target.value])
// Downloaded file only carries columns that are actually filled.
const activeColumns = computed(() =>
  targetColumns.value.filter(c => mapping[c] || (fixed[c] || '').trim()))

function normalize(s) { return String(s).toLowerCase().replace(/[^a-z0-9]/g, '') }

function autoGuess() {
  for (const col of targetColumns.value) {
    if (mapping[col]) continue
    const wanted = [normalize(col), ...(SYNONYMS[normalize(col)] ?? [])]
    const hit = sourceHeaders.value.find(h => wanted.includes(normalize(h)))
    if (hit) mapping[col] = hit
  }
}
watch(target, () => autoGuess())

function onFile(e) {
  const file = e.target.files?.[0]
  e.target.value = ''
  if (!file) return
  parseError.value = ''
  const reader = new FileReader()
  reader.onload = () => {
    try {
      const rows = parseCsv(String(reader.result))
      if (rows.length < 2) { parseError.value = 'The file needs a header line plus at least one data row.'; return }
      sourceHeaders.value = rows[0].map(h => h.trim()).filter(h => h.length)
      sourceRows.value = rows.slice(1)
        .filter(r => r.some(v => String(v).trim().length))
        .map(r => {
          const obj = {}
          rows[0].forEach((h, i) => { if (h.trim()) obj[h.trim()] = (r[i] ?? '').trim() })
          return obj
        })
      for (const k of Object.keys(mapping)) delete mapping[k]
      for (const k of Object.keys(fixed)) delete fixed[k]
      autoGuess()
    } catch {
      parseError.value = 'Could not read that file as CSV.'
    }
  }
  reader.readAsText(file)
}

// Normalizes common date formats to the yyyy-MM-dd the import expects.
function normalizeDate(v) {
  const s = String(v).trim()
  if (!s) return s
  if (/^\d{4}-\d{2}-\d{2}$/.test(s)) return s
  const m = s.match(/^(\d{1,2})[./-](\d{1,2})[./-](\d{4})$/)
  if (m) {
    const [, d, mo, y] = m
    return `${y}-${String(mo).padStart(2, '0')}-${String(d).padStart(2, '0')}`
  }
  return s
}

function convertRow(src) {
  const out = {}
  for (const col of targetColumns.value) {
    let v = mapping[col] ? (src[mapping[col]] ?? '') : (fixed[col] || '').trim()
    if (DATE_COLUMNS.includes(col)) v = normalizeDate(v)
    out[col] = v
  }
  return out
}

const previewRows = computed(() => sourceRows.value.slice(0, 5).map(convertRow))

function csvEscape(v) {
  const s = String(v ?? '')
  return /[",\n]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s
}

function download() {
  const cols = activeColumns.value.length ? activeColumns.value : targetColumns.value
  const lines = [cols.join(',')]
  for (const src of sourceRows.value) {
    const row = convertRow(src)
    lines.push(cols.map(c => csvEscape(row[c])).join(','))
  }
  const blob = new Blob(['\uFEFF' + lines.join('\r\n') + '\r\n'], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = target.value === 'students' ? 'mgw-student-import.csv' : 'mgw-grade-import.csv'
  a.click()
  setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

// Minimal RFC 4180 parser (quotes, doubled quotes, CRLF/LF).
function parseCsv(text) {
  const rows = []
  let cur = []
  let field = ''
  let inQuotes = false
  let any = false
  text = text.replace(/^\uFEFF/, '')
  for (let i = 0; i < text.length; i++) {
    const ch = text[i]
    if (inQuotes) {
      if (ch === '"') {
        if (text[i + 1] === '"') { field += '"'; i++ } else inQuotes = false
      } else field += ch
      any = true
    } else if (ch === '"') { inQuotes = true; any = true }
    else if (ch === ',') { cur.push(field); field = ''; any = true }
    else if (ch === '\r') { /* skip */ }
    else if (ch === '\n') {
      if (any) { cur.push(field); rows.push(cur) }
      cur = []; field = ''; any = false
    } else { field += ch; any = true }
  }
  if (any) { cur.push(field); rows.push(cur) }
  return rows
}
</script>

<style scoped>
.conv-page { min-height: 100vh; background: #f0f4f8; font-family: sans-serif; }
.navbar { background: #003366; color: #fff; padding: 0.85rem 1.5rem; display: flex; align-items: baseline; gap: 1rem; flex-wrap: wrap; }
.brand-text { font-weight: 700; font-size: 1rem; }
.brand-sub { font-size: 0.78rem; opacity: 0.75; }
.container { max-width: 1100px; margin: 0 auto; padding: 1.5rem 2rem; display: flex; flex-direction: column; gap: 1rem; }
.card { background: #fff; border: 1px solid #e8edf4; border-radius: 8px; padding: 1rem 1.2rem; box-shadow: 0 1px 4px rgba(0,0,0,.05); }
.card h2 { margin: 0 0 0.6rem; font-size: 1rem; color: #0a264f; }
.row { display: flex; align-items: center; gap: 1rem; flex-wrap: wrap; }
.fmt { display: flex; align-items: center; gap: 0.4rem; font-size: 0.85rem; color: #333; }
.fmt select, .map-table select, .map-table input { padding: 0.3rem 0.4rem; border: 1px solid #cbd5e1; border-radius: 5px; font-size: 0.82rem; background: #fff; max-width: 220px; }
.map-table input:disabled { background: #f1f5f9; }
.err-banner { background: #fef2f2; border: 1.5px solid #fca5a5; color: #b91c1c; padding: 0.6rem 0.9rem; border-radius: 7px; font-size: 0.85rem; margin: 0.6rem 0 0; }
.ok-note { color: #065f46; font-size: 0.85rem; margin: 0.6rem 0 0; }
.hint { color: #64748b; font-size: 0.82rem; margin: 0.3rem 0; }
.map-wrap { overflow-x: auto; }
.map-table { border-collapse: collapse; font-size: 0.83rem; width: 100%; }
.map-table th { text-align: left; font-size: 0.7rem; text-transform: uppercase; letter-spacing: 0.04em; color: #888; padding: 0.3rem 0.5rem; border-bottom: 1px solid #e8edf4; white-space: nowrap; }
.map-table td { padding: 0.25rem 0.5rem; border-bottom: 1px solid #f0f3f7; }
.mgw-col { font-weight: 700; color: #003366; white-space: nowrap; }
.req { color: #dc2626; margin-left: 2px; }
.preview-table td { white-space: nowrap; max-width: 200px; overflow: hidden; text-overflow: ellipsis; }
.btn-primary { background: #1d4ed8; color: #fff; border: none; border-radius: 6px; padding: 0.55rem 1.2rem; font-weight: 700; font-size: 0.88rem; cursor: pointer; margin-top: 0.6rem; }
.btn-primary:hover { background: #1e40af; }
</style>
