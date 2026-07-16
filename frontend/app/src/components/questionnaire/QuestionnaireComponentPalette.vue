<template>
  <v-card class="component-palette">
    <v-card-title class="d-flex align-center">
      <v-icon icon="mdi-palette" class="mr-2" />
      Components
    </v-card-title>

    <v-card-text>
      <!-- Search -->
      <v-text-field
        v-model="searchQuery"
        label="Search components"
        prepend-inner-icon="mdi-magnify"
        variant="outlined"
        density="compact"
        clearable
        class="mb-4"
      />

      <!-- Category Tabs -->
      <v-tabs v-model="activeCategory" direction="vertical" color="primary">
        <v-tab
          v-for="category in categories"
          :key="category.id"
          :value="category.id"
          class="justify-start"
        >
          <v-icon :icon="category.icon" class="mr-2" />
          {{ category.name }}
          <v-chip size="x-small" class="ml-2">
            {{ category.count }}
          </v-chip>
        </v-tab>
      </v-tabs>

      <!-- Component List -->
      <div class="mt-4">
        <div
          v-for="component in filteredComponents"
          :key="component.type"
          class="component-item"
          draggable="true"
          @dragstart="handleDragStart(component, $event)"
          @click="addComponent(component.type)"
        >
          <div class="d-flex align-center">
            <v-avatar size="32" color="primary" variant="tonal" class="mr-3">
              <v-icon :icon="component.icon" size="16" />
            </v-avatar>
            <div class="flex-grow-1">
              <div class="font-weight-medium">{{ component.title.fallback }}</div>
              <div class="text-caption text-medium-emphasis">
                {{ component.description.fallback }}
              </div>
            </div>
            <v-btn
              icon="mdi-plus"
              size="small"
              variant="text"
              @click.stop="addComponent(component.type)"
            />
          </div>
        </div>

        <!-- Empty State -->
        <div v-if="filteredComponents.length === 0" class="text-center py-6">
          <v-icon size="48" color="grey-lighten-1" class="mb-2">
            mdi-package-variant-closed
          </v-icon>
          <div class="text-body-2 text-medium-emphasis">
            No components found
          </div>
        </div>
      </div>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
// -nocheck — ported SysCase file; TS strict cleanup is a follow-up
import { ref, computed } from 'vue'
import { componentRegistry, getComponentsByCategory, getCategories } from '@/utils/questionnaire/componentRegistry'
import type { ComponentType, ComponentSchema } from '@quvian/shared/types/questionnaire'

// Props
const props = defineProps<{
  builderMode?: 'questionnaire' | 'component'
}>()

// Emits
const emit = defineEmits<{
  addComponent: [componentType: ComponentType]
}>()

// Reactive state
const searchQuery = ref('')
const activeCategory = ref('input')

// Category definitions
const categoryMeta = {
  input: { name: 'Input Fields', icon: 'mdi-form-textbox' },
  selection: { name: 'Selection', icon: 'mdi-form-dropdown' },
  upload: { name: 'Upload', icon: 'mdi-file-upload' },
  display: { name: 'Display', icon: 'mdi-text' },
  special: { name: 'Special', icon: 'mdi-star' },
}

// Computed
const categories = computed(() => {
  const cats = getCategories()
  return cats.map(cat => ({
    id: cat,
    name: categoryMeta[cat as keyof typeof categoryMeta]?.name || cat,
    icon: categoryMeta[cat as keyof typeof categoryMeta]?.icon || 'mdi-help',
    count: getComponentsByCategory(cat).length
  }))
})

const filteredComponents = computed(() => {
  let components: ComponentSchema[]

  if (activeCategory.value === 'all') {
    components = Object.values(componentRegistry)
  } else {
    components = getComponentsByCategory(activeCategory.value)
  }

  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase()
    components = components.filter(component =>
      component.title.fallback.toLowerCase().includes(query) ||
      component.description.fallback.toLowerCase().includes(query) ||
      component.type.toLowerCase().includes(query)
    )
  }

  return components
})

// Methods
const addComponent = (componentType: ComponentType) => {
  emit('addComponent', componentType)
}

const handleDragStart = (component: ComponentSchema, event: DragEvent) => {
  if (event.dataTransfer) {
    event.dataTransfer.setData('application/json', JSON.stringify({
      type: 'component',
      componentType: component.type
    }))
    event.dataTransfer.effectAllowed = 'copy'

    // Add visual feedback during drag
    const target = event.target as HTMLElement
    target.style.opacity = '0.5'

    // Reset opacity after drag ends
    setTimeout(() => {
      target.style.opacity = '1'
    }, 50)
  }
}

</script>

<style scoped>
.component-palette {
  height: fit-content;
  max-height: 80vh;
  overflow-y: auto;
}

.component-item {
  padding: 12px;
  border-radius: 8px;
  margin-bottom: 8px;
  cursor: pointer;
  transition: background-color 0.2s, transform 0.1s;
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
}

.component-item:hover {
  background-color: rgba(var(--v-theme-primary), 0.04);
  transform: translateX(2px);
}

.component-item:active {
  transform: translateX(0);
}

.component-item[draggable="true"] {
  cursor: grab;
}

.component-item[draggable="true"]:active {
  cursor: grabbing;
}


.v-tabs {
  border-right: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
}
</style>