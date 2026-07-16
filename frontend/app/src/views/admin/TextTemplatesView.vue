<template>
  <v-container fluid class="pa-6">
    <!-- Header -->
    <div class="d-flex justify-space-between align-center mb-6">
      <div>
        <h1 class="text-h4 font-weight-bold mb-2">Text Templates</h1>
        <p class="text-subtitle-1 text-medium-emphasis mb-0">
          Create and manage email templates for questionnaire responses
        </p>
      </div>
      <v-btn
        color="primary"
        prepend-icon="mdi-plus"
        size="large"
        @click="showCreateDialog = true"
      >
        Create Template
      </v-btn>
    </div>


    <!-- Search and Filters -->
    <v-row class="mb-6">
      <v-col cols="12" md="6">
        <v-text-field
          v-model="searchQuery"
          placeholder="Search templates..."
          prepend-inner-icon="mdi-magnify"
          variant="outlined"
          density="compact"
          clearable
          hide-details
        />
      </v-col>
      <v-col cols="12" md="6" class="d-flex justify-end align-center">
        <v-chip-group v-model="selectedFilter" color="primary">
          <v-chip value="all" variant="outlined">All ({{ totalTemplates }})</v-chip>
          <v-chip value="recent" variant="outlined">Recent</v-chip>
        </v-chip-group>
      </v-col>
    </v-row>

    <!-- Templates Grid -->
    <div v-if="isLoading" class="d-flex justify-center py-8">
      <v-progress-circular indeterminate color="primary" size="48" />
    </div>

    <div v-else-if="paginatedTemplates.length === 0" class="text-center py-12">
      <v-icon icon="mdi-text-box-outline" size="64" class="text-medium-emphasis mb-4" />
      <h3 class="text-h6 mb-2">No text templates found</h3>
      <p class="text-body-2 text-medium-emphasis mb-4">
        {{ searchQuery ? 'Try adjusting your search query' : 'Create your first text template to get started' }}
      </p>
      <v-btn v-if="!searchQuery" color="primary" @click="showCreateDialog = true">
        Create Your First Template
      </v-btn>
    </div>

    <v-row v-else>
      <v-col
        v-for="template in paginatedTemplates"
        :key="template.id"
        cols="12"
        sm="6"
        md="4"
        lg="3"
      >
        <v-card
          class="template-card"
          elevation="2"
          hover
          @click="openEditor(template)"
        >
          <v-card-text class="pb-2">
            <div class="d-flex justify-space-between align-start mb-3">
              <h3 class="text-h6 font-weight-medium text-truncate" style="flex: 1; margin-right: 8px;">
                {{ template.name }}
              </h3>
              <v-menu>
                <template #activator="{ props }">
                  <v-btn
                    v-bind="props"
                    icon="mdi-dots-vertical"
                    variant="text"
                    size="small"
                    @click.stop
                  />
                </template>
                <v-list>
                  <v-list-item @click="openEditor(template)">
                    <v-list-item-title>
                      <v-icon icon="mdi-pencil" start />
                      Edit
                    </v-list-item-title>
                  </v-list-item>
                  <v-list-item @click="duplicateTemplate(template.id)">
                    <v-list-item-title>
                      <v-icon icon="mdi-content-copy" start />
                      Duplicate
                    </v-list-item-title>
                  </v-list-item>
                  <v-list-item @click="confirmDelete(template)" class="text-error">
                    <v-list-item-title>
                      <v-icon icon="mdi-delete" start />
                      Delete
                    </v-list-item-title>
                  </v-list-item>
                </v-list>
              </v-menu>
            </div>

            <div v-if="template.subject" class="mb-2">
              <v-chip size="small" color="primary" variant="tonal">
                {{ template.subject }}
              </v-chip>
            </div>

            <p class="text-body-2 text-medium-emphasis text-truncate-2 mb-3" style="min-height: 40px;">
              {{ getPlainTextPreview(template.content) }}
            </p>

            <div class="d-flex justify-space-between align-center">
              <v-chip size="small" variant="outlined">
                {{ template.wordCount }} words
              </v-chip>
            </div>
          </v-card-text>

          <v-card-actions class="pt-0">
            <div class="text-caption text-medium-emphasis">
              <div>Created: {{ formatDate(template.createdAt) }}</div>
              <div>Updated: {{ formatDate(template.updatedAt) }}</div>
            </div>
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>

    <!-- Pagination -->
    <div v-if="totalPages > 1" class="d-flex justify-center mt-6">
      <v-pagination
        v-model="currentPage"
        :length="totalPages"
        :total-visible="7"
        color="primary"
      />
    </div>

    <!-- Create/Edit Dialog -->
    <v-dialog
      v-model="showCreateDialog"
      max-width="900px"
      persistent
      scrollable
    >
      <v-card>
        <v-card-title class="pa-6 pb-0">
          <h2 class="text-h5">{{ editingTemplate ? 'Edit Template' : 'Create New Template' }}</h2>
        </v-card-title>

        <v-card-text class="pa-6">
          <v-form ref="createForm" v-model="isFormValid">
            <v-row>
              <v-col cols="12">
                <v-text-field
                  v-model="templateForm.name"
                  label="Template Name"
                  variant="outlined"
                  :rules="[rules.required]"
                  required
                />
              </v-col>
              <v-col cols="12">
                <v-text-field
                  v-model="templateForm.subject"
                  label="Email Subject (Optional)"
                  variant="outlined"
                />
              </v-col>
              <v-col cols="12">
                <v-select
                  v-model="templateForm.questionnaireId"
                  :items="questionnaireOptions"
                  label="Select Questionnaire for Variables (Optional)"
                  variant="outlined"
                  item-title="name"
                  item-value="id"
                  prepend-inner-icon="mdi-form-select"
                  clearable
                  hide-details
                  @update:model-value="onTemplateQuestionnaireSelected"
                >
                  <template v-slot:no-data>
                    <v-list-item>
                      <v-list-item-title class="text-medium-emphasis">
                        No questionnaires available
                      </v-list-item-title>
                    </v-list-item>
                  </template>
                </v-select>
                <div class="text-caption text-medium-emphasis mt-1">
                  <span v-if="selectedTemplateQuestionnaire">
                    {{ templateAvailableFieldsCount }} fields available for variables
                  </span>
                  <span v-else>
                    Choose a questionnaire to enable variable insertion in the content editor
                  </span>
                </div>
              </v-col>
              <v-col cols="12">
                <label class="text-subtitle-2 mb-2 d-block">Content</label>
                <RichTextEditor
                  v-model="templateForm.content"
                  mode="builder"
                  :disabled="isLoading"
                  :selected-questionnaire="selectedTemplateQuestionnaire"
                  :available-fields="templateAvailableFields"
                />
                <div class="text-caption text-medium-emphasis mt-1">
                  {{ countWords(templateForm.content) }} words
                </div>
              </v-col>
            </v-row>
          </v-form>
        </v-card-text>

        <v-card-actions class="pa-6 pt-0">
          <v-spacer />
          <v-btn
            text
            @click="closeDialog"
          >
            Cancel
          </v-btn>
          <v-btn
            color="primary"
            :loading="isLoading"
            :disabled="!isFormValid || !templateForm.content.trim()"
            @click="saveTemplate"
          >
            {{ editingTemplate ? 'Update' : 'Create' }}
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Delete Confirmation Dialog -->
    <v-dialog v-model="showDeleteDialog" max-width="400">
      <v-card>
        <v-card-title class="text-h6">Delete Template</v-card-title>
        <v-card-text>
          Are you sure you want to delete "{{ templateToDelete?.name }}"? This action cannot be undone.
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn text @click="showDeleteDialog = false">Cancel</v-btn>
          <v-btn color="error" @click="deleteTemplateConfirmed">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Success Snackbar -->
    <v-snackbar
      v-model="showSnackbar"
      :color="snackbarColor"
      :timeout="3000"
    >
      {{ snackbarMessage }}
    </v-snackbar>
  </v-container>
</template>

<script setup lang="ts">
// -nocheck — ported SysCase file; TS strict cleanup is a follow-up
import { ref, computed, onMounted, watch } from 'vue'
import RichTextEditor from '@/components/forms/RichTextEditor.vue'
import { useTextTemplatesStore } from '@/stores/textTemplates'
import { useQuestionnaireStore } from '@/stores/questionnaire'
import type { TextTemplate } from '@quvian/shared/api/types'
import type { ComponentType } from '@quvian/shared/types/questionnaire'
import { format } from 'date-fns'

const store = useTextTemplatesStore()
const questionnaireStore = useQuestionnaireStore()

// Reactive state
const showCreateDialog = ref(false)
const showDeleteDialog = ref(false)
const editingTemplate = ref<TextTemplate | null>(null)
const templateToDelete = ref<TextTemplate | null>(null)
const selectedFilter = ref('all')
const isFormValid = ref(false)
const showSnackbar = ref(false)
const snackbarMessage = ref('')
const snackbarColor = ref('success')

// Form state
const templateForm = ref({
  name: '',
  subject: '',
  content: '',
  questionnaireId: null as string | null
})

// Computed properties from store
const {
  templates,
  isLoading,
  error,
  currentPage,
  totalPages,
  searchQuery,
  paginatedTemplates,
  totalTemplates,
  setSearchQuery,
  setPage,
  createTemplate,
  updateTemplate,
  deleteTemplate,
  duplicateTemplate: storeDuplicateTemplate,
  fetchTemplates,
  countWords
} = store

// Questionnaire computed properties
const questionnaireOptions = computed(() => {
  return questionnaireStore.questionnaires.map(q => ({
    id: q.id,
    name: q.name.fallback
  }))
})

const selectedTemplateQuestionnaire = computed(() => {
  if (!templateForm.value.questionnaireId) return null
  return questionnaireStore.questionnaires.find(q => q.id === templateForm.value.questionnaireId)
})

const templateAvailableFields = computed(() => {
  if (!selectedTemplateQuestionnaire.value) return []

  const fields: Array<{id: string, label: string, type: ComponentType, required: boolean}> = []

  // Extract all fields from all pages, sections, and groups
  for (const page of selectedTemplateQuestionnaire.value.pages) {
    for (const section of page.sections) {
      for (const group of section.groups) {
        for (const item of group.items) {
          // Only include input fields (exclude display-only components)
          if (isInputField(item.type) && item.label?.fallback) {
            fields.push({
              id: item.id,
              label: item.label.fallback,
              type: item.type,
              required: item.required || false
            })
          }
        }
      }
    }
  }

  return fields.sort((a, b) => a.label.localeCompare(b.label))
})

const templateAvailableFieldsCount = computed(() => templateAvailableFields.value.length)

// Helper function to check if field type is input
const isInputField = (type: ComponentType): boolean => {
  const inputTypes: ComponentType[] = [
    'text', 'textarea', 'richtext', 'number', 'email', 'phone',
    'date', 'time', 'datetime', 'select', 'radio', 'checkbox',
    'toggle', 'file-upload', 'address'
  ]
  return inputTypes.includes(type)
}

// Questionnaire methods
const onTemplateQuestionnaireSelected = () => {
  // Optional: Add any side effects when template questionnaire changes
}

// Form validation rules
const rules = {
  required: (value: any) => !!value || 'This field is required'
}

// Methods
const openEditor = (template: TextTemplate) => {
  editingTemplate.value = template
  templateForm.value = {
    name: template.name,
    subject: template.subject || '',
    content: template.content,
    questionnaireId: (template as any).questionnaireId || null
  }
  showCreateDialog.value = true
}

const closeDialog = () => {
  showCreateDialog.value = false
  editingTemplate.value = null
  templateForm.value = {
    name: '',
    subject: '',
    content: '',
    questionnaireId: null
  }
}

const saveTemplate = async () => {
  try {
    if (editingTemplate.value) {
      await updateTemplate(editingTemplate.value.id, {
        name: templateForm.value.name,
        subject: templateForm.value.subject || undefined,
        content: templateForm.value.content,
        questionnaireId: templateForm.value.questionnaireId
      } as any)
      showMessage('Template updated successfully', 'success')
    } else {
      await createTemplate({
        name: templateForm.value.name,
        subject: templateForm.value.subject || undefined,
        content: templateForm.value.content,
        questionnaireId: templateForm.value.questionnaireId
      } as any)
      showMessage('Template created successfully', 'success')
    }
    closeDialog()
  } catch (err) {
    showMessage('Failed to save template', 'error')
  }
}

const confirmDelete = (template: TextTemplate) => {
  templateToDelete.value = template
  showDeleteDialog.value = true
}

const deleteTemplateConfirmed = async () => {
  if (!templateToDelete.value) return

  try {
    await deleteTemplate(templateToDelete.value.id)
    showMessage('Template deleted successfully', 'success')
    showDeleteDialog.value = false
    templateToDelete.value = null
  } catch (err) {
    showMessage('Failed to delete template', 'error')
  }
}

const duplicateTemplate = async (templateId: string) => {
  try {
    await storeDuplicateTemplate(templateId)
    showMessage('Template duplicated successfully', 'success')
  } catch (err) {
    showMessage('Failed to duplicate template', 'error')
  }
}

const showMessage = (message: string, color: 'success' | 'error') => {
  snackbarMessage.value = message
  snackbarColor.value = color
  showSnackbar.value = true
}

const formatDate = (dateString: string) => {
  return format(new Date(dateString), 'MMM dd, yyyy')
}

const getPlainTextPreview = (htmlContent: string) => {
  // Strip HTML tags and get first 100 characters
  const div = document.createElement('div')
  div.innerHTML = htmlContent
  const text = div.textContent || div.innerText || ''
  return text.length > 100 ? text.substring(0, 100) + '...' : text
}

// Watch for search query changes
watch(() => searchQuery, (newQuery) => {
  setSearchQuery(newQuery || '')
})

// Watch for page changes
watch(() => currentPage, (newPage) => {
  setPage(newPage)
})

// Initialize
onMounted(async () => {
  await fetchTemplates()
})
</script>

<style scoped>
.template-card {
  cursor: pointer;
  transition: transform 0.2s;
}

.template-card:hover {
  transform: translateY(-2px);
}

.text-truncate-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  line-height: 1.4;
}

</style>