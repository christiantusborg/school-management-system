<template>
  <div>
    <div class="mc-head">
      <div>
        <div class="manage-section-title">Module Cohorts</div>
        <p class="mc-sub">Every module runs in intervals — a cohort is one run of one module: schedule, teacher,
          assigned students, teaching materials, grading sheets and QA. Cohort numbers generate from the pattern
          in System Config → Module Cohorts.</p>
      </div>
      <button v-if="!readOnly" type="button" class="btn-primary-sm" @click="openAdd">+ Add cohort</button>
    </div>

    <div v-if="mode === 'admin'" class="mc-filters">
      <label class="mc-lbl-inline">Start from</label>
      <input v-model="filterFrom" type="date" class="mc-inp" style="width:auto" />
      <label class="mc-lbl-inline">to</label>
      <input v-model="filterTo" type="date" class="mc-inp" style="width:auto" />
      <select v-model="filterReport" class="mc-inp" style="width:auto; min-width:250px">
        <option value="">All cohorts</option>
        <option value="missing-doc-qa">Report: missing Document QA date</option>
        <option value="missing-grade-qa">Report: missing Grade-Sheet QA date</option>
      </select>
    </div>

    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="loading" class="loading-row">Loading…</div>

    <table v-else-if="visibleItems.length" class="data-table" style="margin-bottom:.75rem">
      <thead><tr>
        <th>Cohort #</th><th>Module</th><th>Teacher</th><th>Start → End</th><th>Students</th><th>Status</th>
        <th style="width:130px">Actions</th>
      </tr></thead>
      <tbody>
        <tr v-for="c in visibleItems" :key="c.moduleCohortId" class="data-row">
          <td class="mc-name">{{ c.cohortNumber }}</td>
          <td>{{ c.moduleCode }} · {{ c.moduleName }}<br><small class="mc-muted">{{ c.programmeName }}</small></td>
          <td>{{ c.teacherName || '—' }}</td>
          <td>{{ fmtDate(c.startDate) }} → {{ fmtDate(c.endDate) }}</td>
          <td>{{ c.studentCount }}</td>
          <td>
            <span :class="['mc-chip', c.docQaDate ? 'mc-ok' : 'mc-warn']">Doc QA</span>
            <span :class="['mc-chip', c.gradeQaDate ? 'mc-ok' : 'mc-warn']">Grade QA</span>
            <span v-if="!c.gradingSheetUploadedDate && c.gradingSheetDueDate && new Date(c.gradingSheetDueDate) < new Date()"
                  class="mc-chip mc-overdue">Grading sheet overdue</span>
          </td>
          <td class="actions-cell">
            <button type="button" class="btn-sm" @click="openDetail(c)">✎ Open</button>
            <button v-if="!readOnly" type="button" class="btn-sm btn-danger" @click="removeCohort(c)">✕</button>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-else-if="!loading" class="mc-sub" style="margin:.5rem 0;">
      {{ items.length ? 'No cohorts match the filter.' : `No cohorts yet${readOnly ? '.' : ' — click “+ Add cohort”.'}` }}</p>

    <!-- Add cohort -->
    <div v-if="addOpen" class="mc-backdrop" @click.self="addOpen = false">
      <div class="mc-dialog" style="width:min(520px,100%)">
        <div class="mc-dialog-head"><h3>New module cohort</h3>
          <button type="button" class="mc-x" @click="addOpen = false">✕</button></div>
        <div class="mc-dialog-body">
          <label class="mc-lbl">Programme *</label>
          <select v-model="addProgrammeId" class="mc-inp" @change="addSubjectId = ''">
            <option value="">— pick a programme —</option>
            <option v-for="p in sources.programmes" :key="p.programmeId" :value="p.programmeId">{{ p.code ? p.code + ' — ' : '' }}{{ p.name }}</option>
          </select>
          <label class="mc-lbl" style="margin-top:.6rem">Module *</label>
          <select v-model="addSubjectId" class="mc-inp">
            <option value="">— pick a module —</option>
            <option v-for="m in modulesFor(addProgrammeId)" :key="m.subjectId" :value="m.subjectId">
              {{ m.code }} — {{ m.name }} ({{ m.specializationName }})</option>
          </select>
          <label class="mc-lbl" style="margin-top:.6rem">Faculty (teacher)</label>
          <select v-model="addTeacherId" class="mc-inp">
            <option value="">—</option>
            <option v-for="t in sources.teachers" :key="t.teacherId" :value="t.teacherId">{{ t.displayName }}</option>
          </select>
          <div class="mc-row2">
            <div><label class="mc-lbl" style="margin-top:.6rem">Start date</label>
              <input v-model="addStart" type="date" class="mc-inp" /></div>
            <div><label class="mc-lbl" style="margin-top:.6rem">End date</label>
              <input v-model="addEnd" type="date" class="mc-inp" /></div>
          </div>
          <div v-if="addError" class="err-banner" style="margin-top:.6rem">{{ addError }}</div>
        </div>
        <div class="mc-dialog-foot">
          <button type="button" class="btn-sm" @click="addOpen = false">Cancel</button>
          <button type="button" class="btn-primary-sm" :disabled="adding || !addProgrammeId || !addSubjectId" @click="addCohort">
            {{ adding ? 'Creating…' : 'Create cohort' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Detail dialog -->
    <div v-if="detOpen" class="mc-backdrop" @click.self="detOpen = false">
      <div class="mc-dialog">
        <div class="mc-dialog-head">
          <h3>{{ det.cohort?.cohortNumber }}<span class="mc-muted"> — module cohort</span></h3>
          <button type="button" class="mc-x" @click="detOpen = false">✕</button>
        </div>
        <div class="mc-tabs">
          <button :class="['mc-tab', { active: detTab === 'record' }]" @click="detTab = 'record'">Record</button>
          <button v-if="!readOnly" :class="['mc-tab', { active: detTab === 'students' }]" @click="detTab = 'students'; loadStudents()">
            Assign students ({{ det.cohort?.studentCount ?? 0 }})</button>
          <button :class="['mc-tab', { active: detTab === 'assignments' }]" @click="detTab = 'assignments'; loadStudents()">
            Uploaded Assignments</button>
          <button :class="['mc-tab', { active: detTab === 'questionnaires' }]" @click="detTab = 'questionnaires'">
            Questionnaires</button>
        </div>
        <div class="mc-dialog-body">
          <template v-if="detTab === 'record'">
            <div class="mc-section">
              <div class="mc-section-title">MODULE COHORT INFORMATION</div>
              <div class="mc-grid2">
                <div><label class="mc-lbl">Cohort (Section) Number</label><div class="mc-system">{{ det.cohort.cohortNumber }}</div></div>
                <div><label class="mc-lbl">Number of Students Enrolled</label><div class="mc-system">{{ det.cohort.studentCount }}</div></div>
                <div><label class="mc-lbl">Programme</label><div class="mc-system">{{ det.cohort.programmeName }}</div></div>
                <div><label class="mc-lbl">Module</label><div class="mc-system">{{ det.cohort.moduleCode }} — {{ det.cohort.moduleName }}</div></div>
                <div>
                  <label class="mc-lbl">Faculty (teacher)</label>
                  <select v-if="!readOnly" v-model="form.teacherId" class="mc-inp">
                    <option value="">—</option>
                    <option v-for="t in sources.teachers" :key="t.teacherId" :value="t.teacherId">{{ t.displayName }}</option>
                  </select>
                  <div v-else class="mc-system">{{ det.cohort.teacherName || '—' }}</div>
                </div>
                <div></div>
                <div><label class="mc-lbl">Start Date of Module</label>
                  <input v-if="!readOnly" v-model="form.startDate" type="date" class="mc-inp" />
                  <div v-else class="mc-system">{{ fmtDate(det.cohort.startDate) }}</div></div>
                <div><label class="mc-lbl">End Date of Module</label>
                  <input v-if="!readOnly" v-model="form.endDate" type="date" class="mc-inp" />
                  <div v-else class="mc-system">{{ fmtDate(det.cohort.endDate) }}</div></div>
              </div>
            </div>

            <div class="mc-section">
              <div class="mc-section-title">Teaching Materials</div>
              <div v-for="f in materialFields" :key="f.id" class="mc-upl">
                <label class="mc-lbl">{{ f.label }} <em class="mc-muted">({{ f.allowMultiple ? 'several documents' : '1 document' }})</em></label>
                <div v-for="file in f.files" :key="file.id" class="mc-file-row">
                  <span>📄 {{ file.fileName }}</span>
                  <button type="button" class="btn-sm" @click="downloadFile(file)">⤓</button>
                  <button v-if="!readOnly" type="button" class="btn-sm btn-danger" @click="deleteFile(f, file)">✕</button>
                </div>
                <input v-if="!readOnly" type="file" :multiple="f.allowMultiple" @change="uploadFiles(f, $event)" />
              </div>
            </div>

            <div class="mc-section">
              <div class="mc-section-title">Document Quality Check</div>
              <div class="mc-grid2">
                <div><label class="mc-lbl">QA Check if files are uploaded</label>
                  <select v-if="!readOnly" v-model="form.docQaChecked" class="mc-inp"><option :value="true">Yes</option><option :value="false">No</option></select>
                  <div v-else class="mc-system">{{ det.cohort.docQaChecked ? 'Yes' : 'No' }}</div></div>
                <div><label class="mc-lbl">Date of Document QA Check</label>
                  <input v-if="!readOnly" v-model="form.docQaDate" type="date" class="mc-inp" />
                  <div v-else class="mc-system">{{ fmtDate(det.cohort.docQaDate) }}</div></div>
              </div>
            </div>

            <div class="mc-section">
              <div class="mc-section-title">Grading Sheet(s)</div>
              <div v-for="f in gradingFields" :key="f.id" class="mc-upl">
                <label class="mc-lbl">{{ f.label }} <em class="mc-muted">({{ f.allowMultiple ? 'several documents' : '1 document' }})</em></label>
                <div v-for="file in f.files" :key="file.id" class="mc-file-row">
                  <span>📄 {{ file.fileName }}</span>
                  <button type="button" class="btn-sm" @click="downloadFile(file)">⤓</button>
                  <button v-if="!readOnly" type="button" class="btn-sm btn-danger" @click="deleteFile(f, file)">✕</button>
                </div>
                <input v-if="!readOnly" type="file" :multiple="f.allowMultiple" @change="uploadFiles(f, $event)" />
              </div>
              <div class="mc-grid2" style="margin-top:.6rem">
                <div>
                  <label class="mc-lbl">Upload Grading Sheet Before <em class="mc-muted">(auto: end + 1 month)</em></label>
                  <template v-if="!readOnly">
                    <input v-model="form.gradingSheetDueOverride" type="date" class="mc-inp" style="max-width:200px" />
                    <button v-if="det.cohort.gradingSheetDueIsOverride || form.gradingSheetDueOverride" type="button" class="btn-sm" title="Back to automatic (end + 1 month)"
                            @click="form.gradingSheetDueOverride = ''; form.clearDueOverride = true">↺ auto</button>
                  </template>
                  <div v-else class="mc-system">{{ fmtDate(det.cohort.gradingSheetDueDate) }}</div>
                </div>
                <div><label class="mc-lbl">Date Grading Sheet Uploaded</label>
                  <input v-if="!readOnly" v-model="form.gradingSheetUploadedDate" type="date" class="mc-inp" />
                  <div v-else class="mc-system">{{ fmtDate(det.cohort.gradingSheetUploadedDate) }}</div></div>
              </div>
              <p class="mc-sub" style="margin:.4rem 0 0">Reminders email the teacher 2 weeks before, 1 week before and 1 week after the due date while the uploaded date is blank.</p>
            </div>

            <div class="mc-section">
              <div class="mc-section-title">Grade Sheet Quality Check</div>
              <div class="mc-grid2">
                <div><label class="mc-lbl">QA Check if Grading Sheets Uploaded</label>
                  <select v-if="!readOnly" v-model="form.gradeQaChecked" class="mc-inp"><option :value="true">Yes</option><option :value="false">No</option></select>
                  <div v-else class="mc-system">{{ det.cohort.gradeQaChecked ? 'Yes' : 'No' }}</div></div>
                <div><label class="mc-lbl">Date of Grade Sheet QA Check</label>
                  <input v-if="!readOnly" v-model="form.gradeQaDate" type="date" class="mc-inp" />
                  <div v-else class="mc-system">{{ fmtDate(det.cohort.gradeQaDate) }}</div></div>
              </div>
            </div>
          </template>

          <template v-else-if="detTab === 'assignments'">
            <p class="mc-sub">Assignment uploads and comment chat for this cohort's module
              ({{ det.cohort.moduleCode }}), per assigned student.</p>
            <div v-if="studentsLoading" class="loading-row">Loading…</div>
            <template v-else-if="assignedStudents.length">
              <div v-for="s in assignedStudents" :key="s.enrollmentId" class="mc-section" style="padding:.5rem .8rem;">
                <button type="button" class="mc-asg-stu" @click="asgOpen[s.enrollmentId] = !asgOpen[s.enrollmentId]">
                  {{ asgOpen[s.enrollmentId] ? '▾' : '▸' }} {{ s.firstName }} {{ s.lastName }}
                  <span class="mc-muted">· {{ s.studentNumber }}</span>
                </button>
                <AssignmentsPanel v-if="asgOpen[s.enrollmentId]"
                  :api-base="assignmentsBase(s)"
                  :subject-id="det.cohort.subjectId" />
              </div>
            </template>
            <p v-else class="mc-sub">No students assigned to this cohort yet.</p>
          </template>

          <template v-else-if="detTab === 'questionnaires'">
            <p class="mc-sub" v-if="mode === 'admin'">
              Questionnaires students must fill out before they can see this cohort's grade.
              Results are anonymous; partner staff and teachers see them only once 3+ responses are in.
            </p>
            <p class="mc-sub" v-else>
              Anonymous questionnaire results for this cohort. Each questionnaire unlocks once at
              least 3 students have responded.
            </p>
            <CohortQuestionnairesPanel :key="det.cohort.moduleCohortId"
              :mode="mode" :cohort-id="det.cohort.moduleCohortId" />
          </template>

          <template v-else>
            <p class="mc-sub">Admitted / active students enrolled in {{ det.cohort.programmeName }} at this partner.
              Tick to assign to this cohort.</p>
            <div v-if="studentsLoading" class="loading-row">Loading…</div>
            <table v-else-if="students.length" class="data-table">
              <thead><tr><th></th><th>Student</th><th>Student #</th><th>Status</th></tr></thead>
              <tbody>
                <tr v-for="s in students" :key="s.enrollmentId">
                  <td><input type="checkbox" v-model="s.assigned" /></td>
                  <td>{{ s.firstName }} {{ s.lastName }}</td>
                  <td>{{ s.studentNumber }}</td>
                  <td>{{ s.statusName }}</td>
                </tr>
              </tbody>
            </table>
            <p v-else class="mc-sub">No admitted students in this programme yet.</p>
          </template>

          <div v-if="detError" class="err-banner" style="margin-top:.6rem">{{ detError }}</div>
        </div>
        <div class="mc-dialog-foot">
          <button type="button" class="btn-sm" @click="detOpen = false">Close</button>
          <button v-if="!readOnly" type="button" class="btn-primary-sm" :disabled="savingDet || uploading" @click="saveDetail">
            {{ savingDet ? 'Saving…' : 'Save' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, watch } from 'vue'
import api from '../../api/client.js'
import { auth } from '../../store/auth.js'
import AssignmentsPanel from '../assignments/AssignmentsPanel.vue'
import CohortQuestionnairesPanel from './CohortQuestionnairesPanel.vue'

const props = defineProps({
  // 'admin' (MGW admin drawer, needs partnerId) or 'partner' (partner portal).
  mode: { type: String, default: 'admin' },
  partnerId: { type: String, default: '' },
})

const P = computed(() => props.mode === 'admin'
  ? {
      list: `/v1/admin/partners/${props.partnerId}/cohorts`,
      sources: `/v1/admin/partners/${props.partnerId}/cohort-sources`,
      item: id => `/v1/admin/cohorts/${id}`,
      students: id => `/v1/admin/cohorts/${id}/students`,
      files: id => `/v1/admin/cohorts/${id}/files`,
      file: id => `/v1/admin/cohort-files/${id}`,
    }
  : {
      list: '/v1/partner/my/cohorts',
      sources: '/v1/partner/my/cohort-sources',
      item: id => `/v1/partner/my/cohorts/${id}`,
      students: id => `/v1/partner/my/cohorts/${id}/students`,
      files: id => `/v1/partner/my/cohorts/${id}/files`,
      file: id => `/v1/partner/my/cohort-files/${id}`,
    })

// Teacher partner-users are read-only.
const readOnly = computed(() => props.mode === 'partner' && !!auth.user?.isTeacher)

const items = ref([])
const sources = ref({ programmes: [], modules: [], teachers: [] })
const loading = ref(false)
const error = ref('')

const addOpen = ref(false)
const addProgrammeId = ref('')
const addSubjectId = ref('')
const addTeacherId = ref('')
const addStart = ref('')
const addEnd = ref('')
const adding = ref(false)
const addError = ref('')

const detOpen = ref(false)
const detTab = ref('record')
const det = ref({ cohort: null, uploadFields: [] })
const form = ref({})
const detError = ref('')
const savingDet = ref(false)
const uploading = ref(false)

const students = ref([])
const studentsLoading = ref(false)
// Uploaded Assignments tab: per-student fold-outs within this cohort.
const asgOpen = reactive({})
const assignedStudents = computed(() => students.value.filter(s => s.assigned))
function assignmentsBase(s) {
  return props.mode === 'admin'
    ? `/v1/admin/students/${s.studentId}/enrollments/${s.enrollmentId}/assignments`
    : `/v1/partner/my-students/${s.studentId}/enrollments/${s.enrollmentId}/assignments`
}

// Admin-side QA report filters (client-side, by module start-date range).
const filterFrom = ref('')
const filterTo = ref('')
const filterReport = ref('')
const visibleItems = computed(() => items.value.filter(c => {
  if (filterFrom.value && (!c.startDate || c.startDate.slice(0, 10) < filterFrom.value)) return false
  if (filterTo.value && (!c.startDate || c.startDate.slice(0, 10) > filterTo.value)) return false
  if (filterReport.value === 'missing-doc-qa' && c.docQaDate) return false
  if (filterReport.value === 'missing-grade-qa' && c.gradeQaDate) return false
  return true
}))

const materialFields = computed(() => det.value.uploadFields.filter(f => !f.isGradingSheet))
const gradingFields = computed(() => det.value.uploadFields.filter(f => f.isGradingSheet))

function modulesFor(programmeId) {
  return sources.value.modules.filter(m => m.programmeId === programmeId)
}
function fmtDate(d) {
  return d ? new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }) : '—'
}
function iso(d) { return d ? String(d).slice(0, 10) : '' }

async function load() {
  if (props.mode === 'admin' && !props.partnerId) return
  loading.value = true
  error.value = ''
  try {
    const [listRes, srcRes] = await Promise.all([api.get(P.value.list), api.get(P.value.sources)])
    items.value = listRes.data.items ?? []
    sources.value = srcRes.data
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load cohorts'
  } finally {
    loading.value = false
  }
}

function openAdd() {
  addProgrammeId.value = ''
  addSubjectId.value = ''
  addTeacherId.value = ''
  addStart.value = ''
  addEnd.value = ''
  addError.value = ''
  addOpen.value = true
}

async function addCohort() {
  if (adding.value || !addProgrammeId.value || !addSubjectId.value) return
  adding.value = true
  addError.value = ''
  try {
    const res = await api.post(P.value.list, {
      programmeId: addProgrammeId.value,
      subjectId: addSubjectId.value,
      teacherId: addTeacherId.value || null,
      startDate: addStart.value || null,
      endDate: addEnd.value || null,
    })
    addOpen.value = false
    await load()
    const c = items.value.find(x => x.moduleCohortId === res.data.moduleCohortId)
    if (c) await openDetail(c)
  } catch (e) {
    addError.value = e.response?.data?.error ?? e.message ?? 'Failed to create cohort'
  } finally {
    adding.value = false
  }
}

async function removeCohort(c) {
  if (!confirm(`Remove cohort ${c.cohortNumber}?`)) return
  error.value = ''
  try {
    await api.delete(P.value.item(c.moduleCohortId))
    await load()
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Remove failed'
  }
}

async function openDetail(c) {
  detError.value = ''
  detTab.value = 'record'
  try {
    const res = await api.get(P.value.item(c.moduleCohortId))
    det.value = res.data
    const k = res.data.cohort
    form.value = {
      teacherId: k.teacherId ?? '',
      startDate: iso(k.startDate),
      endDate: iso(k.endDate),
      gradingSheetDueOverride: k.gradingSheetDueIsOverride ? iso(k.gradingSheetDueDate) : '',
      clearDueOverride: false,
      gradingSheetUploadedDate: iso(k.gradingSheetUploadedDate),
      docQaChecked: !!k.docQaChecked,
      docQaDate: iso(k.docQaDate),
      gradeQaChecked: !!k.gradeQaChecked,
      gradeQaDate: iso(k.gradeQaDate),
    }
    detOpen.value = true
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to open cohort'
  }
}

async function saveDetail() {
  if (savingDet.value) return
  savingDet.value = true
  detError.value = ''
  try {
    await api.put(P.value.item(det.value.cohort.moduleCohortId), {
      teacherId: form.value.teacherId || null,
      startDate: form.value.startDate || null,
      endDate: form.value.endDate || null,
      gradingSheetDueOverride: form.value.gradingSheetDueOverride || null,
      clearDueOverride: !!form.value.clearDueOverride && !form.value.gradingSheetDueOverride,
      gradingSheetUploadedDate: form.value.gradingSheetUploadedDate || null,
      docQaChecked: form.value.docQaChecked,
      docQaDate: form.value.docQaDate || null,
      gradeQaChecked: form.value.gradeQaChecked,
      gradeQaDate: form.value.gradeQaDate || null,
    })
    if (detTab.value === 'students' || students.value.length) {
      await api.put(P.value.students(det.value.cohort.moduleCohortId), {
        enrollmentIds: students.value.filter(s => s.assigned).map(s => s.enrollmentId),
      })
    }
    detOpen.value = false
    await load()
  } catch (e) {
    detError.value = e.response?.data?.error ?? e.message ?? 'Save failed'
  } finally {
    savingDet.value = false
  }
}

async function loadStudents() {
  studentsLoading.value = true
  detError.value = ''
  try {
    const res = await api.get(P.value.students(det.value.cohort.moduleCohortId))
    students.value = res.data.students ?? []
  } catch (e) {
    detError.value = e.response?.data?.error ?? e.message ?? 'Failed to load students'
  } finally {
    studentsLoading.value = false
  }
}

async function uploadFiles(field, ev) {
  const files = [...(ev.target.files ?? [])]
  if (!files.length) return
  uploading.value = true
  detError.value = ''
  try {
    const fd = new FormData()
    for (const f of files) fd.append('files', f)
    await api.post(`${P.value.files(det.value.cohort.moduleCohortId)}?fieldId=${field.id}`, fd,
      { headers: { 'Content-Type': 'multipart/form-data' } })
    const res = await api.get(P.value.item(det.value.cohort.moduleCohortId))
    det.value = res.data
    if (!form.value.gradingSheetUploadedDate && res.data.cohort.gradingSheetUploadedDate)
      form.value.gradingSheetUploadedDate = iso(res.data.cohort.gradingSheetUploadedDate)
  } catch (e) {
    detError.value = e.response?.data?.error ?? e.message ?? 'Upload failed'
  } finally {
    uploading.value = false
    ev.target.value = ''
  }
}

async function deleteFile(field, file) {
  if (!confirm(`Delete "${file.fileName}"?`)) return
  detError.value = ''
  try {
    await api.delete(P.value.file(file.id))
    field.files = field.files.filter(x => x.id !== file.id)
  } catch (e) {
    detError.value = e.response?.data?.error ?? e.message ?? 'Delete failed'
  }
}

async function downloadFile(file) {
  try {
    const res = await api.get(`${P.value.file(file.id)}/file`, { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url
    a.download = file.fileName
    a.click()
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch {
    detError.value = 'Download failed'
  }
}

watch(() => props.partnerId, load, { immediate: true })
</script>

<style scoped>
.mc-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; margin-bottom: .4rem; }
.mc-filters { display: flex; align-items: center; gap: .5rem; flex-wrap: wrap; margin-bottom: .6rem; }
.mc-lbl-inline { font-size: .76rem; color: #44536a; font-weight: 600; }
.mc-sub { font-size: .78rem; color: #6b7888; margin: .15rem 0 .5rem; }
.mc-name { font-weight: 600; color: #1a2d4f; white-space: nowrap; }
.mc-muted { color: #8a97a8; font-weight: 400; }
.manage-section-title { font-size: .95rem; font-weight: 700; color: #003366; }
.data-table { width: 100%; border-collapse: collapse; font-size: .85rem; }
.data-table th { text-align: left; padding: .45rem .6rem; color: #6b7888; font-size: .75rem; text-transform: uppercase; letter-spacing: .03em; border-bottom: 1.5px solid #e8edf4; }
.data-table td { padding: .45rem .6rem; border-bottom: 1px solid #eef1f5; vertical-align: middle; }
.actions-cell { white-space: nowrap; }
.mc-chip { display: inline-block; border-radius: 10px; padding: .05rem .5rem; font-size: .68rem; font-weight: 700; margin-right: .25rem; }
.mc-ok { background: #e6f6ec; color: #1c7a4a; border: 1px solid #b9e1c7; }
.mc-warn { background: #fff4e6; color: #b66a00; border: 1px solid #f0d2a8; }
.mc-overdue { background: #fde7e5; color: #a8241e; border: 1px solid #e8b3af; }
.btn-sm { background: #f2f5f9; border: 1px solid #cfd7e3; border-radius: 5px; padding: .28rem .6rem; font-size: .78rem; font-weight: 600; color: #2c3e50; cursor: pointer; margin-right: .25rem; }
.btn-sm:hover { background: #e8eef6; }
.btn-danger { color: #b3261e; border-color: #e2b8b5; background: #fdf3f2; }
.btn-primary-sm { background: #003366; color: #fff; border: none; border-radius: 5px; padding: .35rem .8rem; font-size: .8rem; font-weight: 600; cursor: pointer; }
.btn-primary-sm:disabled { opacity: .5; cursor: default; }
.err-banner { background: #fdf3f2; border: 1px solid #e2b8b5; color: #b3261e; padding: .45rem .7rem; border-radius: 6px; font-size: .8rem; margin-bottom: .5rem; }
.loading-row { color: #6b7888; font-size: .85rem; padding: .5rem 0; }
.mc-backdrop { position: fixed; inset: 0; background: rgba(20,30,50,.55); z-index: 1200; display: flex; align-items: center; justify-content: center; padding: 1rem; }
.mc-dialog { background: #fff; border-radius: 8px; width: min(920px, 100%); max-height: 92vh; display: flex; flex-direction: column; box-shadow: 0 10px 40px rgba(0,0,0,.2); }
.mc-dialog-head { display: flex; justify-content: space-between; align-items: center; padding: .85rem 1.1rem; border-bottom: 1px solid #e6ebf2; }
.mc-dialog-head h3 { margin: 0; font-size: 1rem; color: #1a2d4f; }
.mc-x { background: none; border: none; font-size: 1rem; cursor: pointer; color: #6b7888; }
.mc-tabs { display: flex; gap: 0; border-bottom: 1.5px solid #e8edf4; padding: 0 1.1rem; }
.mc-tab { background: none; border: none; padding: .5rem .9rem; font-size: .85rem; font-weight: 600; color: #5f6e85; cursor: pointer; border-bottom: 2.5px solid transparent; margin-bottom: -1.5px; }
.mc-tab.active { color: #0a264f; border-bottom-color: #0a264f; }
.mc-dialog-body { padding: 1rem 1.1rem; overflow-y: auto; }
.mc-dialog-foot { display: flex; justify-content: flex-end; gap: .5rem; padding: .75rem 1.1rem; border-top: 1px solid #e6ebf2; }
.mc-lbl { display: block; font-size: .75rem; font-weight: 700; color: #44536a; margin-bottom: .25rem; }
.mc-inp { padding: .4rem .55rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .85rem; width: 100%; background: #fff; }
.mc-row2, .mc-grid2 { display: grid; grid-template-columns: 1fr 1fr; gap: .6rem .9rem; }
.mc-section { border: 1px solid #e2e8f0; border-radius: 7px; padding: .7rem .8rem; margin-bottom: .85rem; background: #fafbfd; }
.mc-section-title { font-weight: 700; color: #003366; font-size: .85rem; margin-bottom: .5rem; }
.mc-system { padding: .4rem .55rem; background: #f2f5f9; border: 1px dashed #cfd7e3; border-radius: 5px; font-size: .82rem; color: #44536a; min-height: 1.9rem; }
.mc-upl { margin-bottom: .6rem; }
.mc-asg-stu { background: none; border: none; font-size: .88rem; font-weight: 700; color: #1a2d4f; cursor: pointer; padding: .2rem 0; }
.mc-file-row { display: flex; align-items: center; gap: .4rem; font-size: .82rem; margin: .2rem 0; }
</style>
