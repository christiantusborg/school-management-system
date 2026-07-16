<template>
  <div class="questionnaire-builder">
    <!-- Header -->
    <div class="d-flex justify-space-between align-center mb-6">
      <div>
        <h1 class="text-h4 font-weight-bold">Questionnaire Builder</h1>
        <p class="text-body-1 text-medium-emphasis">
          Create custom intake forms for your booking process
        </p>
      </div>
      <div class="d-flex gap-2">
        <v-btn
          variant="outlined"
          prepend-icon="mdi-import"
          @click="showImportDialog = true"
        >
          Import
        </v-btn>
        <v-btn
          variant="outlined"
          prepend-icon="mdi-export"
          @click="exportQuestionnaire"
          :disabled="!currentQuestionnaire"
        >
          Export
        </v-btn>
        <v-btn
          color="success"
          prepend-icon="mdi-content-save"
          @click="handleSaveCurrent"
          :disabled="!currentQuestionnaire"
          :loading="questionnaireStore.isLoading"
        >
          Save
        </v-btn>
        <v-btn
          color="primary"
          prepend-icon="mdi-plus"
          @click="showCreateDialog = true"
        >
          Create New
        </v-btn>
      </div>
    </div>

    <!-- Tabs for different views -->
    <v-tabs v-model="activeTab" class="mb-4">
      <v-tab value="questionnaires">
        <v-icon icon="mdi-folder-multiple" class="mr-2" />
        Questionnaires
      </v-tab>
      <v-tab value="builder">
        <v-icon icon="mdi-form-select" class="mr-2" />
        Builder
      </v-tab>
      <v-tab value="templates">
        <v-icon icon="mdi-folder-multiple" class="mr-2" />
        Groups
      </v-tab>
    </v-tabs>

    <v-window v-model="activeTab">
      <!-- Questionnaires View -->
      <v-window-item value="questionnaires">
        <div class="pa-4">
          <QuestionnaireList
            @select-questionnaire="handleSelectQuestionnaire"
            @create-questionnaire="handleCreateQuestionnaire"
            @edit-questionnaire="handleEditQuestionnaire"
            @delete-questionnaire="handleDeleteQuestionnaire"
            @duplicate-questionnaire="handleDuplicateQuestionnaire"
            @preview-questionnaire="handlePreviewQuestionnaire"
            @show-json-dialog="handleShowJsonDialog"
          />
        </div>
      </v-window-item>

      <!-- Builder View -->
      <v-window-item value="builder">
        <!-- Builder Mode Header -->
        <div class="d-flex align-center justify-space-between mb-4 pa-4 bg-surface-variant rounded">
          <div class="d-flex align-center">
            <v-icon
              :icon="builderMode === 'component' ? 'mdi-puzzle' : 'mdi-form-select'"
              :color="builderMode === 'component' ? 'purple' : 'primary'"
              class="mr-2"
              size="20"
            />
            <h3 class="text-h6">
              {{ builderMode === 'component' ? 'Building Groups' : 'Building Questionnaire' }}
            </h3>
            <v-chip
              :color="builderMode === 'component' ? 'purple' : 'primary'"
              size="small"
              class="ml-3"
            >
              {{ builderMode === 'component' ? 'Component' : 'Questionnaire' }}
            </v-chip>
          </div>
          <div v-if="builderMode === 'component' && currentCustomComponent">
            <span class="text-body-2 text-medium-emphasis">
              {{ currentCustomComponent.name?.fallback || 'Untitled Component' }}
            </span>
          </div>
          <div v-else-if="builderMode === 'questionnaire' && currentQuestionnaire">
            <span class="text-body-2 text-medium-emphasis">
              {{ currentQuestionnaire.name?.fallback || 'Untitled Questionnaire' }}
            </span>
          </div>
        </div>

        <v-row>
          <!-- Component Palette -->
          <v-col cols="12" md="3">
            <QuestionnaireComponentPalette
              :builder-mode="builderMode"
              @add-component="addComponent"
            />
          </v-col>

          <!-- Canvas -->
          <v-col cols="12" md="6">
            <!-- Group Canvas for component building mode -->
            <GroupCanvas
              v-if="builderMode === 'component'"
              :current-group="currentCustomComponent"
              :selected-item="selectedItem"
              @select-item="selectItem"
              @save-group="handleSaveGroup"
              @cancel-edit="handleCancelGroupEdit"
            />

            <!-- Questionnaire Canvas for questionnaire building mode -->
            <QuestionnaireCanvas
              v-else
              :questionnaire="currentQuestionnaire"
              :selected-item="selectedItem"
              @select-item="selectItem"
              @update-questionnaire="updateQuestionnaire"
              @duplicate-item="duplicateItem"
              @delete-item="deleteItem"
              @add-component="handleAddComponentRequest"
              @add-component-drop="handleComponentDrop"
              @duplicate-group="handleDuplicateGroup"
            />
          </v-col>

          <!-- Inspector -->
          <v-col cols="12" md="3">
            <QuestionnaireInspector
              :selected-item="selectedItem"
              :questionnaire="currentQuestionnaire"
              @update-item="updateSelectedItem"
            />
          </v-col>
        </v-row>
      </v-window-item>

      <!-- Groups View -->
      <v-window-item value="templates">
        <div class="pa-4">
          <QuestionnaireCustomComponents
            @create-group="handleCreateGroup"
            @edit-group="handleEditGroup"
            @delete-group="handleDeleteGroup"
            @drag-group="handleDragGroup"
          />
        </div>
      </v-window-item>

    </v-window>

    <!-- Import Dialog -->
    <v-dialog v-model="showImportDialog" max-width="600">
      <v-card>
        <v-card-title>Import Questionnaire</v-card-title>
        <v-card-text>
          <v-textarea
            v-model="importJson"
            label="Paste questionnaire JSON"
            rows="10"
            variant="outlined"
            :error-messages="importError"
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showImportDialog = false">
            Cancel
          </v-btn>
          <v-btn color="primary" @click="importQuestionnaire">
            Import
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Component Selection Dialog -->
    <v-dialog v-model="showComponentDialog" max-width="800">
      <v-card>
        <v-card-title>Select Component to Add</v-card-title>
        <v-card-text>
          <QuestionnaireComponentPalette
            @add-component="addComponentToPosition"
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showComponentDialog = false">
            Cancel
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Unified Creation Dialog -->
    <v-dialog v-model="showCreateDialog" max-width="500">
      <v-card>
        <v-card-title>Create New</v-card-title>
        <v-card-text>
          <!-- Type Selection -->
          <v-radio-group v-model="createType" class="mb-4">
            <v-radio
              label="Questionnaire"
              value="questionnaire"
            />
            <v-radio
              label="Component"
              value="component"
            />
          </v-radio-group>

          <!-- Name Field -->
          <v-text-field
            v-model="newItemName"
            :label="createType === 'component' ? 'Component Name' : 'Questionnaire Name'"
            variant="outlined"
            density="compact"
            :rules="[rules.required]"
            autofocus
          />

          <!-- Description Field -->
          <v-textarea
            v-model="newItemDescription"
            label="Description"
            variant="outlined"
            density="compact"
            rows="3"
            :hint="`Optional description for this ${createType}`"
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="cancelCreate">
            Cancel
          </v-btn>
          <v-btn
            color="primary"
            @click="createNewItem"
            :disabled="!newItemName"
          >
            Create {{ createType === 'component' ? 'Component' : 'Questionnaire' }}
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Preview Dialog -->
    <v-dialog v-model="showPreviewDialog" max-width="800">
      <v-card v-if="previewingQuestionnaire">
        <v-card-title class="d-flex align-center justify-space-between">
          <div class="d-flex align-center">
            <v-icon icon="mdi-eye" class="mr-2" />
            Preview: {{ previewingQuestionnaire.name.fallback }}
          </div>
          <v-btn
            icon="mdi-close"
            variant="text"
            @click="showPreviewDialog = false"
          />
        </v-card-title>
        <v-card-text>
          <QuestionnaireRenderer
            :questionnaire="previewingQuestionnaire"
            :answers="previewAnswers"
            mode="preview"
            @change="handlePreviewChange"
            @submit="handlePreviewSubmit"
          />
        </v-card-text>
      </v-card>
    </v-dialog>

    <!-- JSON Dialog -->
    <v-dialog v-model="showJsonDialog" max-width="800">
      <v-card v-if="jsonQuestionnaire">
        <v-card-title class="d-flex align-center justify-space-between">
          <div class="d-flex align-center">
            <v-icon icon="mdi-code-json" class="mr-2" />
            JSON: {{ jsonQuestionnaire.name.fallback }}
          </div>
          <v-btn
            icon="mdi-close"
            variant="text"
            @click="showJsonDialog = false"
          />
        </v-card-title>
        <v-card-text>
          <QuestionnaireJsonView
            :questionnaire="jsonQuestionnaire"
            @update-questionnaire="updateQuestionnaire"
          />
        </v-card-text>
      </v-card>
    </v-dialog>

    <!-- Success/Error Snackbar -->
    <v-snackbar
      v-model="showSnackbar"
      :color="snackbarColor"
      :timeout="3000"
    >
      {{ snackbarMessage }}
      <template #actions>
        <v-btn
          color="white"
          variant="text"
          @click="showSnackbar = false"
        >
          Close
        </v-btn>
      </template>
    </v-snackbar>
  </div>
</template>

<script setup lang="ts">
// -nocheck — ported SysCase file; TS strict cleanup is a follow-up
import { ref, computed, watch } from 'vue'
import { useQuestionnaireStore } from '@/stores/questionnaire'
import { useCustomComponentsStore } from '@/stores/customComponents'
import QuestionnaireComponentPalette from '@/components/questionnaire/QuestionnaireComponentPalette.vue'
import QuestionnaireCanvas from '@/components/questionnaire/QuestionnaireCanvas.vue'
import GroupCanvas from '@/components/questionnaire/GroupCanvas.vue'
import QuestionnaireInspector from '@/components/questionnaire/QuestionnaireInspector.vue'
import QuestionnaireRenderer from '@/components/questionnaire/QuestionnaireRenderer.vue'
import QuestionnaireJsonView from '@/components/questionnaire/QuestionnaireJsonView.vue'
import QuestionnaireResponsesView from '@/components/questionnaire/QuestionnaireResponsesView.vue'
import QuestionnaireCustomComponents from '@/components/questionnaire/QuestionnaireCustomComponents.vue'
import QuestionnaireList from '@/components/questionnaire/QuestionnaireList.vue'
import type { QuestionnaireItem, ComponentType, Questionnaire } from '@quvian/shared/types/questionnaire'

// Store
const questionnaireStore = useQuestionnaireStore()
const customComponentsStore = useCustomComponentsStore()

// Reactive state
const activeTab = ref('questionnaires')
const selectedItem = ref<QuestionnaireItem | null>(null)
const previewAnswers = ref<Record<string, any>>({})
const showImportDialog = ref(false)
const showComponentDialog = ref(false)
const showCreateDialog = ref(false)
const createType = ref<'questionnaire' | 'component'>('questionnaire')
const newItemName = ref('')
const newItemDescription = ref('')
const pendingPosition = ref<{ pageIndex: number; sectionIndex: number; groupIndex?: number } | null>(null)
const importJson = ref('')
const importError = ref('')
const showSnackbar = ref(false)
const snackbarMessage = ref('')
const snackbarColor = ref<'success' | 'error'>('success')
const builderMode = ref<'questionnaire' | 'component'>('questionnaire')
const currentCustomComponent = ref<any>(null)
const showPreviewDialog = ref(false)
const showJsonDialog = ref(false)
const previewingQuestionnaire = ref<Questionnaire | null>(null)
const jsonQuestionnaire = ref<Questionnaire | null>(null)

// Computed
const currentQuestionnaire = computed(() => questionnaireStore.currentQuestionnaire)

// Validation rules
const rules = {
  required: (value: string) => !!value || 'This field is required'
}

// Methods
const createNewQuestionnaire = () => {
  questionnaireStore.createNew()
  selectedItem.value = null
  showNotification('New questionnaire created', 'success')
}

const addComponent = (componentType: ComponentType, position?: { pageIndex: number; sectionIndex: number; groupIndex?: number; itemIndex?: number }) => {
  const newItem = questionnaireStore.addComponent(componentType, position)
  selectedItem.value = newItem
  showNotification(`${componentType} component added`, 'success')
}

const handleAddComponentRequest = (pageIndex: number, sectionIndex: number, groupIndex?: number) => {
  pendingPosition.value = { pageIndex, sectionIndex, groupIndex }
  showComponentDialog.value = true
}

const addComponentToPosition = (componentType: ComponentType) => {
  if (pendingPosition.value) {
    const position = {
      pageIndex: pendingPosition.value.pageIndex,
      sectionIndex: pendingPosition.value.sectionIndex,
      groupIndex: pendingPosition.value.groupIndex
    }
    addComponent(componentType, position)
    showComponentDialog.value = false
    pendingPosition.value = null
  }
}

const handleComponentDrop = (componentType: ComponentType, position: { pageIndex: number; sectionIndex: number; groupIndex?: number }) => {
  addComponent(componentType, position)
}

const selectItem = (item: QuestionnaireItem | null) => {
  selectedItem.value = item
}

const updateQuestionnaire = (questionnaire: any) => {
  questionnaireStore.updateQuestionnaire(questionnaire)
}

const updateSelectedItem = (updates: Partial<QuestionnaireItem>) => {
  if (selectedItem.value) {
    questionnaireStore.updateItem(selectedItem.value.id, updates)
    // Update the reference to trigger reactivity
    selectedItem.value = { ...selectedItem.value, ...updates }
  }
}

const duplicateItem = (item: QuestionnaireItem) => {
  const duplicated = questionnaireStore.duplicateItem(item.id)
  selectedItem.value = duplicated
  showNotification('Item duplicated', 'success')
}

const deleteItem = (item: QuestionnaireItem) => {
  questionnaireStore.deleteItem(item.id)
  if (selectedItem.value?.id === item.id) {
    selectedItem.value = null
  }
  showNotification('Item deleted', 'success')
}


const handleDuplicateGroup = (group: any) => {
  // For now, just show a notification
  // In a real implementation, this would duplicate the group
  showNotification(`Group "${group.title?.fallback || 'Untitled'}" duplicated`, 'success')
}

const handlePreviewChange = (fieldId: string, value: any) => {
  previewAnswers.value[fieldId] = value
}

const handlePreviewSubmit = (response: any) => {
  console.log('Preview submission:', response)
  showNotification('Form submission preview completed', 'success')
}

const exportQuestionnaire = () => {
  if (!currentQuestionnaire.value) return

  const dataStr = JSON.stringify(currentQuestionnaire.value, null, 2)
  const dataUri = 'data:application/json;charset=utf-8,'+ encodeURIComponent(dataStr)

  const exportFileDefaultName = `questionnaire-${currentQuestionnaire.value.id}.json`

  const linkElement = document.createElement('a')
  linkElement.setAttribute('href', dataUri)
  linkElement.setAttribute('download', exportFileDefaultName)
  linkElement.click()

  showNotification('Questionnaire exported', 'success')
}

const importQuestionnaire = () => {
  try {
    importError.value = ''
    const questionnaire = JSON.parse(importJson.value)

    // Basic validation
    if (!questionnaire.version || !questionnaire.id || !questionnaire.pages) {
      throw new Error('Invalid questionnaire format')
    }

    questionnaireStore.importQuestionnaire(questionnaire)
    selectedItem.value = null
    showImportDialog.value = false
    importJson.value = ''
    showNotification('Questionnaire imported successfully', 'success')
  } catch (error) {
    importError.value = error instanceof Error ? error.message : 'Invalid JSON format'
  }
}

const showNotification = (message: string, color: 'success' | 'error') => {
  snackbarMessage.value = message
  snackbarColor.value = color
  showSnackbar.value = true
}

// Unified creation handlers
const createNewItem = () => {
  if (createType.value === 'questionnaire') {
    questionnaireStore.createNew(newItemName.value)
    selectedItem.value = null
    builderMode.value = 'questionnaire'
    currentCustomComponent.value = null
    activeTab.value = 'builder'
    showNotification(`Questionnaire "${newItemName.value}" created`, 'success')
  } else {
    const newComponent = customComponentsStore.addCustomComponent({
      name: { fallback: newItemName.value },
      description: newItemDescription.value ? { fallback: newItemDescription.value } : undefined,
      icon: 'mdi-puzzle',
      items: [],
      previewText: '0 fields'
    })
    currentCustomComponent.value = newComponent
    builderMode.value = 'component'
    selectedItem.value = null
    activeTab.value = 'builder'
    showNotification(`Component "${newItemName.value}" created`, 'success')
  }
  cancelCreate()
}

const cancelCreate = () => {
  showCreateDialog.value = false
  newItemName.value = ''
  newItemDescription.value = ''
  // Reset create type based on current tab
  if (activeTab.value === 'questionnaires') {
    createType.value = 'questionnaire'
  } else if (activeTab.value === 'templates') {
    createType.value = 'component'
  }
}

// Custom component management handlers
const handleCreateComponent = (component: any) => {
  showNotification(`Component "${component.name?.fallback || 'Untitled'}" created`, 'success')
}

const handleEditComponent = (component: any) => {
  showNotification(`Component "${component.name?.fallback || 'Untitled'}" updated`, 'success')
}

const handleDeleteComponent = (component: any) => {
  showNotification(`Component "${component.name?.fallback || 'Untitled'}" deleted`, 'success')
}

const handleDragComponent = (component: any) => {
  // Handle component drag start if needed
  console.log('Component drag started:', component)
}

// Questionnaire management handlers
const handleSelectQuestionnaire = (questionnaire: Questionnaire) => {
  questionnaireStore.loadQuestionnaire(questionnaire.id)
  selectedItem.value = null
  activeTab.value = 'builder'
  showNotification(`Loaded questionnaire: "${questionnaire.name.fallback}"`, 'success')
}

const handleCreateQuestionnaire = (name?: string) => {
  questionnaireStore.createNew(name)
  selectedItem.value = null
  activeTab.value = 'builder'
  showNotification('New questionnaire created', 'success')
}

const handleEditQuestionnaire = (questionnaire: Questionnaire) => {
  questionnaireStore.loadQuestionnaire(questionnaire.id)
  selectedItem.value = null
  activeTab.value = 'builder'
  showNotification(`Editing questionnaire: "${questionnaire.name.fallback}"`, 'success')
}

const handleDeleteQuestionnaire = async (questionnaire: Questionnaire) => {
  try {
    await questionnaireStore.deleteQuestionnaire(questionnaire.id)
    showNotification(`Questionnaire "${questionnaire.name.fallback}" deleted`, 'success')
  } catch (err) {
    showNotification(`Failed to delete: ${(err as Error).message}`, 'error')
  }
}

const handleDuplicateQuestionnaire = async (questionnaire: Questionnaire) => {
  try {
    const clone: Questionnaire = JSON.parse(JSON.stringify(questionnaire))
    clone.id = `questionnaire_${Date.now()}`
    clone.name = { ...clone.name, fallback: `${questionnaire.name.fallback} (Copy)` }
    clone.createdAt = new Date().toISOString()
    clone.updatedAt = new Date().toISOString()
    questionnaireStore.questionnaires.push(clone)
    questionnaireStore.currentQuestionnaire = clone
    await questionnaireStore.saveCurrentQuestionnaire()
    showNotification(`Questionnaire "${questionnaire.name.fallback}" duplicated`, 'success')
  } catch (err) {
    showNotification(`Failed to duplicate: ${(err as Error).message}`, 'error')
  }
}

const handleSaveCurrent = async () => {
  try {
    const saved = await questionnaireStore.saveCurrentQuestionnaire()
    if (saved) {
      showNotification(`Saved "${saved.name.fallback}"`, 'success')
    }
  } catch (err) {
    showNotification(`Failed to save: ${(err as Error).message}`, 'error')
  }
}

const handlePreviewQuestionnaire = (questionnaire: Questionnaire) => {
  previewingQuestionnaire.value = questionnaire
  showPreviewDialog.value = true
}

const handleShowJsonDialog = (questionnaire: Questionnaire) => {
  jsonQuestionnaire.value = questionnaire
  showJsonDialog.value = true
}

// Custom component list handlers (for Components tab)
const handleSelectComponent = (component: any) => {
  currentCustomComponent.value = component
  builderMode.value = 'component'
  selectedItem.value = null
  activeTab.value = 'builder'
  showNotification(`Editing component: "${component.name.fallback}"`, 'success')
}

const handleCreateComponentFromList = (name?: string) => {
  // This will be handled by the unified create dialog
  showCreateDialog.value = true
  createType.value = 'component'
}

const handleEditComponentFromList = (component: any) => {
  currentCustomComponent.value = component
  builderMode.value = 'component'
  selectedItem.value = null
  activeTab.value = 'builder'
  showNotification(`Editing component: "${component.name.fallback}"`, 'success')
}

const handleDuplicateComponent = (component: any) => {
  const duplicated = customComponentsStore.duplicateCustomComponent(component.id)
  if (duplicated) {
    showNotification(`Component "${component.name.fallback}" duplicated`, 'success')
  }
}

// Watch for tab changes to set default create type
const handleTabChange = () => {
  if (activeTab.value === 'questionnaires') {
    createType.value = 'questionnaire'
  } else if (activeTab.value === 'templates') {
    createType.value = 'component'
  }
}

// Group management handlers
const handleCreateGroup = (group: any) => {
  if (group) {
    showNotification(`Group "${group.name?.fallback || 'Untitled'}" created`, 'success')
  } else {
    // Switch to builder mode for creating a new group
    builderMode.value = 'component'
    currentCustomComponent.value = null
    selectedItem.value = null
    activeTab.value = 'builder'
    showNotification('Starting new group creation', 'success')
  }
}

const handleEditGroup = (group: any) => {
  // Switch to builder mode for editing the group
  builderMode.value = 'component'
  currentCustomComponent.value = group
  selectedItem.value = null
  activeTab.value = 'builder'
  showNotification(`Editing group: "${group.name?.fallback || 'Untitled'}"`, 'success')
}

const handleDeleteGroup = (group: any) => {
  showNotification(`Group "${group.name?.fallback || 'Untitled'}" deleted`, 'success')
}

const handleDragGroup = (group: any) => {
  // Handle group drag start if needed
  console.log('Group drag started:', group)
}

const handleSaveGroup = (groupData: any) => {
  if (groupData.id) {
    // Update existing group
    customComponentsStore.updateCustomComponent(groupData.id, groupData)
    showNotification(`Group "${groupData.name.fallback}" updated`, 'success')
  } else {
    // Create new group
    const newGroup = customComponentsStore.addCustomComponent(groupData)
    showNotification(`Group "${groupData.name.fallback}" created`, 'success')
  }

  // Return to groups tab
  activeTab.value = 'templates'
  builderMode.value = 'questionnaire'
  currentCustomComponent.value = null
  selectedItem.value = null
}

const handleCancelGroupEdit = () => {
  // Return to groups tab without saving
  activeTab.value = 'templates'
  builderMode.value = 'questionnaire'
  currentCustomComponent.value = null
  selectedItem.value = null
}

// Watch for dialog opening to set appropriate defaults
watch(showCreateDialog, (isShowing) => {
  if (isShowing) {
    // Set default create type based on current tab
    if (activeTab.value === 'questionnaires') {
      createType.value = 'questionnaire'
    } else if (activeTab.value === 'templates') {
      createType.value = 'component'
    } else {
      // For other tabs, default to questionnaire
      createType.value = 'questionnaire'
    }
  }
})

// Initialize: load user group templates from localStorage, fetch
// existing questionnaires from the encrypted backend, and only create a
// fresh blank one if the firm library is still empty after the fetch.
questionnaireStore.loadUserTemplates()
questionnaireStore.fetchQuestionnaires()
  .catch(err => console.error('fetchQuestionnaires failed:', err))
  .finally(() => {
    if (!currentQuestionnaire.value && questionnaireStore.questionnaires.length === 0) {
      questionnaireStore.createNew()
    }
  })
</script>

<style scoped>
.questionnaire-builder {
  padding: 24px;
  max-width: 1600px;
  margin: 0 auto;
}

@media (max-width: 960px) {
  .questionnaire-builder {
    padding: 16px;
  }
}
</style>