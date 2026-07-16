<template>
  <!-- Anonymous public form fill (/f/:slug) — plain white, iframe-friendly,
       same approach as the student-verify page. -->
  <div class="pff-wrap">
    <p v-if="loading" class="pff-muted">Loading…</p>
    <p v-else-if="notFound" class="pff-muted">This form is not available.</p>
    <template v-else-if="done">
      <h2 class="pff-title">Thank you</h2>
      <p class="pff-muted">Your answers have been submitted.</p>
    </template>
    <template v-else>
      <h2 class="pff-title">{{ formName }}</h2>
      <p v-if="description" class="pff-desc">{{ description }}</p>
      <v-app class="pff-vapp">
        <v-main>
          <QuestionnaireRenderer
            :questionnaire="definition"
            :answers="answers"
            mode="live"
            @change="(id, val) => { answers[id] = val }"
            @submit="submit"
          />
        </v-main>
      </v-app>
      <p v-if="error" class="pff-error">{{ error }}</p>
    </template>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import api from '../api/client.js'
import QuestionnaireRenderer from '../components/questionnaire/QuestionnaireRenderer.vue'

const route = useRoute()
const loading = ref(true)
const notFound = ref(false)
const done = ref(false)
const error = ref('')
const formName = ref('')
const description = ref('')
const definition = ref(null)
const answers = ref({})

async function load() {
  try {
    const res = await api.get(`/v1/public/forms/${route.params.slug}`)
    formName.value = res.data.name
    description.value = res.data.description ?? ''
    try { definition.value = JSON.parse(res.data.definitionJson) } catch { definition.value = null }
    if (!definition.value) notFound.value = true
  } catch {
    notFound.value = true
  } finally {
    loading.value = false
  }
}

async function submit() {
  error.value = ''
  try {
    await api.post(`/v1/public/forms/${route.params.slug}/submit`, {
      answersJson: JSON.stringify(answers.value),
      // Best-effort respondent identity if the form asked for it.
      respondentEmail: firstMatching(['email', 'e-mail']),
      respondentName: firstMatching(['name', 'full name', 'fullname']),
    })
    done.value = true
  } catch (e) {
    error.value = e.response?.data?.error ?? 'Submission failed — please try again.'
  }
}

function firstMatching(keys) {
  for (const [id, val] of Object.entries(answers.value)) {
    if (typeof val === 'string' && val && keys.some(k => id.toLowerCase().includes(k))) return val
  }
  return null
}

onMounted(load)
</script>

<style scoped>
.pff-wrap { background: #fff; padding: 1.25rem; font-family: Roboto, 'Segoe UI', system-ui, sans-serif; color: #333; min-height: 100vh; }
.pff-title { font-size: 1.25rem; color: #1a2d4f; margin: 0 0 .3rem; }
.pff-desc { color: #666; font-size: .92rem; margin: 0 0 1rem; }
.pff-vapp { background: transparent; }
.pff-vapp :deep(.v-application__wrap) { min-height: 0; }
.pff-muted { color: #888; }
.pff-error { color: #b42318; font-size: .9rem; }
</style>
