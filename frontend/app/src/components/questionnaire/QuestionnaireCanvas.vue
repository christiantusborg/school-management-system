<template>
  <v-card class="questionnaire-canvas">
    <v-card-title class="d-flex align-center justify-space-between">
      <div class="d-flex align-center">
        <v-icon icon="mdi-view-grid" class="mr-2" />
        Canvas
      </div>
      <div class="d-flex gap-2">
        <v-btn
          icon="mdi-plus"
          size="small"
          variant="tonal"
          @click="addPage"
        />
        <v-btn
          icon="mdi-content-save"
          size="small"
          variant="tonal"
          @click="saveQuestionnaire"
        />
      </div>
    </v-card-title>

    <v-card-text v-if="questionnaire">
      <!-- Questionnaire Header -->
      <div class="questionnaire-header mb-4">
        <v-text-field
          :model-value="questionnaire.name.fallback"
          label="Questionnaire Name"
          variant="outlined"
          density="compact"
          @update:model-value="updateQuestionnaireName"
        />
        <v-textarea
          :model-value="questionnaire.description?.fallback || ''"
          label="Description"
          variant="outlined"
          density="compact"
          rows="2"
          @update:model-value="updateQuestionnaireDescription"
        />
      </div>

      <!-- Pages -->
      <div class="pages-container">
        <div
          v-for="(page, pageIndex) in questionnaire.pages"
          :key="page.id"
          class="page-container mb-4"
        >
          <!-- Page Header -->
          <div class="page-header d-flex align-center justify-space-between mb-3">
            <div class="d-flex align-center flex-grow-1">
              <v-icon icon="mdi-file-document" class="mr-2" />
              <v-text-field
                :model-value="page.title.fallback"
                variant="outlined"
                density="compact"
                hide-details
                @update:model-value="(value) => updatePageTitle(pageIndex, value)"
              />
            </div>
            <div class="d-flex gap-1">
              <v-btn
                icon="mdi-plus"
                size="small"
                variant="text"
                @click="addSection(pageIndex)"
              />
              <v-btn
                icon="mdi-delete"
                size="small"
                variant="text"
                color="error"
                :disabled="questionnaire.pages.length === 1"
                @click="deletePage(pageIndex)"
              />
            </div>
          </div>

          <!-- Sections -->
          <div class="sections-container">
            <div
              v-for="(section, sectionIndex) in page.sections"
              :key="section.id"
              class="section-container mb-3"
            >
              <!-- Section Header -->
              <div class="section-header d-flex align-center justify-space-between mb-2">
                <v-text-field
                  :model-value="section.title?.fallback || ''"
                  label="Section Title"
                  variant="outlined"
                  density="compact"
                  hide-details
                  @update:model-value="(value) => updateSectionTitle(pageIndex, sectionIndex, value)"
                />
                <div class="d-flex gap-1">
                  <v-btn
                    icon="mdi-folder-plus"
                    size="small"
                    variant="text"
                    @click="addGroup(pageIndex, sectionIndex)"
                  />
                  <v-btn
                    icon="mdi-delete"
                    size="small"
                    variant="text"
                    color="error"
                    :disabled="page.sections.length === 1"
                    @click="deleteSection(pageIndex, sectionIndex)"
                  />
                </div>
              </div>

              <!-- Groups -->
              <div class="groups-container">
                <div
                  v-for="(group, groupIndex) in section.groups"
                  :key="group.id"
                  class="group-container mb-3"
                >
                  <!-- Group Header -->
                  <div class="group-header d-flex align-center justify-space-between mb-2">
                    <div class="d-flex align-center flex-grow-1">
                      <v-icon icon="mdi-folder" class="mr-2" size="16" />
                      <v-text-field
                        :model-value="group.title?.fallback || ''"
                        label="Group Title"
                        variant="outlined"
                        density="compact"
                        hide-details
                        @update:model-value="(value) => updateGroupTitle(pageIndex, sectionIndex, groupIndex, value)"
                      />
                      <v-chip v-if="group.templateId" size="small" color="success" class="ml-2">
                        Template: {{ group.templateId }}
                      </v-chip>
                    </div>
                    <div class="d-flex gap-1">
                      <v-btn
                        icon="mdi-repeat"
                        size="small"
                        variant="text"
                        :color="group.isRepeater ? 'info' : 'default'"
                        @click="toggleRepeater(pageIndex, sectionIndex, groupIndex)"
                      />
                      <v-btn
                        icon="mdi-content-duplicate"
                        size="small"
                        variant="text"
                        @click="duplicateGroup(group)"
                      />
                      <v-btn
                        icon="mdi-delete"
                        size="small"
                        variant="text"
                        color="error"
                        :disabled="section.groups.length === 1"
                        @click="deleteGroup(pageIndex, sectionIndex, groupIndex)"
                      />
                    </div>
                  </div>

                  <!-- Simple Repeater Indicator -->
                  <div v-if="group.isRepeater" class="repeater-indicator mb-2">
                    <v-icon icon="mdi-repeat" size="small" color="info" />
                  </div>

                  <!-- Drop Zone for Components -->
                  <div
                    class="drop-zone"
                    :class="{ 'drop-zone-active': isDragOver }"
                    @dragover.prevent="handleDragOver"
                    @dragleave="handleDragLeave"
                    @drop="handleDrop($event, pageIndex, sectionIndex, groupIndex)"
                  >
                    <!-- Items -->
                    <div
                      v-for="(item, itemIndex) in group.items"
                      :key="item.id"
                      class="item-container"
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
                          :mode="'builder'"
                        />
                      </div>
                    </div>

                    <!-- Add Component Button -->
                    <div class="add-component-section mt-2">
                      <v-btn
                        variant="outlined"
                        color="primary"
                        prepend-icon="mdi-plus"
                        @click="showAddComponentDialog(pageIndex, sectionIndex, groupIndex)"
                      >
                        Add Component
                      </v-btn>
                    </div>

                    <!-- Dedicated Drop Zone -->
                    <div
                      class="dedicated-drop-zone"
                      :class="{ 'drop-zone-active': isDragOver }"
                      @dragover.prevent="handleDragOver"
                      @dragleave="handleDragLeave"
                      @drop="handleDrop($event, pageIndex, sectionIndex, groupIndex)"
                    >
                      <div class="drop-zone-content">
                        <v-icon size="32" :color="isDragOver ? 'primary' : 'grey-lighten-1'" class="mb-2">
                          {{ isDragOver ? 'mdi-download-box' : 'mdi-download-box-outline' }}
                        </v-icon>
                        <div class="text-body-2" :class="isDragOver ? 'text-primary' : 'text-medium-emphasis'">
                          {{ isDragOver ? 'Drop component here' : 'Or drag & drop components here' }}
                        </div>
                      </div>
                    </div>

                    <!-- Empty State (when no items) -->
                    <div v-if="group.items.length === 0" class="empty-state-message">
                      <div class="text-caption text-medium-emphasis">
                        This group is empty. Add components using the button or drag & drop above.
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Add Group Button -->
                <div class="add-group-section mt-2">
                  <v-menu>
                    <template v-slot:activator="{ props }">
                      <v-btn
                        variant="outlined"
                        color="secondary"
                        prepend-icon="mdi-folder-plus"
                        v-bind="props"
                      >
                        Add Group
                      </v-btn>
                    </template>
                    <v-list>
                      <v-list-item @click="addGroup(pageIndex, sectionIndex)">
                        <template v-slot:prepend>
                          <v-icon icon="mdi-folder-plus-outline" />
                        </template>
                        <v-list-item-title>Empty Group</v-list-item-title>
                        <v-list-item-subtitle>Start with an empty group</v-list-item-subtitle>
                      </v-list-item>

                      <!-- System Templates -->
                      <v-divider />
                      <v-list-subheader class="d-flex align-center">
                        <v-icon icon="mdi-cog" size="16" class="mr-1" />
                        System Templates
                      </v-list-subheader>
                      <v-list-item
                        v-for="template in systemGroupTemplates"
                        :key="template.id"
                        @click="addGroupFromTemplate(pageIndex, sectionIndex, template)"
                      >
                        <template v-slot:prepend>
                          <v-icon :icon="template.icon" />
                        </template>
                        <v-list-item-title class="d-flex align-center">
                          {{ template.name.fallback }}
                          <v-chip size="x-small" color="blue" variant="outlined" class="ml-2">
                            System
                          </v-chip>
                        </v-list-item-title>
                        <v-list-item-subtitle>{{ template.previewText }}</v-list-item-subtitle>
                      </v-list-item>

                      <!-- User Templates -->
                      <v-divider v-if="userGroupTemplates.length > 0" />
                      <v-list-subheader v-if="userGroupTemplates.length > 0" class="d-flex align-center">
                        <v-icon icon="mdi-account" size="16" class="mr-1" />
                        My Templates
                      </v-list-subheader>
                      <v-list-item
                        v-for="template in userGroupTemplates"
                        :key="template.id"
                        @click="addGroupFromTemplate(pageIndex, sectionIndex, template)"
                      >
                        <template v-slot:prepend>
                          <v-icon :icon="template.icon" />
                        </template>
                        <v-list-item-title>{{ template.name.fallback }}</v-list-item-title>
                        <v-list-item-subtitle>{{ template.previewText }}</v-list-item-subtitle>
                      </v-list-item>
                    </v-list>
                  </v-menu>
                </div>

                <!-- Empty State (when no groups) -->
                <div v-if="section.groups.length === 0" class="empty-state-message">
                  <div class="text-caption text-medium-emphasis">
                    This section is empty. Add groups using the button above.
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </v-card-text>

    <!-- Empty State -->
    <v-card-text v-else class="text-center py-12">
      <v-icon size="64" color="grey-lighten-1" class="mb-4">
        mdi-form-select
      </v-icon>
      <h3 class="text-h6 mb-2">No questionnaire selected</h3>
      <p class="text-body-2 text-medium-emphasis">
        Create a new questionnaire to start building
      </p>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
// -nocheck — ported SysCase file; TS strict cleanup is a follow-up
import { ref, computed } from 'vue'
import { componentRegistry, createGroupFromTemplate } from '@/utils/questionnaire/componentRegistry'
import { useQuestionnaireStore } from '@/stores/questionnaire'
import FormField from './FormField.vue'
import type { Questionnaire, QuestionnaireItem, ComponentType, QuestionnaireGroup, GroupTemplate } from '@quvian/shared/types/questionnaire'

// Props
const props = defineProps<{
  questionnaire: Questionnaire | null
  selectedItem: QuestionnaireItem | null
}>()

// Emits
const emit = defineEmits<{
  selectItem: [item: QuestionnaireItem | null]
  updateQuestionnaire: [questionnaire: Questionnaire]
  duplicateItem: [item: QuestionnaireItem]
  deleteItem: [item: QuestionnaireItem]
  addComponent: [pageIndex: number, sectionIndex: number, groupIndex?: number]
  addComponentDrop: [componentType: ComponentType, position: { pageIndex: number; sectionIndex: number; groupIndex?: number }]
  duplicateGroup: [group: any]
}>()

// Store
const questionnaireStore = useQuestionnaireStore()

// Reactive state
const isDragOver = ref(false)
const dragEnterCount = ref(0)

// Computed
const availableGroupTemplates = computed(() => questionnaireStore.allGroupTemplates)
const systemGroupTemplates = computed(() => availableGroupTemplates.value.filter(t => t.isSystem))
const userGroupTemplates = computed(() => availableGroupTemplates.value.filter(t => !t.isSystem))

// Methods
const selectItem = (item: QuestionnaireItem) => {
  emit('selectItem', item)
}

const updateQuestionnaireName = (name: string) => {
  if (!props.questionnaire) return

  const updated = {
    ...props.questionnaire,
    name: { ...props.questionnaire.name, fallback: name }
  }
  emit('updateQuestionnaire', updated)
}

const updateQuestionnaireDescription = (description: string) => {
  if (!props.questionnaire) return

  const updated = {
    ...props.questionnaire,
    description: { fallback: description }
  }
  emit('updateQuestionnaire', updated)
}

const updatePageTitle = (pageIndex: number, title: string) => {
  if (!props.questionnaire) return

  const updated = { ...props.questionnaire }
  updated.pages[pageIndex].title.fallback = title
  emit('updateQuestionnaire', updated)
}

const updateSectionTitle = (pageIndex: number, sectionIndex: number, title: string) => {
  if (!props.questionnaire) return

  const updated = { ...props.questionnaire }
  if (!updated.pages[pageIndex].sections[sectionIndex].title) {
    updated.pages[pageIndex].sections[sectionIndex].title = { fallback: title }
  } else {
    updated.pages[pageIndex].sections[sectionIndex].title!.fallback = title
  }
  emit('updateQuestionnaire', updated)
}

const addPage = () => {
  if (!props.questionnaire) return

  const newPage = {
    id: `page_${Date.now()}`,
    title: { fallback: `Page ${props.questionnaire.pages.length + 1}` },
    sections: [
      {
        id: `section_${Date.now()}`,
        title: { fallback: 'Section' },
        groups: []
      }
    ]
  }

  const updated = {
    ...props.questionnaire,
    pages: [...props.questionnaire.pages, newPage]
  }
  emit('updateQuestionnaire', updated)
}

const addSection = (pageIndex: number) => {
  if (!props.questionnaire) return

  const updated = { ...props.questionnaire }
  const newSection = {
    id: `section_${Date.now()}`,
    title: { fallback: 'New Section' },
    groups: [
      {
        id: `group_${Date.now()}`,
        title: { fallback: 'Default Group' },
        items: []
      }
    ]
  }

  updated.pages[pageIndex].sections.push(newSection)
  emit('updateQuestionnaire', updated)
}

const deletePage = (pageIndex: number) => {
  if (!props.questionnaire || props.questionnaire.pages.length === 1) return

  const updated = {
    ...props.questionnaire,
    pages: props.questionnaire.pages.filter((_, index) => index !== pageIndex)
  }
  emit('updateQuestionnaire', updated)
}

const deleteSection = (pageIndex: number, sectionIndex: number) => {
  if (!props.questionnaire) return

  const page = props.questionnaire.pages[pageIndex]
  if (page.sections.length === 1) return

  const updated = { ...props.questionnaire }
  updated.pages[pageIndex].sections = page.sections.filter((_, index) => index !== sectionIndex)
  emit('updateQuestionnaire', updated)
}

// Group Management Methods
const addGroup = (pageIndex: number, sectionIndex: number) => {
  if (!props.questionnaire) return

  const updated = { ...props.questionnaire }
  const newGroup = {
    id: `group_${Date.now()}`,
    title: { fallback: 'New Group' },
    items: []
  }

  updated.pages[pageIndex].sections[sectionIndex].groups.push(newGroup)
  emit('updateQuestionnaire', updated)
}

const addGroupFromTemplate = (pageIndex: number, sectionIndex: number, template: GroupTemplate) => {
  if (!props.questionnaire) return

  const updated = { ...props.questionnaire }
  const newGroup = createGroupFromTemplate(template)

  updated.pages[pageIndex].sections[sectionIndex].groups.push(newGroup)
  emit('updateQuestionnaire', updated)
}

const updateGroupTitle = (pageIndex: number, sectionIndex: number, groupIndex: number, title: string) => {
  if (!props.questionnaire) return

  const updated = { ...props.questionnaire }
  if (!updated.pages[pageIndex].sections[sectionIndex].groups[groupIndex].title) {
    updated.pages[pageIndex].sections[sectionIndex].groups[groupIndex].title = { fallback: title }
  } else {
    updated.pages[pageIndex].sections[sectionIndex].groups[groupIndex].title!.fallback = title
  }
  emit('updateQuestionnaire', updated)
}

const deleteGroup = (pageIndex: number, sectionIndex: number, groupIndex: number) => {
  if (!props.questionnaire) return

  const section = props.questionnaire.pages[pageIndex].sections[sectionIndex]
  if (section.groups.length === 1) return

  const updated = { ...props.questionnaire }
  updated.pages[pageIndex].sections[sectionIndex].groups = section.groups.filter((_, index) => index !== groupIndex)
  emit('updateQuestionnaire', updated)
}

const toggleRepeater = (pageIndex: number, sectionIndex: number, groupIndex: number) => {
  if (!props.questionnaire) return

  const updated = { ...props.questionnaire }
  const group = updated.pages[pageIndex].sections[sectionIndex].groups[groupIndex]

  group.isRepeater = !group.isRepeater

  if (group.isRepeater && !group.repeaterConfig) {
    group.repeaterConfig = {
      minInstances: 1,
      maxInstances: 10,
      addButtonText: { fallback: 'Add Item' },
      removeButtonText: { fallback: 'Remove' },
      instanceTitleTemplate: `${group.title?.fallback || 'Item'} #`
    }
  }

  emit('updateQuestionnaire', updated)
}

const getInstanceCount = (group: any) => {
  // Count the base group as 1 plus any additional instances
  return 1 + (group.repeaterInstances?.length || 0)
}

const addRepeaterInstance = (pageIndex: number, sectionIndex: number, groupIndex: number) => {
  // This method is no longer used in the canvas, but kept for compatibility
  // The canvas now just shows info about the repeater
  console.log('Add repeater instance called in canvas - this should be handled in the renderer')
}


const duplicateGroup = (group: QuestionnaireGroup) => {
  emit('duplicateGroup', group)
}

const duplicateItem = (item: QuestionnaireItem) => {
  emit('duplicateItem', item)
}

const deleteItem = (item: QuestionnaireItem) => {
  emit('deleteItem', item)
}

const getComponentIcon = (type: ComponentType) => {
  return componentRegistry[type]?.icon || 'mdi-help'
}

const getComponentTitle = (type: ComponentType) => {
  return componentRegistry[type]?.title.fallback || type
}

const getPreviewComponent = (type: ComponentType) => {
  // For richtext, use FormField to show the actual editor
  if (type === 'richtext') {
    return FormField
  }
  // For now, return a simple preview div for other types
  // In a real implementation, you'd have preview components for each type
  return 'div'
}

const showAddComponentDialog = (pageIndex: number, sectionIndex: number, groupIndex?: number) => {
  emit('addComponent', pageIndex, sectionIndex, groupIndex)
}

const handleDragOver = (event: DragEvent) => {
  event.preventDefault()
  event.dataTransfer!.dropEffect = 'copy'
  if (!isDragOver.value) {
    isDragOver.value = true
    dragEnterCount.value = 1
  } else {
    dragEnterCount.value++
  }
}

const handleDragLeave = (event: DragEvent) => {
  dragEnterCount.value--
  if (dragEnterCount.value <= 0) {
    isDragOver.value = false
    dragEnterCount.value = 0
  }
}

const handleDrop = (event: DragEvent, pageIndex: number, sectionIndex: number, groupIndex?: number) => {
  event.preventDefault()
  event.stopPropagation()
  isDragOver.value = false

  try {
    const data = JSON.parse(event.dataTransfer?.getData('application/json') || '{}')
    if (data.type === 'component' && data.componentType) {
      const position = { pageIndex, sectionIndex, groupIndex }
      emit('addComponentDrop', data.componentType, position)
    }
  } catch (error) {
    console.error('Invalid drop data:', error)
  }
}

const saveQuestionnaire = () => {
  // Emit save event or handle saving logic
  console.log('Save questionnaire')
}
</script>

<style scoped>
.questionnaire-canvas {
  min-height: 70vh;
  max-height: 80vh;
  overflow-y: auto;
}

.page-container {
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 8px;
  padding: 16px;
  background-color: rgba(var(--v-theme-surface-variant), 0.3);
}

.section-container {
  border: 1px dashed rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 4px;
  padding: 12px;
  background-color: rgba(var(--v-theme-surface), 1);
}

.group-container {
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 4px;
  padding: 10px;
  margin-bottom: 8px;
  background-color: rgba(var(--v-theme-surface-variant), 0.5);
}

.group-header {
  background-color: rgba(var(--v-theme-primary), 0.05);
  border-radius: 4px;
  padding: 8px;
  margin-bottom: 8px;
}

.repeater-indicator {
  display: flex;
  justify-content: flex-end;
  padding: 4px;
}

.add-group-section {
  display: flex;
  justify-content: center;
  padding: 16px 16px 8px 16px;
}

.drop-zone {
  min-height: 60px;
  padding: 8px;
  border-radius: 4px;
  transition: background-color 0.2s;
}

.drop-zone-active {
  background-color: rgba(var(--v-theme-primary), 0.1);
  border: 2px dashed rgba(var(--v-theme-primary), 0.5);
}

.add-component-section {
  display: flex;
  justify-content: center;
  padding: 16px 16px 8px 16px;
}

.dedicated-drop-zone {
  margin: 8px 16px 16px 16px;
  border: 2px dashed rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 8px;
  padding: 20px;
  transition: all 0.3s ease;
  background-color: rgba(var(--v-theme-surface-variant), 0.3);
}

.dedicated-drop-zone:hover {
  border-color: rgba(var(--v-theme-primary), 0.5);
  background-color: rgba(var(--v-theme-primary), 0.02);
}

.dedicated-drop-zone.drop-zone-active {
  border-color: rgba(var(--v-theme-primary), 0.8);
  background-color: rgba(var(--v-theme-primary), 0.08);
  transform: scale(1.02);
  box-shadow: 0 4px 12px rgba(var(--v-theme-primary), 0.15);
}

.drop-zone-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  min-height: 80px;
}

.empty-state-message {
  text-align: center;
  padding: 16px;
  margin-top: 8px;
}

.item-container {
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 4px;
  padding: 12px;
  margin-bottom: 8px;
  cursor: pointer;
  transition: all 0.2s;
  background-color: rgba(var(--v-theme-surface), 1);
}

.item-container:hover {
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
</style>