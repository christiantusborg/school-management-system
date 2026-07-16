// Pinia store for ADR-0039 firm-wide questionnaire templates. The form
// definitions themselves are firm configuration (NOT per-recipient
// E2E — client answers are encrypted in a separate later phase), so
// the store deals with plaintext JSON and a server-computed hash.

import { defineStore } from 'pinia'
import { ref } from 'vue'
import {
  intakeApi, type QuestionnaireTemplateListItem,
  type QuestionnaireTemplateGetResponse,
} from '@quvian/shared/api/intakeApi'
import { useNotificationStore } from '@/stores/notification'

export const useIntakeTemplatesStore = defineStore('intakeTemplates', () => {
  const notify = useNotificationStore()

  const templates = ref<QuestionnaireTemplateListItem[]>([])
  const loading = ref(false)
  const includeDeleted = ref(false)

  async function fetchTemplates() {
    loading.value = true
    try {
      const res = await intakeApi.listQuestionnaireTemplates(includeDeleted.value)
      if (res.data.success && res.data.data) {
        templates.value = res.data.data.items
      }
    } catch (err: unknown) {
      notify.error(extractMessage(err) || 'Failed to list questionnaire templates')
    } finally {
      loading.value = false
    }
  }

  async function createTemplate(name: string, definitionJson: string, version = '1.0.0'): Promise<boolean> {
    try {
      const res = await intakeApi.createQuestionnaireTemplate({ name, definitionJson, version })
      if (res.data.success) {
        notify.success(`Created template "${name}"`)
        await fetchTemplates()
        return true
      }
      return false
    } catch (err: unknown) {
      notify.error(extractMessage(err) || 'Failed to create template')
      return false
    }
  }

  async function getTemplate(id: string): Promise<QuestionnaireTemplateGetResponse | null> {
    try {
      const res = await intakeApi.getQuestionnaireTemplate(id)
      if (res.data.success && res.data.data) {
        return res.data.data
      }
      return null
    } catch (err: unknown) {
      notify.error(extractMessage(err) || 'Failed to load template')
      return null
    }
  }

  async function updateTemplate(id: string, name: string, definitionJson: string, version = '1.0.0'): Promise<boolean> {
    try {
      const res = await intakeApi.updateQuestionnaireTemplate(id, { name, definitionJson, version })
      if (res.data.success) {
        notify.success(`Updated "${name}"`)
        await fetchTemplates()
        return true
      }
      return false
    } catch (err: unknown) {
      notify.error(extractMessage(err) || 'Failed to update template')
      return false
    }
  }

  async function deleteTemplate(id: string): Promise<boolean> {
    try {
      const res = await intakeApi.deleteQuestionnaireTemplate(id)
      if (res.data.success) {
        notify.success('Template deleted')
        await fetchTemplates()
        return true
      }
      return false
    } catch (err: unknown) {
      notify.error(extractMessage(err) || 'Failed to delete template')
      return false
    }
  }

  async function restoreTemplate(id: string): Promise<boolean> {
    try {
      const res = await intakeApi.restoreQuestionnaireTemplate(id)
      if (res.data.success) {
        notify.success('Template restored')
        await fetchTemplates()
        return true
      }
      return false
    } catch (err: unknown) {
      notify.error(extractMessage(err) || 'Failed to restore template')
      return false
    }
  }

  async function setIncludeDeleted(v: boolean) {
    includeDeleted.value = v
    await fetchTemplates()
  }

  function extractMessage(err: unknown): string | undefined {
    return (err as { response?: { data?: { message?: string } } })?.response?.data?.message
      || (err as Error)?.message
  }

  function reset() {
    templates.value = []
    includeDeleted.value = false
  }

  return {
    templates, loading, includeDeleted,
    fetchTemplates, createTemplate, getTemplate, updateTemplate, deleteTemplate,
    restoreTemplate, setIncludeDeleted, reset,
  }
})
