<template>
  <div class="modal-backdrop" @click.self="emit('close')">
    <div class="modal">
      <div class="modal-head">
        <strong>Add additional document</strong>
        <button class="btn-x" @click="emit('close')">✕</button>
      </div>
      <div class="modal-body">
        <p class="hint">
          Add a supplementary document to this application. Existing
          approved documents stay unchanged.
        </p>

        <label class="field">
          <span>Document kind</span>
          <select v-model="form.documentTypeId" :disabled="busy || loading">
            <option value="">— Select kind —</option>
            <option v-for="t in types" :key="t.documentTypeId" :value="t.documentTypeId">
              {{ t.name }}
            </option>
          </select>
        </label>

        <label class="field">
          <span>File</span>
          <input
            type="file"
            :accept="ACCEPTED_DOC_ACCEPT_ATTR"
            :disabled="busy"
            @change="onFile" />
        </label>
        <p class="file-name" v-if="form.file">{{ form.file.name }}</p>
        <p class="hint">Accepted: {{ humanReadable }}, up to 10 MB.</p>

        <p class="error" v-if="error">{{ error }}</p>
      </div>
      <div class="modal-foot">
        <button class="btn" @click="emit('close')" :disabled="busy">Cancel</button>
        <button
          class="btn btn-primary"
          :disabled="!canSubmit"
          @click="submit">
          {{ busy ? 'Uploading…' : 'Add document' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import api from '../../api/client.js'
import {
  ACCEPTED_DOC_ACCEPT_ATTR,
  ACCEPTED_DOC_HUMAN,
} from '../../utils/uploadPolicy.js'

const props = defineProps({
  // GET endpoint returning { items: [{ documentTypeId, name }] }
  typesEndpoint: { type: String, required: true },
  // POST endpoint expecting multipart form: documentTypeId, isAdditional, file
  uploadEndpoint: { type: String, required: true },
  // Only required by the student endpoint, which takes enrollmentId in the form
  // body rather than the URL. Admin/partner pass null.
  enrollmentId: { type: String, default: null },
})

const emit = defineEmits(['close', 'uploaded'])

const types = ref([])
const loading = ref(true)
const busy = ref(false)
const error = ref('')
const form = reactive({ documentTypeId: '', file: null })

const humanReadable = ACCEPTED_DOC_HUMAN

const canSubmit = computed(
  () => !!form.documentTypeId && !!form.file && !busy.value
)

function onFile(ev) {
  const f = ev.target.files?.[0]
  form.file = f || null
}

async function submit() {
  if (!canSubmit.value) return
  busy.value = true
  error.value = ''
  try {
    const body = new FormData()
    body.append('documentTypeId', form.documentTypeId)
    body.append('isAdditional', 'true')
    if (props.enrollmentId) body.append('enrollmentId', props.enrollmentId)
    body.append('file', form.file)
    const res = await api.post(props.uploadEndpoint, body)
    emit('uploaded', res?.data ?? null)
    emit('close')
  } catch (e) {
    error.value =
      e?.response?.data?.error ||
      e?.message ||
      'Upload failed. Try again.'
  } finally {
    busy.value = false
  }
}

onMounted(async () => {
  try {
    const res = await api.get(props.typesEndpoint)
    types.value = res?.data?.items ?? []
  } catch (e) {
    error.value = 'Could not load document kinds.'
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.modal-backdrop {
  position: fixed; inset: 0;
  background: rgba(0,0,0,.45);
  display: flex; align-items: center; justify-content: center;
  z-index: 200;
}
.modal {
  background: #fff;
  border-radius: 10px;
  width: min(480px, 92vw);
  box-shadow: 0 10px 32px rgba(0,0,0,.2);
  display: flex; flex-direction: column;
}
.modal-head, .modal-foot {
  display: flex; align-items: center; justify-content: space-between;
  padding: 14px 18px;
  border-bottom: 1px solid #eee;
}
.modal-foot {
  border-bottom: 0;
  border-top: 1px solid #eee;
  gap: 8px;
  justify-content: flex-end;
}
.modal-body { padding: 18px; display: flex; flex-direction: column; gap: 12px; }
.hint { font-size: 12px; color: #666; margin: 0; }
.field { display: flex; flex-direction: column; gap: 4px; font-size: 13px; color: #333; }
.field select, .field input[type=file] {
  border: 1px solid #ccc;
  border-radius: 6px;
  padding: 8px;
  background: #fff;
}
.file-name { font-size: 12px; color: #444; margin: -4px 0 0 0; }
.error { color: #b91c1c; font-size: 13px; margin: 0; }
.btn {
  padding: 8px 14px;
  border-radius: 6px;
  border: 1px solid #ccc;
  background: #fff;
  cursor: pointer;
}
.btn:disabled { opacity: .5; cursor: not-allowed; }
.btn-primary { background: #2563eb; color: #fff; border-color: #2563eb; }
.btn-x {
  border: 0; background: transparent; cursor: pointer; font-size: 18px;
  color: #666;
}
</style>
