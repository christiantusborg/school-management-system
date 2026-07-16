// Minimal shim of @quvian/shared/api/types for the ported intake feature.
// Only the shapes the copied questionnaire/text-template code imports.
export interface TextTemplate {
  id: string
  name: string
  subject?: string
  content: string
  wordCount: number
  questionnaireId?: string
  createdAt: string
  updatedAt: string
  userId: string
}

export interface CreateTextTemplateRequest {
  name: string
  subject?: string
  content: string
  questionnaireId?: string
}

export interface UpdateTextTemplateRequest {
  name?: string
  subject?: string
  content?: string
  questionnaireId?: string
}
