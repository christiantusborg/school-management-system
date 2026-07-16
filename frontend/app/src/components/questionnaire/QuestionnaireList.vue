<template>
  <v-card class="questionnaire-list">
    <v-card-title class="d-flex align-center justify-space-between">
      <div class="d-flex align-center">
        <v-icon icon="mdi-folder-multiple" class="mr-2" />
        My Questionnaires
      </div>
      <v-btn
        color="primary"
        prepend-icon="mdi-plus"
        @click="showCreateDialog = true"
      >
        Add New Questionnaire
      </v-btn>
    </v-card-title>

    <v-card-text>
      <!-- Search Bar -->
      <v-text-field
        v-model="searchQuery"
        label="Search questionnaires..."
        prepend-inner-icon="mdi-magnify"
        variant="outlined"
        density="compact"
        clearable
        class="mb-4"
        hint="Search by name or description"
      />

      <!-- Questionnaires Grid -->
      <div v-if="filteredQuestionnaires.length === 0 && searchQuery" class="empty-search text-center py-8">
        <v-icon size="64" color="grey-lighten-1" class="mb-4">
          mdi-magnify
        </v-icon>
        <h3 class="text-h6 mb-2">No questionnaires found</h3>
        <p class="text-body-2 text-medium-emphasis">
          Try adjusting your search terms or create a new questionnaire
        </p>
      </div>

      <div v-else-if="questionnaires.length === 0" class="empty-state text-center py-8">
        <v-icon size="64" color="grey-lighten-1" class="mb-4">
          mdi-file-document-plus-outline
        </v-icon>
        <h3 class="text-h6 mb-2">No questionnaires yet</h3>
        <p class="text-body-2 text-medium-emphasis mb-4">
          Create your first questionnaire to get started
        </p>
        <v-btn
          color="primary"
          prepend-icon="mdi-plus"
          @click="showCreateDialog = true"
        >
          Create Your First Questionnaire
        </v-btn>
      </div>

      <v-row v-else>
        <v-col
          v-for="questionnaire in filteredQuestionnaires"
          :key="questionnaire.id"
          cols="12"
          md="6"
          lg="4"
        >
          <v-card
            class="questionnaire-card"
            :class="{ 'questionnaire-active': currentQuestionnaire?.id === questionnaire.id }"
            @click="selectQuestionnaire(questionnaire)"
          >
            <v-card-title class="d-flex align-center justify-space-between">
              <div class="d-flex align-center">
                <v-icon icon="mdi-file-document" class="mr-2" />
                <span class="text-truncate">{{ questionnaire.name.fallback }}</span>
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
                  <v-list-item @click="editQuestionnaire(questionnaire)">
                    <template v-slot:prepend>
                      <v-icon icon="mdi-pencil" />
                    </template>
                    <v-list-item-title>Edit</v-list-item-title>
                  </v-list-item>
                  <v-list-item @click="duplicateQuestionnaire(questionnaire)">
                    <template v-slot:prepend>
                      <v-icon icon="mdi-content-duplicate" />
                    </template>
                    <v-list-item-title>Duplicate</v-list-item-title>
                  </v-list-item>
                  <v-divider />
                  <v-list-item @click="exportQuestionnaire(questionnaire)">
                    <template v-slot:prepend>
                      <v-icon icon="mdi-export" />
                    </template>
                    <v-list-item-title>Export</v-list-item-title>
                  </v-list-item>
                  <v-divider />
                  <v-list-item
                    @click="deleteQuestionnaire(questionnaire)"
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
                {{ questionnaire.description?.fallback || 'No description' }}
              </p>

              <!-- Stats -->
              <div class="d-flex gap-2 mb-3">
                <v-chip size="small" color="info">
                  {{ questionnaire.pages.length }} page{{ questionnaire.pages.length !== 1 ? 's' : '' }}
                </v-chip>
                <v-chip size="small" color="secondary">
                  {{ getTotalFields(questionnaire) }} fields
                </v-chip>
                <v-chip v-if="questionnaire.version" size="small" color="success">
                  v{{ questionnaire.version }}
                </v-chip>
              </div>

              <!-- Dates -->
              <div class="text-caption text-medium-emphasis">
                <div class="mb-1">
                  <v-icon size="12" class="mr-1">mdi-calendar-plus</v-icon>
                  Created: {{ formatDate(questionnaire.createdAt) }}
                </div>
                <div>
                  <v-icon size="12" class="mr-1">mdi-calendar-edit</v-icon>
                  Updated: {{ formatDate(questionnaire.updatedAt) }}
                </div>
              </div>
            </v-card-text>

            <v-card-actions>
              <v-btn
                variant="outlined"
                size="small"
                prepend-icon="mdi-eye"
                @click.stop="previewQuestionnaire(questionnaire)"
              >
                Preview
              </v-btn>
              <v-btn
                variant="outlined"
                size="small"
                prepend-icon="mdi-code-json"
                @click.stop="showJsonDialog(questionnaire)"
              >
                JSON
              </v-btn>
              <v-spacer />
              <v-btn
                color="primary"
                size="small"
                prepend-icon="mdi-pencil"
                @click.stop="editQuestionnaire(questionnaire)"
              >
                Edit
              </v-btn>
            </v-card-actions>

            <!-- Active indicator -->
            <div v-if="currentQuestionnaire?.id === questionnaire.id" class="active-indicator">
              <v-icon icon="mdi-check-circle" color="success" size="16" />
            </div>
          </v-card>
        </v-col>
      </v-row>
    </v-card-text>

    <!-- Create Questionnaire Dialog -->
    <v-dialog v-model="showCreateDialog" max-width="500">
      <v-card>
        <v-card-title>Create New Questionnaire</v-card-title>
        <v-card-text>
          <v-text-field
            v-model="newQuestionnaireName"
            label="Questionnaire Name"
            variant="outlined"
            density="compact"
            :rules="[rules.required]"
            autofocus
          />
          <v-textarea
            v-model="newQuestionnaireDescription"
            label="Description"
            variant="outlined"
            density="compact"
            rows="3"
            hint="Optional description for this questionnaire"
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="cancelCreate">
            Cancel
          </v-btn>
          <v-btn
            color="primary"
            @click="createQuestionnaire"
            :disabled="!newQuestionnaireName"
          >
            Create
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Preview Dialog -->
    <v-dialog v-model="showPreviewDialog" max-width="800">
      <v-card v-if="previewingQuestionnaire">
        <v-card-title>{{ previewingQuestionnaire.name.fallback }}</v-card-title>
        <v-card-text>
          <p class="text-body-2 text-medium-emphasis mb-4">
            {{ previewingQuestionnaire.description?.fallback || 'No description' }}
          </p>

          <!-- Quick preview of pages and fields -->
          <div class="preview-content">
            <h4 class="text-subtitle-1 mb-2">Pages Overview</h4>
            <div
              v-for="(page, index) in previewingQuestionnaire.pages"
              :key="page.id"
              class="page-preview mb-3"
            >
              <div class="d-flex align-center mb-2">
                <v-icon icon="mdi-file-document" size="16" class="mr-2" />
                <span class="font-weight-medium">{{ page.title.fallback }}</span>
                <v-chip size="x-small" class="ml-2">
                  Page {{ index + 1 }}
                </v-chip>
              </div>
              <div class="ml-6">
                <div
                  v-for="section in page.sections"
                  :key="section.id"
                  class="text-body-2 text-medium-emphasis mb-1"
                >
                  <v-icon icon="mdi-folder" size="12" class="mr-1" />
                  {{ section.title?.fallback || 'Section' }}
                  ({{ getTotalSectionFields(section) }} fields)
                </div>
              </div>
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
            @click="editQuestionnaire(previewingQuestionnaire!); showPreviewDialog = false"
          >
            Edit This Questionnaire
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
import type { Questionnaire, QuestionnaireSection } from '@quvian/shared/types/questionnaire'

// Emits
const emit = defineEmits<{
  selectQuestionnaire: [questionnaire: Questionnaire]
  createQuestionnaire: [name: string]
  editQuestionnaire: [questionnaire: Questionnaire]
  deleteQuestionnaire: [questionnaire: Questionnaire]
  duplicateQuestionnaire: [questionnaire: Questionnaire]
  previewQuestionnaire: [questionnaire: Questionnaire]
  showJsonDialog: [questionnaire: Questionnaire]
}>()

// Store
const questionnaireStore = useQuestionnaireStore()

// Reactive state
const searchQuery = ref('')
const showCreateDialog = ref(false)
const showPreviewDialog = ref(false)
const previewingQuestionnaire = ref<Questionnaire | null>(null)
const newQuestionnaireName = ref('')
const newQuestionnaireDescription = ref('')

// Validation rules
const rules = {
  required: (value: string) => !!value || 'This field is required'
}

// Computed
const questionnaires = computed(() => questionnaireStore.questionnaires)
const currentQuestionnaire = computed(() => questionnaireStore.currentQuestionnaire)

const filteredQuestionnaires = computed(() => {
  if (!searchQuery.value) {
    return questionnaires.value
  }

  const query = searchQuery.value.toLowerCase()
  return questionnaires.value.filter(questionnaire =>
    questionnaire.name.fallback.toLowerCase().includes(query) ||
    (questionnaire.description?.fallback || '').toLowerCase().includes(query)
  )
})

// Methods
const selectQuestionnaire = (questionnaire: Questionnaire) => {
  emit('selectQuestionnaire', questionnaire)
}

const editQuestionnaire = (questionnaire: Questionnaire) => {
  emit('editQuestionnaire', questionnaire)
}

const deleteQuestionnaire = (questionnaire: Questionnaire) => {
  if (confirm(`Are you sure you want to delete "${questionnaire.name.fallback}"?`)) {
    emit('deleteQuestionnaire', questionnaire)
  }
}

const duplicateQuestionnaire = (questionnaire: Questionnaire) => {
  emit('duplicateQuestionnaire', questionnaire)
}

const previewQuestionnaire = (questionnaire: Questionnaire) => {
  emit('previewQuestionnaire', questionnaire)
}

const showJsonDialog = (questionnaire: Questionnaire) => {
  emit('showJsonDialog', questionnaire)
}

const createQuestionnaire = () => {
  emit('createQuestionnaire', newQuestionnaireName.value)
  cancelCreate()
}

const cancelCreate = () => {
  showCreateDialog.value = false
  newQuestionnaireName.value = ''
  newQuestionnaireDescription.value = ''
}

const exportQuestionnaire = (questionnaire: Questionnaire) => {
  const dataStr = JSON.stringify(questionnaire, null, 2)
  const dataUri = 'data:application/json;charset=utf-8,'+ encodeURIComponent(dataStr)
  const exportFileDefaultName = `questionnaire-${questionnaire.name.fallback.toLowerCase().replace(/\s+/g, '-')}.json`

  const linkElement = document.createElement('a')
  linkElement.setAttribute('href', dataUri)
  linkElement.setAttribute('download', exportFileDefaultName)
  linkElement.click()
}

const getTotalFields = (questionnaire: Questionnaire) => {
  let total = 0
  questionnaire.pages.forEach(page => {
    page.sections.forEach(section => {
      section.groups?.forEach(group => {
        total += group.items?.length || 0
      })
    })
  })
  return total
}

const getTotalSectionFields = (section: QuestionnaireSection) => {
  let total = 0
  section.groups?.forEach(group => {
    total += group.items?.length || 0
  })
  return total
}

const formatDate = (dateString: string | undefined) => {
  if (!dateString) return ''
  const date = new Date(dateString)
  return date.toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}
</script>

<style scoped>
.questionnaire-list {
  min-height: 70vh;
  max-height: 80vh;
  overflow-y: auto;
}

.questionnaire-card {
  cursor: pointer;
  transition: all 0.2s;
  border: 2px solid transparent;
  position: relative;
  height: 100%;
}

.questionnaire-card:hover {
  border-color: rgba(var(--v-theme-primary), 0.3);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.questionnaire-active {
  border-color: rgba(var(--v-theme-primary), 1);
  background-color: rgba(var(--v-theme-primary), 0.05);
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

.page-preview {
  border-left: 3px solid rgba(var(--v-theme-primary), 0.3);
  padding-left: 12px;
  margin-left: 8px;
}

.preview-content {
  max-height: 400px;
  overflow-y: auto;
}

@media (max-width: 960px) {
  .questionnaire-list {
    padding: 16px;
  }
}
</style>