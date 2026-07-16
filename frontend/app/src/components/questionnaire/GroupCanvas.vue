<template>
  <v-card class="group-canvas">
    <v-card-title class="d-flex align-center justify-space-between">
      <div class="d-flex align-center">
        <v-icon icon="mdi-puzzle" class="mr-2" />
        {{ currentGroup?.name?.fallback || 'New Group' }}
      </div>
      <div class="d-flex gap-2">
        <v-btn
          icon="mdi-content-save"
          size="small"
          variant="tonal"
          @click="saveGroup"
          :disabled="!groupForm.name"
        />
        <v-btn
          icon="mdi-close"
          size="small"
          variant="text"
          @click="cancelEdit"
        />
      </div>
    </v-card-title>

    <v-card-text>
      <!-- Group Information -->
      <div class="group-header mb-4">
        <v-text-field
          v-model="groupForm.name"
          label="Group Name"
          variant="outlined"
          density="compact"
          :rules="[rules.required]"
          class="mb-2"
        />
        <v-textarea
          v-model="groupForm.description"
          label="Description"
          variant="outlined"
          density="compact"
          rows="2"
          class="mb-2"
        />
        <v-row>
          <v-col cols="6">
            <v-text-field
              v-model="groupForm.icon"
              label="Icon (MDI name)"
              variant="outlined"
              density="compact"
              placeholder="mdi-text-box"
            />
          </v-col>
          <v-col cols="6">
            <v-select
              v-model="groupForm.category"
              label="Category"
              variant="outlined"
              density="compact"
              :items="categoryOptions"
              item-title="text"
              item-value="value"
            />
          </v-col>
        </v-row>
      </div>

      <!-- Group Fields Builder -->
      <div class="group-fields">
        <div class="d-flex align-center justify-space-between mb-3">
          <h3 class="text-h6">Group Fields</h3>
          <v-chip size="small" color="primary" variant="outlined">
            {{ groupForm.items.length }} fields
          </v-chip>
        </div>

        <div
          class="drop-zone"
          :class="{ 'drop-zone-active': isDragOver }"
          @dragover.prevent="handleDragOver"
          @dragleave="handleDragLeave"
          @drop="handleDrop"
        >
          <!-- Group Items -->
          <div
            v-for="(item, itemIndex) in groupForm.items"
            :key="item.id"
            class="group-item-container"
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
                  icon="mdi-arrow-up"
                  size="x-small"
                  variant="text"
                  :disabled="itemIndex === 0"
                  @click.stop="moveItem(itemIndex, itemIndex - 1)"
                />
                <v-btn
                  icon="mdi-arrow-down"
                  size="x-small"
                  variant="text"
                  :disabled="itemIndex === groupForm.items.length - 1"
                  @click.stop="moveItem(itemIndex, itemIndex + 1)"
                />
                <v-btn
                  icon="mdi-content-duplicate"
                  size="x-small"
                  variant="text"
                  @click.stop="duplicateItem(itemIndex)"
                />
                <v-btn
                  icon="mdi-delete"
                  size="x-small"
                  variant="text"
                  color="error"
                  @click.stop="deleteItem(itemIndex)"
                />
              </div>
            </div>

            <!-- Item Preview -->
            <div class="item-preview mt-2">
              <div class="text-caption text-medium-emphasis">
                Type: {{ item.type }} | Required: {{ item.required ? 'Yes' : 'No' }}
              </div>
            </div>
          </div>

          <!-- Empty State -->
          <div v-if="groupForm.items.length === 0" class="empty-group-state">
            <v-icon size="48" color="grey-lighten-1" class="mb-2">
              mdi-package-variant-closed
            </v-icon>
            <div class="text-body-2 text-medium-emphasis mb-2">
              {{ isDragOver ? 'Drop component here' : 'Drag components here to build your group' }}
            </div>
            <div class="text-caption text-medium-emphasis">
              Use the component palette on the left to add fields
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
      </div>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
// -nocheck — ported SysCase file; TS strict cleanup is a follow-up
import { ref, computed, watch } from 'vue'
import { useCustomComponentsStore } from '@/stores/customComponents'
import { componentRegistry } from '@/utils/questionnaire/componentRegistry'
import type { ComponentType, QuestionnaireItem, GroupTemplate } from '@quvian/shared/types/questionnaire'

// Props
const props = defineProps<{
  currentGroup?: GroupTemplate | null
  selectedItem?: QuestionnaireItem | null
}>()

// Emits
const emit = defineEmits<{
  selectItem: [item: QuestionnaireItem | null]
  saveGroup: [group: any]
  cancelEdit: []
}>()

// Store
const customComponentsStore = useCustomComponentsStore()

// Reactive state
const isDragOver = ref(false)

// Group form
const groupForm = ref({
  name: '',
  description: '',
  icon: 'mdi-text-box',
  category: 'custom',
  items: [] as QuestionnaireItem[]
})

// Category options
const categoryOptions = [
  { text: 'Contact', value: 'contact' },
  { text: 'Address', value: 'address' },
  { text: 'Employment', value: 'employment' },
  { text: 'Education', value: 'education' },
  { text: 'Medical', value: 'medical' },
  { text: 'Payment', value: 'payment' },
  { text: 'Custom', value: 'custom' }
]

// Validation rules
const rules = {
  required: (value: string) => !!value || 'This field is required'
}

// Watch for current group changes
watch(() => props.currentGroup, (newGroup) => {
  if (newGroup) {
    groupForm.value = {
      name: newGroup.name.fallback,
      description: newGroup.description?.fallback || '',
      icon: newGroup.icon,
      category: newGroup.category || 'custom',
      items: [...(newGroup.items || [])]
    }
  } else {
    // New group
    groupForm.value = {
      name: '',
      description: '',
      icon: 'mdi-text-box',
      category: 'custom',
      items: []
    }
  }
}, { immediate: true })

// Methods
const selectItem = (item: QuestionnaireItem) => {
  emit('selectItem', item)
}

const saveGroup = () => {
  const groupData = {
    id: props.currentGroup?.id,
    name: { fallback: groupForm.value.name },
    description: { fallback: groupForm.value.description },
    icon: groupForm.value.icon,
    category: groupForm.value.category,
    items: [...groupForm.value.items],
    previewText: `${groupForm.value.items.length} fields`,
    isSystem: false,
    createdAt: props.currentGroup?.createdAt || new Date().toISOString(),
    updatedAt: new Date().toISOString()
  }

  emit('saveGroup', groupData)
}

const cancelEdit = () => {
  emit('cancelEdit')
}

const moveItem = (fromIndex: number, toIndex: number) => {
  const items = [...groupForm.value.items]
  const [movedItem] = items.splice(fromIndex, 1)
  items.splice(toIndex, 0, movedItem)
  groupForm.value.items = items
}

const duplicateItem = (index: number) => {
  const item = groupForm.value.items[index]
  const duplicated: QuestionnaireItem = {
    ...item,
    id: `item_${Date.now()}`,
    label: item.label ? {
      ...item.label,
      fallback: `${item.label.fallback} (Copy)`
    } : { fallback: `${item.type} (Copy)` }
  }
  groupForm.value.items.splice(index + 1, 0, duplicated)
}

const deleteItem = (index: number) => {
  groupForm.value.items.splice(index, 1)
  emit('selectItem', null)
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
      const component = componentRegistry[data.componentType]
      if (component) {
        const newItem: QuestionnaireItem = {
          id: `item_${Date.now()}`,
          type: data.componentType,
          props: { ...component.defaultProps },
          label: { fallback: component.title.fallback },
          description: component.description.fallback ? { fallback: component.description.fallback } : undefined,
          validation: [],
          required: false
        }

        groupForm.value.items.push(newItem)
        emit('selectItem', newItem)
      }
    }
  } catch (error) {
    console.error('Invalid drop data:', error)
  }
}

const getComponentIcon = (type: ComponentType) => {
  return componentRegistry[type]?.icon || 'mdi-help'
}

const getComponentTitle = (type: ComponentType) => {
  return componentRegistry[type]?.title.fallback || type
}
</script>

<style scoped>
.group-canvas {
  height: fit-content;
  max-height: 80vh;
  overflow-y: auto;
}

.drop-zone {
  min-height: 300px;
  padding: 16px;
  border: 2px dashed rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 8px;
  position: relative;
  transition: all 0.3s ease;
}

.drop-zone-active {
  border-color: rgba(var(--v-theme-primary), 0.8);
  background-color: rgba(var(--v-theme-primary), 0.05);
}

.group-item-container {
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 4px;
  padding: 12px;
  margin-bottom: 8px;
  cursor: pointer;
  transition: all 0.2s;
  background-color: rgba(var(--v-theme-surface), 1);
}

.group-item-container:hover {
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

.empty-group-state {
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
</style>