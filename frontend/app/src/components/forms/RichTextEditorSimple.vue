<template>
  <div class="rich-text-editor-simple">
    <!-- Editor Header with Variable Insertion (Only in Builder Mode) -->
    <div v-if="isBuilderMode" class="editor-header d-flex align-center justify-space-between pa-2 bg-grey-lighten-4">
      <div class="d-flex align-center gap-2">
        <v-btn-toggle
          v-model="activeMode"
          mandatory
          density="compact"
          variant="outlined"
        >
          <v-btn value="edit" size="small">
            <v-icon>mdi-pencil</v-icon>
            Edit
          </v-btn>
          <v-btn value="preview" size="small">
            <v-icon>mdi-eye</v-icon>
            Preview
          </v-btn>
        </v-btn-toggle>

        <!-- Variable Insertion Button -->
        <v-menu v-if="activeMode === 'edit'">
          <template v-slot:activator="{ props }">
            <v-btn
              v-bind="props"
              size="small"
              variant="outlined"
              prepend-icon="mdi-code-brackets"
            >
              Insert Variable
            </v-btn>
          </template>
          <v-list max-height="300" width="300">
            <v-list-subheader>Available Fields</v-list-subheader>
            <v-list-item
              v-for="field in availableFields"
              :key="field.id"
              @click="insertVariable(field)"
            >
              <template v-slot:prepend>
                <v-icon size="16" :icon="getFieldIcon(field.type)" />
              </template>
              <v-list-item-title>{{ field.label }}</v-list-item-title>
              <v-list-item-subtitle>{{ field.type }}</v-list-item-subtitle>
            </v-list-item>
            <v-list-item v-if="availableFields.length === 0">
              <v-list-item-title class="text-medium-emphasis">
                No previous fields available
              </v-list-item-title>
            </v-list-item>
          </v-list>
        </v-menu>
      </div>

      <!-- Show in Live Questionnaire Toggle -->
      <v-switch
        v-model="showInLive"
        label="Show in Live"
        color="primary"
        density="compact"
        hide-details
        @update:model-value="$emit('update:showInLive', $event)"
      />
    </div>

    <!-- Editor Content -->
    <div class="editor-content">
      <!-- Builder Mode - Edit -->
      <v-textarea
        v-if="isBuilderMode && activeMode === 'edit'"
        :model-value="modelValue"
        placeholder="Enter rich text content with variables like [Field Name]..."
        variant="outlined"
        rows="8"
        @update:model-value="$emit('update:modelValue', $event)"
      />

      <!-- Builder Mode - Preview (with variable replacement) -->
      <div v-else-if="isBuilderMode && activeMode === 'preview'" class="preview-container pa-4 bg-white border">
        <div v-if="!showInLive" class="hidden-indicator mb-3">
          <v-alert
            type="info"
            variant="tonal"
            density="compact"
            prepend-icon="mdi-eye-off"
          >
            This content is hidden in live questionnaire
          </v-alert>
        </div>
        <div
          class="preview-content"
          v-html="previewContent"
        ></div>
      </div>

      <!-- Live/Preview Mode - Show literal content (no variable replacement) -->
      <div v-else class="live-content pa-4 bg-white">
        <div
          class="live-text-content"
          v-html="modelValue || '<p>No content</p>'"
        ></div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
// @ts-nocheck — ported SysCase file; TS strict cleanup is a follow-up
import { ref, computed } from 'vue'

// Props
const props = defineProps<{
  modelValue?: string
  showInLive?: boolean
  availableFields?: Array<{
    id: string
    label: string
    type: string
  }>
  fieldAnswers?: Record<string, any>
  mode?: 'builder' | 'live' | 'preview'
  disabled?: boolean
}>()

// Emits
const emit = defineEmits<{
  'update:modelValue': [value: string]
  'update:showInLive': [value: boolean]
}>()

// Reactive state
const activeMode = ref<'edit' | 'preview'>('edit')
const showInLive = ref(props.showInLive ?? true)

// Computed
const availableFields = computed(() => props.availableFields || [])

const isBuilderMode = computed(() => props.mode === 'builder' || props.mode === undefined)

const isLiveMode = computed(() => props.mode === 'live' || props.mode === 'preview')

const previewContent = computed(() => {
  if (!props.modelValue) return '<p>No content</p>'

  let content = props.modelValue

  // Replace variables with actual values or placeholders
  const variableRegex = /\[([^\]]+)\]/g
  content = content.replace(variableRegex, (match, fieldLabel) => {
    const field = availableFields.value.find(f => f.label === fieldLabel)
    if (!field) return match

    const value = props.fieldAnswers?.[field.id]
    if (value !== undefined && value !== null && value !== '') {
      return `<span class="variable-value">${value}</span>`
    } else {
      return `<span class="variable-placeholder">[${fieldLabel}]</span>`
    }
  })

  return content
})

// Methods
const getFieldIcon = (type: string) => {
  const iconMap: Record<string, string> = {
    text: 'mdi-form-textbox',
    textarea: 'mdi-text-box',
    number: 'mdi-numeric',
    email: 'mdi-email',
    phone: 'mdi-phone',
    select: 'mdi-form-dropdown',
    radio: 'mdi-radiobox-marked',
    checkbox: 'mdi-checkbox-marked',
    date: 'mdi-calendar',
    time: 'mdi-clock',
    url: 'mdi-link',
    file: 'mdi-file',
    switch: 'mdi-toggle-switch'
  }
  return iconMap[type] || 'mdi-form-textbox'
}

const insertVariable = (field: { id: string; label: string; type: string }) => {
  const variable = `[${field.label}]`
  const currentValue = props.modelValue || ''
  emit('update:modelValue', currentValue + variable)
}
</script>

<style scoped>
.rich-text-editor-simple {
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 4px;
  overflow: hidden;
}

.editor-header {
  border-bottom: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
}

.preview-container {
  min-height: 200px;
  border-top: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
}

.preview-content :deep(.variable-value) {
  background-color: rgba(var(--v-theme-success), 0.2);
  padding: 2px 4px;
  border-radius: 4px;
  font-weight: 500;
}

.preview-content :deep(.variable-placeholder) {
  background-color: rgba(var(--v-theme-warning), 0.2);
  padding: 2px 4px;
  border-radius: 4px;
  font-style: italic;
  color: rgba(var(--v-theme-warning));
}

.hidden-indicator {
  opacity: 0.8;
}
</style>