<template>
  <!-- Optional signup-wizard survey, driven by SignupWizard intake instances.
       Renders nothing at all when no active survey forms exist. -->
  <div v-if="items.length" class="svy-wrap">
    <h3 class="svy-title">Survey <span class="svy-optional">(optional)</span></h3>
    <p class="svy-hint">Help us improve — your application does not depend on these answers.</p>

    <v-app class="svy-vapp">
      <v-main>
        <div v-for="f in items" :key="f.intakeInstanceId" class="svy-card">
          <div class="svy-card-head">
            <strong>{{ f.name }}</strong>
            <span v-if="f.response?.lifecycleState === 'Submitted'" class="svy-chip-done">✓ Submitted</span>
          </div>
          <template v-if="f.response?.lifecycleState !== 'Submitted'">
            <QuestionnaireRenderer
              :questionnaire="parsed(f)"
              :answers="answersFor(f)"
              mode="live"
              @change="(id, val) => onChange(f, id, val)"
              @submit="() => submit(f)"
            />
          </template>
        </div>
      </v-main>
    </v-app>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '../../api/client.js'
import QuestionnaireRenderer from '../questionnaire/QuestionnaireRenderer.vue'

const props = defineProps({ token: { type: String, required: true } })

const items = ref([])
const localAnswers = ref({})

function headers() { return { 'X-Wizard-Token': props.token } }

async function load() {
  if (!props.token) return
  try {
    const res = await api.get('/v1/public/draft-signup/survey', { headers: headers() })
    items.value = res.data.items ?? []
  } catch { items.value = [] }
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

let draftTimer = null
function onChange(f, fieldId, value) {
  answersFor(f)[fieldId] = value
  // Debounced draft save so a half-finished survey survives a reload.
  clearTimeout(draftTimer)
  draftTimer = setTimeout(() => saveDraft(f), 1200)
}

async function saveDraft(f) {
  try {
    await api.patch(`/v1/public/draft-signup/survey/${f.intakeInstanceId}`,
      { answersJson: JSON.stringify(answersFor(f)), submit: false }, { headers: headers() })
  } catch { /* drafts are best-effort */ }
}

async function submit(f) {
  try {
    await api.patch(`/v1/public/draft-signup/survey/${f.intakeInstanceId}`,
      { answersJson: JSON.stringify(answersFor(f)), submit: true }, { headers: headers() })
    await load()
  } catch { /* keep the wizard usable even if the survey backend hiccups */ }
}

onMounted(load)
defineExpose({ reload: load })
</script>

<style scoped>
.svy-wrap { margin: 1rem 0; }
.svy-title { font-size: 1rem; color: #0b2e59; margin: 0 0 .15rem; }
.svy-optional { font-weight: 400; color: #888; font-size: .82rem; }
.svy-hint { font-size: .8rem; color: #777; margin: 0 0 .6rem; }
.svy-vapp { background: transparent; }
.svy-vapp :deep(.v-application__wrap) { min-height: 0; }
.svy-card { background: #fff; border: 1px solid #e3e9f1; border-radius: 9px; padding: .8rem 1rem; margin-bottom: .7rem; }
.svy-card-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: .4rem; }
.svy-chip-done { font-size: .74rem; background: #dcfce7; color: #15803d; padding: .12rem .5rem; border-radius: 10px; }
</style>
