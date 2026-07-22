<template>
  <span>
    <button type="button" :class="['btn-rubric', { on: subject.rubricTemplateId }]" @click.stop="open()"
            title="Rubric-style grading for this module">
      ▦ {{ subject.rubricName || 'Rubric' }}</button>

    <Teleport to="body">
      <div v-if="rub.open" class="rub-overlay" @click.self="rub.open = false">
        <div class="rub-dialog">
          <h3 class="rub-title">Grading for {{ subject.code }} — {{ subject.name }}</h3>
          <p class="rub-sub">Simple grading is one 0–100 mark. Rubric grading scores every criterion 1–100 and
            calculates the module grade as the weighted total (Max % per row, rows must total 100).</p>

          <label class="rub-choice"><input type="radio" value="none" v-model="rub.mode" /> Simple grade (no rubric)</label>
          <label class="rub-choice"><input type="radio" value="shared" v-model="rub.mode" /> Use a shared rubric template</label>
          <select v-if="rub.mode === 'shared'" v-model="rub.templateId" class="rub-inp" style="margin:.2rem 0 .4rem 1.5rem; width:60%">
            <option :value="null" disabled>Choose a rubric…</option>
            <option v-for="t in rub.sharedTemplates" :key="t.id" :value="t.id">{{ t.name }}</option>
          </select>
          <p v-if="rub.mode === 'shared' && !rub.sharedTemplates.length" class="rub-sub" style="margin:.1rem 0 .3rem 1.5rem">
            No shared templates yet — build them under System Config → Grading Rubrics.</p>
          <label class="rub-choice"><input type="radio" value="custom" v-model="rub.mode" /> Custom rubric for this module only</label>

          <template v-if="rub.mode === 'custom'">
            <div class="rub-rows-head"><span>Section</span><span>Criteria</span><span>Max %</span><span></span></div>
            <div v-for="(r, i) in rub.rows" :key="i" class="rub-row">
              <input v-model="r.section" class="rub-inp" placeholder="e.g. Findings" />
              <textarea v-model="r.criteria" class="rub-inp" rows="2" placeholder="What is assessed…"></textarea>
              <input v-model.number="r.maxPercent" type="number" min="1" max="100" class="rub-inp" />
              <button type="button" class="rub-x" @click="rub.rows.splice(i, 1)">✕</button>
            </div>
            <div style="display:flex; align-items:center; gap:.8rem; margin-top:.5rem;">
              <button type="button" class="rub-btn" @click="rub.rows.push({ section: '', criteria: '', maxPercent: null })">+ Add row</button>
              <span :class="['rub-total', rubTotal === 100 ? 'ok' : 'bad']">
                Total: {{ rubTotal }}% {{ rubTotal === 100 ? '✓' : '(must be exactly 100)' }}</span>
            </div>
          </template>

          <p v-if="rub.error" class="rub-err">{{ rub.error }}</p>
          <div class="rub-actions">
            <button type="button" class="rub-btn" @click="rub.open = false">Cancel</button>
            <button type="button" class="rub-save" :disabled="rub.saving || !rubCanSave" @click="save">
              {{ rub.saving ? 'Saving…' : 'Save' }}</button>
          </div>
        </div>
      </div>
    </Teleport>
  </span>
</template>

<script setup>
import { reactive, computed } from 'vue'
import api from '../../api/client.js'

const props = defineProps({
  // Row object with subjectId (or id), code, name, rubricTemplateId,
  // rubricName — updated in place after a save so the button relabels
  // without a reload.
  subject: { type: Object, required: true },
  // Admin surfaces use the school route; the partner portal passes its own
  // ownership-checked mirror ('/v1/partner/my/subjects').
  base: { type: String, default: '/v1/school/subjects' },
})
const sid = computed(() => props.subject.subjectId ?? props.subject.id)

const rub = reactive({
  open: false, mode: 'none', templateId: null,
  rows: [], sharedTemplates: [], error: '', saving: false,
})
const rubTotal = computed(() => rub.rows.reduce((t, r) => t + (Number(r.maxPercent) || 0), 0))
const rubCanSave = computed(() => {
  if (rub.mode === 'shared') return !!rub.templateId
  if (rub.mode === 'custom') return rub.rows.length > 0 && rubTotal.value === 100 && rub.rows.every(r => r.section.trim())
  return true
})

async function open() {
  rub.error = ''
  rub.saving = false
  try {
    const res = await api.get(`${props.base}/${sid.value}/rubric`)
    rub.mode = res.data.mode ?? 'none'
    rub.templateId = res.data.mode === 'shared' ? res.data.templateId : null
    rub.sharedTemplates = res.data.sharedTemplates ?? []
    rub.rows = res.data.mode === 'custom'
      ? (res.data.rows ?? []).map(r => ({ id: r.id, section: r.section, criteria: r.criteria, maxPercent: r.maxPercent }))
      : [{ section: '', criteria: '', maxPercent: null }]
    rub.open = true
  } catch (e) {
    alert(e.response?.data?.error ?? e.message ?? 'Failed to load rubric')
  }
}

async function save() {
  if (rub.saving || !rubCanSave.value) return
  rub.saving = true
  rub.error = ''
  try {
    const body = { mode: rub.mode }
    if (rub.mode === 'shared') body.templateId = rub.templateId
    if (rub.mode === 'custom') body.rows = rub.rows.map(r => ({ id: r.id, section: r.section, criteria: r.criteria, maxPercent: r.maxPercent }))
    const res = await api.put(`${props.base}/${sid.value}/rubric`, body)
    props.subject.rubricTemplateId = res.data.templateId ?? null
    props.subject.rubricName = rub.mode === 'none' ? null
      : rub.mode === 'shared' ? (rub.sharedTemplates.find(t => t.id === rub.templateId)?.name ?? 'Rubric')
      : `${props.subject.code} rubric`
    rub.open = false
  } catch (e) {
    rub.error = e.response?.data?.error ?? e.message ?? 'Save failed'
  } finally {
    rub.saving = false
  }
}
</script>

<style scoped>
.btn-rubric { background: #f2f5f9; border: 1px solid #cfd7e3; border-radius: 5px; padding: .2rem .55rem; font-size: .74rem; font-weight: 600; color: #44536a; cursor: pointer; white-space: nowrap; }
.btn-rubric.on { background: #e7f0e9; border-color: #9dc4a8; color: #1d7a3e; }
.rub-overlay { position: fixed; inset: 0; background: rgba(15, 30, 55, .45); display: flex; align-items: flex-start; justify-content: center; padding: 4rem 1rem; z-index: 80; overflow-y: auto; }
.rub-dialog { background: #fff; border-radius: 10px; padding: 1.2rem 1.4rem 1.1rem; width: 100%; max-width: 760px; box-shadow: 0 12px 40px rgba(0,0,0,.25); text-align: left; }
.rub-title { margin: 0 0 .3rem; font-size: 1.05rem; color: #003366; }
.rub-sub { font-size: .8rem; color: #6b7888; margin: 0 0 .8rem; }
.rub-choice { display: block; font-size: .87rem; color: #2c3e50; margin: .35rem 0; cursor: pointer; }
.rub-inp { padding: .4rem .55rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .85rem; background: #fff; width: 100%; box-sizing: border-box; }
.rub-rows-head, .rub-row { display: grid; grid-template-columns: 1fr 2fr 90px 36px; gap: .5rem; align-items: start; margin-top: .45rem; }
.rub-rows-head { font-size: .72rem; text-transform: uppercase; letter-spacing: .03em; color: #6b7888; font-weight: 700; margin-top: .8rem; }
.rub-total { font-size: .82rem; font-weight: 700; }
.rub-total.ok { color: #1d7a3e; }
.rub-total.bad { color: #b3261e; }
.rub-actions { display: flex; justify-content: flex-end; gap: .6rem; margin-top: 1rem; }
.rub-btn { background: #f2f5f9; border: 1px solid #cfd7e3; border-radius: 5px; padding: .3rem .6rem; font-size: .78rem; font-weight: 600; color: #2c3e50; cursor: pointer; }
.rub-save { background: #003366; border: 1px solid #003366; color: #fff; border-radius: 5px; padding: .35rem .8rem; font-size: .8rem; font-weight: 600; cursor: pointer; }
.rub-save:disabled { opacity: .5; cursor: default; }
.rub-x { background: none; border: none; color: #b3261e; font-size: .9rem; cursor: pointer; padding: .3rem .2rem; }
.rub-err { background: #fdf3f2; border: 1px solid #e2b8b5; color: #b3261e; padding: .45rem .7rem; border-radius: 6px; font-size: .8rem; margin-top: .6rem; }
</style>
