<template>
  <v-card class="json-view">
    <v-card-title class="d-flex align-center justify-space-between">
      <div class="d-flex align-center">
        <v-icon icon="mdi-code-json" class="mr-2" />
        JSON View
      </div>
      <div class="d-flex gap-2">
        <v-btn
          icon="mdi-content-copy"
          size="small"
          variant="tonal"
          @click="copyToClipboard"
        />
        <v-btn
          icon="mdi-format-align-left"
          size="small"
          variant="tonal"
          @click="formatJson"
        />
      </div>
    </v-card-title>

    <v-card-text>
      <v-textarea
        v-model="jsonString"
        variant="outlined"
        rows="20"
        class="json-editor"
        :error-messages="validationError"
        @update:model-value="validateAndUpdate"
      />
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
// -nocheck — ported SysCase file; TS strict cleanup is a follow-up
import { ref, watch } from 'vue'
import type { Questionnaire } from '@quvian/shared/types/questionnaire'

// Props
const props = defineProps<{
  questionnaire: Questionnaire | null
}>()

// Emits
const emit = defineEmits<{
  updateQuestionnaire: [questionnaire: Questionnaire]
}>()

// Reactive state
const jsonString = ref('')
const validationError = ref('')

// Watch for questionnaire changes
watch(
  () => props.questionnaire,
  (newQuestionnaire) => {
    if (newQuestionnaire) {
      jsonString.value = JSON.stringify(newQuestionnaire, null, 2)
    } else {
      jsonString.value = ''
    }
    validationError.value = ''
  },
  { immediate: true }
)

// Methods
const validateAndUpdate = (value: string) => {
  try {
    validationError.value = ''
    const parsed = JSON.parse(value)

    // Basic validation
    if (!parsed.version || !parsed.id || !parsed.pages) {
      validationError.value = 'Invalid questionnaire structure'
      return
    }

    emit('updateQuestionnaire', parsed)
  } catch (error) {
    validationError.value = 'Invalid JSON format'
  }
}

const formatJson = () => {
  try {
    const parsed = JSON.parse(jsonString.value)
    jsonString.value = JSON.stringify(parsed, null, 2)
    validationError.value = ''
  } catch (error) {
    validationError.value = 'Cannot format invalid JSON'
  }
}

const copyToClipboard = async () => {
  try {
    await navigator.clipboard.writeText(jsonString.value)
    // Could show a success message here
  } catch (error) {
    console.error('Failed to copy to clipboard:', error)
  }
}
</script>

<style scoped>
.json-view {
  height: 80vh;
}

.json-editor :deep(.v-field__input) {
  font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
  font-size: 14px;
  line-height: 1.4;
}
</style>