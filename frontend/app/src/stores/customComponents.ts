// @ts-nocheck — ported SysCase file; TS strict cleanup is a follow-up
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { ComponentType, LocalizableText } from '@quvian/shared/types/questionnaire'

export interface CustomComponent {
  id: string
  name: LocalizableText
  description?: LocalizableText
  icon: string
  items: any[] // The form fields that make up this component
  previewText?: string
  category?: string
  title: LocalizableText // For compatibility with componentRegistry
  isSystem?: boolean
  createdAt?: string
  updatedAt?: string
}

export const useCustomComponentsStore = defineStore('customComponents', () => {
  // State
  const customComponents = ref<CustomComponent[]>([
    // Example custom component
    {
      id: 'custom_company_info',
      name: { fallback: 'Company Information' },
      description: { fallback: 'Complete company details form' },
      icon: 'mdi-office-building',
      items: [
        {
          id: 'company_name',
          type: 'text',
          label: { fallback: 'Company Name' },
          required: true,
          validation: []
        },
        {
          id: 'company_size',
          type: 'select',
          label: { fallback: 'Company Size' },
          required: false,
          props: {
            options: [
              { value: '1-10', label: '1-10 employees' },
              { value: '11-50', label: '11-50 employees' },
              { value: '51-200', label: '51-200 employees' },
              { value: '201-1000', label: '201-1000 employees' },
              { value: '1000+', label: '1000+ employees' }
            ]
          },
          validation: []
        }
      ],
      previewText: '2 fields',
      category: 'custom',
      title: { fallback: 'Company Information' }
    }
  ])

  // Getters
  const allCustomComponents = computed(() => customComponents.value)

  const customComponentsCount = computed(() => customComponents.value.length)

  // Convert custom components to component registry format
  const customComponentsRegistry = computed(() => {
    const registry: Record<string, any> = {}

    customComponents.value.forEach(component => {
      registry[component.id] = {
        type: component.id,
        title: component.name,
        description: component.description || { fallback: '' },
        icon: component.icon,
        category: 'custom',
        properties: {},
        defaultProps: {
          label: component.defaultLabel,
          placeholder: component.defaultPlaceholder,
          required: component.required,
          ...component.customProps
        },
        validation: component.validation,
        accessibility: {}
      }
    })

    return registry
  })

  // Actions
  const addCustomComponent = (component: Omit<CustomComponent, 'id' | 'category' | 'title'>) => {
    const newComponent: CustomComponent = {
      ...component,
      id: `custom_${Date.now()}`,
      category: 'custom',
      title: component.name,
      previewText: component.previewText || `${component.items?.length || 0} fields`
    }

    customComponents.value.push(newComponent)
    saveToLocalStorage()
    return newComponent
  }

  const updateCustomComponent = (id: string, updates: Partial<CustomComponent>) => {
    const index = customComponents.value.findIndex(c => c.id === id)
    if (index !== -1) {
      customComponents.value[index] = {
        ...customComponents.value[index],
        ...updates,
        title: updates.name || customComponents.value[index].title
      }
      saveToLocalStorage()
    }
  }

  const deleteCustomComponent = (id: string) => {
    const index = customComponents.value.findIndex(c => c.id === id)
    if (index !== -1) {
      customComponents.value.splice(index, 1)
      saveToLocalStorage()
    }
  }

  const duplicateCustomComponent = (id: string) => {
    const original = customComponents.value.find(c => c.id === id)
    if (original) {
      const duplicated: CustomComponent = {
        ...original,
        id: `custom_${Date.now()}`,
        name: { fallback: `${original.name.fallback} (Copy)` },
        title: { fallback: `${original.name.fallback} (Copy)` }
      }
      customComponents.value.push(duplicated)
      saveToLocalStorage()
      return duplicated
    }
    return null
  }

  // Persistence
  const saveToLocalStorage = () => {
    try {
      localStorage.setItem('customComponents', JSON.stringify(customComponents.value))
    } catch (error) {
      console.warn('Failed to save custom components to localStorage:', error)
    }
  }

  const loadFromLocalStorage = () => {
    try {
      const saved = localStorage.getItem('customComponents')
      if (saved) {
        const parsed = JSON.parse(saved)
        // Merge with existing components, avoiding duplicates
        parsed.forEach((savedComponent: CustomComponent) => {
          const exists = customComponents.value.find(c => c.id === savedComponent.id)
          if (!exists) {
            customComponents.value.push(savedComponent)
          }
        })
      }
    } catch (error) {
      console.warn('Failed to load custom components from localStorage:', error)
    }
  }

  const clearCustomComponents = () => {
    customComponents.value = []
    saveToLocalStorage()
  }

  // Initialize from localStorage
  loadFromLocalStorage()

  return {
    // State
    customComponents,

    // Getters
    allCustomComponents,
    customComponentsCount,
    customComponentsRegistry,

    // Actions
    addCustomComponent,
    updateCustomComponent,
    deleteCustomComponent,
    duplicateCustomComponent,
    saveToLocalStorage,
    loadFromLocalStorage,
    clearCustomComponents
  }
})