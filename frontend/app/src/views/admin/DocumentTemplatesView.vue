<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useDocumentTemplatesStore } from '@/stores/documentTemplates'
import { useQuestionnaireStore } from '@/stores/questionnaire'
import { useNotificationStore } from '@/stores/notification'
import { intakeApi } from '@quvian/shared/api/intakeApi'
import type { DocumentStrategy, DocumentTemplateListItem } from '@quvian/shared/api/intakeApi'
import type { ComponentType } from '@quvian/shared/types/questionnaire'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import MappingBuilder from '@/components/intake/MappingBuilder.vue'
import RichTextEditor from '@/components/forms/RichTextEditor.vue'
import CanvasDesigner, { type CanvasLayout } from '@/views/admin/CanvasDesigner.vue'
import ImageAssetBank, { type ImageAsset } from '@/views/admin/ImageAssetBank.vue'
import { PDFDocument } from 'pdf-lib'

// ADR-0039 §3 firm-library DocumentTemplates admin page. Same shape as
// IntakeTemplatesView (Questionnaire library). The visual field-mapper
// for MappingJson lands in a follow-up phase; this phase ships JSON
// paste so an admin can author + retire templates from a UI.

const store = useDocumentTemplatesStore()
const questionnaireStore = useQuestionnaireStore()
const notify = useNotificationStore()

const INPUT_TYPES: ComponentType[] = [
  'text', 'textarea', 'richtext', 'number', 'email', 'phone',
  'date', 'time', 'datetime', 'select', 'radio', 'checkbox',
  'toggle', 'file-upload', 'address',
]

const formOpen = ref(false)
const formId = ref<string | null>(null)
const formName = ref('')
const formStrategy = ref<DocumentStrategy>('Generate')
const formBaseAssetRef = ref('')
const formMappingJson = ref('{}')
// HTML body used when Strategy = Generate (no-base-file). The Quill
// editor binds to this; on save we serialize it into formMappingJson
// as { kind: 'html-richtext', html: '...' } so the persisted shape
// stays a single JSON document.
const formGenerateHtml = ref('')
const formQuestionnaireId = ref<string>('')
// Canvas strategy holds the visual layout JSON here while the editor is
// open; on save we serialise it into formMappingJson so the persisted
// shape stays a single JSON document like every other strategy.
const formCanvasLayout = ref<CanvasLayout | null>(null)
// Firm-wide image bank — the ImageAssetBank component loads its own
// list from the backend (DocumentTemplateImage API). Templates store
// references by id only; no per-template image bytes in MappingJson.
const imageDialogOpen = ref(false)
const generateEditorRef = ref<{ insertHtml?: (html: string) => void } | null>(null)

function uid(): string { return crypto.randomUUID() }

function onPickImage(asset: ImageAsset) {
  if (formStrategy.value === 'Generate') {
    // Append the image tag to the v-model. The RichTextEditor's
    // watcher rewrites Quill's innerHTML when modelValue changes, so
    // the image renders inside the editor without going through
    // Quill's clipboard module (which silently dropped the <img>
    // when the modal close racted with focus).
    const imgHtml = `<img src="${asset.dataUrl}" alt="${asset.name}" data-image-asset-id="${asset.id}" style="max-width:100%" />`
    const current = formGenerateHtml.value ?? ''
    formGenerateHtml.value = current + `<p>${imgHtml}</p>`
    notify.success(`Inserted "${asset.name}"`)
    return
  }
  if (formStrategy.value === 'Canvas') {
    // Drop a new positioned image field at the top-left of the stage
    // referencing the asset id (and inlining the data URL so the
    // canvas renderer doesn't have to look it up at draw time).
    const layout = formCanvasLayout.value
      ?? { width: 1240, height: 877, backgroundDataUrl: null, fields: [] }
    layout.fields = [...layout.fields, {
      id: uid(),
      kind: 'image' as const,
      x: 80, y: 80, width: 200, height: 200,
      imageDataUrl: asset.dataUrl,
      imageAssetId: asset.id,
    }]
    formCanvasLayout.value = layout
    return
  }
  if (formStrategy.value === 'Overlay') {
    // Overlay image fields aren't wired yet — surface a TODO toast
    // instead of silently no-op'ing.
    notify.error('Overlay image fields coming next; for now insert under Generate or Canvas.')
    return
  }
}
const submitting = ref(false)

// === Overlay field editor ===
type OverlayField = { name: string; defaultValue: string; x: number; y: number; page: number }
const overlayFields = ref<OverlayField[]>([])
const addFieldName = ref('')
const addFieldCustom = ref('')
const addFieldDefault = ref('')
const addFieldX = ref(150)
const addFieldY = ref(150)
const addFieldPage = ref(1)

function resetOverlayAddForm() {
  addFieldName.value = ''
  addFieldCustom.value = ''
  addFieldDefault.value = ''
  addFieldX.value = 150
  addFieldY.value = 150
  addFieldPage.value = 1
}

function addOverlayField() {
  const finalName = (addFieldName.value === '__custom__'
    ? addFieldCustom.value
    : addFieldName.value).trim()
  if (!finalName) {
    notify.error('Field name is required.')
    return
  }
  if (overlayFields.value.some(f => f.name === finalName)) {
    notify.error(`Field "${finalName}" is already on the list.`)
    return
  }
  overlayFields.value.push({
    name: finalName,
    defaultValue: addFieldDefault.value,
    x: Number(addFieldX.value) || 0,
    y: Number(addFieldY.value) || 0,
    page: Math.max(1, Number(addFieldPage.value) || 1),
  })
  resetOverlayAddForm()
}

function removeOverlayField(name: string) {
  overlayFields.value = overlayFields.value.filter(f => f.name !== name)
}

// === AcroFormFill detection + mapping ===
type AcroMapping = {
  pdfFieldName: string
  mappingType: 'questionnaire' | 'literal' | 'unmapped'
  questionnaireFieldLabel: string
  literalValue: string
}
const acroFields = ref<AcroMapping[]>([])
const acroDetecting = ref(false)
const acroDetectFileName = ref('')
// Pending plaintext PDF bytes captured by the detect picker; uploaded
// as the template's base asset after the template row itself is
// created/updated.
const pendingAssetBase64 = ref<string | null>(null)
const pendingAssetContentType = ref<string>('application/pdf')

function normalizeKey(s: string): string {
  return s.toLowerCase().replace(/[^a-z0-9]/g, '')
}

function autoMatchField(pdfFieldName: string): string {
  const target = normalizeKey(pdfFieldName)
  if (!target) {
    return ''
  }
  let best: { label: string; score: number } | null = null
  for (const f of availableFields.value) {
    const key = normalizeKey(f.label)
    if (!key) {
      continue
    }
    let score = 0
    if (key === target) {
      score = 100
    } else if (key.includes(target) || target.includes(key)) {
      score = Math.min(key.length, target.length)
    }
    if (score > 0 && (!best || score > best.score)) {
      best = { label: f.label, score }
    }
  }
  return best?.label ?? ''
}

async function onAcroDetectFile(e: Event) {
  const target = e.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) {
    return
  }
  acroDetectFileName.value = file.name
  acroDetecting.value = true
  try {
    const bytes = await file.arrayBuffer()
    pendingAssetBase64.value = bytesToBase64(new Uint8Array(bytes))
    pendingAssetContentType.value = file.type || 'application/pdf'
    let names: string[] = []
    try {
      const doc = await PDFDocument.load(bytes, { ignoreEncryption: true })
      names = doc.getForm().getFields().map(f => f.getName())
    } catch {
      // pdf-lib can't parse XFA / encrypted forms; the PyMuPDF fallback
      // below handles them.
    }
    if (names.length === 0) {
      try {
        const res = await intakeApi.pdfServiceExtractFields({
          bytesBase64: pendingAssetBase64.value,
          filename: file.name,
        })
        if (res.data.success && res.data.data) {
          names = (res.data.data.fields ?? []).map(f => f.name).filter(Boolean)
        }
      } catch (err: unknown) {
        notify.error(extractMessage(err)
          || 'Browser parse found 0 fields and the WebApi → pdf-service bridge failed (is the Python service running?)')
      }
    }
    const existingByName = new Map(acroFields.value.map(a => [a.pdfFieldName, a]))
    const next: AcroMapping[] = []
    for (const name of names) {
      const prev = existingByName.get(name)
      if (prev) {
        next.push(prev)
        continue
      }
      const match = autoMatchField(name)
      next.push({
        pdfFieldName: name,
        mappingType: match ? 'questionnaire' : 'unmapped',
        questionnaireFieldLabel: match,
        literalValue: '',
      })
    }
    acroFields.value = next
    if (next.length === 0) {
      notify.error('No fillable form fields found (tried in-browser and pdf-service).')
    } else {
      const matched = next.filter(f => f.mappingType === 'questionnaire').length
      notify.success(`Detected ${next.length} field(s); auto-matched ${matched}.`)
    }
  } catch (err: unknown) {
    notify.error((err as Error).message || 'Failed to parse PDF fields.')
  } finally {
    acroDetecting.value = false
    // Reset the input so picking the same file again re-fires
    target.value = ''
  }
}

function removeAcroField(pdfFieldName: string) {
  acroFields.value = acroFields.value.filter(f => f.pdfFieldName !== pdfFieldName)
}

function clearAcroFields() {
  if (acroFields.value.length === 0) {
    return
  }
  if (!confirm('Drop all detected AcroForm field mappings?')) {
    return
  }
  acroFields.value = []
  acroDetectFileName.value = ''
  pendingAssetBase64.value = null
}

function bytesToBase64(bytes: Uint8Array): string {
  let binary = ''
  const chunkSize = 0x8000
  for (let i = 0; i < bytes.length; i += chunkSize) {
    binary += String.fromCharCode(...bytes.subarray(i, i + chunkSize))
  }
  return btoa(binary)
}

function base64ToBytes(b64: string): Uint8Array {
  const binary = atob(b64)
  const bytes = new Uint8Array(binary.length)
  for (let i = 0; i < binary.length; i++) {
    bytes[i] = binary.charCodeAt(i)
  }
  return bytes
}

const selectedQuestionnaire = computed(() => {
  if (!formQuestionnaireId.value) {
    return null
  }
  return questionnaireStore.questionnaires.find(q => q.id === formQuestionnaireId.value) ?? null
})

const availableFields = computed(() => {
  const q = selectedQuestionnaire.value
  if (!q) {
    return []
  }
  const fields: Array<{ id: string; label: string; type: string; required: boolean }> = []
  for (const page of q.pages) {
    for (const section of page.sections) {
      for (const group of section.groups) {
        for (const item of group.items) {
          if (INPUT_TYPES.includes(item.type) && item.label?.fallback) {
            fields.push({
              id: item.id,
              label: item.label.fallback,
              type: item.type,
              required: item.required || false,
            })
          }
        }
      }
    }
  }
  return fields.sort((a, b) => a.label.localeCompare(b.label))
})

function openCreate() {
  formId.value = null
  formName.value = ''
  formStrategy.value = 'Generate'
  formBaseAssetRef.value = ''
  formMappingJson.value = '{}'
  formGenerateHtml.value = ''
  formQuestionnaireId.value = ''
  overlayFields.value = []
  resetOverlayAddForm()
  acroFields.value = []
  acroDetectFileName.value = ''
  formCanvasLayout.value = null
  formOpen.value = true
}

async function openEdit(row: DocumentTemplateListItem) {
  formId.value = row.documentTemplateId
  formName.value = row.name
  formStrategy.value = row.strategy
  formBaseAssetRef.value = row.baseAssetRef ?? ''
  formMappingJson.value = '{}'
  formGenerateHtml.value = ''
  formQuestionnaireId.value = ''
  overlayFields.value = []
  resetOverlayAddForm()
  acroFields.value = []
  acroDetectFileName.value = ''
  pendingAssetBase64.value = null
  formCanvasLayout.value = null
  formOpen.value = true
  if (row.strategy === 'Overlay' || row.strategy === 'AcroFormFill') {
    try {
      const assetRes = await intakeApi.getDocumentTemplateAsset(row.documentTemplateId)
      if (assetRes.data.success && assetRes.data.data) {
        acroDetectFileName.value = assetRes.data.data.filename
        if (row.strategy === 'AcroFormFill') {
          const bytes = base64ToBytes(assetRes.data.data.bytesBase64)
          try {
            const doc = await PDFDocument.load(bytes, { ignoreEncryption: true })
            const names = doc.getForm().getFields().map(f => f.getName())
            // Names will get reconciled with the saved mapping below once
            // mappingJson is parsed. Stash the detected list so the
            // mapping shape is populated even if mappingJson is empty.
            if (acroFields.value.length === 0) {
              acroFields.value = names.map(n => ({
                pdfFieldName: n,
                mappingType: 'unmapped' as const,
                questionnaireFieldLabel: '',
                literalValue: '',
              }))
            }
          } catch {
            // PDF parse failure — the user can re-upload to re-detect
          }
        }
      }
    } catch {
      // no asset yet — fine on first edit pass
    }
  }
  const full = await store.getTemplate(row.documentTemplateId)
  if (full) {
    formMappingJson.value = full.mappingJson || '{}'
    if (row.strategy === 'Generate') {
      try {
        const parsed = JSON.parse(full.mappingJson || '{}')
        if (parsed && parsed.kind === 'html-richtext' && typeof parsed.html === 'string') {
          formGenerateHtml.value = parsed.html
        }
        // ADR-0044 follow-up: seeded DocumentTemplates carry
        // sourceQuestionnaireTemplateId so the Variables-source dropdown
        // auto-selects the matching questionnaire.
        if (parsed && typeof parsed.sourceQuestionnaireTemplateId === 'string') {
          formQuestionnaireId.value = parsed.sourceQuestionnaireTemplateId
        }
      } catch {
        // mappingJson isn't the new richtext shape — leave editor blank
      }
    } else if (row.strategy === 'Overlay') {
      try {
        const parsed = JSON.parse(full.mappingJson || '{}')
        if (parsed && parsed.kind === 'overlay-fields' && Array.isArray(parsed.fields)) {
          overlayFields.value = parsed.fields
            .filter((f: unknown): f is OverlayField => {
              const r = f as Record<string, unknown>
              return typeof r?.name === 'string'
            })
            .map((f: OverlayField) => ({
              name: String(f.name),
              defaultValue: String(f.defaultValue ?? ''),
              x: Number(f.x) || 0,
              y: Number(f.y) || 0,
              page: Math.max(1, Number(f.page) || 1),
            }))
        }
      } catch {
        // legacy mapping JSON — keep table empty
      }
    } else if (row.strategy === 'AcroFormFill') {
      try {
        const parsed = JSON.parse(full.mappingJson || '{}')
        if (parsed && parsed.kind === 'acroform-mapping') {
          if (Array.isArray(parsed.acroFields)) {
            acroFields.value = parsed.acroFields
              .filter((f: unknown) => typeof (f as Record<string, unknown>)?.pdfFieldName === 'string')
              .map((f: Record<string, unknown>) => ({
                pdfFieldName: String(f.pdfFieldName),
                mappingType: (f.mappingType === 'questionnaire' || f.mappingType === 'literal')
                  ? f.mappingType
                  : 'unmapped' as const,
                questionnaireFieldLabel: String(f.questionnaireFieldLabel ?? ''),
                literalValue: String(f.literalValue ?? ''),
              }))
          }
          if (Array.isArray(parsed.overlayFields)) {
            overlayFields.value = parsed.overlayFields
              .filter((f: unknown) => typeof (f as Record<string, unknown>)?.name === 'string')
              .map((f: Record<string, unknown>) => ({
                name: String(f.name),
                defaultValue: String(f.defaultValue ?? ''),
                x: Number(f.x) || 0,
                y: Number(f.y) || 0,
                page: Math.max(1, Number(f.page) || 1),
              }))
          }
        }
      } catch {
        // legacy mapping JSON — keep tables empty
      }
    } else if (row.strategy === 'Canvas') {
      try {
        const parsed = JSON.parse(full.mappingJson || '{}')
        if (parsed && parsed.kind === 'canvas-layout' && parsed.layout) {
          formCanvasLayout.value = parsed.layout as CanvasLayout
        }
      } catch {
        // legacy / empty mappingJson — start with a blank canvas
      }
    }
  } else {
    formOpen.value = false
  }
}

function cancelForm() {
  formOpen.value = false
  formId.value = null
}

async function handleSubmit() {
  if (!formName.value.trim() || submitting.value) {
    return
  }
  // Overlay + AcroFormFill use the per-template DocumentTemplateAsset
  // upload as the base PDF. Warn if a brand-new row has nothing
  // attached yet, but let the user save — they can upload from the
  // edit view.
  if (formStrategy.value !== 'Generate'
      && !pendingAssetBase64.value
      && formId.value === null) {
    if (!confirm('No base PDF uploaded yet for this template. Save anyway and upload later?')) {
      return
    }
  }
  let mappingJsonToSave = formMappingJson.value || '{}'
  if (formStrategy.value === 'Generate') {
    mappingJsonToSave = JSON.stringify({
      kind: 'html-richtext',
      html: formGenerateHtml.value || '',
      // Preserve the questionnaire link so the generator can fetch the
      // matching questionnaire definition at render time, and the editor
      // re-loads it the next time the row is opened.
      sourceQuestionnaireTemplateId: formQuestionnaireId.value || undefined,
      title: formName.value || undefined,
    })
  } else if (formStrategy.value === 'Overlay') {
    mappingJsonToSave = JSON.stringify({
      kind: 'overlay-fields',
      fields: overlayFields.value,
    })
  } else if (formStrategy.value === 'AcroFormFill') {
    mappingJsonToSave = JSON.stringify({
      kind: 'acroform-mapping',
      acroFields: acroFields.value,
      overlayFields: overlayFields.value,
    })
  } else if (formStrategy.value === 'Canvas') {
    mappingJsonToSave = JSON.stringify({
      kind: 'canvas-layout',
      layout: formCanvasLayout.value ?? { width: 1240, height: 877, backgroundDataUrl: null, fields: [] },
    })
  } else {
    try {
      JSON.parse(mappingJsonToSave)
    } catch {
      notify.error('Mapping JSON is not valid JSON.')
      return
    }
  }
  submitting.value = true
  const body = {
    name: formName.value.trim(),
    strategy: formStrategy.value,
    baseAssetRef: formBaseAssetRef.value.trim() || null,
    mappingJson: mappingJsonToSave,
  }
  let savedId: string | null = null
  try {
    if (formId.value === null) {
      const res = await intakeApi.createDocumentTemplate(body)
      if (res.data.success && res.data.data) {
        savedId = res.data.data.documentTemplateId
        notify.success(`Created "${body.name}"`)
      }
    } else {
      const res = await intakeApi.updateDocumentTemplate(formId.value, body)
      if (res.data.success) {
        savedId = formId.value
        notify.success(`Updated "${body.name}"`)
      }
    }
  } catch (err: unknown) {
    notify.error(extractMessage(err) || 'Failed to save document template')
  }

  if (savedId && pendingAssetBase64.value) {
    try {
      await intakeApi.uploadDocumentTemplateAsset(savedId, {
        filename: acroDetectFileName.value || 'template.pdf',
        contentType: pendingAssetContentType.value,
        bytesBase64: pendingAssetBase64.value,
      })
      notify.success('Base PDF uploaded')
      pendingAssetBase64.value = null
    } catch (err: unknown) {
      notify.error(extractMessage(err) || 'Template saved, but PDF upload failed')
    }
  }

  await store.fetchTemplates()
  submitting.value = false
  if (savedId) {
    formOpen.value = false
    formId.value = null
  }
}

function extractMessage(err: unknown): string | undefined {
  return (err as { response?: { data?: { message?: string } } })?.response?.data?.message
    || (err as Error)?.message
}

async function handleDelete(row: DocumentTemplateListItem) {
  if (!confirm(`Delete document template "${row.name}"? Historical IntakeOutputs that were generated from this template keep their bytes; the template row stays soft-deleted so the lineage still resolves.`)) {
    return
  }
  await store.deleteTemplate(row.documentTemplateId)
}

async function handleRestore(row: DocumentTemplateListItem) {
  await store.restoreTemplate(row.documentTemplateId)
}

function strategyBadgeClass(s: DocumentStrategy): string {
  return s === 'Generate' ? 'badge-blue'
    : s === 'Overlay' ? 'badge-green'
    : 'badge-purple'
}

function fmtDate(iso: string): string {
  try {
    return new Date(iso).toLocaleString()
  } catch {
    return iso
  }
}

onMounted(async () => {
  await Promise.all([
    store.fetchTemplates(),
    questionnaireStore.fetchQuestionnaires().catch(() => { /* optional dependency */ }),
  ])
})
</script>

<template>
  <div class="page">
    <h1>Document Templates</h1>
    <p class="muted">
      Firm-wide document templates (ADR-0039 §3). Each carries a strategy
      (<strong>Generate</strong> from scratch with pdf-lib, <strong>Overlay</strong> a
      base PDF, or <strong>AcroFormFill</strong> form fields via the firm-local Python
      service) and a mapping that binds questionnaire field ids to the document's
      placeholders.
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
            {{ formOpen ? 'Cancel' : '+ New document template' }}
          </button>
        </div>
      </header>

      <div v-if="formOpen" class="composer">
        <label class="form-row">
          <span>Name</span>
          <input v-model="formName" class="text-input" placeholder="e.g. Intake Acknowledgement Letter" maxlength="200" />
        </label>
        <label class="form-row">
          <span>Strategy</span>
          <select v-model="formStrategy" class="text-input small">
            <option value="Generate">Generate (pdf-lib, no base file)</option>
            <option value="Overlay">Overlay (draw on top of a base PDF)</option>
            <option value="AcroFormFill">AcroFormFill (Python service)</option>
            <option value="Canvas">Canvas (visual designer)</option>
          </select>
        </label>
        <!--
          Legacy `BaseAssetRef` SharedFile-id path is no longer exposed
          to the user. The PDF picker in the Overlay / AcroFormFill
          branches uploads bytes directly into DocumentTemplateAsset,
          which is the only base-PDF concept the UI surfaces now. The
          column stays on the backend for older rows but the frontend
          neither reads nor writes it.
        -->

        <div v-if="formStrategy === 'Generate' || formStrategy === 'Overlay' || formStrategy === 'Canvas'"
          class="form-row align-top">
          <span class="form-label">Images</span>
          <button type="button" class="btn-ghost" @click="imageDialogOpen = true">
            🖼 Open image bank
          </button>
        </div>

        <div class="form-row align-top">
          <span class="form-label">{{ formStrategy === 'Generate' ? 'Content' : 'Mapping' }}</span>
        </div>
        <template v-if="formStrategy === 'Generate'">
          <label class="form-row">
            <span>Variables source</span>
            <select v-model="formQuestionnaireId" class="text-input small">
              <option value="">— pick a questionnaire (optional) —</option>
              <option v-for="q in questionnaireStore.questionnaires" :key="q.id" :value="q.id">
                {{ q.name?.fallback || 'Untitled' }}
              </option>
            </select>
          </label>
          <p v-if="selectedQuestionnaire" class="muted small caption">
            {{ availableFields.length }} fields available for variables.
          </p>
          <p v-else class="muted small caption">
            Choose a questionnaire above to enable variable insertion in the editor.
          </p>
          <RichTextEditor
            ref="generateEditorRef"
            v-model="formGenerateHtml"
            mode="builder"
            :selected-questionnaire="selectedQuestionnaire"
            :available-fields="availableFields"
          />
          <p class="muted small">
            Write the PDF body WYSIWYG. Insert questionnaire variables as
            <code v-pre>{{fieldName}}</code> placeholders; the Generate strategy
            renders them inline at generation time.
          </p>
        </template>
        <template v-else-if="formStrategy === 'Overlay'">
          <label class="form-row">
            <span>Variables source</span>
            <select v-model="formQuestionnaireId" class="text-input small">
              <option value="">— pick a questionnaire (optional) —</option>
              <option v-for="q in questionnaireStore.questionnaires" :key="q.id" :value="q.id">
                {{ q.name?.fallback || 'Untitled' }}
              </option>
            </select>
          </label>
          <div class="overlay-editor">
            <div class="overlay-pane">
              <h4>Add Text Field</h4>
              <label class="overlay-row">
                <span>Field name</span>
                <select v-model="addFieldName" class="text-input small">
                  <option value="">— pick a questionnaire field —</option>
                  <option v-for="f in availableFields" :key="f.id" :value="f.label">
                    {{ f.label }}
                  </option>
                  <option value="__custom__">Custom (type below)…</option>
                </select>
              </label>
              <label v-if="addFieldName === '__custom__'" class="overlay-row">
                <span>Custom name</span>
                <input v-model="addFieldCustom" class="text-input small" placeholder="e.g. petitioner_name" />
              </label>
              <label class="overlay-row">
                <span>Default value</span>
                <input v-model="addFieldDefault" class="text-input small" placeholder="Preview / fallback text" />
              </label>
              <div class="overlay-row coords">
                <label>
                  <span>X</span>
                  <input v-model.number="addFieldX" type="number" class="text-input small" />
                </label>
                <label>
                  <span>Y</span>
                  <input v-model.number="addFieldY" type="number" class="text-input small" />
                </label>
                <label>
                  <span>Page</span>
                  <input v-model.number="addFieldPage" type="number" min="1" class="text-input small" />
                </label>
              </div>
              <button class="btn-primary small" :disabled="!addFieldName || (addFieldName === '__custom__' && !addFieldCustom.trim())"
                @click="addOverlayField">
                + Add field
              </button>
            </div>

            <div class="overlay-pane">
              <h4>Current Fields ({{ overlayFields.length }})</h4>
              <p v-if="overlayFields.length === 0" class="muted small empty">
                No overlay fields yet. Add one on the left to drop a value at (x, y) on a page.
              </p>
              <ul v-else class="overlay-list">
                <li v-for="f in overlayFields" :key="f.name" class="overlay-item">
                  <div class="overlay-item-body">
                    <div class="overlay-item-name">{{ f.name }}</div>
                    <div class="overlay-item-meta muted small">
                      Value: <strong>{{ f.defaultValue || '—' }}</strong>
                    </div>
                    <span class="badge badge-blue">Page {{ f.page }} at ({{ f.x }}, {{ f.y }})</span>
                  </div>
                  <button class="link-btn link-danger" @click="removeOverlayField(f.name)" aria-label="Remove">×</button>
                </li>
              </ul>
            </div>
          </div>
          <p class="muted small">
            Each field draws its value at the (x, y) coordinate of the given
            page on top of the base PDF supplied as Base asset ref. Coordinates
            are in points from the page's lower-left corner.
          </p>
        </template>
        <template v-else-if="formStrategy === 'AcroFormFill'">
          <label class="form-row">
            <span>Variables source</span>
            <select v-model="formQuestionnaireId" class="text-input small">
              <option value="">— pick a questionnaire (optional) —</option>
              <option v-for="q in questionnaireStore.questionnaires" :key="q.id" :value="q.id">
                {{ q.name?.fallback || 'Untitled' }}
              </option>
            </select>
          </label>

          <div class="acro-detect">
            <label class="acro-detect-pick">
              <span class="btn-secondary small">
                {{ acroDetecting ? 'Detecting…' : 'Detect fields from PDF' }}
              </span>
              <input type="file" accept="application/pdf" :disabled="acroDetecting"
                @change="onAcroDetectFile" hidden />
            </label>
            <span v-if="acroDetectFileName" class="muted small">Loaded: {{ acroDetectFileName }}</span>
            <button v-if="acroFields.length > 0" class="link-btn link-danger" @click="clearAcroFields">
              Clear all
            </button>
          </div>
          <p class="muted small caption">
            Upload the base PDF to read its AcroForm field names. Each detected
            field is auto-matched to the closest questionnaire field by name.
            You can override the mapping, drop it to a literal value, or remove it.
          </p>

          <div v-if="acroFields.length > 0" class="acro-table-wrap">
            <table class="acro-table">
              <thead>
                <tr>
                  <th>PDF field</th>
                  <th>Source</th>
                  <th>Value</th>
                  <th class="actions-col"></th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="row in acroFields" :key="row.pdfFieldName"
                  :class="{ unmapped: row.mappingType === 'unmapped' }">
                  <td class="mono small">{{ row.pdfFieldName }}</td>
                  <td>
                    <select v-model="row.mappingType" class="text-input small">
                      <option value="questionnaire">Questionnaire field</option>
                      <option value="literal">Literal value</option>
                      <option value="unmapped">Leave empty</option>
                    </select>
                  </td>
                  <td>
                    <select v-if="row.mappingType === 'questionnaire'"
                      v-model="row.questionnaireFieldLabel" class="text-input small">
                      <option value="">— pick a questionnaire field —</option>
                      <option v-for="f in availableFields" :key="f.id" :value="f.label">{{ f.label }}</option>
                    </select>
                    <input v-else-if="row.mappingType === 'literal'"
                      v-model="row.literalValue" class="text-input small" placeholder="Literal value" />
                    <span v-else class="muted small">—</span>
                  </td>
                  <td class="actions-col">
                    <button class="link-btn link-danger" @click="removeAcroField(row.pdfFieldName)">×</button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <details class="acro-overlay-extra" :open="overlayFields.length > 0">
            <summary>Extra overlay fields (draw on top, optional)</summary>
            <div class="overlay-editor">
              <div class="overlay-pane">
                <h4>Add Overlay Field</h4>
                <label class="overlay-row">
                  <span>Field name</span>
                  <select v-model="addFieldName" class="text-input small">
                    <option value="">— pick a questionnaire field —</option>
                    <option v-for="f in availableFields" :key="f.id" :value="f.label">{{ f.label }}</option>
                    <option value="__custom__">Custom (type below)…</option>
                  </select>
                </label>
                <label v-if="addFieldName === '__custom__'" class="overlay-row">
                  <span>Custom name</span>
                  <input v-model="addFieldCustom" class="text-input small" />
                </label>
                <label class="overlay-row">
                  <span>Default value</span>
                  <input v-model="addFieldDefault" class="text-input small" />
                </label>
                <div class="overlay-row coords">
                  <label><span>X</span>
                    <input v-model.number="addFieldX" type="number" class="text-input small" /></label>
                  <label><span>Y</span>
                    <input v-model.number="addFieldY" type="number" class="text-input small" /></label>
                  <label><span>Page</span>
                    <input v-model.number="addFieldPage" type="number" min="1" class="text-input small" /></label>
                </div>
                <button class="btn-primary small"
                  :disabled="!addFieldName || (addFieldName === '__custom__' && !addFieldCustom.trim())"
                  @click="addOverlayField">+ Add overlay field</button>
              </div>
              <div class="overlay-pane">
                <h4>Overlay Fields ({{ overlayFields.length }})</h4>
                <p v-if="overlayFields.length === 0" class="muted small empty">
                  No extra overlay fields. Add one to draw a value at fixed coordinates.
                </p>
                <ul v-else class="overlay-list">
                  <li v-for="f in overlayFields" :key="f.name" class="overlay-item">
                    <div class="overlay-item-body">
                      <div class="overlay-item-name">{{ f.name }}</div>
                      <div class="overlay-item-meta muted small">Value: <strong>{{ f.defaultValue || '—' }}</strong></div>
                      <span class="badge badge-blue">Page {{ f.page }} at ({{ f.x }}, {{ f.y }})</span>
                    </div>
                    <button class="link-btn link-danger" @click="removeOverlayField(f.name)">×</button>
                  </li>
                </ul>
              </div>
            </div>
          </details>

          <p class="muted small">
            AcroForm fields are filled by the firm-local Python service.
            Extra overlay fields are drawn on top in the same render pass.
          </p>
        </template>
        <template v-else-if="formStrategy === 'Canvas'">
          <label class="form-row">
            <span class="form-label">Visual layout</span>
          </label>
          <CanvasDesigner v-model="formCanvasLayout" />
          <p class="muted small">
            Drop a background image, add positioned text + image
            fields, save. Text fields render with the standard
            <code v-pre>{{fieldName}}</code> placeholder substitution
            at PDF generation time.
          </p>
        </template>
        <template v-else>
          <MappingBuilder v-model="formMappingJson" />
        </template>
        <div class="form-actions">
          <button class="link-btn" @click="cancelForm">Cancel</button>
          <button class="btn-primary small" :disabled="submitting || !formName.trim()" @click="handleSubmit">
            {{ submitting ? 'Saving…' : (formId === null ? 'Create template' : 'Save changes') }}
          </button>
        </div>
      </div>

      <LoadingSpinner v-if="store.loading" />
      <p v-else-if="store.templates.length === 0" class="muted small empty center">
        No firm document templates yet. Click "New document template" to add one.
      </p>
      <table v-else class="data-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Strategy</th>
            <th>Base asset</th>
            <th>Modified</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="t in store.templates" :key="t.documentTemplateId" :class="{ deleted: !!t.deletedAt }">
            <td><strong>{{ t.name }}</strong>
              <span v-if="t.deletedAt" class="badge badge-gray">deleted</span>
            </td>
            <td><span :class="['badge', strategyBadgeClass(t.strategy)]">{{ t.strategy }}</span></td>
            <td class="muted small mono">
              {{ t.baseAssetRef ? t.baseAssetRef.slice(0, 12) + '…' : '—' }}
            </td>
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

    <!-- Shared image-bank dialog. Opened from any strategy's "Open
         image bank" button; the picked image routes through
         onPickImage which inserts it via the right path for the
         active strategy. -->
    <ImageAssetBank
      :as-dialog="true"
      :open="imageDialogOpen"
      @close="imageDialogOpen = false"
      @pick="onPickImage" />
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
.form-row.align-top { align-items: flex-start; }
.form-row > span { min-width: 8rem; font-size: 0.85rem; color: #4b5563; }
.form-label { font-size: 0.85rem; color: #4b5563; font-weight: 500; }
.form-actions { display: flex; justify-content: flex-end; gap: 0.5rem; }
.text-input { flex: 1; padding: 0.4rem 0.6rem; border: 1px solid var(--border-strong); border-radius: 6px; font-size: 0.9rem; box-sizing: border-box; }
.text-input.small { max-width: 22rem; }
.text-input:disabled { background: var(--surface-3); color: var(--text-subtle); cursor: not-allowed; }
.json-area { font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-size: 0.8rem; min-height: 10rem; }
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
.badge-gray { background: var(--surface-3); color: #4b5563; }
.badge { padding: 0.15rem 0.5rem; border-radius: 999px; font-size: 0.75rem; font-weight: 500; }
.badge-blue { background: #dbeafe; color: #1e3a8a; }
.badge-green { background: #d1fae5; color: #065f46; }
.badge-purple { background: #ede9fe; color: var(--accent-active); }

.overlay-editor { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; margin-top: 0.5rem; }
.overlay-pane { background: white; border: 1px solid var(--border); border-radius: 8px; padding: 0.85rem 1rem; }
.overlay-pane h4 { margin: 0 0 0.6rem 0; font-size: 0.95rem; color: #374151; }
.overlay-row { display: flex; align-items: center; gap: 0.6rem; margin-bottom: 0.5rem; }
.overlay-row > span { min-width: 6.5rem; font-size: 0.8rem; color: #4b5563; }
.overlay-row.coords { gap: 0.4rem; }
.overlay-row.coords label { display: flex; flex-direction: column; gap: 0.15rem; flex: 1; }
.overlay-row.coords span { font-size: 0.75rem; color: var(--text-muted); min-width: auto; }
.overlay-list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 0.4rem; }
.overlay-item {
  display: flex; align-items: center; justify-content: space-between;
  border: 1px solid var(--border); border-radius: 6px; padding: 0.5rem 0.65rem;
}
.overlay-item-body { display: flex; flex-direction: column; gap: 0.15rem; }
.overlay-item-name { font-weight: 600; font-size: 0.9rem; color: #1f2937; }
.overlay-item-meta { font-size: 0.8rem; }

.acro-detect { display: flex; align-items: center; gap: 0.75rem; margin-top: 0.5rem; }
.acro-detect-pick { cursor: pointer; display: inline-flex; align-items: center; }
.btn-secondary { padding: 0.35rem 0.8rem; background: #eef2ff; color: var(--accent-hover); border: 1px solid #c7d2fe; border-radius: 6px; font-weight: 500; cursor: pointer; }
.btn-secondary.small { font-size: 0.85rem; }
.acro-table-wrap { overflow-x: auto; border: 1px solid var(--border); border-radius: 6px; margin-top: 0.5rem; }
.acro-table { width: 100%; border-collapse: collapse; font-size: 0.9rem; }
.acro-table th, .acro-table td { padding: 0.5rem 0.7rem; border-bottom: 1px solid var(--surface-3); text-align: left; vertical-align: middle; }
.acro-table th { font-size: 0.75rem; font-weight: 600; text-transform: uppercase; color: var(--text-muted); background: var(--surface-2); }
.acro-table tr.unmapped td { background: #fff7ed; }
.acro-table .actions-col { width: 2rem; text-align: right; }

.acro-overlay-extra { border: 1px solid var(--border); border-radius: 6px; padding: 0.5rem 0.75rem; margin-top: 0.75rem; background: #fafafa; }
.acro-overlay-extra > summary { cursor: pointer; font-weight: 500; color: #4b5563; font-size: 0.85rem; }
.acro-overlay-extra .overlay-editor { margin-top: 0.6rem; }

.caption { margin: 0; }
</style>
