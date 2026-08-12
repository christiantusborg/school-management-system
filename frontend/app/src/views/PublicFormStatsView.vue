<template>
  <div class="page-wrapper">
    <nav class="navbar">
      <span class="brand-text">MGW Admin Portal</span>
      <div class="nav-links">
        <RouterLink to="/admin" class="nav-link">Dashboard</RouterLink>
        <RouterLink to="/admin/academic" class="nav-link">Academic</RouterLink>
        <RouterLink to="/admin/questionnaires" class="nav-link">Questionnaires</RouterLink>
        <RouterLink to="/admin/config" class="nav-link">System Config</RouterLink>
      </div>
      <div class="nav-right">
        <span class="nav-user">{{ auth.user?.displayName }}</span>
        <button class="btn-logout" @click="logout">Log out</button>
      </div>
    </nav>

    <v-app class="qb-vapp">
      <v-main>
        <div class="qb-content">
          <div class="d-flex align-center ga-3 mb-2 flex-wrap">
            <v-btn variant="text" prepend-icon="mdi-arrow-left" @click="back">Public forms</v-btn>
            <div>
              <h2 class="text-h6 font-weight-bold mb-0">Public form statistics</h2>
              <p class="text-body-2 text-medium-emphasis mb-0">
                Aggregated, anonymous results — answers are only ever shown as "Respondent #N".
              </p>
            </div>
          </div>

          <v-alert v-if="error" type="error" density="compact" class="mb-3">{{ error }}</v-alert>

          <!-- Run selector: this run, each sibling run of the same questionnaire, or all combined -->
          <v-card variant="tonal" class="pa-3 mb-4">
            <div class="d-flex align-center ga-3 flex-wrap">
              <v-select
                v-model="selectedRun" :items="runOptions" item-title="title" item-value="value"
                label="Run" density="compact" hide-details style="max-width:460px; min-width:300px"
                @update:model-value="loadStats" />
              <div v-if="group.length > 1" class="text-caption text-medium-emphasis">
                {{ group.length }} runs of "{{ questionnaireName }}"
              </div>
            </div>
          </v-card>

          <v-progress-linear v-if="loading" indeterminate class="mb-3" />

          <template v-if="stats">
            <!-- Version warning: pooled submissions span more than one version -->
            <v-alert v-if="stats.versionWarning" type="warning" variant="tonal" density="comfortable" class="mb-4">
              <div class="font-weight-medium">Data may be inaccurate: mixed questionnaire versions</div>
              These results combine submissions collected under
              {{ stats.distinctVersionCount > 1 ? `${stats.distinctVersionCount} different questionnaire versions` : 'a changed questionnaire' }}.
              Questions that changed wording or were removed are flagged below; combined figures for those should be read with care.
            </v-alert>

            <div class="d-flex ga-3 mb-4 flex-wrap">
              <v-card variant="tonal" class="pa-3"><div class="text-h5 font-weight-bold">{{ stats.respondentCount }}</div><div class="text-caption">Submitted responses</div></v-card>
              <v-card variant="tonal" class="pa-3"><div class="text-h5 font-weight-bold">{{ stats.runCount }}</div><div class="text-caption">Run{{ stats.runCount !== 1 ? 's' : '' }} included</div></v-card>
              <v-card variant="tonal" class="pa-3"><div class="text-h5 font-weight-bold">v{{ stats.currentVersion }}</div><div class="text-caption">{{ stats.versionCount }} version{{ stats.versionCount !== 1 ? 's' : '' }}</div></v-card>
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
                <p v-if="!stats.respondents?.length" class="text-medium-emphasis">No submissions yet.</p>
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
          </template>
          <p v-else-if="!loading" class="text-medium-emphasis">No data for this run yet.</p>
        </div>
      </v-main>
    </v-app>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { auth } from '../store/auth.js'
import api from '../api/client.js'

const route = useRoute()
const router = useRouter()

const ALL = '__all__'
const focusId = ref(route.params.id)
const group = ref([])              // sibling runs of the same questionnaire
const questionnaireName = ref('')
const selectedRun = ref(route.params.id)
const stats = ref(null)
const loading = ref(false)
const error = ref('')

const fmt = d => d ? new Date(d).toLocaleString('en-GB', { dateStyle: 'medium', timeStyle: 'short' }) : ''
const fmtDate = d => d ? new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }) : '…'
const barWidth = (count, total) => total ? `${Math.round((count / total) * 100)}%` : '0%'

const choiceQuestions = computed(() => (stats.value?.questions ?? []).filter(q => q.isChoice))
const textQuestions = computed(() => (stats.value?.questions ?? []).filter(q => q.isText))

const runOptions = computed(() => {
  const opts = group.value.map(f => ({
    value: f.publicFormId,
    title: runOptionLabel(f),
  }))
  if (group.value.length > 1)
    opts.unshift({ value: ALL, title: `⭐ All ${group.value.length} runs combined` })
  return opts
})

function runOptionLabel(f) {
  const window = (f.runStartDate || f.runEndDate)
    ? `${fmtDate(f.runStartDate)} – ${fmtDate(f.runEndDate)}`
    : 'always open'
  const closed = f.isClosed ? ' · closed' : ''
  return `${f.name} (${window}, ${f.submissionCount} response${f.submissionCount !== 1 ? 's' : ''}${closed})`
}

function display(q, raw) {
  if (raw === null || raw === undefined || raw === '') return '—'
  if (q.isChoice) {
    const m = q.choices?.options?.find(o => o.value === raw)
    return m?.label ?? raw
  }
  return raw
}

function logout() {
  auth.logout()
  router.push('/login')
}
function back() {
  router.push('/admin/questionnaires')
}

// Load the focused form + all sibling runs of the same questionnaire so the
// selector can offer per-run and combined views.
async function loadGroup() {
  error.value = ''
  try {
    const res = await api.get('/v1/intake/public-forms')
    const all = res.data?.data?.items ?? []
    const focus = all.find(f => f.publicFormId === focusId.value)
    if (!focus) { error.value = 'Public form not found.'; return }
    questionnaireName.value = focus.questionnaireName ?? focus.name
    group.value = all
      .filter(f => f.questionnaireTemplateId === focus.questionnaireTemplateId)
      .sort((a, b) => (a.runStartDate ?? a.createdAt ?? '').localeCompare(b.runStartDate ?? b.createdAt ?? ''))
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load runs'
  }
}

async function loadStats() {
  loading.value = true
  error.value = ''
  stats.value = null
  try {
    let res
    if (selectedRun.value === ALL) {
      const ids = group.value.map(f => f.publicFormId).join(',')
      res = await api.get('/v1/intake/public-forms/stats', { params: { ids } })
    } else {
      res = await api.get(`/v1/intake/public-forms/${selectedRun.value}/stats`)
    }
    stats.value = res.data?.data ?? null
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load statistics'
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  await loadGroup()
  await loadStats()
})
</script>

<style scoped>
.page-wrapper { min-height: 100vh; display: flex; flex-direction: column; background: #f2f5f9; }
.navbar {
  background: #0b2e59;
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.85rem 2rem;
  gap: 1rem;
}
.brand-text { font-size: 1.05rem; font-weight: 700; white-space: nowrap; }
.nav-links { display: flex; gap: 0.25rem; flex: 1; padding: 0 1rem; }
.nav-link {
  color: rgba(255,255,255,0.75);
  text-decoration: none;
  padding: 0.35rem 0.9rem;
  border-radius: 5px;
  font-size: 0.88rem;
  transition: background 0.15s, color 0.15s;
}
.nav-link:hover, .nav-link.router-link-active { background: rgba(255,255,255,0.15); color: #fff; }
.nav-right { display: flex; align-items: center; gap: 1rem; }
.nav-user { font-size: 0.88rem; opacity: 0.85; }
.btn-logout {
  background: rgba(255,255,255,0.12);
  border: 1px solid rgba(255,255,255,0.25);
  color: #fff;
  border-radius: 5px;
  padding: 0.3rem 0.8rem;
  font-size: 0.85rem;
  cursor: pointer;
}
.btn-logout:hover { background: rgba(255,255,255,0.22); }
.qb-vapp { flex: 1; }
.qb-vapp :deep(.v-application__wrap) { min-height: 0; }
.qb-content { padding: 1.25rem 2rem; max-width: 1100px; }

.qs-bar-row { display: flex; align-items: center; gap: .6rem; margin-bottom: .25rem; }
.qs-bar-label { width: 220px; font-size: .82rem; flex-shrink: 0; }
.qs-bar-track { flex: 1; background: rgba(0,0,0,.06); border-radius: 4px; height: 16px; overflow: hidden; }
.qs-bar-fill { background: #1a4d8c; height: 100%; border-radius: 4px; transition: width .3s; }
.qs-bar-count { width: 34px; text-align: right; font-size: .82rem; font-weight: 600; }
</style>
