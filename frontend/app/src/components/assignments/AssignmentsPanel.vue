<template>
  <div class="asg-panel">
    <div v-if="error" class="asg-err">{{ error }}</div>
    <div v-if="loading" class="asg-muted">Loading…</div>

    <p v-else-if="!modules.length" class="asg-muted">No modules on this enrolment yet.</p>

    <div v-for="m in modules" :key="m.subjectId" class="asg-module">
      <button class="asg-module-head" @click="toggle(m.subjectId)">
        <span class="asg-caret">{{ open[m.subjectId] ? '▾' : '▸' }}</span>
        <span class="asg-code">{{ m.code }}</span>
        <span class="asg-name">{{ m.name }}</span>
        <span v-if="m.cohortNumber" class="asg-cohort">{{ m.cohortNumber }}</span>
        <span class="asg-count" :class="{ 'asg-count-zero': !m.uploads.length }">{{ m.uploads.length }}</span>
      </button>

      <div v-if="open[m.subjectId]" class="asg-module-body">
        <p v-if="!m.uploads.length" class="asg-muted" style="margin:.2rem 0 .4rem;">
          {{ canUpload ? 'No assignments uploaded for this module yet.' : 'The student has not uploaded any assignments for this module yet.' }}
        </p>

        <div v-for="u in m.uploads" :key="u.assignmentUploadId" class="asg-upload">
          <div class="asg-upload-head">
            <span class="asg-title">{{ u.title }}</span>
            <button class="asg-btn" :disabled="downloadingId === u.assignmentUploadId" @click="download(u)">
              {{ downloadingId === u.assignmentUploadId ? '…' : '⤓ Download' }}
            </button>
          </div>
          <div class="asg-upload-sub">
            {{ u.fileName }} · uploaded by {{ u.uploadedByName || u.uploadedByRole }}
            <span class="asg-role-pill">{{ u.uploadedByRole }}</span> · {{ fmtDate(u.uploadedAt) }}
          </div>

          <!-- Comment chat -->
          <div class="asg-chat">
            <div v-for="c in u.comments" :key="c.assignmentCommentId"
                 :class="['asg-msg', msgSide(c.authorRole)]">
              <div class="asg-msg-meta">
                <strong>{{ c.authorName || c.authorRole }}</strong>
                <span class="asg-role-pill">{{ c.authorRole }}</span>
                <span class="asg-msg-time">{{ fmtDate(c.createdAt) }}</span>
              </div>
              <div class="asg-msg-text">{{ c.text }}</div>
            </div>
            <div v-if="canComment" class="asg-chat-add">
              <input v-model="drafts[u.assignmentUploadId]" type="text" maxlength="4000"
                     placeholder="Write a comment…" @keyup.enter="sendComment(u)" />
              <button class="asg-btn" :disabled="!(drafts[u.assignmentUploadId] || '').trim() || sendingId === u.assignmentUploadId"
                      @click="sendComment(u)">
                {{ sendingId === u.assignmentUploadId ? '…' : 'Send' }}
              </button>
            </div>
          </div>
        </div>

        <!-- Upload into this module -->
        <div v-if="canUpload" class="asg-add">
          <input v-model="newTitle[m.subjectId]" type="text" maxlength="300"
                 placeholder="Document title (required)" class="asg-add-title" />
          <input type="file" :accept="ACCEPTED_DOC_ACCEPT_ATTR" class="asg-add-file"
                 @change="onFilePick(m.subjectId, $event)" />
          <button class="asg-btn asg-btn-primary"
                  :disabled="!(newTitle[m.subjectId] || '').trim() || !newFile[m.subjectId] || uploadingSubject === m.subjectId"
                  @click="upload(m)">
            {{ uploadingSubject === m.subjectId ? 'Uploading…' : '⇧ Upload assignment' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, watch } from 'vue'
import api from '../../api/client.js'
import { ACCEPTED_DOC_ACCEPT_ATTR } from '../../utils/uploadPolicy.js'

const props = defineProps({
  // e.g. /v1/admin/students/{sid}/enrollments/{eid}/assignments
  apiBase: { type: String, required: true },
  canUpload: { type: Boolean, default: true },
  canComment: { type: Boolean, default: true },
  // When set, only this module is shown (cohort context: one module per cohort).
  subjectId: { type: String, default: '' },
})

const allModules = ref([])
const modules = computed(() =>
  props.subjectId ? allModules.value.filter(m => m.subjectId === props.subjectId) : allModules.value)
const loading = ref(false)
const error = ref('')
const open = reactive({})
const drafts = reactive({})
const newTitle = reactive({})
const newFile = reactive({})
const downloadingId = ref('')
const sendingId = ref('')
const uploadingSubject = ref('')

function fmtDate(v) {
  if (!v) return ''
  try { return new Date(v).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' }) } catch { return v }
}
// Student messages left, staff (teacher/partner/admission) right — chat feel.
function msgSide(role) { return role === 'Student' ? 'asg-msg-left' : 'asg-msg-right' }
function toggle(id) { open[id] = !open[id] }

async function load() {
  if (!props.apiBase) return
  loading.value = true
  error.value = ''
  try {
    const res = await api.get(props.apiBase)
    allModules.value = res.data.modules ?? []
    // Auto-open modules that already have uploads.
    for (const m of modules.value) if (m.uploads.length && open[m.subjectId] === undefined) open[m.subjectId] = true
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load assignments'
  } finally {
    loading.value = false
  }
}

function onFilePick(subjectId, ev) {
  newFile[subjectId] = ev.target.files?.[0] ?? null
}

async function upload(m) {
  const title = (newTitle[m.subjectId] || '').trim()
  const file = newFile[m.subjectId]
  if (!title || !file || uploadingSubject.value) return
  uploadingSubject.value = m.subjectId
  error.value = ''
  try {
    const fd = new FormData()
    fd.append('subjectId', m.subjectId)
    fd.append('title', title)
    fd.append('file', file)
    await api.post(props.apiBase, fd, { headers: { 'Content-Type': 'multipart/form-data' } })
    newTitle[m.subjectId] = ''
    newFile[m.subjectId] = null
    await load()
    open[m.subjectId] = true
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Upload failed'
  } finally {
    uploadingSubject.value = ''
  }
}

async function download(u) {
  if (downloadingId.value) return
  downloadingId.value = u.assignmentUploadId
  error.value = ''
  try {
    const res = await api.get(`${props.apiBase}/${u.assignmentUploadId}/file`, { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url; a.download = u.fileName; a.target = '_blank'
    document.body.appendChild(a); a.click(); document.body.removeChild(a)
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch {
    error.value = 'Download failed'
  } finally {
    downloadingId.value = ''
  }
}

async function sendComment(u) {
  const text = (drafts[u.assignmentUploadId] || '').trim()
  if (!text || sendingId.value) return
  sendingId.value = u.assignmentUploadId
  error.value = ''
  try {
    const res = await api.post(`${props.apiBase}/${u.assignmentUploadId}/comments`, { text })
    u.comments.push(res.data)
    drafts[u.assignmentUploadId] = ''
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Comment failed'
  } finally {
    sendingId.value = ''
  }
}

watch(() => props.apiBase, load, { immediate: true })
defineExpose({ reload: load })
</script>

<style scoped>
.asg-panel { font-size: .88rem; }
.asg-err { background: #fde7e5; color: #a8241e; padding: .5rem .75rem; border-radius: 6px; margin-bottom: .5rem; font-size: .82rem; }
.asg-muted { color: #6b7888; font-size: .82rem; }
.asg-module { border: 1px solid #e3e9f1; border-radius: 8px; margin-bottom: .5rem; background: #fff; }
.asg-module-head { display: flex; align-items: center; gap: .55rem; width: 100%; background: #f7f9fc; border: none; padding: .55rem .75rem; cursor: pointer; font-size: .86rem; border-radius: 8px; text-align: left; }
.asg-caret { color: #667; width: 1em; }
.asg-code { font-family: monospace; font-weight: 700; color: #003366; }
.asg-cohort { background: #eef3fa; border: 1px solid #d5e0ee; color: #2c3e50; border-radius: 12px; padding: .05rem .55rem; font-size: .72rem; font-weight: 700; }
.asg-name { flex: 1; color: #2c3e50; }
.asg-count { background: #003366; color: #fff; border-radius: 10px; font-size: .7rem; font-weight: 700; padding: .05rem .5rem; }
.asg-count-zero { background: #c3ccd8; }
.asg-module-body { padding: .6rem .75rem .7rem; }
.asg-upload { border: 1px solid #e8edf4; border-radius: 8px; padding: .55rem .7rem; margin-bottom: .55rem; background: #fbfcfe; }
.asg-upload-head { display: flex; align-items: center; gap: .75rem; }
.asg-title { font-weight: 700; color: #1a2d4f; flex: 1; }
.asg-upload-sub { color: #6b7888; font-size: .76rem; margin-top: .15rem; }
.asg-role-pill { background: #e8eef6; color: #1a4d8c; border-radius: 8px; font-size: .66rem; font-weight: 700; padding: 0 .4rem; margin-left: .25rem; }
.asg-btn { background: #f2f5f9; border: 1px solid #cfd7e3; border-radius: 5px; padding: .3rem .65rem; font-size: .78rem; font-weight: 600; color: #2c3e50; cursor: pointer; white-space: nowrap; }
.asg-btn:hover:not(:disabled) { background: #e8eef6; }
.asg-btn:disabled { opacity: .5; cursor: default; }
.asg-btn-primary { background: #003366; border-color: #003366; color: #fff; }
.asg-btn-primary:hover:not(:disabled) { background: #00264d; }
.asg-chat { margin-top: .5rem; border-top: 1px dashed #e3e9f1; padding-top: .5rem; }
.asg-msg { max-width: 85%; margin-bottom: .4rem; padding: .4rem .6rem; border-radius: 10px; }
.asg-msg-left { background: #eef3fb; margin-right: auto; }
.asg-msg-right { background: #e9f7ef; margin-left: auto; }
.asg-msg-meta { font-size: .7rem; color: #4b5a6d; display: flex; align-items: center; gap: .3rem; flex-wrap: wrap; }
.asg-msg-time { color: #97a2b0; }
.asg-msg-text { font-size: .84rem; color: #1f2c3d; white-space: pre-wrap; margin-top: .1rem; }
.asg-chat-add { display: flex; gap: .4rem; margin-top: .35rem; }
.asg-chat-add input { flex: 1; padding: .35rem .55rem; border: 1px solid #ccd5e0; border-radius: 6px; font-size: .82rem; }
.asg-add { display: flex; gap: .45rem; align-items: center; flex-wrap: wrap; border-top: 1px dashed #e3e9f1; padding-top: .55rem; margin-top: .3rem; }
.asg-add-title { flex: 1 1 220px; padding: .35rem .55rem; border: 1px solid #ccd5e0; border-radius: 6px; font-size: .82rem; }
.asg-add-file { font-size: .78rem; max-width: 260px; }
</style>
