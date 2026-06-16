<template>
  <div v-if="open" class="modal-backdrop" @click.self="$emit('close')">
    <div class="modal">
      <div class="modal-header">
        <h3>✉ {{ titleFor(letterType) }} email <span class="muted">— {{ programmeName || 'Programme' }}</span></h3>
        <button class="btn-close" @click="$emit('close')">✕</button>
      </div>

      <div v-if="loading" class="modal-loading">Loading…</div>
      <div v-else-if="loadError" class="modal-err">{{ loadError }}</div>
      <template v-else>
        <div class="em-body">
          <label class="em-toggle">
            <input type="checkbox" v-model="form.isEmailEnabled" />
            <span>Enable automatic email when this letter is released</span>
          </label>
          <p class="em-hint">
            To is always the student's email. The released PDF is attached automatically.
            Subject and body support letter tags (e.g. <code>[student full name]</code>,
            <code>[student number]</code>).
          </p>

          <div class="em-field">
            <label>Subject</label>
            <input type="text" v-model="form.subject" placeholder="Your IBSS Admission Letter — [student full name]" />
            <select class="em-tag" @change="insertTag('subject', $event)">
              <option value="">+ tag…</option>
              <option v-for="t in tags" :key="t.token" :value="t.token">{{ t.token }}</option>
            </select>
          </div>

          <div class="em-field em-field-col">
            <label>Body (HTML supported — paste your signature here)</label>
            <textarea v-model="form.bodyHtml" rows="12"
              placeholder="Dear [student full name],&#10;&#10;Congratulations…"></textarea>
            <select class="em-tag em-tag-block" @change="insertTag('bodyHtml', $event)">
              <option value="">+ insert tag…</option>
              <option v-for="t in tags" :key="t.token" :value="t.token">{{ t.token }}</option>
            </select>
          </div>

          <div class="em-recip">
            <RecipientList label="CC" :list="cc" @add="cc.push({ email: $event, enabled: true })" @remove="cc.splice($event, 1)" />
            <RecipientList label="BCC" :list="bcc" @add="bcc.push({ email: $event, enabled: true })" @remove="bcc.splice($event, 1)" />
          </div>
        </div>

        <div v-if="saveError" class="modal-err">{{ saveError }}</div>
        <div class="modal-actions">
          <button class="btn-ghost" @click="$emit('close')">Cancel</button>
          <button class="btn-primary" :disabled="saving" @click="onSave">{{ saving ? 'Saving…' : 'Save' }}</button>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, watch, h } from 'vue'
import apiClient from '../../api/client.js'

const props = defineProps({
  open: { type: Boolean, required: true },
  programmeId: { type: String, default: '' },
  programmeName: { type: String, default: '' },
  letterType: { type: String, default: '' },
})
const emit = defineEmits(['close', 'saved'])

// Small inline child: a toggleable recipient list with an add field.
const RecipientList = {
  props: { label: String, list: Array },
  emits: ['add', 'remove'],
  setup(p, { emit }) {
    const draft = ref('')
    function add() {
      const v = draft.value.trim()
      if (v) { emit('add', v); draft.value = '' }
    }
    return () => h('div', { class: 'rl' }, [
      h('div', { class: 'rl-label' }, p.label),
      ...p.list.map((r, i) => h('div', { class: 'rl-row', key: i }, [
        h('input', { type: 'checkbox', checked: r.enabled, onChange: e => (r.enabled = e.target.checked) }),
        h('span', { class: 'rl-email' }, r.email),
        h('button', { class: 'rl-del', onClick: () => emit('remove', i) }, '✕'),
      ])),
      h('div', { class: 'rl-add' }, [
        h('input', {
          type: 'email', placeholder: `Add ${p.label} address`,
          value: draft.value,
          onInput: e => (draft.value = e.target.value),
          onKeyup: e => { if (e.key === 'Enter') add() },
        }),
        h('button', { class: 'rl-add-btn', onClick: add }, 'Add'),
      ]),
    ])
  },
}

const loading = ref(false)
const loadError = ref('')
const saving = ref(false)
const saveError = ref('')
const tags = ref([])
const cc = reactive([])
const bcc = reactive([])
const form = reactive({ isEmailEnabled: false, subject: '', bodyHtml: '' })

function titleFor(t) {
  return t === 'OfferLetter' ? 'Offer Letter' : t === 'AdmissionLetter' ? 'Admission Letter' : t
}

function insertTag(field, ev) {
  const token = ev.target.value
  ev.target.value = ''
  if (!token) return
  form[field] = (form[field] || '') + token
}

function parseList(json) {
  if (!json) return []
  try {
    const arr = JSON.parse(json)
    return Array.isArray(arr) ? arr.filter(r => r && r.email).map(r => ({ email: r.email, enabled: r.enabled !== false })) : []
  } catch { return [] }
}

async function load() {
  if (!props.open || !props.programmeId || !props.letterType) return
  loading.value = true
  loadError.value = ''
  try {
    const [tplRes, tagRes] = await Promise.all([
      apiClient.get(`/v1/admin/programmes/${props.programmeId}/letter-email-templates`),
      apiClient.get('/v1/admin/letter-tags'),
    ])
    tags.value = tagRes.data.items ?? []
    const existing = (tplRes.data.items ?? []).find(t => t.letterType === props.letterType)
    form.isEmailEnabled = !!existing?.isEmailEnabled
    form.subject = existing?.subject ?? ''
    form.bodyHtml = existing?.bodyHtml ?? ''
    cc.splice(0, cc.length, ...parseList(existing?.ccRecipientsJson))
    bcc.splice(0, bcc.length, ...parseList(existing?.bccRecipientsJson))
  } catch (err) {
    loadError.value = err.response?.data?.message ?? err.message ?? 'Failed to load'
  } finally {
    loading.value = false
  }
}

async function onSave() {
  saving.value = true
  saveError.value = ''
  try {
    await apiClient.put(
      `/v1/admin/programmes/${props.programmeId}/letter-email-templates/${props.letterType}`,
      {
        isEmailEnabled: form.isEmailEnabled,
        subject: form.subject,
        bodyHtml: form.bodyHtml,
        ccRecipientsJson: JSON.stringify(cc),
        bccRecipientsJson: JSON.stringify(bcc),
      })
    emit('saved')
    emit('close')
  } catch (err) {
    saveError.value = err.response?.data?.message ?? err.message ?? 'Save failed'
  } finally {
    saving.value = false
  }
}

watch(() => [props.open, props.programmeId, props.letterType], () => { if (props.open) load() }, { immediate: true })
</script>

<style scoped>
.modal-backdrop { position: fixed; inset: 0; background: rgba(0,0,0,.45); display: flex; align-items: center; justify-content: center; z-index: 1000; }
.modal { background: #fff; border-radius: 10px; width: min(680px, 94vw); max-height: 92vh; overflow: auto; padding: 1.1rem 1.3rem; }
.modal-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: .6rem; }
.modal-header h3 { margin: 0; font-size: 1.05rem; }
.muted { color: #8a94a6; font-weight: 400; font-size: .85rem; }
.btn-close { border: none; background: none; font-size: 1.1rem; cursor: pointer; color: #6b7888; }
.modal-loading { padding: 1.5rem; text-align: center; color: #6b7888; }
.modal-err { color: #b42318; background: #fff0ef; border: 1px solid #f3c0bb; padding: .5rem .7rem; border-radius: 6px; margin: .4rem 0; font-size: .85rem; }
.em-toggle { display: flex; align-items: center; gap: .5rem; font-weight: 600; font-size: .9rem; }
.em-hint { font-size: .78rem; color: #6b7888; margin: .35rem 0 .8rem; }
.em-hint code { background: #f1f5f9; padding: 0 .25rem; border-radius: 3px; }
.em-field { display: flex; align-items: center; gap: .5rem; margin-bottom: .7rem; }
.em-field-col { flex-direction: column; align-items: stretch; }
.em-field > label { font-size: .8rem; font-weight: 600; color: #44506a; min-width: 64px; }
.em-field-col > label { margin-bottom: .25rem; }
.em-field input[type=text], .em-field textarea { flex: 1; width: 100%; padding: .4rem .55rem; border: 1px solid #d8dde5; border-radius: 5px; font-size: .85rem; font-family: inherit; }
.em-tag { padding: .35rem; border: 1px solid #d8dde5; border-radius: 5px; font-size: .78rem; }
.em-tag-block { align-self: flex-start; margin-top: .3rem; }
.em-recip { display: flex; gap: 1rem; flex-wrap: wrap; }
.em-recip :deep(.rl) { flex: 1; min-width: 250px; border: 1px solid #eef2f7; border-radius: 7px; padding: .5rem .6rem; }
.em-recip :deep(.rl-label) { font-size: .72rem; font-weight: 700; text-transform: uppercase; letter-spacing: .04em; color: #6b7888; margin-bottom: .35rem; }
.em-recip :deep(.rl-row) { display: flex; align-items: center; gap: .4rem; padding: .15rem 0; }
.em-recip :deep(.rl-email) { flex: 1; font-size: .82rem; }
.em-recip :deep(.rl-del) { border: none; background: none; color: #b42318; cursor: pointer; }
.em-recip :deep(.rl-add) { display: flex; gap: .35rem; margin-top: .4rem; }
.em-recip :deep(.rl-add input) { flex: 1; padding: .3rem .45rem; border: 1px solid #d8dde5; border-radius: 5px; font-size: .8rem; }
.em-recip :deep(.rl-add-btn) { border: 1px solid #1a4d8c; background: #1a4d8c; color: #fff; border-radius: 5px; padding: .3rem .6rem; font-size: .78rem; cursor: pointer; }
.modal-actions { display: flex; justify-content: flex-end; gap: .5rem; margin-top: 1rem; }
.btn-ghost { border: 1px solid #d8dde5; background: #fff; border-radius: 6px; padding: .45rem .9rem; cursor: pointer; }
.btn-primary { border: 1px solid #1a4d8c; background: #1a4d8c; color: #fff; border-radius: 6px; padding: .45rem 1rem; cursor: pointer; font-weight: 600; }
.btn-primary:disabled { opacity: .6; cursor: not-allowed; }
</style>
