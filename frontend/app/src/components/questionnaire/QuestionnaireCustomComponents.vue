<template>
  <div class="groups-list">
    <!-- Header -->
    <div class="d-flex align-center mb-4">
      <v-icon icon="mdi-folder-multiple" class="mr-2" />
      <h2 class="text-h6 font-weight-medium">My Groups</h2>
      <v-spacer />
      <v-btn
        color="primary"
        prepend-icon="mdi-plus"
        @click="createNewGroup"
      >
        ADD NEW GROUP
      </v-btn>
    </div>

    <!-- Search -->
    <v-text-field
      v-model="searchQuery"
      label="Search groups..."
      prepend-inner-icon="mdi-magnify"
      variant="outlined"
      density="compact"
      clearable
      class="mb-4"
    />

    <!-- System Groups Section -->
    <div v-if="filteredSystemGroups.length > 0" class="mb-6">
      <h3 class="text-subtitle-1 text-medium-emphasis mb-3">
        System Templates
      </h3>

      <div class="groups-grid">
        <v-card
          v-for="group in filteredSystemGroups"
          :key="group.id"
          class="group-card"
          @click="previewGroup(group)"
        >
          <div class="d-flex pa-4">
            <v-icon
              :icon="group.icon"
              size="20"
              class="mr-3 mt-1"
              :color="group.category === 'medical' ? 'red' : 'blue'"
            />

            <div class="flex-grow-1">
              <div class="d-flex align-center mb-1">
                <h4 class="text-subtitle-1 font-weight-medium mr-2">
                  {{ group.name.fallback }}
                </h4>
                <v-chip size="x-small" color="blue" variant="outlined">
                  System
                </v-chip>
              </div>

              <p class="text-body-2 text-medium-emphasis mb-2">
                {{ group.description?.fallback || 'No description' }}
              </p>

              <div class="d-flex align-center text-caption text-medium-emphasis mb-3">
                <v-icon icon="mdi-puzzle" size="12" class="mr-1" />
                {{ group.previewText }}
              </div>

              <div class="d-flex gap-2">
                <v-btn
                  size="small"
                  variant="outlined"
                  prepend-icon="mdi-content-copy"
                  @click.stop="cloneSystemGroup(group)"
                >
                  CLONE
                </v-btn>
              </div>
            </div>

            <v-menu>
              <template #activator="{ props }">
                <v-btn
                  icon="mdi-dots-vertical"
                  variant="text"
                  size="small"
                  v-bind="props"
                  @click.stop
                />
              </template>
              <v-list density="compact">
                <v-list-item @click="cloneSystemGroup(group)">
                  <template #prepend>
                    <v-icon icon="mdi-content-copy" />
                  </template>
                  <v-list-item-title>Clone</v-list-item-title>
                </v-list-item>
              </v-list>
            </v-menu>
          </div>
        </v-card>
      </div>
    </div>

    <!-- User Groups Section -->
    <div>
      <h3 class="text-subtitle-1 text-medium-emphasis mb-3">
        My Custom Groups
      </h3>

      <div v-if="filteredUserGroups.length > 0" class="groups-grid">
        <v-card
          v-for="group in filteredUserGroups"
          :key="group.id"
          class="group-card"
          @click="editGroup(group)"
        >
          <div class="d-flex pa-4">
            <v-icon
              :icon="group.icon"
              size="20"
              class="mr-3 mt-1"
              color="primary"
            />

            <div class="flex-grow-1">
              <div class="d-flex align-center mb-1">
                <h4 class="text-subtitle-1 font-weight-medium mr-2">
                  {{ group.name.fallback }}
                </h4>
                <v-chip size="x-small" color="primary" variant="outlined">
                  Custom
                </v-chip>
              </div>

              <p class="text-body-2 text-medium-emphasis mb-2">
                {{ group.description?.fallback || 'No description' }}
              </p>

              <div class="d-flex align-center text-caption text-medium-emphasis mb-2">
                <v-icon icon="mdi-puzzle" size="12" class="mr-1" />
                {{ group.previewText }}
              </div>

              <div class="d-flex align-center text-caption text-medium-emphasis mb-3">
                <v-icon icon="mdi-calendar" size="12" class="mr-1" />
                Created: {{ formatDate(group.createdAt) }}
                <span class="mx-1">•</span>
                <v-icon icon="mdi-update" size="12" class="mr-1" />
                Updated: {{ formatDate(group.updatedAt) }}
              </div>

              <div class="d-flex gap-2">
                <v-btn
                  size="small"
                  variant="outlined"
                  prepend-icon="mdi-pencil"
                  @click.stop="editGroup(group)"
                >
                  EDIT
                </v-btn>
              </div>
            </div>

            <v-menu>
              <template #activator="{ props }">
                <v-btn
                  icon="mdi-dots-vertical"
                  variant="text"
                  size="small"
                  v-bind="props"
                  @click.stop
                />
              </template>
              <v-list density="compact">
                <v-list-item @click="editGroup(group)">
                  <template #prepend>
                    <v-icon icon="mdi-pencil" />
                  </template>
                  <v-list-item-title>Edit</v-list-item-title>
                </v-list-item>
                <v-list-item @click="cloneGroup(group)">
                  <template #prepend>
                    <v-icon icon="mdi-content-copy" />
                  </template>
                  <v-list-item-title>Clone</v-list-item-title>
                </v-list-item>
                <v-divider />
                <v-list-item @click="deleteGroup(group)" class="text-error">
                  <template #prepend>
                    <v-icon icon="mdi-delete" color="error" />
                  </template>
                  <v-list-item-title>Delete</v-list-item-title>
                </v-list-item>
              </v-list>
            </v-menu>
          </div>
        </v-card>
      </div>

      <!-- Empty State for User Groups -->
      <div v-else class="text-center py-12">
        <v-icon size="64" color="grey-lighten-1" class="mb-4">
          mdi-folder-plus
        </v-icon>
        <h3 class="text-h6 mb-2">No custom groups yet</h3>
        <p class="text-body-2 text-medium-emphasis mb-4">
          Create your first group or clone a system template
        </p>
        <v-btn
          color="primary"
          prepend-icon="mdi-plus"
          @click="createNewGroup"
        >
          ADD NEW GROUP
        </v-btn>
      </div>
    </div>

    <!-- Group Preview Dialog -->
    <v-dialog v-model="showPreviewDialog" max-width="600">
      <v-card v-if="previewingGroup">
        <v-card-title class="d-flex align-center">
          {{ previewingGroup.name.fallback }}
          <v-chip v-if="previewingGroup.isSystem" size="small" color="blue" variant="outlined" class="ml-2">
            System
          </v-chip>
        </v-card-title>
        <v-card-text>
          <p class="text-body-2 text-medium-emphasis mb-4">
            {{ previewingGroup.description?.fallback || 'No description' }}
          </p>

          <!-- Preview of group fields -->
          <div class="group-preview">
            <div
              v-for="item in previewingGroup.items"
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
            </div>
          </div>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showPreviewDialog = false">
            Close
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup lang="ts">
// -nocheck — ported SysCase file; TS strict cleanup is a follow-up
import { ref, computed } from 'vue'
import { useCustomComponentsStore } from '@/stores/customComponents'
import { componentRegistry, groupTemplates } from '@/utils/questionnaire/componentRegistry'
import type { ComponentType, QuestionnaireItem, GroupTemplate } from '@quvian/shared/types/questionnaire'

// Emits
const emit = defineEmits<{
  createGroup: [group: any]
  editGroup: [group: any]
  deleteGroup: [group: any]
  dragGroup: [group: any]
}>()

// Store
const customComponentsStore = useCustomComponentsStore()

// Reactive state
const searchQuery = ref('')
const showPreviewDialog = ref(false)
const previewingGroup = ref<GroupTemplate | null>(null)

// System and user groups
const systemGroups = computed(() => groupTemplates.filter(t => t.isSystem))
const userGroups = computed(() => customComponentsStore.allCustomComponents)

// Filtered groups based on search
const filteredSystemGroups = computed(() => {
  if (!searchQuery.value) return systemGroups.value
  const query = searchQuery.value.toLowerCase()
  return systemGroups.value.filter(group =>
    group.name.fallback.toLowerCase().includes(query) ||
    group.description?.fallback?.toLowerCase().includes(query) ||
    group.category?.toLowerCase().includes(query)
  )
})

const filteredUserGroups = computed(() => {
  if (!searchQuery.value) return userGroups.value
  const query = searchQuery.value.toLowerCase()
  return userGroups.value.filter(group =>
    group.name.fallback.toLowerCase().includes(query) ||
    group.description?.fallback?.toLowerCase().includes(query) ||
    group.category?.toLowerCase().includes(query)
  )
})

// Methods
const createNewGroup = () => {
  // Emit to parent to handle group creation with 3-column builder
  emit('createGroup', null)
}

const editGroup = (group: GroupTemplate) => {
  // Emit to parent to handle group editing with 3-column builder
  emit('editGroup', group)
}

const cloneSystemGroup = (systemGroup: GroupTemplate) => {
  const clonedData = {
    name: { fallback: `${systemGroup.name.fallback} (Copy)` },
    description: { fallback: systemGroup.description?.fallback || '' },
    icon: systemGroup.icon,
    items: [...(systemGroup.items || [])],
    previewText: systemGroup.previewText,
    isSystem: false
  }

  const newGroup = customComponentsStore.addCustomComponent(clonedData)
  emit('createGroup', newGroup)
}

const cloneGroup = (group: GroupTemplate) => {
  const duplicated = customComponentsStore.duplicateCustomComponent(group.id)
  if (duplicated) {
    emit('createGroup', duplicated)
  }
}

const deleteGroup = (group: GroupTemplate) => {
  if (confirm(`Are you sure you want to delete the group "${group.name.fallback}"?`)) {
    customComponentsStore.deleteCustomComponent(group.id)
    emit('deleteGroup', group)
  }
}

const previewGroup = (group: GroupTemplate) => {
  previewingGroup.value = group
  showPreviewDialog.value = true
}

const formatDate = (dateString?: string) => {
  if (!dateString) return 'N/A'
  return new Date(dateString).toLocaleDateString()
}

const getComponentIcon = (type: ComponentType) => {
  return componentRegistry[type]?.icon || 'mdi-help'
}

const getComponentTitle = (type: ComponentType) => {
  return componentRegistry[type]?.title.fallback || type
}
</script>

<style scoped>
.groups-list {
  max-width: 1200px;
  margin: 0 auto;
}

.groups-grid {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.group-card {
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.group-card:hover {
  border-color: rgba(var(--v-theme-primary), 0.5);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.group-card .d-flex {
  align-items: flex-start;
}

.selected {
  border-color: rgba(var(--v-theme-primary), 1);
  background-color: rgba(var(--v-theme-primary), 0.02);
}

.group-preview .preview-item {
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 4px;
  padding: 8px;
  background-color: rgba(var(--v-theme-surface-variant), 0.3);
}

@media (max-width: 768px) {
  .groups-list {
    padding: 16px;
  }

  .group-card .d-flex {
    flex-direction: column;
    align-items: stretch;
  }

  .group-card .d-flex > .flex-grow-1 {
    margin-left: 0 !important;
    margin-top: 8px;
  }
}
</style>