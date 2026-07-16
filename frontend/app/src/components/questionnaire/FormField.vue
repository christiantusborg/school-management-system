<template>
  <div class="form-field">
    <!-- Label -->
    <v-label v-if="item.label" class="mb-2">
      {{ item.label.fallback }}
      <span v-if="item.required" class="text-error ml-1">*</span>
    </v-label>

    <!-- Description -->
    <div v-if="item.description" class="text-body-2 text-medium-emphasis mb-2">
      {{ item.description.fallback }}
    </div>

    <!-- Input Field -->
    <div class="field-input">
      <!-- Text Input -->
      <v-text-field
        v-if="item.type === 'text' || item.type === 'email'"
        :model-value="value"
        :placeholder="item.props.placeholder"
        :type="item.type === 'email' ? 'email' : 'text'"
        :maxlength="item.props.maxLength"
        :disabled="disabled"
        :error-messages="error"
        variant="outlined"
        @update:model-value="$emit('update:value', $event)"
      />

      <!-- Textarea -->
      <v-textarea
        v-else-if="item.type === 'textarea'"
        :model-value="value"
        :placeholder="item.props.placeholder"
        :rows="item.props.rows || 4"
        :maxlength="item.props.maxLength"
        :disabled="disabled"
        :error-messages="error"
        variant="outlined"
        @update:model-value="$emit('update:value', $event)"
      />

      <!-- Dynamic Text Block -->
      <div v-else-if="item.type === 'richtext'" class="dynamic-text-preview">
        <div v-if="mode === 'builder'" class="builder-preview pa-3 bg-grey-lighten-5 rounded">
          <div class="text-caption text-medium-emphasis mb-2">Dynamic Text Block Preview:</div>
          <div
            v-if="item.props.content"
            v-html="item.props.content"
            class="preview-content"
          ></div>
          <div v-else class="text-body-2 text-medium-emphasis">
            Configure content in Inspector →
          </div>
        </div>
        <div v-else class="live-preview">
          <div
            v-if="item.props.content && item.props.showInLive"
            v-html="item.props.content"
            class="live-content"
          ></div>
        </div>
      </div>

      <!-- Number Input -->
      <v-text-field
        v-else-if="item.type === 'number'"
        :model-value="value"
        :placeholder="item.props.placeholder"
        type="number"
        :min="item.props.min"
        :max="item.props.max"
        :step="item.props.step"
        :disabled="disabled"
        :error-messages="error"
        variant="outlined"
        @update:model-value="$emit('update:value', parseFloat($event) || $event)"
      />

      <!-- Phone Input (rendered as text — tel mobile keyboard caused
           input issues in the SysCase port, type=text is more reliable). -->
      <v-text-field
        v-else-if="item.type === 'phone' || (item.type as string) === 'tel'"
        :model-value="value"
        :placeholder="item.props.placeholder"
        type="text"
        inputmode="tel"
        :disabled="disabled"
        :error-messages="error"
        variant="outlined"
        @update:model-value="$emit('update:value', $event)"
      />

      <!-- Date Input -->
      <v-text-field
        v-else-if="item.type === 'date'"
        :model-value="value"
        type="date"
        :min="item.props.minDate"
        :max="item.props.maxDate"
        :disabled="disabled"
        :error-messages="error"
        variant="outlined"
        @update:model-value="$emit('update:value', $event)"
      />

      <!-- Time Input -->
      <v-text-field
        v-else-if="item.type === 'time'"
        :model-value="value"
        type="time"
        :step="item.props.step ? item.props.step * 60 : undefined"
        :disabled="disabled"
        :error-messages="error"
        variant="outlined"
        @update:model-value="$emit('update:value', $event)"
      />

      <!-- Select -->
      <v-select
        v-else-if="item.type === 'select'"
        :model-value="value"
        :items="item.props.options || []"
        :placeholder="item.props.placeholder"
        :multiple="item.props.multiple"
        :disabled="disabled"
        :error-messages="error"
        item-title="label"
        item-value="value"
        variant="outlined"
        @update:model-value="$emit('update:value', $event)"
      />

      <!-- Radio Buttons -->
      <v-radio-group
        v-else-if="item.type === 'radio'"
        :model-value="value"
        :disabled="disabled"
        :error-messages="error"
        @update:model-value="$emit('update:value', $event)"
      >
        <v-radio
          v-for="option in item.props.options || []"
          :key="option.value"
          :label="option.label"
          :value="option.value"
          :disabled="disabled || option.disabled"
        />
      </v-radio-group>

      <!-- Checkboxes -->
      <div v-else-if="item.type === 'checkbox'" class="checkbox-group">
        <v-checkbox
          v-for="option in item.props.options || []"
          :key="option.value"
          :model-value="Array.isArray(value) ? value.includes(option.value) : false"
          :label="option.label"
          :disabled="disabled || option.disabled"
          @update:model-value="updateCheckbox(option.value, $event)"
        />
      </div>

      <!-- Toggle -->
      <v-switch
        v-else-if="item.type === 'toggle'"
        :model-value="value"
        :disabled="disabled"
        :error-messages="error"
        :true-value="true"
        :false-value="false"
        color="primary"
        @update:model-value="$emit('update:value', $event)"
      >
        <template #label>
          {{ value ? item.props.trueLabel : item.props.falseLabel }}
        </template>
      </v-switch>

      <!-- File Upload -->
      <v-file-input
        v-else-if="item.type === 'file-upload'"
        :model-value="value"
        :accept="item.props.accept"
        :multiple="item.props.multiple"
        :disabled="disabled"
        :error-messages="error"
        variant="outlined"
        prepend-icon=""
        prepend-inner-icon="mdi-file-upload"
        @update:model-value="$emit('update:value', $event)"
      />

      <!-- Heading (Display Only) -->
      <component
        v-else-if="item.type === 'heading'"
        :is="`h${item.props.level || 2}`"
        class="text-h6"
      >
        {{ item.props.text }}
      </component>

      <!-- Paragraph (Display Only) -->
      <p v-else-if="item.type === 'paragraph'" class="text-body-1">
        {{ item.props.text }}
      </p>

      <!-- Divider (Display Only) -->
      <v-divider v-else-if="item.type === 'divider'" />

      <!-- Consent Checkbox -->
      <v-checkbox
        v-else-if="item.type === 'consent'"
        :model-value="value"
        :disabled="disabled"
        :error-messages="error"
        color="primary"
        @update:model-value="$emit('update:value', $event)"
      >
        <template #label>
          <span v-html="item.props.text"></span>
        </template>
      </v-checkbox>

      <!-- Fallback for unsupported types -->
      <v-alert v-else type="warning" variant="tonal">
        Unsupported field type: {{ item.type }}
      </v-alert>
    </div>
  </div>
</template>

<script setup lang="ts">
// -nocheck — ported SysCase file; TS strict cleanup is a follow-up
import type { QuestionnaireItem } from '@quvian/shared/types/questionnaire'

// Props
const props = defineProps<{
  item: QuestionnaireItem
  value?: any
  error?: string
  disabled?: boolean
  mode?: 'builder' | 'live' | 'preview'
  availableFields?: Array<{
    id: string
    label: string
    type: string
  }>
  fieldAnswers?: Record<string, any>
}>()

// Emits
const emit = defineEmits<{
  'update:value': [value: any]
  'update:showInLive': [value: boolean]
}>()

// Methods
const updateCheckbox = (optionValue: string, checked: boolean | null) => {
  const currentValue = Array.isArray(props.value) ? [...props.value] : []

  if (checked) {
    if (!currentValue.includes(optionValue)) {
      currentValue.push(optionValue)
    }
  } else {
    const index = currentValue.indexOf(optionValue)
    if (index > -1) {
      currentValue.splice(index, 1)
    }
  }

  emit('update:value', currentValue)
}
</script>

<style scoped>
.form-field {
  margin-bottom: 16px;
}

.checkbox-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.field-input {
  position: relative;
}

.dynamic-text-preview .builder-preview {
  border: 1px dashed rgba(var(--v-theme-primary), 0.3);
  min-height: 80px;
}

.dynamic-text-preview .preview-content {
  line-height: 1.4;
}

.dynamic-text-preview .live-content {
  line-height: 1.4;
}
</style>