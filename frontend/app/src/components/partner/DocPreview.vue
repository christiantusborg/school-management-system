<template>
  <div class="rw-preview">
    <div v-if="state === 'none'" class="rw-preview-empty">
      <div class="rw-preview-icon">📋</div>
      <div><strong>No document uploaded.</strong></div>
      <div class="rw-preview-sub">The applicant has not submitted this document.</div>
    </div>
    <div v-else-if="state === 'loading'" class="rw-preview-empty">
      <div class="rw-preview-icon">⏳</div>
      <div><strong>Loading…</strong></div>
    </div>
    <div v-else-if="state === 'error'" class="rw-preview-empty">
      <div class="rw-preview-icon">⚠</div>
      <div><strong>{{ errorText || 'Could not load file.' }}</strong></div>
      <div v-if="filename" class="rw-preview-file">{{ filename }}</div>
    </div>
    <img v-else-if="state === 'image'" :src="objectUrl" :alt="filename" class="rw-preview-img" />
    <iframe v-else-if="state === 'pdf'" :src="objectUrl" class="rw-preview-pdf" :title="filename"></iframe>
    <div v-else class="rw-preview-empty">
      <div class="rw-preview-icon">⚠</div>
      <div><strong>Preview not supported.</strong></div>
      <div class="rw-preview-sub">Only PNG, JPG, and PDF files can be previewed.</div>
      <div class="rw-preview-file">{{ filename }}</div>
    </div>
  </div>
</template>

<script setup>
import { ref, watch, onBeforeUnmount } from 'vue'
import api from '../../api/client.js'

const props = defineProps({
  filename:   { type: String, default: null },
  studentId:  { type: String, default: null },
  documentId: { type: String, default: null },
  /** 'partner' (default) → /v1/partner/my-students/.../documents/.../file
   *  'admin'             → /v1/admin/students/.../documents/.../file */
  role:       { type: String, default: 'partner' },
})

// state ∈ 'none' | 'loading' | 'image' | 'pdf' | 'unsupported' | 'error'
const state = ref('none')
const objectUrl = ref('')
const errorText = ref('')

function kindFor(filename, mimeType) {
  const ext = (filename || '').split('.').pop()?.toLowerCase()
  if (mimeType?.startsWith('image/') || ['jpg', 'jpeg', 'png', 'gif', 'webp'].includes(ext)) return 'image'
  if (mimeType === 'application/pdf' || ext === 'pdf') return 'pdf'
  return 'unsupported'
}

function release() {
  if (objectUrl.value) {
    URL.revokeObjectURL(objectUrl.value)
    objectUrl.value = ''
  }
}

async function load() {
  release()
  errorText.value = ''
  if (!props.filename) { state.value = 'none'; return }
  if (!props.studentId || !props.documentId) {
    // No backend coordinates — can't fetch. Show "no document" rather than
    // a misleading sample image (the legacy mock behaviour).
    state.value = 'none'
    return
  }
  state.value = 'loading'
  try {
    const url = props.role === 'admin'
      ? `/v1/admin/students/${props.studentId}/documents/${props.documentId}/file`
      : `/v1/partner/my-students/${props.studentId}/documents/${props.documentId}/file`
    const res = await api.get(url, { responseType: 'blob' })
    const blob = res.data
    const k = kindFor(props.filename, blob.type)
    if (k === 'unsupported') {
      state.value = 'unsupported'
      return
    }
    objectUrl.value = URL.createObjectURL(blob)
    state.value = k
  } catch (err) {
    state.value = 'error'
    errorText.value = err.response?.status === 404
      ? 'File not found on the server.'
      : (err.message || 'Failed to load file')
  }
}

watch(() => [props.filename, props.studentId, props.documentId], load, { immediate: true })
onBeforeUnmount(release)
</script>

<style scoped>
.rw-preview { flex: 1; min-height: 420px; background: #f2f5f9; border: 1px solid #e8edf4; border-radius: 7px; padding: 0.75rem; display: flex; align-items: center; justify-content: center; overflow: auto; }
.rw-preview-img { max-width: 100%; max-height: 65vh; border-radius: 5px; background: #fff; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }
.rw-preview-pdf { width: 100%; height: 65vh; border: none; background: #fff; border-radius: 5px; }
.rw-preview-empty { text-align: center; color: #666; padding: 1.5rem; }
.rw-preview-icon { font-size: 2rem; margin-bottom: 0.4rem; }
.rw-preview-sub { font-size: 0.82rem; color: #888; margin-top: 0.3rem; }
.rw-preview-file { margin-top: 0.75rem; font-family: ui-monospace, monospace; font-size: 0.78rem; color: #003366; background: #fff; padding: 0.3rem 0.65rem; border-radius: 4px; border: 1px solid #e8edf4; display: inline-block; }
</style>
