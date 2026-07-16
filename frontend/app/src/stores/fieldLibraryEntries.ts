// Pinia store for ADR-0039 §1 firm-wide FieldLibraryEntries. Same
// shape as documentTemplates / textTemplates. Entries are reusable
// questionnaire-group building blocks — drop one into a questionnaire
// to inline its DefinitionJson.

import { defineStore } from 'pinia'
import { ref } from 'vue'
import {
  intakeApi,
  type FieldLibraryEntryListItem,
  type FieldLibraryEntryGetResponse,
  type FieldLibraryEntryCreateRequest,
  type FieldLibraryEntryUpdateRequest,
} from '@quvian/shared/api/intakeApi'
import { useNotificationStore } from '@/stores/notification'

export const useFieldLibraryEntriesStore = defineStore('fieldLibraryEntries', () => {
  const notify = useNotificationStore()

  const entries = ref<FieldLibraryEntryListItem[]>([])
  const loading = ref(false)
  const includeDeleted = ref(false)

  async function fetchEntries() {
    loading.value = true
    try {
      const res = await intakeApi.listFieldLibraryEntries(includeDeleted.value)
      if (res.data.success && res.data.data) {
        entries.value = res.data.data.items
      }
    } catch (err: unknown) {
      notify.error(extractMessage(err) || 'Failed to list field library entries')
    } finally {
      loading.value = false
    }
  }

  async function createEntry(body: FieldLibraryEntryCreateRequest): Promise<boolean> {
    try {
      const res = await intakeApi.createFieldLibraryEntry(body)
      if (res.data.success) {
        notify.success(`Created "${body.name}"`)
        await fetchEntries()
        return true
      }
      return false
    } catch (err: unknown) {
      notify.error(extractMessage(err) || 'Failed to create field library entry')
      return false
    }
  }

  async function getEntry(id: string): Promise<FieldLibraryEntryGetResponse | null> {
    try {
      const res = await intakeApi.getFieldLibraryEntry(id)
      if (res.data.success && res.data.data) {
        return res.data.data
      }
      return null
    } catch (err: unknown) {
      notify.error(extractMessage(err) || 'Failed to load field library entry')
      return null
    }
  }

  async function updateEntry(id: string, body: FieldLibraryEntryUpdateRequest): Promise<boolean> {
    try {
      const res = await intakeApi.updateFieldLibraryEntry(id, body)
      if (res.data.success) {
        notify.success(`Updated "${body.name}"`)
        await fetchEntries()
        return true
      }
      return false
    } catch (err: unknown) {
      notify.error(extractMessage(err) || 'Failed to update field library entry')
      return false
    }
  }

  async function deleteEntry(id: string): Promise<boolean> {
    try {
      const res = await intakeApi.deleteFieldLibraryEntry(id)
      if (res.data.success) {
        notify.success('Entry deleted')
        await fetchEntries()
        return true
      }
      return false
    } catch (err: unknown) {
      notify.error(extractMessage(err) || 'Failed to delete field library entry')
      return false
    }
  }

  async function restoreEntry(id: string): Promise<boolean> {
    try {
      const res = await intakeApi.restoreFieldLibraryEntry(id)
      if (res.data.success) {
        notify.success('Entry restored')
        await fetchEntries()
        return true
      }
      return false
    } catch (err: unknown) {
      notify.error(extractMessage(err) || 'Failed to restore field library entry')
      return false
    }
  }

  async function setIncludeDeleted(v: boolean) {
    includeDeleted.value = v
    await fetchEntries()
  }

  function extractMessage(err: unknown): string | undefined {
    return (err as { response?: { data?: { message?: string } } })?.response?.data?.message
      || (err as Error)?.message
  }

  function reset() {
    entries.value = []
    includeDeleted.value = false
  }

  return {
    entries, loading, includeDeleted,
    fetchEntries, createEntry, getEntry, updateEntry, deleteEntry,
    restoreEntry, setIncludeDeleted, reset,
  }
})
