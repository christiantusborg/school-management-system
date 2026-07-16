<template>
  <v-card class="inspector">
    <v-card-title class="d-flex align-center">
      <v-icon icon="mdi-cog" class="mr-2" />
      Inspector
    </v-card-title>

    <v-card-text v-if="selectedItem">
      <v-form>
        <!-- Basic Properties -->
        <div class="mb-4">
          <h3 class="text-subtitle-1 mb-3">Basic Properties</h3>

          <v-text-field
            :model-value="selectedItem.label?.fallback || ''"
            label="Label"
            variant="outlined"
            density="compact"
            @update:model-value="updateLabel"
          />

          <v-textarea
            :model-value="selectedItem.description?.fallback || ''"
            label="Description"
            variant="outlined"
            density="compact"
            rows="2"
            @update:model-value="updateDescription"
          />

          <v-switch
            :model-value="selectedItem.required || false"
            label="Required"
            color="primary"
            @update:model-value="updateRequired"
          />
        </div>

        <!-- Component-Specific Properties -->
        <div v-if="componentSchema" class="mb-4">
          <h3 class="text-subtitle-1 mb-3">Component Properties</h3>

          <!-- Text Input Properties -->
          <template v-if="selectedItem.type === 'text' || selectedItem.type === 'email'">
            <v-text-field
              :model-value="selectedItem.props.placeholder || ''"
              label="Placeholder"
              variant="outlined"
              density="compact"
              @update:model-value="updateProp('placeholder', $event)"
            />
            <v-text-field
              :model-value="selectedItem.props.maxLength || ''"
              label="Max Length"
              type="number"
              variant="outlined"
              density="compact"
              @update:model-value="updateProp('maxLength', parseInt($event) || undefined)"
            />
          </template>

          <!-- Textarea Properties -->
          <template v-if="selectedItem.type === 'textarea'">
            <v-text-field
              :model-value="selectedItem.props.placeholder || ''"
              label="Placeholder"
              variant="outlined"
              density="compact"
              @update:model-value="updateProp('placeholder', $event)"
            />
            <v-text-field
              :model-value="selectedItem.props.rows || 4"
              label="Rows"
              type="number"
              variant="outlined"
              density="compact"
              @update:model-value="updateProp('rows', parseInt($event) || 4)"
            />
          </template>

          <!-- Number Properties -->
          <template v-if="selectedItem.type === 'number'">
            <v-text-field
              :model-value="selectedItem.props.min || ''"
              label="Minimum Value"
              type="number"
              variant="outlined"
              density="compact"
              @update:model-value="updateProp('min', parseFloat($event) || undefined)"
            />
            <v-text-field
              :model-value="selectedItem.props.max || ''"
              label="Maximum Value"
              type="number"
              variant="outlined"
              density="compact"
              @update:model-value="updateProp('max', parseFloat($event) || undefined)"
            />
            <v-text-field
              :model-value="selectedItem.props.step || 1"
              label="Step"
              type="number"
              variant="outlined"
              density="compact"
              @update:model-value="updateProp('step', parseFloat($event) || 1)"
            />
          </template>

          <!-- Select/Radio/Checkbox Properties -->
          <template v-if="['select', 'radio', 'checkbox'].includes(selectedItem.type)">
            <div class="options-editor">
              <div class="d-flex align-center justify-space-between mb-2">
                <label class="text-subtitle-2">Options</label>
                <v-btn
                  icon="mdi-plus"
                  size="small"
                  variant="tonal"
                  @click="addOption"
                />
              </div>
              <div
                v-for="(option, index) in selectedItem.props.options || []"
                :key="index"
                class="d-flex gap-2 mb-2"
              >
                <v-text-field
                  :model-value="option.value"
                  label="Value"
                  variant="outlined"
                  density="compact"
                  @update:model-value="updateOption(index, 'value', $event)"
                />
                <v-text-field
                  :model-value="option.label"
                  label="Label"
                  variant="outlined"
                  density="compact"
                  @update:model-value="updateOption(index, 'label', $event)"
                />
                <v-btn
                  icon="mdi-delete"
                  size="small"
                  variant="text"
                  color="error"
                  @click="removeOption(index)"
                />
              </div>
            </div>

            <v-switch
              v-if="selectedItem.type === 'select'"
              :model-value="selectedItem.props.multiple || false"
              label="Allow Multiple Selection"
              color="primary"
              @update:model-value="updateProp('multiple', $event)"
            />
          </template>

          <!-- File Upload Properties -->
          <template v-if="selectedItem.type === 'file-upload'">
            <v-text-field
              :model-value="selectedItem.props.accept || '*/*'"
              label="Accepted File Types"
              variant="outlined"
              density="compact"
              @update:model-value="updateProp('accept', $event)"
            />
            <v-text-field
              :model-value="selectedItem.props.maxSize || 10"
              label="Max File Size (MB)"
              type="number"
              variant="outlined"
              density="compact"
              @update:model-value="updateProp('maxSize', parseInt($event) || 10)"
            />
            <v-switch
              :model-value="selectedItem.props.multiple || false"
              label="Allow Multiple Files"
              color="primary"
              @update:model-value="updateProp('multiple', $event)"
            />
          </template>

          <!-- Display Component Properties -->
          <template v-if="selectedItem.type === 'heading'">
            <v-text-field
              :model-value="selectedItem.props.text || ''"
              label="Heading Text"
              variant="outlined"
              density="compact"
              @update:model-value="updateProp('text', $event)"
            />
            <v-select
              :model-value="selectedItem.props.level || 2"
              :items="[1, 2, 3, 4, 5, 6]"
              label="Heading Level"
              variant="outlined"
              density="compact"
              @update:model-value="updateProp('level', $event)"
            />
          </template>

          <template v-if="selectedItem.type === 'paragraph'">
            <v-textarea
              :model-value="selectedItem.props.text || ''"
              label="Text Content"
              variant="outlined"
              density="compact"
              rows="3"
              @update:model-value="updateProp('text', $event)"
            />
          </template>

          <!-- Dynamic Text Block Properties -->
          <template v-if="selectedItem.type === 'richtext'">
            <div class="richtext-editor-container">
              <label class="text-subtitle-2 mb-2 d-block">Content Editor</label>
              <RichTextEditorSimple
                :model-value="selectedItem.props.content || ''"
                :show-in-live="selectedItem.props.showInLive"
                :available-fields="availableFields"
                :mode="'builder'"
                @update:model-value="updateProp('content', $event)"
                @update:show-in-live="updateProp('showInLive', $event)"
              />
            </div>
          </template>
        </div>

        <!-- Validation Rules -->
        <div class="mb-4">
          <h3 class="text-subtitle-1 mb-3">Validation</h3>
          <div class="text-body-2 text-medium-emphasis">
            Validation rules will be added here
          </div>
        </div>

        <!-- Conditional Logic -->
        <div class="mb-4">
          <h3 class="text-subtitle-1 mb-3">Conditional Logic</h3>
          <div class="conditions-editor">
            <div class="d-flex align-center justify-space-between mb-2">
              <label class="text-subtitle-2">Show/Hide Conditions</label>
              <v-btn
                icon="mdi-plus"
                size="small"
                variant="tonal"
                @click="addCondition"
              />
            </div>
            <div
              v-for="(condition, index) in selectedItem.conditions || []"
              :key="index"
              class="condition-item mb-3 pa-3"
            >
              <div class="d-flex gap-2 mb-2">
                <v-select
                  :model-value="condition.dependsOn"
                  :items="availableFields"
                  label="Depends On Field"
                  variant="outlined"
                  density="compact"
                  item-title="label"
                  item-value="id"
                  @update:model-value="updateCondition(index, 'dependsOn', $event)"
                />
                <v-select
                  :model-value="condition.operator"
                  :items="getOperatorsForField(condition.dependsOn)"
                  label="Operator"
                  variant="outlined"
                  density="compact"
                  @update:model-value="updateCondition(index, 'operator', $event)"
                />
              </div>
              <div class="d-flex gap-2 mb-2" v-if="!['isEmpty', 'isNotEmpty'].includes(condition.operator)">
                <v-text-field
                  :model-value="condition.value"
                  label="Value"
                  variant="outlined"
                  density="compact"
                  @update:model-value="updateCondition(index, 'value', $event)"
                />
                <v-select
                  :model-value="condition.action"
                  :items="[
                    { title: 'Show when true', value: 'show' },
                    { title: 'Hide when true', value: 'hide' }
                  ]"
                  label="Action"
                  variant="outlined"
                  density="compact"
                  @update:model-value="updateCondition(index, 'action', $event)"
                />
                <v-btn
                  icon="mdi-delete"
                  size="small"
                  variant="text"
                  color="error"
                  @click="removeCondition(index)"
                />
              </div>
            </div>
            <div v-if="(selectedItem.conditions || []).length === 0" class="text-center py-4">
              <v-icon size="32" color="grey-lighten-1" class="mb-2">
                mdi-eye-settings
              </v-icon>
              <div class="text-body-2 text-medium-emphasis">
                No conditions set. This field will always be visible.
              </div>
            </div>
          </div>
        </div>

        <!-- Layout -->
        <div class="mb-4">
          <h3 class="text-subtitle-1 mb-3">Layout</h3>
          <v-select
            :model-value="selectedItem.layout?.width || 'full'"
            :items="[
              { title: 'Full Width', value: 'full' },
              { title: 'Half Width', value: 'half' },
              { title: 'Third Width', value: 'third' },
              { title: 'Quarter Width', value: 'quarter' }
            ]"
            label="Width"
            variant="outlined"
            density="compact"
            @update:model-value="updateLayout('width', $event)"
          />
        </div>
      </v-form>
    </v-card-text>

    <!-- No Selection State -->
    <v-card-text v-else class="text-center py-8">
      <v-icon size="48" color="grey-lighten-1" class="mb-2">
        mdi-cursor-default-click
      </v-icon>
      <div class="text-body-2 text-medium-emphasis">
        Select a component to edit its properties
      </div>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
// -nocheck — ported SysCase file; TS strict cleanup is a follow-up
import { computed } from 'vue'
import { componentRegistry } from '@/utils/questionnaire/componentRegistry'
import RichTextEditorSimple from '@/components/forms/RichTextEditorSimple.vue'
import type { QuestionnaireItem, ConditionalVisibility } from '@quvian/shared/types/questionnaire'

// Props
const props = defineProps<{
  selectedItem: QuestionnaireItem | null
  questionnaire?: any // For getting available fields
}>()

// Emits
const emit = defineEmits<{
  updateItem: [updates: Partial<QuestionnaireItem>]
}>()

// Computed
const componentSchema = computed(() => {
  if (!props.selectedItem) return null
  return componentRegistry[props.selectedItem.type]
})

const availableFields = computed(() => {
  if (!props.questionnaire) return []

  const fields: { id: string; label: string; type: string }[] = []

  // Get all fields from all pages, sections, and groups
  props.questionnaire.pages?.forEach((page: any) => {
    page.sections?.forEach((section: any) => {
      section.groups?.forEach((group: any) => {
        group.items?.forEach((item: any) => {
        // Include all field types that can have values (exclude display-only components)
        if (!['heading', 'paragraph', 'divider'].includes(item.type) &&
            item.id !== props.selectedItem?.id) {
          const typeLabel = ({
            'text': 'Text',
            'textarea': 'Textarea',
            'number': 'Number',
            'email': 'Email',
            'phone': 'Phone',
            'date': 'Date',
            'time': 'Time',
            'select': 'Select',
            'radio': 'Radio',
            'checkbox': 'Checkbox',
            'toggle': 'Toggle',
            'file-upload': 'File Upload',
            'consent': 'Consent'
          } as Record<string, string>)[item.type] || item.type

          fields.push({
            id: item.id,
            label: item.label?.fallback || typeLabel,
            type: item.type
          })
        }
        })
      })
    })
  })

  return fields
})

const getOperatorsForField = (fieldId: string) => {
  if (!fieldId || !props.questionnaire) {
    return [
      { title: 'Equals', value: 'equals' },
      { title: 'Not Equals', value: 'notEquals' },
      { title: 'Is Empty', value: 'isEmpty' },
      { title: 'Is Not Empty', value: 'isNotEmpty' }
    ]
  }

  // Find the field type
  let fieldType = ''
  props.questionnaire.pages?.forEach((page: any) => {
    page.sections?.forEach((section: any) => {
      section.items?.forEach((item: any) => {
        if (item.id === fieldId) {
          fieldType = item.type
        }
      })
    })
  })

  const baseOperators = [
    { title: 'Equals', value: 'equals' },
    { title: 'Not Equals', value: 'notEquals' },
    { title: 'Is Empty', value: 'isEmpty' },
    { title: 'Is Not Empty', value: 'isNotEmpty' }
  ]

  const textOperators = [
    { title: 'Contains', value: 'contains' },
    { title: 'Not Contains', value: 'notContains' },
    { title: 'Starts With', value: 'startsWith' },
    { title: 'Ends With', value: 'endsWith' }
  ]

  const numberOperators = [
    { title: 'Greater Than', value: 'greaterThan' },
    { title: 'Less Than', value: 'lessThan' },
    { title: 'Greater or Equal', value: 'greaterOrEqual' },
    { title: 'Less or Equal', value: 'lessOrEqual' }
  ]

  const arrayOperators = [
    { title: 'Contains', value: 'contains' },
    { title: 'Not Contains', value: 'notContains' }
  ]

  switch (fieldType) {
    case 'number':
    case 'date':
    case 'time':
      return [...baseOperators, ...numberOperators]

    case 'text':
    case 'textarea':
    case 'email':
    case 'phone':
      return [...baseOperators, ...textOperators]

    case 'checkbox':
      return [...baseOperators, ...arrayOperators]

    case 'select':
    case 'radio':
    case 'toggle':
    case 'consent':
    case 'file-upload':
    default:
      return baseOperators
  }
}

// Methods
const updateLabel = (value: string) => {
  emit('updateItem', {
    label: { fallback: value }
  })
}

const updateDescription = (value: string) => {
  emit('updateItem', {
    description: value ? { fallback: value } : undefined
  })
}

const updateRequired = (value: boolean | null) => {
  emit('updateItem', { required: value ?? false })
}

const updateProp = (key: string, value: any) => {
  if (!props.selectedItem) return

  const updatedProps = {
    ...props.selectedItem.props,
    [key]: value
  }

  emit('updateItem', { props: updatedProps })
}

const updateLayout = (key: string, value: any) => {
  if (!props.selectedItem) return

  const updatedLayout = {
    ...props.selectedItem.layout,
    [key]: value
  }

  emit('updateItem', { layout: updatedLayout })
}

const addOption = () => {
  if (!props.selectedItem) return

  const currentOptions = props.selectedItem.props.options || []
  const newOption = {
    value: `option${currentOptions.length + 1}`,
    label: `Option ${currentOptions.length + 1}`
  }

  updateProp('options', [...currentOptions, newOption])
}

const updateOption = (index: number, key: string, value: string) => {
  if (!props.selectedItem) return

  const options = [...(props.selectedItem.props.options || [])]
  options[index] = { ...options[index], [key]: value }
  updateProp('options', options)
}

const removeOption = (index: number) => {
  if (!props.selectedItem) return

  const options = [...(props.selectedItem.props.options || [])]
  options.splice(index, 1)
  updateProp('options', options)
}

// Condition management methods
const addCondition = () => {
  if (!props.selectedItem) return

  const currentConditions = props.selectedItem.conditions || []
  const newCondition: ConditionalVisibility = {
    dependsOn: '',
    operator: 'equals',
    value: '',
    action: 'show'
  }

  emit('updateItem', {
    conditions: [...currentConditions, newCondition]
  })
}

const updateCondition = (index: number, field: keyof ConditionalVisibility, value: any) => {
  if (!props.selectedItem) return

  const conditions = [...(props.selectedItem.conditions || [])]
  conditions[index] = { ...conditions[index], [field]: value }

  emit('updateItem', { conditions })
}

const removeCondition = (index: number) => {
  if (!props.selectedItem) return

  const conditions = [...(props.selectedItem.conditions || [])]
  conditions.splice(index, 1)

  emit('updateItem', { conditions })
}
</script>

<style scoped>
.inspector {
  height: fit-content;
  max-height: 80vh;
  overflow-y: auto;
}

.options-editor {
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 4px;
  padding: 12px;
  margin-bottom: 16px;
}

.conditions-editor {
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 4px;
  padding: 12px;
  margin-bottom: 16px;
}

.condition-item {
  background-color: rgba(var(--v-theme-surface-variant), 0.3);
  border-radius: 4px;
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
}

.richtext-editor-container {
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 4px;
  padding: 8px;
  margin-bottom: 16px;
}
</style>