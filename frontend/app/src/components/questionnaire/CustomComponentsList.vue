<template>
  <v-card class="components-list">
    <v-card-title class="d-flex align-center justify-space-between">
      <div class="d-flex align-center">
        <v-icon icon="mdi-puzzle" class="mr-2" />
        My Components
      </div>
      <v-btn
        color="primary"
        prepend-icon="mdi-plus"
        @click="$emit('createComponent')"
      >
        Add New Component
      </v-btn>
    </v-card-title>

    <v-card-text>
      <!-- Search Bar -->
      <v-text-field
        v-model="searchQuery"
        label="Search components..."
        prepend-inner-icon="mdi-magnify"
        variant="outlined"
        density="compact"
        clearable
        class="mb-4"
        hint="Search by name or description"
      />

      <!-- Components Grid -->
      <div v-if="filteredComponents.length === 0 && searchQuery" class="empty-search text-center py-8">
        <v-icon size="64" color="grey-lighten-1" class="mb-4">
          mdi-magnify
        </v-icon>
        <h3 class="text-h6 mb-2">No components found</h3>
        <p class="text-body-2 text-medium-emphasis">
          Try adjusting your search terms or create a new component
        </p>
      </div>

      <div v-else-if="components.length === 0" class="empty-state text-center py-8">
        <v-icon size="64" color="grey-lighten-1" class="mb-4">
          mdi-puzzle-plus-outline
        </v-icon>
        <h3 class="text-h6 mb-2">No components yet</h3>
        <p class="text-body-2 text-medium-emphasis mb-4">
          Create your first custom component to get started
        </p>
        <v-btn
          color="primary"
          prepend-icon="mdi-plus"
          @click="$emit('createComponent')"
        >
          Create Your First Component
        </v-btn>
      </div>

      <v-row v-else>
        <v-col
          v-for="component in filteredComponents"
          :key="component.id"
          cols="12"
          md="6"
          lg="4"
        >
          <v-card
            class="component-card"
            :class="{ 'component-active': currentComponent?.id === component.id }"
            @click="selectComponent(component)"
          >
            <v-card-title class="d-flex align-center justify-space-between">
              <div class="d-flex align-center">
                <v-icon :icon="component.icon" class="mr-2" />
                <span class="text-truncate">{{ component.name.fallback }}</span>
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
                  <v-list-item @click="editComponent(component)">
                    <template v-slot:prepend>
                      <v-icon icon="mdi-pencil" />
                    </template>
                    <v-list-item-title>Edit</v-list-item-title>
                  </v-list-item>
                  <v-list-item @click="duplicateComponent(component)">
                    <template v-slot:prepend>
                      <v-icon icon="mdi-content-duplicate" />
                    </template>
                    <v-list-item-title>Duplicate</v-list-item-title>
                  </v-list-item>
                  <v-divider />
                  <v-list-item @click="exportComponent(component)">
                    <template v-slot:prepend>
                      <v-icon icon="mdi-export" />
                    </template>
                    <v-list-item-title>Export</v-list-item-title>
                  </v-list-item>
                  <v-divider />
                  <v-list-item
                    @click="deleteComponent(component)"
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
              <p class="text-body-2 text-medium-emphasis mb-3">
                {{ component.description?.fallback || 'No description' }}
              </p>

              <!-- Stats -->
              <div class="d-flex gap-2 mb-3">
                <v-chip size="small" color="purple">
                  {{ component.items?.length || 0 }} field{{ (component.items?.length || 0) !== 1 ? 's' : '' }}
                </v-chip>
                <v-chip size="small" color="secondary">
                  {{ component.category }}
                </v-chip>
                <v-chip v-if="component.previewText" size="small" color="info">
                  {{ component.previewText }}
                </v-chip>
              </div>

              <!-- Dates (if available) -->
              <div v-if="component.createdAt || component.updatedAt" class="text-caption text-medium-emphasis">
                <div v-if="component.createdAt" class="mb-1">
                  <v-icon size="12" class="mr-1">mdi-calendar-plus</v-icon>
                  Created: {{ formatDate(component.createdAt) }}
                </div>
                <div v-if="component.updatedAt">
                  <v-icon size="12" class="mr-1">mdi-calendar-edit</v-icon>
                  Updated: {{ formatDate(component.updatedAt) }}
                </div>
              </div>
            </v-card-text>

            <v-card-actions>
              <v-btn
                variant="outlined"
                size="small"
                prepend-icon="mdi-eye"
                @click.stop="previewComponent(component)"
              >
                Preview
              </v-btn>
              <v-spacer />
              <v-btn
                color="primary"
                size="small"
                prepend-icon="mdi-pencil"
                @click.stop="editComponent(component)"
              >
                Edit
              </v-btn>
            </v-card-actions>

            <!-- Active indicator -->
            <div v-if="currentComponent?.id === component.id" class="active-indicator">
              <v-icon icon="mdi-check-circle" color="success" size="16" />
            </div>
          </v-card>
        </v-col>
      </v-row>
    </v-card-text>

    <!-- Preview Dialog -->
    <v-dialog v-model="showPreviewDialog" max-width="600">
      <v-card v-if="previewingComponent">
        <v-card-title>{{ previewingComponent.name.fallback }}</v-card-title>
        <v-card-text>
          <p class="text-body-2 text-medium-emphasis mb-4">
            {{ previewingComponent.description?.fallback || 'No description' }}
          </p>

          <!-- Preview of component fields -->
          <div class="preview-content">
            <h4 class="text-subtitle-1 mb-2">Component Fields</h4>
            <div v-if="previewingComponent.items && previewingComponent.items.length > 0">
              <div
                v-for="(item, index) in previewingComponent.items"
                :key="item.id || index"
                class="field-preview mb-2 pa-2 border rounded"
              >
                <div class="d-flex align-center">
                  <v-icon size="16" class="mr-2">mdi-form-textbox</v-icon>
                  <span class="font-weight-medium">{{ item.label?.fallback || item.type }}</span>
                  <v-chip v-if="item.required" size="x-small" color="error" class="ml-2">
                    Required
                  </v-chip>
                </div>
                <div class="text-caption text-medium-emphasis ml-6">
                  Type: {{ item.type }}
                </div>
              </div>
            </div>
            <div v-else class="text-center py-4 text-medium-emphasis">
              No fields defined yet
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
            @click="editComponent(previewingComponent!); showPreviewDialog = false"
          >
            Edit This Component
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-card>
</template>

<script setup lang="ts">
// -nocheck — ported SysCase file; TS strict cleanup is a follow-up
import { ref, computed } from 'vue'
import { useCustomComponentsStore } from '@/stores/customComponents'
import type { CustomComponent } from '@/stores/customComponents'

// Props
defineProps<{
  currentComponent?: CustomComponent | null
}>()

// Emits
const emit = defineEmits<{
  selectComponent: [component: CustomComponent]
  createComponent: [name?: string]
  editComponent: [component: CustomComponent]
  deleteComponent: [component: CustomComponent]
  duplicateComponent: [component: CustomComponent]
}>()

// Store
const customComponentsStore = useCustomComponentsStore()

// Reactive state
const searchQuery = ref('')
const showPreviewDialog = ref(false)
const previewingComponent = ref<CustomComponent | null>(null)

// Computed
const components = computed(() => customComponentsStore.allCustomComponents)

const filteredComponents = computed(() => {
  if (!searchQuery.value) {
    return components.value
  }

  const query = searchQuery.value.toLowerCase()
  return components.value.filter(component =>
    component.name.fallback.toLowerCase().includes(query) ||
    (component.description?.fallback || '').toLowerCase().includes(query)
  )
})

// Methods
const selectComponent = (component: CustomComponent) => {
  emit('selectComponent', component)
}

const editComponent = (component: CustomComponent) => {
  emit('editComponent', component)
}

const deleteComponent = (component: CustomComponent) => {
  if (confirm(`Are you sure you want to delete "${component.name.fallback}"?`)) {
    emit('deleteComponent', component)
  }
}

const duplicateComponent = (component: CustomComponent) => {
  emit('duplicateComponent', component)
}

const previewComponent = (component: CustomComponent) => {
  previewingComponent.value = component
  showPreviewDialog.value = true
}

const exportComponent = (component: CustomComponent) => {
  const dataStr = JSON.stringify(component, null, 2)
  const dataUri = 'data:application/json;charset=utf-8,'+ encodeURIComponent(dataStr)
  const exportFileDefaultName = `component-${component.name.fallback.toLowerCase().replace(/\s+/g, '-')}.json`

  const linkElement = document.createElement('a')
  linkElement.setAttribute('href', dataUri)
  linkElement.setAttribute('download', exportFileDefaultName)
  linkElement.click()
}

const formatDate = (dateString: string) => {
  const date = new Date(dateString)
  return date.toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}
</script>

<style scoped>
.components-list {
  min-height: 70vh;
  max-height: 80vh;
  overflow-y: auto;
}

.component-card {
  cursor: pointer;
  transition: all 0.2s;
  border: 2px solid transparent;
  position: relative;
  height: 100%;
}

.component-card:hover {
  border-color: rgba(var(--v-theme-purple), 0.3);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.component-active {
  border-color: rgba(var(--v-theme-purple), 1);
  background-color: rgba(var(--v-theme-purple), 0.05);
}

.active-indicator {
  position: absolute;
  top: 8px;
  right: 8px;
}

.empty-state,
.empty-search {
  background-color: rgba(var(--v-theme-surface-variant), 0.3);
  border-radius: 8px;
}

.field-preview {
  border: 1px solid rgba(var(--v-theme-outline), 0.2);
}

.preview-content {
  max-height: 300px;
  overflow-y: auto;
}

@media (max-width: 960px) {
  .components-list {
    padding: 16px;
  }
}
</style>