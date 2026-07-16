<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import type { IntakeMapping, IntakeMappingBlock } from '@/utils/intakePdfGenerate'

// Local strict shape — IntakeMapping.blocks is optional in the
// runtime/persistence shape, but inside the builder it is always
// initialized to [], so we type it as required to avoid threading
// undefined checks through every iteration.
interface BuilderMapping {
  title: string
  subtitle: string
  blocks: IntakeMappingBlock[]
}

// ADR-0039 §3 visual field-mapper for DocumentTemplate.MappingJson.
// Block-list editor for the v1 IntakeMapping shape: title + subtitle
// + ordered blocks (heading / paragraph / field / divider). Lives on
// top of the JSON string used elsewhere — parses in, edits as a
// typed object, re-serializes on every change so the parent's
// existing JSON form-field continues to work unchanged. A "Show JSON"
// escape hatch surfaces the raw text for advanced edits.

const props = defineProps<{
  modelValue: string
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', json: string): void
}>()

type BlockType = IntakeMappingBlock['type']
const BLOCK_TYPES: { value: BlockType; label: string }[] = [
  { value: 'heading',   label: 'Heading' },
  { value: 'paragraph', label: 'Paragraph' },
  { value: 'field',     label: 'Field' },
  { value: 'divider',   label: 'Divider' },
]

function emptyMapping(): BuilderMapping {
  return { title: '', subtitle: '', blocks: [] }
}

function parse(raw: string): { mapping: BuilderMapping; error: string | null } {
  if (!raw || !raw.trim()) {
    return { mapping: emptyMapping(), error: null }
  }
  try {
    const obj = JSON.parse(raw) as Partial<IntakeMapping>
    const mapping: BuilderMapping = {
      title: typeof obj.title === 'string' ? obj.title : '',
      subtitle: typeof obj.subtitle === 'string' ? obj.subtitle : '',
      blocks: Array.isArray(obj.blocks)
        ? obj.blocks.filter((b): b is IntakeMappingBlock => !!b && typeof b === 'object')
        : [],
    }
    // Promote a legacy top-level fields[] into blocks[] so older
    // templates open cleanly. The serializer always emits blocks[].
    if (mapping.blocks.length === 0 && Array.isArray(obj.fields)) {
      mapping.blocks = obj.fields.map(f => ({
        type: 'field',
        label: f.label,
        valueFrom: f.valueFrom,
        literal: f.literal,
      }))
    }
    return { mapping, error: null }
  } catch (e) {
    return { mapping: emptyMapping(), error: (e as Error).message }
  }
}

const initial = parse(props.modelValue)
const mapping = ref<BuilderMapping>(initial.mapping)
const parseError = ref<string | null>(initial.error)
const showJson = ref(false)
const jsonDraft = ref(props.modelValue || '{}')

// Re-parse when the parent swaps in a fresh value (e.g. user opens
// a different template for edit). Only when the incoming JSON does
// not match what we just emitted, to avoid feedback loops.
let lastEmitted = props.modelValue
watch(() => props.modelValue, (next) => {
  if (next === lastEmitted) {
    return
  }
  const r = parse(next)
  mapping.value = r.mapping
  parseError.value = r.error
  jsonDraft.value = next
})

function serialize(): string {
  const m = mapping.value
  // Drop empty title/subtitle so re-loads don't show stale blank lines.
  const out: IntakeMapping = {
    ...(m.title.trim() ? { title: m.title.trim() } : {}),
    ...(m.subtitle.trim() ? { subtitle: m.subtitle.trim() } : {}),
    blocks: m.blocks.map(b => sanitizeBlock(b)),
  }
  const s = JSON.stringify(out, null, 2)
  lastEmitted = s
  return s
}

function sanitizeBlock(b: IntakeMappingBlock): IntakeMappingBlock {
  if (b.type === 'heading' || b.type === 'paragraph') {
    return { type: b.type, text: b.text ?? '' }
  }
  if (b.type === 'divider') {
    return { type: 'divider' }
  }
  // field
  return {
    type: 'field',
    label: b.label ?? '',
    ...(b.valueFrom ? { valueFrom: b.valueFrom } : {}),
    ...(b.literal !== undefined && b.literal !== '' ? { literal: b.literal } : {}),
  }
}

function pushEdit() {
  parseError.value = null
  jsonDraft.value = serialize()
  emit('update:modelValue', jsonDraft.value)
}

function addBlock(type: BlockType) {
  const next: IntakeMappingBlock = type === 'divider'
    ? { type: 'divider' }
    : type === 'field'
      ? { type: 'field', label: '', valueFrom: '' }
      : { type, text: '' }
  mapping.value.blocks = [...mapping.value.blocks, next]
  pushEdit()
}

function removeBlock(idx: number) {
  mapping.value.blocks = mapping.value.blocks.filter((_, i) => i !== idx)
  pushEdit()
}

function moveBlock(idx: number, delta: -1 | 1) {
  const arr = [...mapping.value.blocks]
  const j = idx + delta
  if (j < 0 || j >= arr.length) {
    return
  }
  [arr[idx], arr[j]] = [arr[j], arr[idx]]
  mapping.value.blocks = arr
  pushEdit()
}

function applyJsonDraft() {
  const r = parse(jsonDraft.value)
  if (r.error) {
    parseError.value = r.error
    return
  }
  mapping.value = r.mapping
  parseError.value = null
  showJson.value = false
  // Re-serialize from the parsed mapping so the parent sees the
  // canonical form (drops unknown properties, normalizes spacing).
  jsonDraft.value = serialize()
  emit('update:modelValue', jsonDraft.value)
}

const blockCount = computed(() => mapping.value.blocks.length)
</script>

<template>
  <div class="builder">
    <div class="head">
      <strong>Mapping builder</strong>
      <span class="muted small">({{ blockCount }} block{{ blockCount === 1 ? '' : 's' }})</span>
      <button class="link-btn" @click="showJson = !showJson">
        {{ showJson ? 'Hide JSON' : 'Show JSON' }}
      </button>
    </div>

    <p v-if="parseError" class="error small">
      Could not parse the saved JSON ({{ parseError }}). Starting from an empty mapping —
      the existing JSON is preserved below; use "Show JSON" if you'd rather hand-edit it.
    </p>

    <label class="form-row">
      <span>Title</span>
      <input v-model="mapping.title" class="text-input" placeholder="Optional H1 at the top of the PDF"
        @change="pushEdit" />
    </label>
    <label class="form-row">
      <span>Subtitle</span>
      <input v-model="mapping.subtitle" class="text-input" placeholder="Optional smaller line below the title"
        @change="pushEdit" />
    </label>

    <div class="blocks">
      <p v-if="mapping.blocks.length === 0" class="muted small empty">
        No blocks yet. Use the buttons below to add a heading, paragraph, field, or divider.
      </p>
      <div v-for="(b, idx) in mapping.blocks" :key="idx" class="block">
        <header class="block-head">
          <span class="badge">{{ b.type }}</span>
          <button class="link-btn" :disabled="idx === 0" @click="moveBlock(idx, -1)">↑</button>
          <button class="link-btn" :disabled="idx === mapping.blocks.length - 1" @click="moveBlock(idx, 1)">↓</button>
          <button class="link-btn link-danger" @click="removeBlock(idx)">Remove</button>
        </header>

        <template v-if="b.type === 'heading' || b.type === 'paragraph'">
          <textarea v-model="b.text" class="text-input"
            :placeholder="b.type === 'heading' ? 'Section heading text' : 'Paragraph text — wraps automatically.'"
            :rows="b.type === 'heading' ? 1 : 3" @change="pushEdit" />
        </template>
        <template v-else-if="b.type === 'field'">
          <div class="field-row">
            <label class="field-label">
              <span>Label</span>
              <input v-model="b.label" class="text-input" placeholder="Client name" @change="pushEdit" />
            </label>
            <label class="field-label">
              <span>Value from</span>
              <input v-model="b.valueFrom" class="text-input" placeholder="client.name (dotted path)" @change="pushEdit" />
            </label>
            <label class="field-label">
              <span>or Literal</span>
              <input v-model="b.literal" class="text-input" placeholder="(static text — leave empty if Value from is set)"
                @change="pushEdit" />
            </label>
          </div>
        </template>
        <template v-else-if="b.type === 'divider'">
          <p class="muted small">Horizontal rule.</p>
        </template>
      </div>
    </div>

    <div class="add-row">
      <span class="muted small">Add block:</span>
      <button v-for="t in BLOCK_TYPES" :key="t.value" class="btn-add small" @click="addBlock(t.value)">
        + {{ t.label }}
      </button>
    </div>

    <div v-if="showJson" class="json-editor">
      <p class="muted small">
        Raw JSON of the mapping. Click Apply to parse it and replace the builder state;
        invalid JSON shows an inline error and the builder keeps the last good value.
      </p>
      <textarea v-model="jsonDraft" class="text-input json-area" rows="14"></textarea>
      <div class="json-actions">
        <button class="link-btn" @click="jsonDraft = serialize()">Reset from builder</button>
        <button class="btn-primary small" @click="applyJsonDraft">Apply JSON</button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.builder { display: flex; flex-direction: column; gap: 0.5rem; }
.head { display: flex; align-items: center; gap: 0.75rem; }
.muted { color: var(--text-muted); }
.small { font-size: 0.85rem; }
.error { color: #b91c1c; }
.form-row { display: flex; gap: 0.6rem; align-items: center; margin: 0; }
.form-row > span { min-width: 5rem; font-size: 0.85rem; color: #4b5563; }
.text-input { flex: 1; padding: 0.4rem 0.6rem; border: 1px solid var(--border-strong); border-radius: 6px; font-size: 0.9rem; box-sizing: border-box; }
.json-area { font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-size: 0.8rem; min-height: 12rem; }
.empty { padding: 0.5rem 0.75rem; background: var(--surface-2); border-radius: 4px; }
.blocks { display: flex; flex-direction: column; gap: 0.5rem; margin: 0.25rem 0; }
.block { padding: 0.5rem 0.75rem; background: var(--surface-2); border: 1px solid var(--border); border-radius: 6px; display: flex; flex-direction: column; gap: 0.4rem; }
.block-head { display: flex; align-items: center; gap: 0.4rem; }
.badge { padding: 0.15rem 0.45rem; border-radius: 999px; background: #e0e7ff; color: #312e81; font-size: 0.7rem; font-weight: 500; text-transform: uppercase; }
.field-row { display: flex; flex-direction: column; gap: 0.35rem; }
.field-label { display: flex; gap: 0.5rem; align-items: center; }
.field-label > span { min-width: 6rem; font-size: 0.8rem; color: #4b5563; }
.add-row { display: flex; align-items: center; gap: 0.5rem; flex-wrap: wrap; padding-top: 0.25rem; }
.btn-add { padding: 0.25rem 0.55rem; background: white; border: 1px solid var(--border-strong); border-radius: 4px; font-size: 0.85rem; cursor: pointer; color: #4b5563; }
.btn-add:hover { background: var(--surface-3); }
.btn-primary { padding: 0.4rem 0.8rem; background: var(--accent); color: white; border: none; border-radius: 4px; cursor: pointer; font-weight: 500; }
.btn-primary.small { padding: 0.3rem 0.6rem; font-size: 0.85rem; }
.link-btn { background: none; border: none; color: var(--accent); cursor: pointer; font-size: 0.85rem; padding: 0; }
.link-btn:hover { text-decoration: underline; }
.link-btn:disabled { opacity: 0.4; cursor: not-allowed; }
.link-danger { color: #dc2626; }
.json-editor { display: flex; flex-direction: column; gap: 0.5rem; padding: 0.5rem 0.75rem; border: 1px dashed var(--border-strong); border-radius: 6px; }
.json-actions { display: flex; justify-content: flex-end; gap: 0.5rem; }
</style>
