<template>
  <div class="mcc-wrap">
    <div class="mcc-head">
      <div>
        <h2 class="mcc-title">Module Cohorts</h2>
        <p class="mcc-sub">Settings for the Module Cohort Schedule: the cohort number pattern and the configurable
          upload fields every cohort shows. Removing a field hides it; re-adding the same label restores its files.</p>
      </div>
      <button type="button" class="btn-primary-sm" :disabled="saving || loading" @click="save">
        {{ saving ? 'Saving…' : 'Save' }}
      </button>
    </div>

    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="savedFlash" class="ok-banner">✓ Saved.</div>
    <div v-if="loading" class="loading-row">Loading…</div>

    <template v-else>
      <label class="mcc-lbl">Cohort number pattern</label>
      <input v-model="pattern" class="mcc-inp" style="max-width:420px"
             placeholder="{partner}-{module}-{n}" />
      <p class="mcc-sub" style="margin:.25rem 0 1rem">{partner} = partner name, {module} = module code, {n} = sequence per partner+module (3 digits).</p>

      <div class="mcc-fields-head">
        <label class="mcc-lbl" style="margin:0">Upload fields</label>
        <button type="button" class="btn-sm" @click="fields.push({ id: null, label: '', allowMultiple: true, isGradingSheet: false })">+ Add field</button>
      </div>
      <div v-for="(f, i) in fields" :key="i" class="mcc-field-row">
        <input v-model="f.label" class="mcc-inp" placeholder="Field label" style="flex:1.6" />
        <select v-model="f.allowMultiple" class="mcc-inp" style="flex:.8">
          <option :value="false">1 document</option>
          <option :value="true">Several documents</option>
        </select>
        <label class="mcc-check" title="Uploading here stamps 'Date Grading Sheet Uploaded'">
          <input type="checkbox" v-model="f.isGradingSheet" /> grading sheet
        </label>
        <button type="button" class="btn-sm btn-danger" @click="fields.splice(i, 1)">✕</button>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '../../api/client.js'

const pattern = ref('{partner}-{module}-{n}')
const fields = ref([])
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const savedFlash = ref(false)

async function load() {
  loading.value = true
  error.value = ''
  try {
    const res = await api.get('/v1/admin/cohort-settings')
    pattern.value = res.data.cohortNumberPattern ?? '{partner}-{module}-{n}'
    fields.value = (res.data.fields ?? []).map(f => ({
      id: f.id, label: f.label, allowMultiple: !!f.allowMultiple, isGradingSheet: !!f.isGradingSheet,
    }))
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load cohort settings'
  } finally {
    loading.value = false
  }
}

async function save() {
  if (saving.value) return
  saving.value = true
  error.value = ''
  savedFlash.value = false
  try {
    await api.put('/v1/admin/cohort-settings', {
      cohortNumberPattern: pattern.value.trim() || '{partner}-{module}-{n}',
      fields: fields.value.filter(f => f.label.trim()).map(f => ({
        id: f.id, label: f.label.trim(), allowMultiple: f.allowMultiple, isGradingSheet: f.isGradingSheet,
      })),
    })
    savedFlash.value = true
    setTimeout(() => { savedFlash.value = false }, 4000)
    await load()
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Save failed'
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.mcc-wrap { background: #fff; border-radius: 8px; padding: 1.25rem 1.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.07); }
.mcc-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; margin-bottom: .75rem; }
.mcc-title { margin: 0; font-size: 1.15rem; color: #003366; }
.mcc-sub { font-size: .78rem; color: #6b7888; margin: .2rem 0 0; max-width: 720px; }
.mcc-lbl { display: block; font-size: .75rem; font-weight: 700; color: #44536a; margin-bottom: .25rem; }
.mcc-inp { padding: .4rem .55rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .85rem; width: 100%; background: #fff; }
.mcc-fields-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: .5rem; }
.mcc-field-row { display: flex; gap: .4rem; align-items: center; margin-bottom: .35rem; }
.mcc-check { display: flex; align-items: center; gap: .25rem; font-size: .74rem; color: #44536a; white-space: nowrap; }
.btn-sm { background: #f2f5f9; border: 1px solid #cfd7e3; border-radius: 5px; padding: .28rem .6rem; font-size: .78rem; font-weight: 600; color: #2c3e50; cursor: pointer; }
.btn-sm:hover { background: #e8eef6; }
.btn-danger { color: #b3261e; border-color: #e2b8b5; background: #fdf3f2; }
.btn-primary-sm { background: #003366; color: #fff; border: none; border-radius: 5px; padding: .4rem .9rem; font-size: .8rem; font-weight: 600; cursor: pointer; }
.btn-primary-sm:disabled { opacity: .5; cursor: default; }
.err-banner { background: #fdf3f2; border: 1px solid #e2b8b5; color: #b3261e; padding: .45rem .7rem; border-radius: 6px; font-size: .8rem; margin-bottom: .5rem; }
.ok-banner { background: #e6f6ec; border: 1px solid #b9e1c7; color: #1c7a4a; padding: .45rem .7rem; border-radius: 6px; font-size: .8rem; margin-bottom: .5rem; font-weight: 600; }
.loading-row { color: #6b7888; font-size: .85rem; padding: .5rem 0; }
</style>
