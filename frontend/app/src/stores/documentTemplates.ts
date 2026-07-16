// Pinia store for ADR-0039 §3 firm-library DocumentTemplates. Mirrors
// stores/intakeTemplates.ts. Document templates are firm configuration
// (NOT per-recipient E2E); they pair a strategy (Generate / Overlay /
// AcroFormFill) with an optional base asset ref + a MappingJson blob
// produced by the visual field-mapper.

import { defineStore } from 'pinia'
import { ref } from 'vue'
import {
  intakeApi,
  type DocumentTemplateListItem,
  type DocumentTemplateGetResponse,
  type DocumentTemplateCreateRequest,
  type DocumentTemplateUpdateRequest,
} from '@quvian/shared/api/intakeApi'
import { useNotificationStore } from '@/stores/notification'

export const useDocumentTemplatesStore = defineStore('documentTemplates', () => {
  const notify = useNotificationStore()

  const templates = ref<DocumentTemplateListItem[]>([])
  const loading = ref(false)
  const includeDeleted = ref(false)

  async function fetchTemplates() {
    loading.value = true
    try {
      const res = await intakeApi.listDocumentTemplates(includeDeleted.value)
      if (res.data.success && res.data.data) {
        templates.value = res.data.data.items
      }
    } catch (err: unknown) {
      notify.error(extractMessage(err) || 'Failed to list document templates')
    } finally {
      loading.value = false
    }
  }

  async function createTemplate(body: DocumentTemplateCreateRequest): Promise<boolean> {
    try {
      const res = await intakeApi.createDocumentTemplate(body)
      if (res.data.success) {
        notify.success(`Created "${body.name}"`)
        await fetchTemplates()
        return true
      }
      return false
    } catch (err: unknown) {
      notify.error(extractMessage(err) || 'Failed to create document template')
      return false
    }
  }

  async function getTemplate(id: string): Promise<DocumentTemplateGetResponse | null> {
    try {
      const res = await intakeApi.getDocumentTemplate(id)
      if (res.data.success && res.data.data) {
        return res.data.data
      }
      return null
    } catch (err: unknown) {
      notify.error(extractMessage(err) || 'Failed to load document template')
      return null
    }
  }

  async function updateTemplate(id: string, body: DocumentTemplateUpdateRequest): Promise<boolean> {
    try {
      const res = await intakeApi.updateDocumentTemplate(id, body)
      if (res.data.success) {
        notify.success(`Updated "${body.name}"`)
        await fetchTemplates()
        return true
      }
      return false
    } catch (err: unknown) {
      notify.error(extractMessage(err) || 'Failed to update document template')
      return false
    }
  }

  async function deleteTemplate(id: string): Promise<boolean> {
    try {
      const res = await intakeApi.deleteDocumentTemplate(id)
      if (res.data.success) {
        notify.success('Document template deleted')
        await fetchTemplates()
        return true
      }
      return false
    } catch (err: unknown) {
      notify.error(extractMessage(err) || 'Failed to delete document template')
      return false
    }
  }

  async function restoreTemplate(id: string): Promise<boolean> {
    try {
      const res = await intakeApi.restoreDocumentTemplate(id)
      if (res.data.success) {
        notify.success('Document template restored')
        await fetchTemplates()
        return true
      }
      return false
    } catch (err: unknown) {
      notify.error(extractMessage(err) || 'Failed to restore document template')
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
    restoreTemplate, setIncludeDeleted,
    reset,
  }
})
