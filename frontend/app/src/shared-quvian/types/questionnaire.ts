// Core questionnaire types following the specification

export interface LocalizableText {
  key?: string
  fallback: string
}

export interface ValidationRule {
  type: 'required' | 'minLength' | 'maxLength' | 'pattern' | 'email' | 'number' | 'custom'
  value?: any
  message: LocalizableText
  async?: boolean
}

export interface LogicExpression {
  type: 'equals' | 'notEquals' | 'contains' | 'greaterThan' | 'lessThan' | 'and' | 'or' | 'not'
  field?: string
  value?: any
  operands?: LogicExpression[]
}

export interface LogicAction {
  target: string
  action: 'show' | 'hide' | 'enable' | 'disable' | 'require' | 'optional' | 'setValue' | 'validate'
  args?: any
}

export interface LogicRule {
  when: LogicExpression
  then: LogicAction[]
}

export interface LayoutHints {
  width?: 'full' | 'half' | 'third' | 'quarter'
  order?: number
  breakAfter?: boolean
}

export type ComponentType =
  | 'text' | 'textarea' | 'richtext' | 'number' | 'email' | 'phone' | 'date' | 'time' | 'datetime'
  | 'select' | 'radio' | 'checkbox' | 'toggle' | 'file-upload'
  | 'address' | 'repeater' | 'heading' | 'paragraph' | 'divider' | 'consent'
  // Owner-set reference fields (filled in the builder, hidden from respondents)
  | 'refSchool' | 'refPartner' | 'refPartnerProgramme' | 'refText'

export interface ComponentSchema {
  type: ComponentType
  title: LocalizableText
  description: LocalizableText
  icon: string
  category: 'input' | 'selection' | 'upload' | 'group' | 'display' | 'special'
  properties: Record<string, any> // Schema for the inspector
  defaultProps: Record<string, any>
  validation: ValidationRule[]
  accessibility: {
    role?: string
    describedBy?: string[]
  }
}

export interface ConditionalVisibility {
  dependsOn: string // ID of the field this condition depends on
  operator: 'equals' | 'notEquals' | 'contains' | 'notContains' | 'isEmpty' | 'isNotEmpty' |
           'greaterThan' | 'lessThan' | 'greaterOrEqual' | 'lessOrEqual' | 'startsWith' | 'endsWith'
  value: any // The value to compare against
  action: 'show' | 'hide' // What to do when condition is met
}

export interface QuestionnaireItem {
  id: string
  type: ComponentType
  props: Record<string, any>
  validation?: ValidationRule[]
  logic?: LogicRule[]
  conditions?: ConditionalVisibility[] // Simple show/hide conditions
  layout?: LayoutHints
  label?: LocalizableText
  description?: LocalizableText
  required?: boolean
}

export interface QuestionnaireGroup {
  id: string
  title?: LocalizableText
  description?: LocalizableText
  items: QuestionnaireItem[]
  layout?: LayoutHints
  isRepeater?: boolean
  repeaterConfig?: {
    minInstances?: number
    maxInstances?: number
    addButtonText?: LocalizableText
    removeButtonText?: LocalizableText
    instanceTitleTemplate?: string // e.g., "Workplace {index}"
  }
  // Renderer-managed runtime state for isRepeater groups: each instance
  // is a clone of the group's items with re-prefixed ids (the renderer
  // builds { id, title, items }, never a values map). Persisted only on a
  // draft snapshot, not on the canonical questionnaire definition.
  repeaterInstances?: Array<{ id: string; title?: LocalizableText; items: QuestionnaireItem[] }>
  templateId?: string // Reference to saved group template
  isTemplate?: boolean // Mark as reusable template
}

export interface GroupTemplate {
  id: string
  name: LocalizableText
  title?: LocalizableText
  description?: LocalizableText
  category?: string
  icon: string
  items?: QuestionnaireItem[] // Legacy support
  group?: QuestionnaireGroup // New template structure
  previewText?: string
  tags?: string[]
  isSystem?: boolean // True for system templates, false/undefined for user templates
  createdAt?: string
  updatedAt?: string
}

// Custom component shape used by the questionnaire builder's
// CustomComponentsList / QuestionnaireCustomComponents screens.
// Loose by design: it's the bag of properties the SysCase builder
// reads/writes — types/questionnaire.ts is shared with that code.
export interface CustomComponent {
  id: string
  name: LocalizableText
  title?: LocalizableText
  description?: LocalizableText
  category?: string
  icon: string
  items?: QuestionnaireItem[]
  group?: QuestionnaireGroup
  previewText?: string
  isSystem?: boolean
  createdAt?: string
  updatedAt?: string
}

export interface QuestionnaireSection {
  id: string
  title?: LocalizableText
  description?: LocalizableText
  groups: QuestionnaireGroup[]
}

export interface QuestionnairePage {
  id: string
  title: LocalizableText
  description?: LocalizableText
  sections: QuestionnaireSection[]
}

export interface QuestionnaireMetadata {
  author?: string
  tags?: string[]
  category?: string
  estimatedDuration?: number // minutes
  version?: string
}

export interface Questionnaire {
  version: string
  id: string
  name: LocalizableText
  description?: LocalizableText
  metadata?: QuestionnaireMetadata
  pages: QuestionnairePage[]
  logic?: LogicRule[]
  theming?: Record<string, any>
  createdAt?: string
  updatedAt?: string
}

export interface QuestionnaireResponse {
  questionnaireId: string
  version: string
  submittedAt: string
  answers: Record<string, any>
  metadata?: {
    duration?: number
    userAgent?: string
    locale?: string
  }
}

export interface ValidationError {
  field: string
  code: string
  message: string
  value?: any
}

export interface ComponentRegistry {
  [type: string]: ComponentSchema
}

// Component-specific prop types
export interface TextInputProps {
  placeholder?: string
  maxLength?: number
  mask?: string
}

export interface SelectProps {
  options: Array<{ value: string; label: LocalizableText; disabled?: boolean }>
  multiple?: boolean
  searchable?: boolean
  placeholder?: string
}

export interface FileUploadProps {
  accept?: string
  maxSize?: number // bytes
  maxFiles?: number
  multiple?: boolean
}

export interface AddressProps {
  fields: Array<'street' | 'city' | 'state' | 'zip' | 'country'>
  defaultCountry?: string
  validateAddress?: boolean
}

export interface RepeaterProps {
  minItems?: number
  maxItems?: number
  addButtonText?: LocalizableText
  removeButtonText?: LocalizableText
  template: QuestionnaireItem[]
}

// Builder-specific types
export interface BuilderState {
  questionnaire: Questionnaire | null
  selectedItemId: string | null
  clipboard: QuestionnaireItem | null
  undoStack: Questionnaire[]
  redoStack: Questionnaire[]
}

export interface DragDropData {
  type: 'component' | 'item'
  componentType?: ComponentType
  item?: QuestionnaireItem
  sourcePageIndex?: number
  sourceSectionIndex?: number
  sourceItemIndex?: number
}

// Renderer-specific types
export interface RendererState {
  answers: Record<string, any>
  errors: Record<string, ValidationError[]>
  touched: Record<string, boolean>
  currentPage: number
  isSubmitting: boolean
}

export interface RendererEvents {
  change: (fieldId: string, value: any) => void
  validate: (fieldId?: string) => void
  submit: (response: QuestionnaireResponse) => void
  pageChange: (pageIndex: number) => void
  error: (error: Error) => void
}