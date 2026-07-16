<script setup lang="ts">
import { ref, watch } from 'vue'
import type { LogicExpression } from '@/utils/intakeRuleEngine'
import LogicNode from './LogicNode.vue'

// ADR-0039 §1 visual rule builder for GenerationRule.RuleJson.
// Edits a LogicExpression tree, re-serializes to JSON on every
// change so the existing form submission flow (which sends a
// ruleJson string) stays unchanged. "Show JSON" escape hatch
// surfaces the raw text for advanced edits.

const props = defineProps<{
  modelValue: string
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', json: string): void
}>()

function emptyRoot(): LogicExpression {
  return { type: 'equals', field: '', value: '' }
}

function parse(raw: string): { root: LogicExpression; error: string | null } {
  if (!raw || !raw.trim()) {
    return { root: emptyRoot(), error: null }
  }
  try {
    const obj = JSON.parse(raw) as LogicExpression
    if (!obj || typeof obj !== 'object' || typeof obj.type !== 'string') {
      return { root: emptyRoot(), error: 'Root must be an object with a "type" field.' }
    }
    return { root: obj, error: null }
  } catch (e) {
    return { root: emptyRoot(), error: (e as Error).message }
  }
}

const initial = parse(props.modelValue)
const root = ref<LogicExpression>(initial.root)
const parseError = ref<string | null>(initial.error)
const showJson = ref(false)
const jsonDraft = ref(props.modelValue || JSON.stringify(emptyRoot(), null, 2))

let lastEmitted = props.modelValue
watch(() => props.modelValue, (next) => {
  if (next === lastEmitted) {
    return
  }
  const r = parse(next)
  root.value = r.root
  parseError.value = r.error
  jsonDraft.value = next
})

function serialize(): string {
  const s = JSON.stringify(root.value, null, 2)
  lastEmitted = s
  return s
}

function onRootUpdate(next: LogicExpression) {
  root.value = next
  parseError.value = null
  jsonDraft.value = serialize()
  emit('update:modelValue', jsonDraft.value)
}

function applyJsonDraft() {
  const r = parse(jsonDraft.value)
  if (r.error) {
    parseError.value = r.error
    return
  }
  root.value = r.root
  parseError.value = null
  showJson.value = false
  jsonDraft.value = serialize()
  emit('update:modelValue', jsonDraft.value)
}

function resetToEmpty() {
  root.value = emptyRoot()
  jsonDraft.value = serialize()
  emit('update:modelValue', jsonDraft.value)
}
</script>

<template>
  <div class="builder">
    <div class="head">
      <strong>Rule expression</strong>
      <button class="link-btn" @click="resetToEmpty">Reset</button>
      <button class="link-btn" @click="showJson = !showJson">
        {{ showJson ? 'Hide JSON' : 'Show JSON' }}
      </button>
    </div>

    <p v-if="parseError" class="error small">
      Could not parse the saved JSON ({{ parseError }}). Starting from an empty rule —
      use "Show JSON" if you'd rather hand-edit it.
    </p>

    <LogicNode :node="root" :removable="false" @update="onRootUpdate" />

    <p class="muted small">
      Same operator vocabulary as the questionnaire renderer's conditional-visibility engine
      (ADR-0039 §1). <em>and / or</em> combine child expressions; <em>not</em> negates a
      single child; everything else compares a field's value against a literal.
    </p>

    <div v-if="showJson" class="json-editor">
      <textarea v-model="jsonDraft" class="text-input json-area" rows="10"></textarea>
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
.text-input { padding: 0.4rem 0.6rem; border: 1px solid var(--border-strong); border-radius: 6px; font-size: 0.9rem; box-sizing: border-box; width: 100%; }
.json-area { font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-size: 0.8rem; min-height: 10rem; }
.btn-primary { padding: 0.4rem 0.8rem; background: var(--accent); color: white; border: none; border-radius: 4px; cursor: pointer; font-weight: 500; }
.btn-primary.small { padding: 0.3rem 0.6rem; font-size: 0.85rem; }
.link-btn { background: none; border: none; color: var(--accent); cursor: pointer; font-size: 0.85rem; padding: 0; }
.link-btn:hover { text-decoration: underline; }
.json-editor { display: flex; flex-direction: column; gap: 0.5rem; padding: 0.5rem 0.75rem; border: 1px dashed var(--border-strong); border-radius: 6px; }
.json-actions { display: flex; justify-content: flex-end; gap: 0.5rem; }
</style>
