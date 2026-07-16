// @ts-nocheck — ported SysCase file; TS strict cleanup is a follow-up.
//
// Phase 15e: persistence is the QuVian encrypted IntakeApi (per-tenant DB).
// The SysCase TextTemplate shape ({ id, name, subject, content, wordCount,
// questionnaireId, createdAt, updatedAt, userId }) is preserved client-side
// by stuffing subject/content/wordCount/questionnaireId into the server's
// bodyJson column on every write, and reconstituting it on every read.
//
// fetchTemplates() does N+1 (list + per-template GET) because the list
// endpoint only returns metadata. Acceptable for the firm's template
// libraries (low cardinality); revisit if it ever bites.
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { TextTemplate, CreateTextTemplateRequest, UpdateTextTemplateRequest } from '@quvian/shared/api/types'
import { intakeApi } from '@quvian/shared/api/intakeApi'
import type { TextTemplateGetResponse } from '@quvian/shared/api/intakeApi'

interface BodyJson {
  subject?: string
  content: string
  wordCount: number
  questionnaireId?: string
}

const countWords = (text: string): number => {
  if (!text) return 0
  return text.trim().split(/\s+/).filter(word => word.length > 0).length
}

const toTextTemplate = (server: TextTemplateGetResponse): TextTemplate => {
  let body: BodyJson = { content: '', wordCount: 0 }
  try {
    body = JSON.parse(server.bodyJson) as BodyJson
  } catch {
    body = { content: server.bodyJson ?? '', wordCount: countWords(server.bodyJson ?? '') }
  }
  return {
    id: server.textTemplateId,
    name: server.name,
    subject: body.subject ?? '',
    content: body.content ?? '',
    wordCount: body.wordCount ?? countWords(body.content ?? ''),
    questionnaireId: body.questionnaireId,
    createdAt: server.createdAt,
    updatedAt: server.modifiedAt,
    userId: server.createdByUserId
  }
}

const toBodyJson = (
  subject: string | undefined,
  content: string,
  questionnaireId: string | undefined
): string => JSON.stringify({
  subject: subject ?? '',
  content: content ?? '',
  wordCount: countWords(content ?? ''),
  questionnaireId
} satisfies BodyJson)

export const useTextTemplatesStore = defineStore('textTemplates', () => {
  const templates = ref<TextTemplate[]>([])
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  const selectedTemplate = ref<TextTemplate | null>(null)

  const currentPage = ref(1)
  const itemsPerPage = ref(12)
  const searchQuery = ref('')

  const filteredTemplates = computed(() => {
    if (!searchQuery.value) return templates.value
    const query = searchQuery.value.toLowerCase()
    return templates.value.filter(template =>
      template.name.toLowerCase().includes(query) ||
      template.subject?.toLowerCase().includes(query) ||
      template.content.toLowerCase().includes(query)
    )
  })

  const paginatedTemplates = computed(() => {
    const start = (currentPage.value - 1) * itemsPerPage.value
    return filteredTemplates.value.slice(start, start + itemsPerPage.value)
  })

  const totalPages = computed(() =>
    Math.ceil(filteredTemplates.value.length / itemsPerPage.value)
  )
  const totalTemplates = computed(() => templates.value.length)
  const totalWords = computed(() =>
    templates.value.reduce((sum, t) => sum + t.wordCount, 0)
  )
  const averageWordsPerTemplate = computed(() => {
    if (templates.value.length === 0) return 0
    return Math.round(totalWords.value / templates.value.length)
  })

  const fetchTemplates = async () => {
    isLoading.value = true
    error.value = null
    try {
      const listResp = await intakeApi.listTextTemplates(false)
      const items = listResp.data.data?.items ?? []
      const full = await Promise.all(
        items.map(async item => {
          const fullResp = await intakeApi.getTextTemplate(item.textTemplateId)
          return toTextTemplate(fullResp.data.data!)
        })
      )
      full.sort((a, b) => (b.createdAt || '').localeCompare(a.createdAt || ''))
      templates.value = full
    } catch (err) {
      error.value = 'Failed to fetch text templates'
      console.error('Error fetching text templates:', err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  const fetchTemplateById = async (templateId: string) => {
    isLoading.value = true
    error.value = null
    try {
      const resp = await intakeApi.getTextTemplate(templateId)
      const template = toTextTemplate(resp.data.data!)
      selectedTemplate.value = template
      const idx = templates.value.findIndex(t => t.id === templateId)
      if (idx >= 0) templates.value[idx] = template
      return template
    } catch (err) {
      error.value = 'Failed to fetch template details'
      console.error('Error fetching template:', err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  const createTemplate = async (templateData: CreateTextTemplateRequest) => {
    isLoading.value = true
    error.value = null
    try {
      const bodyJson = toBodyJson(templateData.subject, templateData.content, templateData.questionnaireId)
      await intakeApi.createTextTemplate({ name: templateData.name, bodyJson })
      await fetchTemplates()
      return templates.value[0]
    } catch (err) {
      error.value = 'Failed to create text template'
      console.error('Error creating template:', err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  const updateTemplate = async (templateId: string, templateData: UpdateTextTemplateRequest) => {
    isLoading.value = true
    error.value = null
    try {
      const existing = templates.value.find(t => t.id === templateId)
        ?? toTextTemplate((await intakeApi.getTextTemplate(templateId)).data.data!)
      const merged = {
        name: templateData.name ?? existing.name,
        subject: templateData.subject ?? existing.subject,
        content: templateData.content ?? existing.content,
        questionnaireId: templateData.questionnaireId ?? existing.questionnaireId
      }
      const bodyJson = toBodyJson(merged.subject, merged.content, merged.questionnaireId)
      await intakeApi.updateTextTemplate(templateId, { name: merged.name, bodyJson })
      const fresh = toTextTemplate((await intakeApi.getTextTemplate(templateId)).data.data!)
      const idx = templates.value.findIndex(t => t.id === templateId)
      if (idx >= 0) templates.value[idx] = fresh
      if (selectedTemplate.value?.id === templateId) selectedTemplate.value = fresh
      return fresh
    } catch (err) {
      error.value = 'Failed to update template'
      console.error('Error updating template:', err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  const deleteTemplate = async (templateId: string) => {
    isLoading.value = true
    error.value = null
    try {
      await intakeApi.deleteTextTemplate(templateId)
      const idx = templates.value.findIndex(t => t.id === templateId)
      if (idx >= 0) templates.value.splice(idx, 1)
      if (selectedTemplate.value?.id === templateId) selectedTemplate.value = null
    } catch (err) {
      error.value = 'Failed to delete template'
      console.error('Error deleting template:', err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  const duplicateTemplate = async (templateId: string) => {
    isLoading.value = true
    error.value = null
    try {
      const source = templates.value.find(t => t.id === templateId)
        ?? toTextTemplate((await intakeApi.getTextTemplate(templateId)).data.data!)
      const bodyJson = toBodyJson(source.subject, source.content, source.questionnaireId)
      await intakeApi.createTextTemplate({ name: `${source.name} (Copy)`, bodyJson })
      await fetchTemplates()
      return templates.value[0]
    } catch (err) {
      error.value = 'Failed to duplicate template'
      console.error('Error duplicating template:', err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  const setSearchQuery = (query: string) => {
    searchQuery.value = query
    currentPage.value = 1
  }
  const clearFilters = () => {
    searchQuery.value = ''
    currentPage.value = 1
  }
  const setPage = (page: number) => { currentPage.value = page }
  const setItemsPerPage = (items: number) => {
    itemsPerPage.value = items
    currentPage.value = 1
  }
  const clearError = () => { error.value = null }
  const clearSelectedTemplate = () => { selectedTemplate.value = null }

  return {
    templates,
    isLoading,
    error,
    selectedTemplate,
    currentPage,
    itemsPerPage,
    searchQuery,
    filteredTemplates,
    paginatedTemplates,
    totalPages,
    totalTemplates,
    totalWords,
    averageWordsPerTemplate,
    fetchTemplates,
    fetchTemplateById,
    createTemplate,
    updateTemplate,
    deleteTemplate,
    duplicateTemplate,
    setSearchQuery,
    clearFilters,
    setPage,
    setItemsPerPage,
    clearError,
    clearSelectedTemplate,
    countWords
  }
})
