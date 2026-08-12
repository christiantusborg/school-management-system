<template>
  <div class="cq-panel">
    <!-- Admin: attach questionnaires -->
    <div v-if="mode === 'admin'" class="cq-attach">
      <select v-model="pickId">
        <option value="">— choose a questionnaire —</option>
        <option v-for="t in availableTemplates" :key="t.questionnaireTemplateId" :value="t.questionnaireTemplateId">
          {{ t.name }}
        </option>
      </select>
      <button v-if="auth.can('cohorts.assign_questionnaire')" class="cq-btn" :disabled="!pickId || busy" @click="attach">+ Attach to cohort</button>
    </div>

    <p v-if="error" class="cq-error">{{ error }}</p>
    <p v-if="loading" class="cq-muted">Loading…</p>

    <p v-else-if="!list.length" class="cq-muted">
      No questionnaires attached to this cohort{{ mode === 'admin' ? ' yet. Attach one above; students must fill all of them before they can see their grade.' : '.' }}
    </p>

    <div v-for="q in list" :key="q.moduleCohortQuestionnaireId" class="cq-card">
      <div class="cq-card-head">
        <strong>{{ q.name }}</strong>
        <span class="cq-counts">{{ q.responses }} response{{ q.responses !== 1 ? 's' : '' }} · {{ q.completed }}/{{ assigned }} students completed</span>
        <button v-if="mode === 'admin' && auth.can('cohorts.assign_questionnaire')" class="cq-btn cq-btn-danger" :disabled="busy" @click="detach(q)">Remove</button>
      </div>

      <template v-if="auth.access('cohorts.aggregate_view') > 0">
      <div v-if="statsFor(q)?.locked" class="cq-locked">
        🔒 Results unlock at {{ statsFor(q).requiredResponses }} responses to keep answers anonymous
        (currently {{ statsFor(q).responses }}). The Admission Office can already see them.
      </div>

      <template v-else-if="statsFor(q)?.questions">
        <div v-for="qu in statsFor(q).questions" :key="qu.id" class="cq-question">
          <div class="cq-q-label">
            {{ qu.label }}
            <span v-if="qu.average != null" class="cq-avg">avg {{ qu.average }}</span>
            <span class="cq-answered">{{ qu.answered }} answered</span>
          </div>
          <div v-if="qu.options" class="cq-bars">
            <div v-for="o in qu.options" :key="o.value" class="cq-bar-row">
              <span class="cq-bar-label">{{ o.label }}</span>
              <div class="cq-bar-track">
                <div class="cq-bar-fill" :style="{ width: barWidth(o, qu) }"></div>
              </div>
              <span class="cq-bar-count">{{ o.count }}</span>
            </div>
          </div>
          <ul v-else-if="qu.texts?.length" class="cq-texts">
            <li v-for="(t, i) in qu.texts" :key="i">“{{ t }}”</li>
          </ul>
          <p v-else-if="!qu.options" class="cq-muted">No answers yet.</p>
        </div>
      </template>
      </template>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '../../api/client.js'
import { auth } from '../../store/auth.js'

const props = defineProps({
  mode: { type: String, required: true },   // 'admin' | 'partner'
  cohortId: { type: String, required: true },
})

const loading = ref(false)
const busy = ref(false)
const error = ref('')
const list = ref([])
const assigned = ref(0)
const stats = ref(null)
const templates = ref([])
const pickId = ref('')

const availableTemplates = computed(() =>
  templates.value.filter(t => !list.value.some(q => q.questionnaireTemplateId === t.questionnaireTemplateId)))

function statsFor(q) {
  return stats.value?.questionnaires?.find(s => s.moduleCohortQuestionnaireId === q.moduleCohortQuestionnaireId)
}

function barWidth(o, qu) {
  const max = Math.max(1, ...qu.options.map(x => x.count))
  return `${Math.round((o.count / max) * 100)}%`
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    if (props.mode === 'admin') {
      const [listRes, statsRes] = await Promise.all([
        api.get(`/v1/admin/cohorts/${props.cohortId}/questionnaires`),
        api.get(`/v1/admin/cohorts/${props.cohortId}/questionnaires/stats`),
      ])
      list.value = listRes.data.items ?? []
      assigned.value = listRes.data.assigned ?? 0
      stats.value = statsRes.data
      if (!templates.value.length) {
        const tRes = await api.get('/v1/intake/questionnaire-templates')
        templates.value = tRes.data?.data?.items ?? []
      }
    } else {
      const res = await api.get(`/v1/partner/my/cohorts/${props.cohortId}/questionnaires/stats`)
      stats.value = res.data
      assigned.value = res.data.assigned ?? 0
      list.value = (res.data.questionnaires ?? []).map(s => ({
        moduleCohortQuestionnaireId: s.moduleCohortQuestionnaireId,
        questionnaireTemplateId: null,
        name: s.name,
        responses: s.responses,
        completed: s.completed,
      }))
    }
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load questionnaires'
  } finally {
    loading.value = false
  }
}

async function attach() {
  if (!pickId.value || busy.value) return
  busy.value = true
  error.value = ''
  try {
    await api.post(`/v1/admin/cohorts/${props.cohortId}/questionnaires`, {
      questionnaireTemplateId: pickId.value,
    })
    pickId.value = ''
    await load()
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Attach failed'
  } finally {
    busy.value = false
  }
}

async function detach(q) {
  if (busy.value) return
  if (!confirm(`Remove "${q.name}" from this cohort? Existing responses are kept.`)) return
  busy.value = true
  try {
    await api.delete(`/v1/admin/cohort-questionnaires/${q.moduleCohortQuestionnaireId}`)
    await load()
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Remove failed'
  } finally {
    busy.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.cq-panel { display: flex; flex-direction: column; gap: .7rem; }
.cq-attach { display: flex; gap: .5rem; align-items: center; flex-wrap: wrap; }
.cq-attach select { padding: .35rem .5rem; border: 1px solid #cbd5e1; border-radius: 5px; font-size: .84rem; min-width: 260px; background: #fff; }
.cq-btn { background: #0a264f; color: #fff; border: none; border-radius: 5px; padding: .35rem .8rem; font-size: .8rem; font-weight: 600; cursor: pointer; }
.cq-btn:disabled { opacity: .5; cursor: not-allowed; }
.cq-btn-danger { background: #b91c1c; }
.cq-error { background: #fef2f2; border: 1px solid #fecaca; color: #b91c1c; border-radius: 6px; padding: .5rem .8rem; font-size: .83rem; margin: 0; }
.cq-muted { color: #94a3b8; font-size: .84rem; font-style: italic; margin: 0; }
.cq-card { border: 1px solid #e2e8f0; border-radius: 8px; padding: .7rem .9rem; background: #fafcff; }
.cq-card-head { display: flex; align-items: center; gap: .7rem; flex-wrap: wrap; }
.cq-card-head strong { color: #0a264f; font-size: .9rem; }
.cq-counts { color: #64748b; font-size: .78rem; flex: 1; }
.cq-locked { margin-top: .5rem; background: #fffbeb; border: 1px solid #fde68a; color: #92400e; border-radius: 6px; padding: .5rem .8rem; font-size: .82rem; }
.cq-question { margin-top: .7rem; padding-top: .55rem; border-top: 1px dashed #e2e8f0; }
.cq-q-label { font-size: .84rem; font-weight: 600; color: #1e293b; display: flex; gap: .6rem; align-items: baseline; flex-wrap: wrap; }
.cq-avg { background: #ecfdf5; color: #047857; border-radius: 4px; padding: 0 .45rem; font-size: .74rem; }
.cq-answered { color: #94a3b8; font-size: .72rem; font-weight: 400; }
.cq-bars { margin-top: .35rem; display: flex; flex-direction: column; gap: .2rem; }
.cq-bar-row { display: grid; grid-template-columns: minmax(120px, 220px) 1fr 2.2rem; align-items: center; gap: .5rem; }
.cq-bar-label { font-size: .78rem; color: #475569; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.cq-bar-track { background: #eef2f7; border-radius: 4px; height: 14px; overflow: hidden; }
.cq-bar-fill { background: #2563eb; height: 100%; border-radius: 4px; min-width: 2px; }
.cq-bar-count { font-size: .78rem; color: #334155; text-align: right; }
.cq-texts { margin: .35rem 0 0; padding-left: 1.1rem; display: flex; flex-direction: column; gap: .2rem; }
.cq-texts li { font-size: .82rem; color: #475569; }
</style>
