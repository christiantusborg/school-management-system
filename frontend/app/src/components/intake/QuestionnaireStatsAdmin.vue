<template>
  <div>
    <div v-if="auth.access('questionnaires.aggregate_view') > 0">
    <div class="d-flex justify-space-between align-center mb-4 flex-wrap ga-2">
      <div>
        <h2 class="text-h6 font-weight-bold">Questionnaire statistics</h2>
        <p class="text-body-2 text-medium-emphasis mb-0">
          Aggregated, anonymous results — answers are only ever shown as "Respondent #N".
          Aggregation spans all versions of the questionnaire.
        </p>
      </div>
      <v-select v-model="instanceId" :items="instances" item-title="label" item-value="id"
        label="Questionnaire" density="compact" hide-details style="max-width:380px; min-width:280px"
        @update:model-value="loadAll" />
    </div>

    <v-alert v-if="error" type="error" density="compact" class="mb-3">{{ error }}</v-alert>
    <v-progress-linear v-if="loading" indeterminate class="mb-3" />

    <template v-if="stats">
      <div class="d-flex ga-3 mb-4 flex-wrap">
        <v-card variant="tonal" class="pa-3"><div class="text-h5 font-weight-bold">{{ stats.respondentCount }}</div><div class="text-caption">Submitted responses</div></v-card>
        <v-card variant="tonal" class="pa-3"><div class="text-h5 font-weight-bold">v{{ stats.currentVersion }}</div><div class="text-caption">{{ stats.versionCount }} version{{ stats.versionCount !== 1 ? 's' : '' }}</div></v-card>
        <v-card variant="tonal" class="pa-3"><div class="text-h5 font-weight-bold">{{ completion.answered.length }} / {{ completion.answered.length + completion.notAnswered.length }}</div><div class="text-caption">Completion</div></v-card>
      </div>

      <!-- Per-question aggregates -->
      <v-card v-for="q in choiceQuestions" :key="q.questionId" class="mb-3">
        <v-card-text>
          <div class="d-flex align-center ga-2 flex-wrap mb-2">
            <strong>{{ q.label }}</strong>
            <v-chip v-if="q.average != null" size="x-small" color="primary">avg {{ q.average }}</v-chip>
            <v-chip size="x-small">{{ q.answeredCount }} answers</v-chip>
            <v-chip v-if="q.changedBetweenVersions" size="x-small" color="warning" title="This question's wording changed between versions — compare with care.">changed between versions</v-chip>
            <v-chip v-if="q.removedInCurrentVersion" size="x-small" color="grey" title="Removed in the current version; answers come from older versions.">removed in current version</v-chip>
          </div>
          <div v-for="o in q.choices.options" :key="o.value" class="qs-bar-row">
            <span class="qs-bar-label">{{ o.label }}</span>
            <div class="qs-bar-track">
              <div class="qs-bar-fill" :style="{ width: barWidth(o.count, q.answeredCount) }"></div>
            </div>
            <span class="qs-bar-count">{{ o.count }}</span>
          </div>
          <p v-if="q.choices.other > 0" class="text-caption text-medium-emphasis mb-0">
            + {{ q.choices.other }} answer(s) not matching current options (older versions).
          </p>
        </v-card-text>
      </v-card>

      <!-- Text comments section -->
      <v-card class="mb-3">
        <v-card-title class="text-subtitle-1">💬 Text answers &amp; comments</v-card-title>
        <v-card-text>
          <p v-if="!textQuestions.length" class="text-medium-emphasis">This questionnaire has no text questions.</p>
          <div v-for="q in textQuestions" :key="q.questionId" class="mb-4">
            <div class="font-weight-medium mb-1">{{ q.label }}</div>
            <p v-if="!q.texts?.length" class="text-caption text-medium-emphasis">No answers yet.</p>
            <v-card v-for="t in q.texts" :key="t.respondent + t.text" variant="tonal" class="pa-2 mb-1">
              <span class="text-caption text-medium-emphasis mr-2">Respondent #{{ t.respondent }}</span>{{ t.text }}
            </v-card>
          </div>
        </v-card-text>
      </v-card>

      <!-- Drill-down -->
      <v-card class="mb-3">
        <v-card-title class="text-subtitle-1">🔎 Drill-down (anonymous)</v-card-title>
        <v-card-text>
          <v-expansion-panels>
            <v-expansion-panel v-for="r in stats.respondents" :key="r.respondent">
              <v-expansion-panel-title>
                Respondent #{{ r.respondent }}
                <span class="text-caption text-medium-emphasis ml-2">v{{ r.version }} · {{ fmt(r.submittedAt) }}</span>
              </v-expansion-panel-title>
              <v-expansion-panel-text>
                <v-table density="compact">
                  <tbody>
                    <tr v-for="q in stats.questions" :key="q.questionId">
                      <td style="width:50%" class="font-weight-medium">{{ q.label }}</td>
                      <td>{{ display(q, r.answers[q.questionId]) }}</td>
                    </tr>
                  </tbody>
                </v-table>
              </v-expansion-panel-text>
            </v-expansion-panel>
          </v-expansion-panels>
        </v-card-text>
      </v-card>

      <!-- Completion checklist -->
      <v-card class="mb-3">
        <v-card-title class="text-subtitle-1">✅ Completion (names are never linked to answers)</v-card-title>
        <v-card-text>
          <div class="d-flex ga-6 flex-wrap">
            <div style="flex:1; min-width:260px">
              <div class="font-weight-medium mb-1">Answered ({{ completion.answered.length }})</div>
              <p v-if="!completion.answered.length" class="text-caption text-medium-emphasis">Nobody yet.</p>
              <div v-for="n in completion.answered" :key="'a' + n" class="text-body-2">✓ {{ n }}</div>
            </div>
            <div style="flex:1; min-width:260px">
              <div class="font-weight-medium mb-1">Not answered ({{ completion.notAnswered.length }})</div>
              <div v-for="n in completion.notAnswered" :key="'n' + n" class="text-body-2 text-medium-emphasis">○ {{ n }}</div>
            </div>
          </div>
        </v-card-text>
      </v-card>

      <!-- LLM chat -->
      <v-card>
        <v-card-title class="text-subtitle-1">🤖 Chat with the responses (local LLM)</v-card-title>
        <v-card-text>
          <p class="text-caption text-medium-emphasis">
            Ask questions about the answers — "What are the top complaints?", "Summarise the text feedback",
            "Which respondents are unhappy with grading?". Runs on the school's own Ollama server; no data leaves the network.
          </p>
          <div class="qs-chat">
            <div v-for="(m, idx) in chat" :key="idx" :class="['qs-msg', m.role === 'user' ? 'qs-msg-user' : 'qs-msg-bot']">
              <div class="text-caption font-weight-bold">{{ m.role === 'user' ? 'You' : 'Analyst' }}</div>
              <div style="white-space:pre-wrap">{{ m.content }}</div>
            </div>
            <p v-if="!chat.length" class="text-caption text-medium-emphasis">No messages yet.</p>
          </div>
          <div class="d-flex ga-2 mt-2">
            <v-text-field v-model="chatInput" density="compact" hide-details
              placeholder="Ask about the responses…" @keyup.enter="sendChat" />
            <v-btn color="primary" :loading="chatBusy" :disabled="!chatInput.trim()" @click="sendChat">Send</v-btn>
          </div>
          <p v-if="chatError" class="text-error text-body-2 mt-1">{{ chatError }}</p>
        </v-card-text>
      </v-card>
    </template>
    <p v-else-if="!loading && instanceId" class="text-medium-emphasis">No data.</p>
    <p v-else-if="!loading" class="text-medium-emphasis">Pick a questionnaire above to see its statistics.</p>
    </div>
    <p v-else class="text-medium-emphasis">You do not have access to questionnaire statistics.</p>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '../../api/client.js'
import { auth } from '../../store/auth.js'

const instances = ref([])
const instanceId = ref(null)
const stats = ref(null)
const completion = ref({ answered: [], notAnswered: [] })
const loading = ref(false)
const error = ref('')

const chat = ref([])
const chatInput = ref('')
const chatBusy = ref(false)
const chatError = ref('')

const fmt = d => d ? new Date(d).toLocaleString('en-GB', { dateStyle: 'medium', timeStyle: 'short' }) : ''
const barWidth = (count, total) => total ? `${Math.round((count / total) * 100)}%` : '0%'

const choiceQuestions = computed(() => (stats.value?.questions ?? []).filter(q => q.isChoice))
const textQuestions = computed(() => (stats.value?.questions ?? []).filter(q => q.isText))

function display(q, raw) {
  if (raw === null || raw === undefined || raw === '') return '—'
  if (q.isChoice) {
    const m = q.choices?.options?.find(o => o.value === raw)
    return m?.label ?? raw
  }
  return raw
}

async function loadInstances() {
  try {
    const res = await api.get('/v1/intake/intake-instances')
    instances.value = (res.data?.data?.items ?? [])
      .filter(i => !i.deletedAt)
      .map(i => ({ id: i.intakeInstanceId, label: `${i.name} (${i.submittedCount} submitted)` }))
  } catch (e) {
    error.value = e.response?.data?.error ?? 'Failed to load questionnaires'
  }
}

async function loadAll() {
  if (!instanceId.value) return
  loading.value = true
  error.value = ''
  stats.value = null
  chat.value = []
  chatError.value = ''
  try {
    const [st, comp] = await Promise.all([
      api.get(`/v1/intake/intake-instances/${instanceId.value}/stats`),
      api.get(`/v1/intake/intake-instances/${instanceId.value}/completion`),
    ])
    stats.value = st.data?.data ?? null
    completion.value = comp.data?.data ?? { answered: [], notAnswered: [] }
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load statistics'
  } finally {
    loading.value = false
  }
}

async function sendChat() {
  const q = chatInput.value.trim()
  if (!q || chatBusy.value || !instanceId.value) return
  chatBusy.value = true
  chatError.value = ''
  chat.value.push({ role: 'user', content: q })
  chatInput.value = ''
  try {
    const res = await api.post(`/v1/intake/intake-instances/${instanceId.value}/chat`, {
      question: q,
      history: chat.value.slice(0, -1).slice(-8),
    })
    chat.value.push({ role: 'assistant', content: res.data?.data?.answer ?? '(no answer)' })
  } catch (e) {
    const code = e.response?.data?.error
    chatError.value = code === 'llm_not_configured' ? 'The local LLM is not configured on the server.'
      : code === 'llm_unreachable' ? 'Could not reach the local LLM (is Ollama running?).'
      : (code ?? e.message ?? 'Chat failed')
    chat.value.pop()
  } finally {
    chatBusy.value = false
  }
}

onMounted(loadInstances)
</script>

<style scoped>
.qs-bar-row { display: flex; align-items: center; gap: .6rem; margin-bottom: .25rem; }
.qs-bar-label { width: 220px; font-size: .82rem; flex-shrink: 0; }
.qs-bar-track { flex: 1; background: rgba(0,0,0,.06); border-radius: 4px; height: 16px; overflow: hidden; }
.qs-bar-fill { background: #1a4d8c; height: 100%; border-radius: 4px; transition: width .3s; }
.qs-bar-count { width: 34px; text-align: right; font-size: .82rem; font-weight: 600; }
.qs-chat { max-height: 340px; overflow-y: auto; border: 1px solid rgba(0,0,0,.08); border-radius: 8px; padding: .6rem; }
.qs-msg { max-width: 88%; padding: .4rem .6rem; border-radius: 10px; margin-bottom: .4rem; }
.qs-msg-user { background: #e8eef6; margin-left: auto; }
.qs-msg-bot { background: #eef7ee; margin-right: auto; }
</style>
