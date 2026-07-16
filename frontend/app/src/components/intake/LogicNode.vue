<script setup lang="ts">
import { computed } from 'vue'
import type { LogicExpression, LogicOperator } from '@/utils/intakeRuleEngine'

// Recursive node of the GenerationRule expression builder (ADR-0039
// §1). The whole-tree component (RuleBuilder.vue) owns the root node
// and the v-model:string contract; this component just renders one
// node + its child nodes, emitting `update` upward on every edit.

const props = defineProps<{
  node: LogicExpression
  removable: boolean
}>()

const emit = defineEmits<{
  (e: 'update', next: LogicExpression): void
  (e: 'remove'): void
}>()

const COMPARE_OPS: LogicOperator[] = [
  'equals', 'notEquals',
  'contains', 'notContains',
  'startsWith', 'endsWith',
  'greaterThan', 'lessThan', 'greaterOrEqual', 'lessOrEqual',
]
const EXISTENCE_OPS: LogicOperator[] = ['isEmpty', 'isNotEmpty']
const COMBINE_OPS: LogicOperator[] = ['and', 'or', 'not']

function isLeafCompare(op: LogicOperator): boolean {
  return COMPARE_OPS.includes(op)
}
function isLeafExistence(op: LogicOperator): boolean {
  return EXISTENCE_OPS.includes(op)
}
function isCombinator(op: LogicOperator): boolean {
  return COMBINE_OPS.includes(op)
}

function emptyLeaf(): LogicExpression {
  return { type: 'equals', field: '', value: '' }
}

const operands = computed<LogicExpression[]>(() => Array.isArray(props.node.operands) ? props.node.operands : [])

function onOperatorChange(nextOp: LogicOperator) {
  // Switching shape: drop fields the new operator doesn't use, seed
  // ones it does. and/or default to a single empty leaf; not coerces
  // to exactly one child; existence ops drop value; the four ordered
  // compares keep field + value.
  const base: LogicExpression = { type: nextOp }
  if (isCombinator(nextOp)) {
    if (nextOp === 'not') {
      base.operands = [operands.value[0] ?? emptyLeaf()]
    } else {
      base.operands = operands.value.length > 0 ? operands.value : [emptyLeaf()]
    }
  } else if (isLeafExistence(nextOp)) {
    base.field = props.node.field ?? ''
  } else {
    base.field = props.node.field ?? ''
    base.value = props.node.value ?? ''
  }
  emit('update', base)
}

function onFieldChange(v: string) {
  emit('update', { ...props.node, field: v })
}

function onValueChange(v: string) {
  emit('update', { ...props.node, value: v })
}

function onChildUpdate(idx: number, next: LogicExpression) {
  const arr = [...operands.value]
  arr[idx] = next
  emit('update', { ...props.node, operands: arr })
}

function onChildRemove(idx: number) {
  const arr = operands.value.filter((_, i) => i !== idx)
  emit('update', { ...props.node, operands: arr })
}

function addChild() {
  emit('update', { ...props.node, operands: [...operands.value, emptyLeaf()] })
}

const childRemovable = computed(() => {
  // not: exactly one child; and/or: at least one child must remain.
  if (props.node.type === 'not') {
    return false
  }
  return operands.value.length > 1
})

const valueDisplay = computed(() => {
  if (props.node.value === undefined || props.node.value === null) {
    return ''
  }
  return String(props.node.value)
})
</script>

<template>
  <div :class="['node', isCombinator(node.type) ? 'combinator' : 'leaf']">
    <div class="node-head">
      <select :value="node.type" class="op-select"
        @change="onOperatorChange(($event.target as HTMLSelectElement).value as LogicOperator)">
        <optgroup label="Compare">
          <option v-for="op in COMPARE_OPS" :key="op" :value="op">{{ op }}</option>
        </optgroup>
        <optgroup label="Existence">
          <option v-for="op in EXISTENCE_OPS" :key="op" :value="op">{{ op }}</option>
        </optgroup>
        <optgroup label="Combine">
          <option v-for="op in COMBINE_OPS" :key="op" :value="op">{{ op }}</option>
        </optgroup>
      </select>
      <button v-if="removable" class="link-btn link-danger" @click="emit('remove')">Remove</button>
    </div>

    <template v-if="isLeafCompare(node.type)">
      <div class="leaf-row">
        <input class="text-input" placeholder="field (e.g. client.matterType)"
          :value="node.field ?? ''"
          @change="onFieldChange(($event.target as HTMLInputElement).value)" />
        <input class="text-input" placeholder="value"
          :value="valueDisplay"
          @change="onValueChange(($event.target as HTMLInputElement).value)" />
      </div>
    </template>
    <template v-else-if="isLeafExistence(node.type)">
      <div class="leaf-row">
        <input class="text-input" placeholder="field (e.g. petitioner.email)"
          :value="node.field ?? ''"
          @change="onFieldChange(($event.target as HTMLInputElement).value)" />
        <span class="muted small">(no value — operator only checks presence)</span>
      </div>
    </template>
    <template v-else>
      <div class="children">
        <LogicNode v-for="(c, idx) in operands" :key="idx" :node="c" :removable="childRemovable"
          @update="next => onChildUpdate(idx, next)"
          @remove="onChildRemove(idx)" />
        <button v-if="node.type !== 'not' || operands.length === 0"
          class="btn-add small" @click="addChild">+ Add child</button>
      </div>
    </template>
  </div>
</template>

<style scoped>
.node { display: flex; flex-direction: column; gap: 0.4rem; padding: 0.5rem 0.6rem; border-radius: 6px; border: 1px solid var(--border); background: white; }
.node.combinator { background: var(--surface-2); border-color: var(--border-strong); }
.node-head { display: flex; align-items: center; gap: 0.5rem; }
.op-select { padding: 0.25rem 0.45rem; border: 1px solid var(--border-strong); border-radius: 4px; font-size: 0.85rem; font-family: ui-monospace, SFMono-Regular, Menlo, monospace; }
.leaf-row { display: flex; gap: 0.5rem; align-items: center; flex-wrap: wrap; }
.text-input { flex: 1; padding: 0.3rem 0.5rem; border: 1px solid var(--border-strong); border-radius: 4px; font-size: 0.85rem; min-width: 10rem; }
.children { display: flex; flex-direction: column; gap: 0.5rem; padding-left: 0.6rem; border-left: 2px solid #e0e7ff; }
.muted { color: var(--text-muted); }
.small { font-size: 0.8rem; }
.link-btn { background: none; border: none; color: var(--accent); cursor: pointer; font-size: 0.8rem; padding: 0; }
.link-btn:hover { text-decoration: underline; }
.link-danger { color: #dc2626; }
.btn-add { padding: 0.25rem 0.55rem; background: white; border: 1px dashed var(--border-strong); border-radius: 4px; font-size: 0.8rem; cursor: pointer; color: #4b5563; align-self: flex-start; }
.btn-add:hover { background: var(--surface-3); }
</style>
