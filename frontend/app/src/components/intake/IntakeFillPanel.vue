<template>
  <!-- Vuetify island: the ported QuestionnaireRenderer needs v-app for
       theme + overlays (select menus, date pickers). -->
  <v-app class="ifp-vapp">
    <v-main>
      <div class="ifp-wrap">
        <p v-if="loading" class="ifp-muted">Loading forms…</p>
        <p v-else-if="error" class="ifp-error">{{ error }}</p>
        <p v-else-if="!items.length" class="ifp-muted">No forms to fill out right now.</p>

        <div v-for="f in items" :key="f.intakeInstanceId" class="ifp-card">
          <div class="ifp-card-head" @click="toggle(f)">
            <div>
              <strong>{{ f.name }}</strong>
              <span v-if="f.response?.lifecycleState === 'Submitted'" class="ifp-chip ifp-chip-done">
                Submitted {{ fmtDate(f.response.submittedAt) }}
              </span>
              <span v-else-if="f.response" class="ifp-chip ifp-chip-draft">Draft saved</span>
              <span v-else class="ifp-chip">Not started</span>
            </div>
            <span class="ifp-toggle">{{ openId === f.intakeInstanceId ? '▾' : '▸' }}</span>
          </div>

          <div v-if="openId === f.intakeInstanceId" class="ifp-card-body">
            <template v-if="f.response?.lifecycleState === 'Submitted'">
              <p class="ifp-muted">This form was submitted and can no longer be changed.</p>
            </template>
            <template v-else>
              <QuestionnaireRenderer
                :questionnaire="parsed(f)"
                :answers="answersFor(f)"
                mode="live"
                @change="(id, val) => onChange(f, id, val)"
                @submit="() => onSubmit(f)"
              />
              <div class="ifp-actions">
                <button class="ifp-btn" :disabled="busy" @click="saveDraft(f)">
                  {{ busy ? 'Saving…' : 'Save draft' }}
                </button>
                <span v-if="notice" class="ifp-notice">{{ notice }}</span>
              </div>
            </template>
          </div>
        </div>
      </div>
    </v-main>
  </v-app>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '../../api/client.js'
import QuestionnaireRenderer from '../questionnaire/QuestionnaireRenderer.vue'

// apiBase: '/v1/student/me/intake-forms' or '/v1/partner/intake-forms'
const props = defineProps({ apiBase: { type: String, required: true } })

const items = ref([])
const loading = ref(false)
const error = ref('')
const busy = ref(false)
const notice = ref('')
const openId = ref(null)
const localAnswers = ref({})   // instanceId -> answers object

async function load() {
  loading.value = true
  error.value = ''
  try {
    const res = await api.get(props.apiBase)
    items.value = res.data?.data?.items ?? []
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load forms'
  } finally {
    loading.value = false
  }
}

function toggle(f) {
  openId.value = openId.value === f.intakeInstanceId ? null : f.intakeInstanceId
}

function parsed(f) {
  try { return JSON.parse(f.definitionJson) } catch { return null }
}

function answersFor(f) {
  if (!localAnswers.value[f.intakeInstanceId]) {
    let saved = {}
    try { saved = f.response?.answersJson ? JSON.parse(f.response.answersJson) : {} } catch { saved = {} }
    localAnswers.value[f.intakeInstanceId] = saved
  }
  return localAnswers.value[f.intakeInstanceId]
}

function onChange(f, fieldId, value) {
  answersFor(f)[fieldId] = value
}

function flash(msg) {
  notice.value = msg
  setTimeout(() => { notice.value = '' }, 2500)
}

async function saveDraft(f) {
  busy.value = true
  try {
    await api.put(`${props.apiBase}/${f.intakeInstanceId}/response`,
      { answersJson: JSON.stringify(answersFor(f)) })
    flash('Draft saved')
    await load()
  } catch (e) {
    flash(e.response?.data?.error ?? 'Save failed')
  } finally {
    busy.value = false
  }
}

async function onSubmit(f) {
  busy.value = true
  try {
    await api.post(`${props.apiBase}/${f.intakeInstanceId}/response/submit`,
      { answersJson: JSON.stringify(answersFor(f)) })
    flash('Submitted, thank you')
    openId.value = null
    await load()
  } catch (e) {
    flash(e.response?.data?.error ?? 'Submit failed')
  } finally {
    busy.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.ifp-vapp { background: transparent; }
.ifp-vapp :deep(.v-application__wrap) { min-height: 0; }
.ifp-wrap { display: flex; flex-direction: column; gap: .75rem; }
.ifp-card { background: #fff; border: 1px solid #e3e9f1; border-radius: 9px; overflow: hidden; }
.ifp-card-head { display: flex; justify-content: space-between; align-items: center; padding: .75rem 1rem; cursor: pointer; }
.ifp-card-head:hover { background: #f7f9fc; }
.ifp-card-body { padding: .5rem 1rem 1rem; border-top: 1px solid #eef1f5; }
.ifp-chip { margin-left: .6rem; font-size: .72rem; background: #eef1f5; color: #555; padding: .12rem .5rem; border-radius: 10px; }
.ifp-chip-done { background: #dcfce7; color: #15803d; }
.ifp-chip-draft { background: #fef9c3; color: #854d0e; }
.ifp-toggle { color: #888; }
.ifp-actions { display: flex; align-items: center; gap: .8rem; margin-top: .5rem; }
.ifp-btn { padding: .45rem 1rem; border: 1.5px solid #ccd5e0; background: #f7f9fb; border-radius: 6px; cursor: pointer; font-size: .85rem; }
.ifp-btn:disabled { opacity: .6; }
.ifp-notice { font-size: .82rem; color: #15803d; }
.ifp-muted { color: #888; font-size: .88rem; }
.ifp-error { color: #b42318; font-size: .88rem; }
</style>
