<template>
  <v-card class="questionnaire-renderer">
    <v-card-title v-if="questionnaire">
      {{ questionnaire.name.fallback }}
    </v-card-title>
    <v-card-subtitle v-if="questionnaire?.description">
      {{ questionnaire.description.fallback }}
    </v-card-subtitle>

    <v-card-text v-if="questionnaire">
      <!-- Progress Indicator -->
      <v-progress-linear
        :model-value="progressPercentage"
        color="primary"
        height="4"
        rounded
        class="mb-4"
      />

      <!-- Current Page -->
      <div v-if="currentPageData" class="page-content">
        <h2 class="text-h5 mb-3">{{ currentPageData.title.fallback }}</h2>
        <p v-if="currentPageData.description" class="text-body-1 mb-4">
          {{ currentPageData.description.fallback }}
        </p>

        <!-- Sections -->
        <div
          v-for="section in currentPageData.sections"
          :key="section.id"
          class="section mb-6"
        >
          <h3 v-if="section.title" class="text-h6 mb-3">
            {{ section.title.fallback }}
          </h3>

          <!-- Groups -->
          <div
            v-for="group in section.groups"
            :key="group.id"
            class="group mb-4"
          >
            <h4 v-if="group.title && section.groups.length > 1" class="text-h6 mb-2">
              {{ group.title.fallback }}
            </h4>

            <!-- Repeater Groups -->
            <div v-if="group.isRepeater">
              <!-- Show the base group as the first instance -->
              <div class="repeater-instance mb-4">
                <div class="d-flex justify-space-between align-center mb-2">
                  <h5 class="text-subtitle-1">
                    {{ getRepeaterInstanceTitle(group, 1) }}
                  </h5>
                  <!-- Show delete for the first instance if there are multiple instances -->
                  <v-btn
                    v-if="getRepeaterInstances(group) > 1"
                    icon="mdi-close"
                    size="small"
                    variant="text"
                    color="error"
                    @click="removeRepeaterInstance(group.id, 0)"
                  />
                </div>

                <!-- First Instance Items (from the original group) -->
                <div class="items-container">
                  <div
                    v-for="item in group.items"
                    :key="item.id"
                    v-show="isFieldVisible(item)"
                    class="mb-3"
                  >
                    <FormField
                      :item="getItemWithNumberedLabel(item, 1)"
                      :value="answers[item.id]"
                      :error="errors[item.id]"
                      :disabled="mode === 'preview'"
                      :mode="mode === 'preview' ? 'preview' : 'live'"
                      :available-fields="availableFields"
                      :field-answers="answers"
                      @update:value="updateAnswer(item.id, $event)"
                    />
                  </div>
                </div>

                <!-- Add button after first instance if it's the only one -->
                <div v-if="getAdditionalInstances(group).length === 0" class="d-flex justify-center mt-2">
                  <v-btn
                    icon="mdi-plus"
                    size="small"
                    variant="outlined"
                    color="primary"
                    @click="addRepeaterInstance(group)"
                  />
                </div>
              </div>

              <!-- Additional Repeater Instances -->
              <div
                v-for="(instance, instanceIndex) in getAdditionalInstances(group)"
                :key="instance.id"
                class="repeater-instance mb-4"
              >
                <div class="d-flex justify-space-between align-center mb-2">
                  <h5 class="text-subtitle-1">{{ instance.title.fallback }}</h5>
                  <v-btn
                    icon="mdi-close"
                    size="small"
                    variant="text"
                    color="error"
                    @click="removeRepeaterInstance(group.id, instanceIndex + 1)"
                  />
                </div>

                <!-- Instance Items -->
                <div class="items-container">
                  <div
                    v-for="item in instance.items"
                    :key="item.id"
                    v-show="isFieldVisible(item)"
                    class="mb-3"
                  >
                    <FormField
                      :item="getItemWithNumberedLabel(item, instanceIndex + 2)"
                      :value="answers[item.id]"
                      :error="errors[item.id]"
                      :disabled="mode === 'preview'"
                      :mode="mode === 'preview' ? 'preview' : 'live'"
                      :available-fields="availableFields"
                      :field-answers="answers"
                      @update:value="updateAnswer(item.id, $event)"
                    />
                  </div>
                </div>

                <!-- Add button after this instance if it's the last one -->
                <div v-if="instanceIndex === getAdditionalInstances(group).length - 1" class="d-flex justify-center mt-2">
                  <v-btn
                    icon="mdi-plus"
                    size="small"
                    variant="outlined"
                    color="primary"
                    @click="addRepeaterInstance(group)"
                  />
                </div>
              </div>
            </div>

            <!-- Regular Group Items -->
            <div v-else class="items-container">
              <div
                v-for="item in group.items"
                :key="item.id"
                v-show="isFieldVisible(item)"
                class="mb-3"
              >
                <FormField
                  :item="item"
                  :value="answers[item.id]"
                  :error="errors[item.id]"
                  :disabled="mode === 'preview'"
                  :mode="mode === 'preview' ? 'preview' : 'live'"
                  :available-fields="availableFields"
                  :field-answers="answers"
                  @update:value="updateAnswer(item.id, $event)"
                />
              </div>
            </div>
          </div>
        </div>

        <!-- Navigation -->
        <div class="d-flex justify-space-between mt-6">
          <v-btn
            v-if="currentPage > 0"
            variant="outlined"
            prepend-icon="mdi-arrow-left"
            @click="previousPage"
          >
            Previous
          </v-btn>
          <v-spacer />
          <v-btn
            v-if="currentPage < questionnaire.pages.length - 1"
            color="primary"
            append-icon="mdi-arrow-right"
            @click="nextPage"
          >
            Next
          </v-btn>
          <v-btn
            v-else
            color="primary"
            prepend-icon="mdi-check"
            :loading="isSubmitting"
            @click="submitForm"
          >
            Submit
          </v-btn>
        </div>
      </div>
    </v-card-text>

    <!-- Empty State -->
    <v-card-text v-else class="text-center py-12">
      <v-icon size="64" color="grey-lighten-1" class="mb-4">
        mdi-form-select
      </v-icon>
      <h3 class="text-h6 mb-2">No questionnaire to render</h3>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
// -nocheck — ported SysCase file; TS strict cleanup is a follow-up
import { ref, computed } from 'vue'
import FormField from './FormField.vue'
import type { Questionnaire, QuestionnaireResponse, QuestionnaireItem } from '@quvian/shared/types/questionnaire'

// Props
const props = defineProps<{
  questionnaire: Questionnaire | null
  answers?: Record<string, any>
  mode?: 'preview' | 'live'
}>()

// Emits
const emit = defineEmits<{
  change: [fieldId: string, value: any]
  submit: [response: QuestionnaireResponse]
}>()

// Reactive state
const currentPage = ref(0)
const errors = ref<Record<string, string>>({})
const isSubmitting = ref(false)

// Computed
const currentPageData = computed(() => {
  if (!props.questionnaire || !props.questionnaire.pages[currentPage.value]) {
    return null
  }
  return props.questionnaire.pages[currentPage.value]
})

const progressPercentage = computed(() => {
  if (!props.questionnaire) return 0
  return ((currentPage.value + 1) / props.questionnaire.pages.length) * 100
})

const answers = computed(() => props.answers || {})

const availableFields = computed(() => {
  if (!props.questionnaire) return []

  const fields: Array<{ id: string; label: string; type: string }> = []

  // Collect all fields from all pages and sections before the current item
  for (let pageIndex = 0; pageIndex <= currentPage.value; pageIndex++) {
    const page = props.questionnaire.pages[pageIndex]
    if (!page) continue

    for (const section of page.sections) {
      for (const group of section.groups || []) {
        for (const item of group.items || []) {
          // Skip non-input fields like headings, paragraphs, etc.
          if (['heading', 'paragraph', 'divider'].includes(item.type)) continue

          fields.push({
            id: item.id,
            label: item.label?.fallback || item.type,
            type: item.type
          })
        }
      }
    }
  }

  return fields
})

// Methods
const updateAnswer = (fieldId: string, value: any) => {
  emit('change', fieldId, value)
}

const nextPage = () => {
  if (!props.questionnaire || currentPage.value >= props.questionnaire.pages.length - 1) return
  currentPage.value++
}

const previousPage = () => {
  if (currentPage.value <= 0) return
  currentPage.value--
}

const isFieldVisible = (item: QuestionnaireItem) => {
  // If no conditions, field is always visible
  if (!item.conditions || item.conditions.length === 0) {
    return true
  }

  // Evaluate all conditions (AND logic by default)
  return item.conditions.every(condition => {
    const dependentValue = answers.value[condition.dependsOn]
    let conditionMet = false

    switch (condition.operator) {
      case 'equals':
        conditionMet = dependentValue === condition.value
        break
      case 'notEquals':
        conditionMet = dependentValue !== condition.value
        break
      case 'contains':
        if (Array.isArray(dependentValue)) {
          conditionMet = dependentValue.includes(condition.value)
        } else {
          conditionMet = String(dependentValue).includes(String(condition.value))
        }
        break
      case 'notContains':
        if (Array.isArray(dependentValue)) {
          conditionMet = !dependentValue.includes(condition.value)
        } else {
          conditionMet = !String(dependentValue).includes(String(condition.value))
        }
        break
      case 'startsWith':
        conditionMet = String(dependentValue).startsWith(String(condition.value))
        break
      case 'endsWith':
        conditionMet = String(dependentValue).endsWith(String(condition.value))
        break
      case 'greaterThan':
        conditionMet = Number(dependentValue) > Number(condition.value)
        break
      case 'lessThan':
        conditionMet = Number(dependentValue) < Number(condition.value)
        break
      case 'greaterOrEqual':
        conditionMet = Number(dependentValue) >= Number(condition.value)
        break
      case 'lessOrEqual':
        conditionMet = Number(dependentValue) <= Number(condition.value)
        break
      case 'isEmpty':
        conditionMet = !dependentValue || dependentValue === '' ||
                      (Array.isArray(dependentValue) && dependentValue.length === 0)
        break
      case 'isNotEmpty':
        conditionMet = !!dependentValue && dependentValue !== '' &&
                      (!Array.isArray(dependentValue) || dependentValue.length > 0)
        break
    }

    // Apply the action (show/hide)
    return condition.action === 'show' ? conditionMet : !conditionMet
  })
}

// Helper functions for repeater logic
const getRepeaterInstanceTitle = (group: any, instanceNumber: number) => {
  const groupTitle = group.title?.fallback || 'Item'
  return `${groupTitle} ${instanceNumber}`
}

const getRepeaterInstances = (group: any) => {
  // Total instances = 1 (base group) + additional instances
  const additionalInstances = group.repeaterInstances?.length || 0
  return 1 + additionalInstances
}

const getAdditionalInstances = (group: any) => {
  return group.repeaterInstances || []
}

const getItemWithNumberedLabel = (item: any, instanceNumber: number) => {
  return {
    ...item,
    label: item.label ? {
      ...item.label,
      fallback: `${item.label.fallback} ${instanceNumber}`
    } : { fallback: `Item ${instanceNumber}` }
  }
}

const addRepeaterInstance = (group: any) => {
  if (!props.questionnaire || !group.isRepeater) return

  // Find the group in the questionnaire and add an instance
  for (const page of props.questionnaire.pages) {
    for (const section of page.sections) {
      const targetGroup = section.groups.find(g => g.id === group.id)
      if (targetGroup) {
        if (!targetGroup.repeaterInstances) {
          targetGroup.repeaterInstances = []
        }

        // Instance number starts from 2 (since 1 is the base group)
        const instanceNumber = targetGroup.repeaterInstances.length + 2
        const instanceId = `${targetGroup.id}_instance_${instanceNumber}`

        const newInstance = {
          id: instanceId,
          title: { fallback: getRepeaterInstanceTitle(targetGroup, instanceNumber) },
          items: targetGroup.items.map(item => ({
            ...item,
            id: `${instanceId}_${item.id.split('_').pop()}`
          }))
        }

        targetGroup.repeaterInstances.push(newInstance)
        return
      }
    }
  }
}

const removeRepeaterInstance = (groupId: string, instanceIndex: number) => {
  if (!props.questionnaire) return

  // Find the group and remove the instance
  for (const page of props.questionnaire.pages) {
    for (const section of page.sections) {
      const targetGroup = section.groups.find(g => g.id === groupId)
      if (targetGroup) {
        if (instanceIndex === 0) {
          // Removing the first instance (base group)
          // If there are additional instances, we need to move one up to become the base
          if (targetGroup.repeaterInstances && targetGroup.repeaterInstances.length > 0) {
            const newBaseInstance = targetGroup.repeaterInstances.shift()
            if (newBaseInstance) {
              // Clear answers for the original base items
              targetGroup.items.forEach(item => {
                delete answers.value[item.id]
              })

              // Update the base group with new instance items but keep original IDs
              targetGroup.items = newBaseInstance.items.map(item => ({
                ...item,
                id: targetGroup.items.find(baseItem =>
                  baseItem.id.split('_').pop() === item.id.split('_').pop()
                )?.id || item.id
              }))
            }
          }
          // If no additional instances, we can't remove the only instance
        } else {
          // Removing an additional instance
          const actualIndex = instanceIndex - 1 // Adjust for base group
          if (targetGroup.repeaterInstances && targetGroup.repeaterInstances[actualIndex]) {
            // Remove answers for all items in this instance
            const instance = targetGroup.repeaterInstances[actualIndex]
            instance.items.forEach(item => {
              delete answers.value[item.id]
            })

            targetGroup.repeaterInstances.splice(actualIndex, 1)
          }
        }
        return
      }
    }
  }
}

const submitForm = async () => {
  if (!props.questionnaire) return

  isSubmitting.value = true

  try {
    // Basic validation would go here

    const response: QuestionnaireResponse = {
      questionnaireId: props.questionnaire.id,
      version: props.questionnaire.version,
      submittedAt: new Date().toISOString(),
      answers: answers.value
    }

    emit('submit', response)
  } finally {
    isSubmitting.value = false
  }
}
</script>

<style scoped>
.questionnaire-renderer {
  max-width: 800px;
  margin: 0 auto;
}

.section {
  border-left: 3px solid rgba(var(--v-theme-primary), 0.3);
  padding-left: 16px;
}

.items-container {
  display: grid;
  gap: 16px;
}

.group {
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 8px;
  padding: 16px;
  background-color: rgba(var(--v-theme-surface-variant), 0.3);
}

.repeater-instance {
  border: 1px dashed rgba(var(--v-theme-info), 0.5);
  border-radius: 6px;
  padding: 12px;
  background-color: rgba(var(--v-theme-info), 0.05);
}
</style>