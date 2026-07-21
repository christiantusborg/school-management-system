<template>
  <div class="import-tab">
    <!-- ── Students ─────────────────────────────────────────────── -->
    <section class="import-card">
      <h2>Students</h2>
      <div class="import-head">
        <div>
          <p class="hint">
            Upload a CSV with one line per programme a student joins. A student joining several
            programmes appears on several lines, matched by Student ID. Leave the StudentNumber
            cell empty to create a new student with an auto-generated ID; fill in an existing ID
            to only add the programme (personal columns are then ignored); an unknown ID creates
            the student with that legacy ID. Commencement date, duration of study and instruction
            language apply per line (per enrolment). New students get a login account but no email is sent.
            <template v-if="scoped"> The partner is taken from this page, so the CSV needs no PartnerNumber column.</template>
          </p>
        </div>
        <button class="btn-outline" :disabled="students.busy" @click="downloadSample(students)">Download sample file</button>
      </div>

      <div class="upload-row">
        <input type="file" accept=".csv,text/csv" @change="onFile(students, $event)" />
        <button class="btn-primary" :disabled="!students.file || students.busy" @click="run(students, true)">Validate</button>
        <button class="btn-primary" :disabled="!students.file || students.busy || !students.validated" @click="run(students, false)">Import</button>
        <span v-if="students.busy" class="working">Working…</span>
      </div>
      <p class="flow-hint">
        Validate first: it checks every line without saving anything, then Import applies the file.
        Have a spreadsheet in another layout? <a href="/#/import-converter" target="_blank" rel="noopener">Open the CSV converter</a> to map it into this format.
      </p>

      <div v-if="students.error" class="err-banner">{{ students.error }}</div>

      <template v-if="students.report">
        <div class="summary-row">
          <span class="pill pill-ok">{{ students.mode === 'import' ? 'Created' : 'Will create' }}: {{ students.report.created ?? students.report.create }}</span>
          <span class="pill pill-ok">{{ students.mode === 'import' ? 'Enrolled' : 'Will enrol' }}: {{ students.report.enrolled ?? students.report.enroll }}</span>
          <span class="pill">Skipped: {{ students.report.skipped ?? students.report.skip }}</span>
          <span class="pill" :class="{ 'pill-err': students.report.errors > 0 }">Errors: {{ students.report.errors }}</span>
        </div>

        <div v-if="students.report.partners?.length" class="partners-box">
          <h3>Admission handling</h3>
          <p class="hint" v-if="canToggleDirect">Checked partners get imported students admitted directly; unchecked ones send them into the normal admission review queue.</p>
          <p class="hint" v-else>Where imported students land is set by the Admission Office.</p>
          <label v-for="p in students.report.partners" :key="p.partnerNumber" class="partner-setting">
            <input type="checkbox" :checked="p.directAdmission" :disabled="!canToggleDirect"
                   @change="toggleDirect(p, $event.target.checked)" />
            <strong>{{ p.name }}</strong> <code>{{ p.partnerNumber }}</code>
            <span class="setting-label">{{ p.directAdmission ? 'Direct admission' : 'Admission review queue' }}</span>
          </label>
        </div>

        <div class="table-wrap">
          <table class="data-table">
            <thead>
              <tr>
                <th>Line</th>
                <th>Student ID</th>
                <th>Student</th>
                <th>Partner</th>
                <th>Programme</th>
                <th>Specialization</th>
                <th>Result</th>
                <th>Message</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="r in students.report.rows" :key="r.row" :class="rowClass(r)">
                <td>{{ r.row }}</td>
                <td>{{ r.studentNumber }}</td>
                <td>{{ r.student }}</td>
                <td>{{ r.partner }}</td>
                <td>{{ r.programme }}</td>
                <td>{{ r.specialization }}</td>
                <td><span class="action-badge" :class="badgeClass(r)">{{ r.action }}</span></td>
                <td class="msg-cell">{{ r.message }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </template>
    </section>

    <!-- ── Grades ───────────────────────────────────────────────── -->
    <section class="import-card">
      <h2>Grades</h2>
      <div class="import-head">
        <div>
          <p class="hint">
            Upload a CSV with one line per grade: Student ID (e.g. ST-20260710-A11413), module code
            (e.g. LXIIS.BIT.0011) and the grade (0-100). The module is matched against the student's
            enrolments; an existing score for that module is overwritten. Enrolment status is not
            changed, and already released transcripts/certificates are re-rendered with the new scores.
          </p>
        </div>
        <button class="btn-outline" :disabled="grades.busy" @click="downloadSample(grades)">Download sample file</button>
      </div>

      <div class="upload-row">
        <input type="file" accept=".csv,text/csv" @change="onFile(grades, $event)" />
        <button class="btn-primary" :disabled="!grades.file || grades.busy" @click="run(grades, true)">Validate</button>
        <button class="btn-primary" :disabled="!grades.file || grades.busy || !grades.validated" @click="run(grades, false)">Import</button>
        <span v-if="grades.busy" class="working">Working…</span>
      </div>
      <p class="flow-hint">Validate first: it checks every line without saving anything, then Import applies the file.</p>

      <div v-if="grades.error" class="err-banner">{{ grades.error }}</div>

      <template v-if="grades.report">
        <div class="summary-row">
          <span class="pill pill-ok">{{ grades.mode === 'import' ? 'Added' : 'Will add' }}: {{ grades.report.added }}</span>
          <span class="pill pill-ok">{{ grades.mode === 'import' ? 'Updated' : 'Will update' }}: {{ grades.report.updated }}</span>
          <span class="pill">Skipped: {{ grades.report.skipped }}</span>
          <span class="pill" :class="{ 'pill-err': grades.report.errors > 0 }">Errors: {{ grades.report.errors }}</span>
        </div>

        <div class="table-wrap">
          <table class="data-table">
            <thead>
              <tr>
                <th>Line</th>
                <th>Student ID</th>
                <th>Module</th>
                <th>Module Name</th>
                <th>Grade</th>
                <th>Result</th>
                <th>Message</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="r in grades.report.rows" :key="r.row" :class="rowClass(r)">
                <td>{{ r.row }}</td>
                <td>{{ r.studentNumber }}</td>
                <td>{{ r.module }}</td>
                <td>{{ r.moduleName }}</td>
                <td>{{ r.grade }}</td>
                <td><span class="action-badge" :class="badgeClass(r)">{{ r.action }}</span></td>
                <td class="msg-cell">{{ r.message }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </template>
    </section>
  </div>
</template>

<script setup>
import { reactive, computed } from 'vue'
import api from '../../api/client.js'

const emit = defineEmits(['imported'])

const props = defineProps({
  // '/v1/admin/students/import' (admin, optionally scoped via partnerId)
  // or '/v1/partner/students/import' (partner portal, always own partner).
  apiPrefix: { type: String, default: '/v1/admin/students/import' },
  // Admin partner-manage view: scope the import to this partner.
  partnerId: { type: String, default: '' },
  // Partners may not change where their imports land; Admission may.
  canToggleDirect: { type: Boolean, default: true },
})

const scoped = computed(() => !!props.partnerId || props.apiPrefix.startsWith('/v1/partner'))
const qs = computed(() => (props.partnerId ? `?partnerId=${props.partnerId}` : ''))

function makeSection(baseUrl, sampleName) {
  return reactive({
    baseUrl,
    sampleName,
    file: null,
    busy: false,
    error: '',
    report: null,
    mode: 'validate',
    validated: false,
  })
}

const students = makeSection(props.apiPrefix, 'student-import-sample.csv')
const grades = makeSection(`${props.apiPrefix}/grades`, 'grade-import-sample.csv')

function onFile(section, e) {
  section.file = e.target.files?.[0] ?? null
  section.report = null
  section.validated = false
  section.error = ''
}

async function downloadSample(section) {
  section.error = ''
  try {
    const res = await api.get(`${section.baseUrl}/sample${scoped.value ? '?scoped=1' : ''}`, { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url
    a.download = section.sampleName
    a.click()
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch (err) {
    section.error = err.response?.data?.error ?? err.message ?? 'Download failed'
  }
}

async function run(section, dryRun) {
  if (!section.file || section.busy) return
  section.busy = true
  section.error = ''
  try {
    const form = new FormData()
    form.append('file', section.file)
    const url = (dryRun ? `${section.baseUrl}/validate` : section.baseUrl) + qs.value
    const res = await api.post(url, form, { headers: { 'Content-Type': 'multipart/form-data' } })
    section.report = res.data
    section.mode = dryRun ? 'validate' : 'import'
    if (dryRun) {
      section.validated = true
    } else {
      // Force a fresh validate before the same file can be imported again.
      section.validated = false
      emit('imported')
    }
  } catch (err) {
    section.report = null
    section.validated = false
    section.error = err.response?.data?.error ?? err.message ?? 'Upload failed'
  } finally {
    section.busy = false
  }
}

async function toggleDirect(partner, checked) {
  try {
    const res = await api.post('/v1/admin/students/import/partner-setting', {
      partnerNumber: partner.partnerNumber,
      directAdmission: checked,
    })
    partner.directAdmission = res.data.directAdmission
  } catch (err) {
    students.error = err.response?.data?.error ?? err.message ?? 'Could not save the partner setting'
  }
}

function rowClass(r) {
  return { 'row-error': r.action === 'error', 'row-skip': r.action === 'skip' }
}
function badgeClass(r) {
  return `action-${r.action.replace(/[^a-z]/g, '')}`
}
</script>

<style scoped>
.import-tab { display: flex; flex-direction: column; gap: 1.5rem; }
.import-card {
  border: 1px solid #e2e8f0; border-radius: 10px; padding: 1.1rem 1.25rem;
  display: flex; flex-direction: column; gap: 1rem; background: #fff;
}
.import-card h2 { margin: 0; font-size: 1.1rem; }
.import-head { display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem; }
.hint { color: #64748b; font-size: .875rem; max-width: 60rem; margin: 0; }
.flow-hint { color: #94a3b8; font-size: .8rem; margin: 0; }
.upload-row { display: flex; align-items: center; gap: .75rem; flex-wrap: wrap; }
.working { color: #64748b; font-size: .875rem; }
.btn-primary {
  background: #1d4ed8; color: #fff; border: none; border-radius: 6px;
  padding: .5rem 1rem; font-weight: 600; cursor: pointer;
}
.btn-primary:disabled { opacity: .5; cursor: not-allowed; }
.btn-outline {
  background: transparent; color: #1d4ed8; border: 1px solid #1d4ed8;
  border-radius: 6px; padding: .5rem 1rem; font-weight: 600; cursor: pointer; white-space: nowrap;
}
.err-banner {
  background: #fef2f2; color: #b91c1c; border: 1px solid #fecaca;
  border-radius: 6px; padding: .6rem .9rem; font-size: .875rem;
}
.summary-row { display: flex; gap: .5rem; flex-wrap: wrap; }
.pill {
  background: #f1f5f9; color: #334155; border-radius: 999px;
  padding: .25rem .75rem; font-size: .8rem; font-weight: 600;
}
.pill-ok { background: #ecfdf5; color: #047857; }
.pill-err { background: #fef2f2; color: #b91c1c; }
.partners-box { border: 1px solid #e2e8f0; border-radius: 8px; padding: .9rem 1rem; }
.partners-box h3 { margin: 0 0 .25rem; font-size: .95rem; }
.partner-setting { display: flex; align-items: center; gap: .5rem; padding: .3rem 0; font-size: .875rem; }
.partner-setting code { color: #64748b; }
.setting-label { color: #64748b; }
.table-wrap { overflow-x: auto; }
.data-table { width: 100%; border-collapse: collapse; font-size: .85rem; }
.data-table th, .data-table td { text-align: left; padding: .45rem .6rem; border-bottom: 1px solid #e2e8f0; }
.data-table th { color: #64748b; font-weight: 600; white-space: nowrap; }
.row-error { background: #fef2f2; }
.row-skip { background: #fffbeb; }
.action-badge {
  border-radius: 4px; padding: .1rem .45rem; font-size: .75rem; font-weight: 600;
  background: #ecfdf5; color: #047857; white-space: nowrap;
}
.action-error { background: #fef2f2; color: #b91c1c; }
.action-skip { background: #fffbeb; color: #b45309; }
.msg-cell { max-width: 28rem; }
</style>
