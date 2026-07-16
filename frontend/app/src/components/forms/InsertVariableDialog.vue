<template>
  <v-dialog
    v-model="isOpen"
    max-width="600px"
    persistent
    scrollable
  >
    <v-card>
      <v-card-title class="pa-6 pb-0">
        <div class="d-flex align-center">
          <v-icon icon="mdi-code-brackets" class="mr-3" />
          <h2 class="text-h5">Insert Variable</h2>
        </div>
      </v-card-title>

      <v-card-text class="pa-6">
        <!-- Available Fields -->
        <div v-if="props.selectedQuestionnaire && availableFields.length > 0">
          <v-subheader class="px-0 mb-2">
            <v-icon icon="mdi-format-list-bulleted" start />
            Available Fields ({{ availableFields.length }})
          </v-subheader>

          <v-list class="border rounded" max-height="400" style="overflow-y: auto;">
            <v-list-item
              v-for="field in availableFields"
              :key="field.id"
              @click="selectField(field)"
              :class="{ 'bg-primary-lighten-5': selectedFieldId === field.id }"
              class="field-item"
            >
              <template v-slot:prepend>
                <v-avatar size="32" :color="getFieldColor(field.type)" variant="tonal">
                  <v-icon :icon="getFieldIcon(field.type)" size="16" />
                </v-avatar>
              </template>

              <v-list-item-title class="font-weight-medium">
                {{ field.label }}
              </v-list-item-title>

              <v-list-item-subtitle class="d-flex align-center mt-1">
                <v-chip size="x-small" :color="getFieldColor(field.type)" variant="outlined">
                  {{ getFieldTypeLabel(field.type) }}
                </v-chip>
                <span v-if="field.required" class="ml-2">
                  <v-chip size="x-small" color="error" variant="outlined">
                    Required
                  </v-chip>
                </span>
              </v-list-item-subtitle>

              <template v-slot:append>
                <v-btn
                  icon="mdi-plus"
                  size="small"
                  variant="outlined"
                  color="primary"
                  @click.stop="insertField(field)"
                />
              </template>
            </v-list-item>
          </v-list>
        </div>

        <!-- No Fields Message -->
        <div v-else-if="props.selectedQuestionnaire && availableFields.length === 0" class="text-center py-8">
          <v-icon icon="mdi-folder-open-outline" size="48" class="text-medium-emphasis mb-4" />
          <h3 class="text-h6 mb-2">No Fields Found</h3>
          <p class="text-body-2 text-medium-emphasis">
            The selected questionnaire doesn't contain any insertable fields.
          </p>
        </div>

        <!-- No Questionnaire Selected -->
        <div v-else-if="!props.selectedQuestionnaire" class="text-center py-8">
          <v-icon icon="mdi-form-select" size="48" class="text-medium-emphasis mb-4" />
          <h3 class="text-h6 mb-2">Select a Questionnaire</h3>
          <p class="text-body-2 text-medium-emphasis">
            Choose a questionnaire from the main page to see its available fields.
          </p>
        </div>

        <!-- Preview Section -->
        <div v-if="selectedFieldId" class="mt-6">
          <v-divider class="mb-4" />
          <v-subheader class="px-0 mb-2">
            <v-icon icon="mdi-eye" start />
            Variable Preview
          </v-subheader>
          <v-card variant="outlined" class="preview-card">
            <v-card-text class="d-flex align-center justify-space-between">
              <div>
                <code class="text-primary font-weight-bold">{{ getVariableText(selectedField ?? null) }}</code>
                <div class="text-caption text-medium-emphasis mt-1">
                  This will be replaced with the actual field value when generating content
                </div>
              </div>
              <v-btn
                color="primary"
                variant="outlined"
                prepend-icon="mdi-content-copy"
                @click="copyVariableText"
              >
                Copy
              </v-btn>
            </v-card-text>
          </v-card>
        </div>
      </v-card-text>

      <v-card-actions class="pa-6 pt-0">
        <v-spacer />
        <v-btn text @click="closeDialog">
          Cancel
        </v-btn>
        <v-btn
          color="primary"
          :disabled="!selectedFieldId"
          @click="insertSelectedField"
        >
          Insert Variable
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>

  <!-- Success Snackbar -->
  <v-snackbar
    v-model="showSuccessSnackbar"
    color="success"
    :timeout="2000"
  >
    Variable copied to clipboard!
  </v-snackbar>
</template>

<script setup lang="ts">
// -nocheck — ported SysCase file; TS strict cleanup is a follow-up
import { ref, computed, watch } from 'vue'
import { useQuestionnaireStore } from '@/stores/questionnaire'
import type { QuestionnaireItem, ComponentType } from '@quvian/shared/types/questionnaire'

interface Field {
  id: string
  label: string
  type: ComponentType
  required: boolean
}

// Props
const props = defineProps<{
  modelValue: boolean
  selectedQuestionnaire?: any
  availableFields?: Array<{
    id: string
    label: string
    type: ComponentType
    required: boolean
  }>
}>()

// Emits
const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  'insert-variable': [variable: string]
}>()

// State
const selectedFieldId = ref<string | null>(null)
const showSuccessSnackbar = ref(false)

// Computed
const isOpen = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})

const availableFields = computed((): Field[] => {
  return props.availableFields || []
})

const selectedField = computed(() => {
  if (!selectedFieldId.value) return null
  return availableFields.value.find(f => f.id === selectedFieldId.value)
})

// Methods
const isInputField = (type: ComponentType): boolean => {
  const inputTypes: ComponentType[] = [
    'text', 'textarea', 'richtext', 'number', 'email', 'phone',
    'date', 'time', 'datetime', 'select', 'radio', 'checkbox',
    'toggle', 'file-upload', 'address'
  ]
  return inputTypes.includes(type)
}

const getFieldIcon = (type: ComponentType): string => {
  const iconMap: Record<ComponentType, string> = {
    text: 'mdi-form-textbox',
    textarea: 'mdi-text-box',
    richtext: 'mdi-format-text',
    number: 'mdi-numeric',
    email: 'mdi-email',
    phone: 'mdi-phone',
    date: 'mdi-calendar',
    time: 'mdi-clock',
    datetime: 'mdi-calendar-clock',
    select: 'mdi-form-dropdown',
    radio: 'mdi-radiobox-marked',
    checkbox: 'mdi-checkbox-marked',
    toggle: 'mdi-toggle-switch',
    'file-upload': 'mdi-file-upload',
    address: 'mdi-map-marker',
    repeater: 'mdi-repeat',
    heading: 'mdi-format-header-1',
    paragraph: 'mdi-text',
    divider: 'mdi-minus',
    consent: 'mdi-check-circle'
  }
  return iconMap[type] || 'mdi-form-textbox'
}

const getFieldColor = (type: ComponentType): string => {
  const colorMap: Record<string, string> = {
    text: 'blue',
    textarea: 'blue',
    richtext: 'purple',
    number: 'green',
    email: 'orange',
    phone: 'teal',
    date: 'indigo',
    time: 'indigo',
    datetime: 'indigo',
    select: 'cyan',
    radio: 'pink',
    checkbox: 'pink',
    toggle: 'amber',
    'file-upload': 'red',
    address: 'brown'
  }
  return colorMap[type] || 'grey'
}

const getFieldTypeLabel = (type: ComponentType): string => {
  const labelMap: Record<ComponentType, string> = {
    text: 'Text',
    textarea: 'Long Text',
    richtext: 'Rich Text',
    number: 'Number',
    email: 'Email',
    phone: 'Phone',
    date: 'Date',
    time: 'Time',
    datetime: 'Date & Time',
    select: 'Dropdown',
    radio: 'Radio',
    checkbox: 'Checkbox',
    toggle: 'Toggle',
    'file-upload': 'File Upload',
    address: 'Address',
    repeater: 'Repeater',
    heading: 'Heading',
    paragraph: 'Paragraph',
    divider: 'Divider',
    consent: 'Consent'
  }
  return labelMap[type] || type
}

const getVariableText = (field: Field | null): string => {
  if (!field) return ''
  return `{{${field.label}}}`
}

const selectField = (field: Field) => {
  selectedFieldId.value = field.id
}

const insertField = (field: Field) => {
  const variableText = getVariableText(field)
  emit('insert-variable', variableText)
  closeDialog()
}

const insertSelectedField = () => {
  if (selectedField.value) {
    insertField(selectedField.value)
  }
}

const copyVariableText = async () => {
  if (!selectedField.value) return

  try {
    await navigator.clipboard.writeText(getVariableText(selectedField.value))
    showSuccessSnackbar.value = true
  } catch (err) {
    console.error('Failed to copy to clipboard:', err)
  }
}

const closeDialog = () => {
  isOpen.value = false
  selectedFieldId.value = null
}

// Reset selection when dialog closes
watch(() => props.modelValue, (newValue) => {
  if (!newValue) {
    selectedFieldId.value = null
  }
})
</script>

<style scoped>
.field-item {
  cursor: pointer;
  transition: background-color 0.2s;
}

.field-item:hover {
  background-color: rgba(var(--v-theme-primary), 0.04);
}

.preview-card {
  background-color: rgba(var(--v-theme-surface), 1);
}

code {
  background-color: rgba(var(--v-theme-primary), 0.1);
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 0.875rem;
}
</style>