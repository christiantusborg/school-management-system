<template>
  <v-card class="templates-manager">
    <v-card-title class="d-flex align-center justify-space-between">
      <div class="d-flex align-center">
        <v-icon icon="mdi-folder-multiple" class="mr-2" />
        Group Templates
      </div>
      <v-btn
        color="primary"
        prepend-icon="mdi-plus"
        @click="createNewTemplate"
      >
        New Template
      </v-btn>
    </v-card-title>

    <v-card-text>
      <!-- Template Library -->
      <div class="templates-library mb-6">
        <h3 class="text-h6 mb-3">Your Templates</h3>

        <div v-if="userTemplates.length === 0" class="empty-state text-center py-8">
          <v-icon size="64" color="grey-lighten-1" class="mb-4">
            mdi-folder-open-outline
          </v-icon>
          <h4 class="text-h6 mb-2">No templates yet</h4>
          <p class="text-body-2 text-medium-emphasis mb-4">
            Create reusable group templates to speed up form building
          </p>
          <v-btn
            color="primary"
            prepend-icon="mdi-plus"
            @click="createNewTemplate"
          >
            Create Your First Template
          </v-btn>
        </div>

        <v-row v-else>
          <v-col
            v-for="template in userTemplates"
            :key="template.id"
            cols="12"
            md="6"
            lg="4"
          >
            <v-card
              class="template-card"
              :class="{ 'template-selected': selectedTemplate?.id === template.id }"
              @click="selectTemplate(template)"
            >
              <v-card-title class="d-flex align-center justify-space-between">
                <div class="d-flex align-center">
                  <v-icon :icon="template.icon" class="mr-2" />
                  <span class="text-truncate">{{ template.name.fallback }}</span>
                </div>
                <v-menu>
                  <template v-slot:activator="{ props }">
                    <v-btn
                      icon="mdi-dots-vertical"
                      size="small"
                      variant="text"
                      v-bind="props"
                      @click.stop
                    />
                  </template>
                  <v-list>
                    <v-list-item @click="editTemplate(template)">
                      <template v-slot:prepend>
                        <v-icon icon="mdi-pencil" />
                      </template>
                      <v-list-item-title>Edit</v-list-item-title>
                    </v-list-item>
                    <v-list-item @click="duplicateTemplate(template)">
                      <template v-slot:prepend>
                        <v-icon icon="mdi-content-duplicate" />
                      </template>
                      <v-list-item-title>Duplicate</v-list-item-title>
                    </v-list-item>
                    <v-divider />
                    <v-list-item
                      @click="deleteTemplate(template)"
                      class="text-error"
                    >
                      <template v-slot:prepend>
                        <v-icon icon="mdi-delete" color="error" />
                      </template>
                      <v-list-item-title>Delete</v-list-item-title>
                    </v-list-item>
                  </v-list>
                </v-menu>
              </v-card-title>

              <v-card-text>
                <p class="text-body-2 text-medium-emphasis mb-2">
                  {{ template.description?.fallback || 'No description' }}
                </p>
                <v-chip size="small" color="info" class="mr-2">
                  {{ template.group.items.length }} fields
                </v-chip>
                <v-chip
                  v-if="template.group.isRepeater"
                  size="small"
                  color="secondary"
                  class="mr-2"
                >
                  Repeater
                </v-chip>
                <div class="mt-2">
                  <span class="text-caption text-medium-emphasis">
                    {{ template.previewText }}
                  </span>
                </div>
              </v-card-text>

              <v-card-actions>
                <v-btn
                  variant="outlined"
                  size="small"
                  prepend-icon="mdi-eye"
                  @click.stop="previewTemplate(template)"
                >
                  Preview
                </v-btn>
                <v-spacer />
                <v-btn
                  color="primary"
                  size="small"
                  prepend-icon="mdi-plus"
                  @click.stop="applyTemplate(template)"
                >
                  Use Template
                </v-btn>
              </v-card-actions>
            </v-card>
          </v-col>
        </v-row>
      </div>

      <!-- Template Editor (when creating/editing) -->
      <div v-if="isEditing" class="template-editor">
        <v-divider class="mb-4" />
        <h3 class="text-h6 mb-3">
          {{ editingTemplate ? 'Edit Template' : 'Create New Template' }}
        </h3>

        <v-row>
          <!-- Template Info -->
          <v-col cols="12" md="4">
            <v-card class="template-info-card">
              <v-card-title>Template Information</v-card-title>
              <v-card-text>
                <v-text-field
                  v-model="templateForm.name"
                  label="Template Name"
                  variant="outlined"
                  density="compact"
                  :rules="[rules.required]"
                />
                <v-textarea
                  v-model="templateForm.description"
                  label="Description"
                  variant="outlined"
                  density="compact"
                  rows="3"
                />
                <v-text-field
                  v-model="templateForm.icon"
                  label="Icon (MDI name)"
                  variant="outlined"
                  density="compact"
                  placeholder="mdi-folder"
                />
                <v-switch
                  v-model="templateForm.isRepeater"
                  label="Make this a repeater group"
                  color="primary"
                />
              </v-card-text>
              <v-card-actions>
                <v-btn
                  variant="outlined"
                  @click="cancelEdit"
                >
                  Cancel
                </v-btn>
                <v-spacer />
                <v-btn
                  color="primary"
                  @click="saveTemplate"
                  :disabled="!templateForm.name"
                >
                  {{ editingTemplate ? 'Update' : 'Create' }}
                </v-btn>
              </v-card-actions>
            </v-card>
          </v-col>

          <!-- Template Builder -->
          <v-col cols="12" md="8">
            <v-card class="template-builder">
              <v-card-title class="d-flex align-center justify-space-between">
                <span>Template Builder</span>
                <v-btn
                  icon="mdi-plus"
                  size="small"
                  variant="tonal"
                  @click="showAddComponentDialog = true"
                />
              </v-card-title>
              <v-card-text>
                <!-- Drop Zone for Components -->
                <div
                  class="template-drop-zone"
                  :class="{ 'drop-zone-active': isDragOver }"
                  @dragover.prevent="handleDragOver"
                  @dragleave="handleDragLeave"
                  @drop="handleDrop"
                >
                  <!-- Template Items -->
                  <div
                    v-for="(item, itemIndex) in templateForm.items"
                    :key="item.id"
                    class="template-item"
                    :class="{ 'item-selected': selectedItem?.id === item.id }"
                    @click="selectItem(item)"
                  >
                    <div class="item-header d-flex align-center justify-space-between">
                      <div class="d-flex align-center">
                        <v-icon
                          :icon="getComponentIcon(item.type)"
                          size="16"
                          class="mr-2"
                        />
                        <span class="text-body-2 font-weight-medium">
                          {{ item.label?.fallback || getComponentTitle(item.type) }}
                        </span>
                        <v-chip v-if="item.required" size="x-small" color="error" class="ml-2">
                          Required
                        </v-chip>
                      </div>
                      <div class="d-flex gap-1">
                        <v-btn
                          icon="mdi-content-duplicate"
                          size="x-small"
                          variant="text"
                          @click.stop="duplicateItem(item)"
                        />
                        <v-btn
                          icon="mdi-delete"
                          size="x-small"
                          variant="text"
                          color="error"
                          @click.stop="deleteItem(item)"
                        />
                      </div>
                    </div>

                    <!-- Item Preview -->
                    <div class="item-preview mt-2">
                      <component
                        :is="getPreviewComponent(item.type)"
                        :item="item"
                        :disabled="true"
                      />
                    </div>
                  </div>

                  <!-- Empty State -->
                  <div v-if="templateForm.items.length === 0" class="empty-template-state">
                    <v-icon size="48" color="grey-lighten-1" class="mb-2">
                      mdi-package-variant-closed
                    </v-icon>
                    <div class="text-body-2 text-medium-emphasis">
                      Drag components here or click the + button to add fields
                    </div>
                  </div>

                  <!-- Drop Zone Indicator -->
                  <div v-if="isDragOver" class="drop-zone-indicator">
                    <v-icon size="32" color="primary" class="mb-2">
                      mdi-download-box
                    </v-icon>
                    <div class="text-body-2 text-primary">
                      Drop component here
                    </div>
                  </div>
                </div>
              </v-card-text>
            </v-card>
          </v-col>
        </v-row>
      </div>
    </v-card-text>

    <!-- Component Selection Dialog -->
    <v-dialog v-model="showAddComponentDialog" max-width="800">
      <v-card>
        <v-card-title>Add Component to Template</v-card-title>
        <v-card-text>
          <QuestionnaireComponentPalette
            @add-component="addComponentToTemplate"
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showAddComponentDialog = false">
            Cancel
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Template Preview Dialog -->
    <v-dialog v-model="showPreviewDialog" max-width="600">
      <v-card v-if="previewingTemplate">
        <v-card-title>{{ previewingTemplate.name.fallback }}</v-card-title>
        <v-card-text>
          <p class="text-body-2 text-medium-emphasis mb-4">
            {{ previewingTemplate.description?.fallback || 'No description' }}
          </p>

          <!-- Preview of template fields -->
          <div class="template-preview">
            <div
              v-for="item in previewingTemplate.group?.items"
              :key="item.id"
              class="preview-item mb-3"
            >
              <div class="d-flex align-center mb-2">
                <v-icon :icon="getComponentIcon(item.type)" size="16" class="mr-2" />
                <span class="text-body-2 font-weight-medium">
                  {{ item.label?.fallback || getComponentTitle(item.type) }}
                </span>
                <v-chip v-if="item.required" size="x-small" color="error" class="ml-2">
                  Required
                </v-chip>
              </div>
              <component
                :is="getPreviewComponent(item.type)"
                :item="item"
                :disabled="true"
              />
            </div>
          </div>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showPreviewDialog = false">
            Close
          </v-btn>
          <v-btn
            color="primary"
            @click="applyTemplate(previewingTemplate!); showPreviewDialog = false"
          >
            Use This Template
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-card>
</template>

<script setup lang="ts">
// -nocheck — ported SysCase file; TS strict cleanup is a follow-up
import { ref, computed } from 'vue'
import { useQuestionnaireStore } from '@/stores/questionnaire'
import { componentRegistry } from '@/utils/questionnaire/componentRegistry'
import QuestionnaireComponentPalette from './QuestionnaireComponentPalette.vue'
import type { ComponentType, QuestionnaireItem, GroupTemplate } from '@quvian/shared/types/questionnaire'

// Emits
const emit = defineEmits<{
  createTemplate: [template: GroupTemplate]
  editTemplate: [template: GroupTemplate]
  deleteTemplate: [template: GroupTemplate]
  applyTemplate: [template: GroupTemplate]
}>()

// Store
const questionnaireStore = useQuestionnaireStore()

// Reactive state
const selectedTemplate = ref<GroupTemplate | null>(null)
const selectedItem = ref<QuestionnaireItem | null>(null)
const isEditing = ref(false)
const editingTemplate = ref<GroupTemplate | null>(null)
const showAddComponentDialog = ref(false)
const showPreviewDialog = ref(false)
const previewingTemplate = ref<GroupTemplate | null>(null)
const isDragOver = ref(false)

// Template form
const templateForm = ref({
  name: '',
  description: '',
  icon: 'mdi-folder',
  isRepeater: false,
  items: [] as QuestionnaireItem[]
})

// Validation rules
const rules = {
  required: (value: string) => !!value || 'This field is required'
}

// Computed
const userTemplates = computed(() => questionnaireStore.userGroupTemplates)

// Methods
const selectTemplate = (template: GroupTemplate) => {
  selectedTemplate.value = template
}

const selectItem = (item: QuestionnaireItem) => {
  selectedItem.value = item
}

const createNewTemplate = () => {
  isEditing.value = true
  editingTemplate.value = null
  templateForm.value = {
    name: '',
    description: '',
    icon: 'mdi-folder',
    isRepeater: false,
    items: []
  }
}

const editTemplate = (template: GroupTemplate) => {
  isEditing.value = true
  editingTemplate.value = template
  templateForm.value = {
    name: template.name.fallback,
    description: template.description?.fallback || '',
    icon: template.icon,
    isRepeater: template.group?.isRepeater || false,
    items: [...(template.group?.items ?? [])]
  }
}

const cancelEdit = () => {
  isEditing.value = false
  editingTemplate.value = null
  selectedItem.value = null
}

const saveTemplate = () => {
  const template: GroupTemplate = {
    id: editingTemplate.value?.id || `template_${Date.now()}`,
    name: { fallback: templateForm.value.name },
    description: { fallback: templateForm.value.description },
    icon: templateForm.value.icon,
    previewText: generatePreviewText(templateForm.value.items),
    group: {
      id: `group_${Date.now()}`,
      title: { fallback: templateForm.value.name },
      items: templateForm.value.items,
      isRepeater: templateForm.value.isRepeater
    }
  }

  if (editingTemplate.value) {
    questionnaireStore.updateGroupTemplate(template)
    emit('editTemplate', template)
  } else {
    questionnaireStore.saveGroupAsTemplate(template.group!, template.name.fallback, template.description?.fallback)
    emit('createTemplate', template)
  }

  cancelEdit()
}

const duplicateTemplate = (template: GroupTemplate) => {
  const duplicated: GroupTemplate = {
    ...template,
    id: `template_${Date.now()}`,
    name: { fallback: `${template.name.fallback} (Copy)` },
    group: {
      ...template.group,
      id: `group_${Date.now()}`,
      items: (template.group?.items ?? []).map(item => ({
        ...item,
        id: `${item.id}_copy_${Date.now()}`
      }))
    }
  }

  questionnaireStore.saveGroupAsTemplate(duplicated.group!, duplicated.name.fallback, duplicated.description?.fallback)
  emit('createTemplate', duplicated)
}

const deleteTemplate = (template: GroupTemplate) => {
  if (confirm(`Are you sure you want to delete the template "${template.name.fallback}"?`)) {
    questionnaireStore.deleteGroupTemplate(template.id)
    emit('deleteTemplate', template)

    if (selectedTemplate.value?.id === template.id) {
      selectedTemplate.value = null
    }
  }
}

const applyTemplate = (template: GroupTemplate) => {
  emit('applyTemplate', template)
}

const previewTemplate = (template: GroupTemplate) => {
  previewingTemplate.value = template
  showPreviewDialog.value = true
}

const addComponentToTemplate = (componentType: ComponentType) => {
  const newItem: QuestionnaireItem = {
    id: `item_${Date.now()}`,
    type: componentType,
    label: { fallback: componentRegistry[componentType]?.title.fallback || componentType },
    description: { fallback: componentRegistry[componentType]?.description.fallback || '' },
    required: false,
    validation: [],
    props: {}
  }

  templateForm.value.items.push(newItem)
  showAddComponentDialog.value = false
}

const duplicateItem = (item: QuestionnaireItem) => {
  const duplicated = {
    ...item,
    id: `${item.id}_copy_${Date.now()}`,
    label: item.label ? {
      ...item.label,
      fallback: `${item.label.fallback} (Copy)`
    } : { fallback: `${item.type} (Copy)` }
  }

  const index = templateForm.value.items.findIndex(i => i.id === item.id)
  templateForm.value.items.splice(index + 1, 0, duplicated)
}

const deleteItem = (item: QuestionnaireItem) => {
  const index = templateForm.value.items.findIndex(i => i.id === item.id)
  if (index > -1) {
    templateForm.value.items.splice(index, 1)
  }

  if (selectedItem.value?.id === item.id) {
    selectedItem.value = null
  }
}

const getComponentIcon = (type: ComponentType) => {
  return componentRegistry[type]?.icon || 'mdi-help'
}

const getComponentTitle = (type: ComponentType) => {
  return componentRegistry[type]?.title.fallback || type
}

const getPreviewComponent = (type: ComponentType) => {
  // For now, return a simple preview div
  // In a real implementation, you'd have preview components for each type
  return 'div'
}

const generatePreviewText = (items: QuestionnaireItem[]) => {
  if (items.length === 0) return 'Empty template'

  const types = [...new Set(items.map(item => item.type))]
  return types.slice(0, 3).join(', ') + (types.length > 3 ? '...' : '')
}

// Drag and drop handlers
const handleDragOver = (event: DragEvent) => {
  event.preventDefault()
  event.dataTransfer!.dropEffect = 'copy'
  isDragOver.value = true
}

const handleDragLeave = () => {
  isDragOver.value = false
}

const handleDrop = (event: DragEvent) => {
  event.preventDefault()
  isDragOver.value = false

  try {
    const data = JSON.parse(event.dataTransfer?.getData('application/json') || '{}')
    if (data.type === 'component' && data.componentType) {
      addComponentToTemplate(data.componentType)
    }
  } catch (error) {
    console.error('Invalid drop data:', error)
  }
}
</script>

<style scoped>
.templates-manager {
  min-height: 70vh;
  max-height: 80vh;
  overflow-y: auto;
}

.template-card {
  cursor: pointer;
  transition: all 0.2s;
  border: 2px solid transparent;
}

.template-card:hover {
  border-color: rgba(var(--v-theme-primary), 0.3);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.template-selected {
  border-color: rgba(var(--v-theme-primary), 1);
  background-color: rgba(var(--v-theme-primary), 0.05);
}

.template-info-card {
  height: fit-content;
  position: sticky;
  top: 0;
}

.template-builder {
  min-height: 500px;
}

.template-drop-zone {
  min-height: 400px;
  padding: 16px;
  border: 2px dashed rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 8px;
  position: relative;
  transition: all 0.3s ease;
}

.template-drop-zone.drop-zone-active {
  border-color: rgba(var(--v-theme-primary), 0.8);
  background-color: rgba(var(--v-theme-primary), 0.05);
}

.template-item {
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 4px;
  padding: 12px;
  margin-bottom: 8px;
  cursor: pointer;
  transition: all 0.2s;
  background-color: rgba(var(--v-theme-surface), 1);
}

.template-item:hover {
  border-color: rgba(var(--v-theme-primary), 0.5);
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.item-selected {
  border-color: rgba(var(--v-theme-primary), 1);
  background-color: rgba(var(--v-theme-primary), 0.05);
}

.item-preview {
  padding: 8px;
  background-color: rgba(var(--v-theme-surface-variant), 0.3);
  border-radius: 4px;
  pointer-events: none;
}

.empty-template-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  min-height: 200px;
  color: rgba(var(--v-theme-on-surface), 0.6);
}

.drop-zone-indicator {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  pointer-events: none;
}

.template-preview .preview-item {
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 4px;
  padding: 8px;
  background-color: rgba(var(--v-theme-surface-variant), 0.3);
}

.empty-state {
  background-color: rgba(var(--v-theme-surface-variant), 0.3);
  border-radius: 8px;
}
</style>