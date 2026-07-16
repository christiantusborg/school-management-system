<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useIntakeTemplatesStore } from '@/stores/intakeTemplates'
import { useNotificationStore } from '@/stores/notification'
import type { Questionnaire } from '@quvian/shared/types/questionnaire'
import type { QuestionnaireTemplateListItem } from '@quvian/shared/api/intakeApi'
import LoadingSpinner from '@/components/LoadingSpinner.vue'

// ADR-0039 Phase 2b/2c: firm-library questionnaire-templates page.
// Lists what's there, lets an admin paste a Questionnaire JSON to
// create + edit + soft-delete firm templates. The full drag-and-drop
// builder (Canvas / ComponentPalette / Inspector / Renderer) lands in
// a follow-up phase.

const store = useIntakeTemplatesStore()
const notify = useNotificationStore()

// formId === null means "Create"; a GUID means "Edit that template".
const formOpen = ref(false)
const formId = ref<string | null>(null)
const formName = ref('')
const formVersion = ref('1.0.0')
const formJson = ref('')
const submitting = ref(false)

const starterDefinition = computed<Questionnaire>(() => ({
  version: '1.0.0',
  id: crypto.randomUUID(),
  name: { fallback: formName.value || 'New questionnaire' },
  description: { fallback: '' },
  pages: [
    {
      id: crypto.randomUUID(),
      title: { fallback: 'Page 1' },
      sections: [
        {
          id: crypto.randomUUID(),
          title: { fallback: 'Section 1' },
          groups: [
            {
              id: crypto.randomUUID(),
              title: { fallback: 'Client details' },
              items: [],
            },
          ],
        },
      ],
    },
  ],
}))

function loadStarter() {
  formJson.value = JSON.stringify(starterDefinition.value, null, 2)
}

function openCreate() {
  formId.value = null
  formName.value = ''
  formVersion.value = '1.0.0'
  formJson.value = ''
  formOpen.value = true
}

async function openEdit(row: QuestionnaireTemplateListItem) {
  formId.value = row.questionnaireTemplateId
  formName.value = row.name
  formVersion.value = row.version
  formJson.value = ''
  formOpen.value = true
  const full = await store.getTemplate(row.questionnaireTemplateId)
  if (full) {
    formJson.value = full.definitionJson
  } else {
    formOpen.value = false
  }
}

function cancelForm() {
  formOpen.value = false
  formId.value = null
}

async function handleSubmit() {
  if (!formName.value.trim() || !formJson.value.trim() || submitting.value) {
    return
  }
  try {
    JSON.parse(formJson.value)
  } catch {
    notify.error('Definition is not valid JSON.')
    return
  }
  submitting.value = true
  let ok: boolean
  if (formId.value === null) {
    ok = await store.createTemplate(formName.value.trim(), formJson.value, formVersion.value || '1.0.0')
  } else {
    ok = await store.updateTemplate(formId.value, formName.value.trim(), formJson.value, formVersion.value || '1.0.0')
  }
  submitting.value = false
  if (ok) {
    formOpen.value = false
    formId.value = null
  }
}

async function handleDelete(row: QuestionnaireTemplateListItem) {
  if (!confirm(`Delete template "${row.name}"? Already-submitted responses keep their version stamp and stay readable.`)) {
    return
  }
  await store.deleteTemplate(row.questionnaireTemplateId)
}

async function handleRestore(row: QuestionnaireTemplateListItem) {
  await store.restoreTemplate(row.questionnaireTemplateId)
}

function fmtDate(iso: string): string {
  try {
    return new Date(iso).toLocaleString()
  } catch {
    return iso
  }
}

onMounted(() => store.fetchTemplates())
</script>

<template>
  <div class="page">
    <h1>Questionnaire Templates</h1>
    <p class="muted">
      Firm-wide intake forms (ADR-0039). Each case can attach one of these
      and run an encrypted intake; client answers + generated documents
      always stay end-to-end encrypted inside the case.
    </p>

    <section class="card">
      <header class="row-spread">
        <h2>Library ({{ store.templates.length }})</h2>
        <div class="header-actions">
          <label class="check-row">
            <input type="checkbox" :checked="store.includeDeleted"
              @change="store.setIncludeDeleted(($event.target as HTMLInputElement).checked)" />
            Show deleted
          </label>
          <button class="btn-primary small" @click="formOpen ? cancelForm() : openCreate()">
            {{ formOpen ? 'Cancel' : '+ New template' }}
          </button>
        </div>
      </header>

      <div v-if="formOpen" class="composer">
        <p v-if="formId" class="muted small" style="margin:0">
          Editing template — saving recomputes the version hash; already-submitted
          responses keep the hash they were stamped with.
        </p>
        <label class="form-row">
          <span>Name</span>
          <input v-model="formName" class="text-input" placeholder="e.g. Immigration Case Intake" maxlength="200" />
        </label>
        <label class="form-row">
          <span>Version</span>
          <input v-model="formVersion" class="text-input small" placeholder="1.0.0" maxlength="40" />
        </label>
        <div class="form-row">
          <span class="form-label">Definition JSON</span>
          <button v-if="!formId" class="link-btn" @click="loadStarter">Load starter</button>
        </div>
        <textarea v-model="formJson" class="text-input json-area" rows="14"
          placeholder='Paste a Questionnaire JSON (see /types/questionnaire.ts for the shape).' />
        <p class="muted small">
          Phase 2 ships JSON paste; the drag-and-drop builder lands in a
          follow-up phase. The server stamps a SHA-256 DefinitionHash so
          every submitted response binds to the exact version it was
          filled against.
        </p>
        <div class="form-actions">
          <button class="link-btn" @click="cancelForm">Cancel</button>
          <button class="btn-primary small" :disabled="submitting || !formName.trim() || !formJson.trim()"
            @click="handleSubmit">
            {{ submitting ? 'Saving…' : (formId === null ? 'Create template' : 'Save changes') }}
          </button>
        </div>
      </div>

      <LoadingSpinner v-if="store.loading" />
      <p v-else-if="store.templates.length === 0" class="muted small empty center">
        No firm templates yet. Click "New template" to add one.
      </p>
      <table v-else class="data-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Version</th>
            <th>Hash</th>
            <th>Created</th>
            <th>Modified</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="t in store.templates" :key="t.questionnaireTemplateId" :class="{ deleted: !!t.deletedAt }">
            <td><strong>{{ t.name }}</strong>
              <span v-if="t.deletedAt" class="badge badge-gray">deleted</span>
            </td>
            <td>{{ t.version }}</td>
            <td class="muted small mono">{{ t.definitionHash.slice(0, 12) }}…</td>
            <td class="muted small">{{ fmtDate(t.createdAt) }}</td>
            <td class="muted small">{{ fmtDate(t.modifiedAt) }}</td>
            <td class="row-actions">
              <template v-if="!t.deletedAt">
                <button class="link-btn" @click="openEdit(t)">Edit</button>
                <button class="link-btn link-danger" @click="handleDelete(t)">Delete</button>
              </template>
              <button v-else class="link-btn" @click="handleRestore(t)">Restore</button>
            </td>
          </tr>
        </tbody>
      </table>
    </section>
  </div>
</template>

<style scoped>
.page { max-width: 980px; }
.muted { color: var(--text-muted); }
.small { font-size: 0.85rem; }
.center { text-align: center; }
.mono { font-family: ui-monospace, SFMono-Regular, Menlo, monospace; }
.row-spread { display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.75rem; }
.card { background: white; padding: 1.5rem; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); margin-bottom: 1.5rem; }
.card h2 { margin: 0; font-size: 1.15rem; }
.composer { display: flex; flex-direction: column; gap: 0.5rem; padding: 0.75rem; background: var(--surface-2); border-radius: 6px; margin-bottom: 1rem; }
.form-row { display: flex; gap: 0.6rem; align-items: center; margin: 0; }
.form-row > span { min-width: 6rem; font-size: 0.85rem; color: #4b5563; }
.form-label { font-size: 0.85rem; color: #4b5563; font-weight: 500; }
.form-actions { display: flex; justify-content: flex-end; gap: 0.5rem; }
.text-input { flex: 1; padding: 0.4rem 0.6rem; border: 1px solid var(--border-strong); border-radius: 6px; font-size: 0.9rem; box-sizing: border-box; }
.text-input.small { max-width: 8rem; }
.json-area { font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-size: 0.8rem; min-height: 14rem; }
.btn-primary { padding: 0.5rem 1rem; background: var(--accent); color: white; border: none; border-radius: 6px; cursor: pointer; font-weight: 500; }
.btn-primary.small { padding: 0.35rem 0.75rem; font-size: 0.85rem; }
.btn-primary:disabled { opacity: 0.5; cursor: not-allowed; }
.link-btn { background: none; border: none; color: var(--accent); cursor: pointer; font-size: 0.85rem; padding: 0; }
.link-btn:hover { text-decoration: underline; }
.link-danger { color: #dc2626; }
.row-actions { display: flex; gap: 0.5rem; }
.data-table { width: 100%; border-collapse: collapse; }
.data-table th, .data-table td { padding: 0.6rem 0.75rem; text-align: left; border-bottom: 1px solid var(--surface-3); font-size: 0.9rem; }
.data-table th { font-size: 0.75rem; font-weight: 600; text-transform: uppercase; color: var(--text-muted); }
.empty { background: var(--surface-2); border-radius: 8px; padding: 1.5rem; }
.header-actions { display: flex; align-items: center; gap: 0.75rem; }
.check-row { display: inline-flex; align-items: center; gap: 0.35rem; font-size: 0.85rem; cursor: pointer; user-select: none; color: #4b5563; }
.deleted { opacity: 0.55; }
.badge { padding: 0.15rem 0.5rem; border-radius: 999px; font-size: 0.7rem; font-weight: 500; margin-left: 0.4rem; }
.badge-gray { background: var(--surface-3); color: #4b5563; }
</style>
