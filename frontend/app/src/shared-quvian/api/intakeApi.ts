import api from './axiosInstance'
import type { ApiResponse } from '../types'

// ADR-0039 IntakeApi client. Phase 2a ships the firm-wide
// QuestionnaireTemplate list + create endpoints; case-local CRUD and
// the in-case usage surfaces land in later phases.

export interface QuestionnaireTemplateListItem {
  questionnaireTemplateId: string
  name: string
  version: string
  definitionHash: string
  isFirmLibrary: boolean
  groupId: string | null
  createdByUserId: string
  createdAt: string
  modifiedAt: string
  deletedAt: string | null
}

export interface QuestionnaireTemplateListResponse {
  items: QuestionnaireTemplateListItem[]
}

export interface QuestionnaireTemplateCreateRequest {
  name: string
  version?: string
  definitionJson: string
}

export interface QuestionnaireTemplateCreateResponse {
  questionnaireTemplateId: string
  name: string
  version: string
  definitionHash: string
  createdAt: string
}

export interface QuestionnaireTemplateGetResponse {
  questionnaireTemplateId: string
  name: string
  version: string
  definitionJson: string
  definitionHash: string
  isFirmLibrary: boolean
  groupId: string | null
  createdByUserId: string
  createdAt: string
  modifiedAt: string
  deletedAt: string | null
}

export interface QuestionnaireTemplateUpdateRequest {
  name: string
  version?: string
  definitionJson: string
}

export interface QuestionnaireTemplateUpdateResponse {
  questionnaireTemplateId: string
  name: string
  version: string
  definitionHash: string
  modifiedAt: string
}

export const intakeApi = {
  listQuestionnaireTemplates: (includeDeleted = false) =>
    api.get<ApiResponse<QuestionnaireTemplateListResponse>>(
      `/intake/questionnaire-templates`, { params: { includeDeleted } }),

  createQuestionnaireTemplate: (body: QuestionnaireTemplateCreateRequest) =>
    api.post<ApiResponse<QuestionnaireTemplateCreateResponse>>(
      `/intake/questionnaire-templates`, body),

  getQuestionnaireTemplate: (id: string) =>
    api.get<ApiResponse<QuestionnaireTemplateGetResponse>>(
      `/intake/questionnaire-templates/${id}`),

  updateQuestionnaireTemplate: (id: string, body: QuestionnaireTemplateUpdateRequest) =>
    api.put<ApiResponse<QuestionnaireTemplateUpdateResponse>>(
      `/intake/questionnaire-templates/${id}`, body),

  deleteQuestionnaireTemplate: (id: string) =>
    api.delete<ApiResponse<{ questionnaireTemplateId: string; deletedAt: string }>>(
      `/intake/questionnaire-templates/${id}`),

  restoreQuestionnaireTemplate: (id: string) =>
    api.post<ApiResponse<{ questionnaireTemplateId: string; modifiedAt: string }>>(
      `/intake/questionnaire-templates/${id}/restore`),

  // In-case Intake instances (ADR-0039 §1 usage plane).
  listIntakeInstances: (groupId: string, includeDeleted = false) =>
    api.get<ApiResponse<IntakeInstanceListResponse>>(
      `/groups/${groupId}/intake-instances`, { params: { includeDeleted } }),

  // Reverse view: tasks that link to this intake form (ADR-0048 links).
  linkedFrom: (intakeInstanceId: string) =>
    api.get<ApiResponse<IntakeLinkedFromResponse>>(
      `/intake-instances/${intakeInstanceId}/linked-from`),

  createIntakeInstance: (groupId: string, body: IntakeInstanceCreateRequest) =>
    api.post<ApiResponse<IntakeInstanceCreateResponse>>(
      `/groups/${groupId}/intake-instances`, body),

  restoreIntakeInstance: (intakeInstanceId: string) =>
    api.post<ApiResponse<{ intakeInstanceId: string; modifiedAt: string }>>(
      `/intake-instances/${intakeInstanceId}/restore`),

  deleteIntakeInstance: (intakeInstanceId: string) =>
    api.delete<ApiResponse<{ intakeInstanceId: string; deletedAt: string }>>(
      `/intake-instances/${intakeInstanceId}`),

  updateIntakeInstance: (intakeInstanceId: string, body: {
    classification: string
    outputProfileJson: string
  }) =>
    api.put<ApiResponse<{ intakeInstanceId: string; modifiedAt: string }>>(
      `/intake-instances/${intakeInstanceId}`, body),

  // IntakeResponse (Phase 3c backend).
  listIntakeResponses: (instanceId: string, includeDeleted = false) =>
    api.get<ApiResponse<IntakeResponseListResponse>>(
      `/intake-instances/${instanceId}/responses`, { params: { includeDeleted } }),

  createIntakeResponse: (instanceId: string, body: IntakeResponseCreateRequest) =>
    api.post<ApiResponse<IntakeResponseCreateResponse>>(
      `/intake-instances/${instanceId}/responses`, body),

  submitIntakeResponse: (responseId: string) =>
    api.post<ApiResponse<IntakeResponseSubmitResponse>>(
      `/intake-responses/${responseId}/submit`),

  getIntakeResponse: (responseId: string) =>
    api.get<ApiResponse<IntakeResponseGetResponse>>(
      `/intake-responses/${responseId}`),

  // Deeplink ("magic link") tokens: lawyer mints one per
  // IntakeResponse, hands it to the client; the client opens the
  // public renderer at /intake/link/<token>. Raw token is returned
  // exactly once at create time; only the hash is persisted.
  listIntakeResponseAccessTokens: (responseId: string) =>
    api.get<ApiResponse<{
      items: Array<{
        intakeResponseAccessTokenId: string
        createdAt: string
        expiresAt: string
        lastUsedAt: string | null
        revokedAt: string | null
      }>
      total: number
    }>>(`/intake-responses/${responseId}/access-tokens`),

  revokeIntakeResponseAccessToken: (responseId: string, tokenId: string) =>
    api.delete<ApiResponse<{
      intakeResponseAccessTokenId: string
      revokedAt: string
    }>>(`/intake-responses/${responseId}/access-tokens/${tokenId}`),

  createIntakeResponseAccessToken: (responseId: string, expiresInDays: number) =>
    api.post<ApiResponse<{
      intakeResponseAccessTokenId: string
      intakeResponseId: string
      token: string
      expiresAt: string
    }>>(`/intake-responses/${responseId}/access-tokens`,
      { expiresInDays }),

  redeemIntakeResponseAccessToken: (token: string) =>
    api.post<ApiResponse<{
      intakeResponseId: string
      intakeInstanceId: string
      questionnaireTemplateId: string | null
      definitionJson: string
      expiresAt: string
      existingDraftPayloadJson: string | null
    }>>(`/intake/access-tokens/redeem`, { token }),

  submitIntakeResponseViaAccessToken: (token: string, payloadJson: string, final: boolean) =>
    api.post<ApiResponse<{
      intakeLinkSubmissionId: string
      intakeResponseId: string
      final: boolean
      savedAt: string
    }>>(`/intake/access-tokens/submit`, { token, payloadJson, final }),

  // ADR-0039 §6 IntakeResponseContactLinks (Phase 7 backend) — binds
  // a PartyKey inside a response (e.g. "petitioner") to a Contact on
  // the case so downstream document generation can reference real
  // contact rows instead of free-text answers.
  listContactLinks: (responseId: string) =>
    api.get<ApiResponse<IntakeResponseContactLinksListResponse>>(
      `/intake-responses/${responseId}/contact-links`),

  createContactLink: (responseId: string, body: IntakeResponseContactLinkCreateRequest) =>
    api.post<ApiResponse<IntakeResponseContactLinkCreateResponse>>(
      `/intake-responses/${responseId}/contact-links`, body),

  // DocumentTemplate firm-library CRUD (Phase 8a + 8b backend).
  listDocumentTemplates: (includeDeleted = false) =>
    api.get<ApiResponse<DocumentTemplateListResponse>>(
      `/intake/document-templates`, { params: { includeDeleted } }),

  createDocumentTemplate: (body: DocumentTemplateCreateRequest) =>
    api.post<ApiResponse<DocumentTemplateCreateResponse>>(
      `/intake/document-templates`, body),

  getDocumentTemplate: (id: string) =>
    api.get<ApiResponse<DocumentTemplateGetResponse>>(
      `/intake/document-templates/${id}`),

  updateDocumentTemplate: (id: string, body: DocumentTemplateUpdateRequest) =>
    api.put<ApiResponse<DocumentTemplateUpdateResponse>>(
      `/intake/document-templates/${id}`, body),

  deleteDocumentTemplate: (id: string) =>
    api.delete<ApiResponse<{ documentTemplateId: string; deletedAt: string }>>(
      `/intake/document-templates/${id}`),

  restoreDocumentTemplate: (id: string) =>
    api.post<ApiResponse<{ documentTemplateId: string; modifiedAt: string }>>(
      `/intake/document-templates/${id}/restore`),

  uploadDocumentTemplateAsset: (id: string, body: {
    filename: string
    contentType: string
    bytesBase64: string
  }) =>
    api.post<ApiResponse<{
      documentTemplateAssetId: string
      documentTemplateId: string
      filename: string
      contentType: string
      sizeBytes: number
      modifiedAt: string
    }>>(`/intake/document-templates/${id}/asset`, body),

  getDocumentTemplateAsset: (id: string) =>
    api.get<ApiResponse<{
      documentTemplateAssetId: string
      documentTemplateId: string
      filename: string
      contentType: string
      bytesBase64: string
      sizeBytes: number
      modifiedAt: string
    }>>(`/intake/document-templates/${id}/asset`),

  // DocumentTemplateImage firm-library bank: one upload, referenced by
  // every strategy's editor via its asset id. Not encrypted: these are
  // authoring assets the firm uploads once.
  listDocumentTemplateImages: (includeDeleted = false) =>
    api.get<ApiResponse<{
      items: Array<{
        documentTemplateImageId: string
        name: string
        mimeType: string
        sizeBytes: number
        uploadedAt: string
        deletedAt: string | null
      }>
      total: number
    }>>(`/intake/document-template-images`, { params: { includeDeleted } }),

  uploadDocumentTemplateImage: (body: {
    name: string
    mimeType: string
    bytesBase64: string
  }) =>
    api.post<ApiResponse<{
      documentTemplateImageId: string
      name: string
      mimeType: string
      sizeBytes: number
      uploadedAt: string
    }>>(`/intake/document-template-images`, body),

  getDocumentTemplateImageFile: (id: string) =>
    api.get<ApiResponse<{
      documentTemplateImageId: string
      name: string
      mimeType: string
      bytesBase64: string
    }>>(`/intake/document-template-images/${id}/file`),

  deleteDocumentTemplateImage: (id: string) =>
    api.delete<ApiResponse<{
      documentTemplateImageId: string
      deletedAt: string
    }>>(`/intake/document-template-images/${id}`),

  pdfServiceExtractFields: (body: { bytesBase64: string; filename?: string }) =>
    api.post<ApiResponse<{
      count: number
      fields: Array<{
        name: string
        type: string
        page: number
        rect?: number[]
        choices?: string[]
        isReadonly: boolean
      }>
    }>>(`/intake/pdf-service/extract-fields`, body),

  // TextTemplate firm-library CRUD (Phase 8c backend).
  listTextTemplates: (includeDeleted = false) =>
    api.get<ApiResponse<TextTemplateListResponse>>(
      `/intake/text-templates`, { params: { includeDeleted } }),

  createTextTemplate: (body: TextTemplateCreateRequest) =>
    api.post<ApiResponse<TextTemplateCreateResponse>>(
      `/intake/text-templates`, body),

  getTextTemplate: (id: string) =>
    api.get<ApiResponse<TextTemplateGetResponse>>(
      `/intake/text-templates/${id}`),

  updateTextTemplate: (id: string, body: TextTemplateUpdateRequest) =>
    api.put<ApiResponse<TextTemplateUpdateResponse>>(
      `/intake/text-templates/${id}`, body),

  deleteTextTemplate: (id: string) =>
    api.delete<ApiResponse<{ textTemplateId: string; deletedAt: string }>>(
      `/intake/text-templates/${id}`),

  restoreTextTemplate: (id: string) =>
    api.post<ApiResponse<{ textTemplateId: string; modifiedAt: string }>>(
      `/intake/text-templates/${id}/restore`),

  // FieldLibraryEntry firm-wide CRUD (Phase 8d backend).
  listFieldLibraryEntries: (includeDeleted = false) =>
    api.get<ApiResponse<FieldLibraryEntryListResponse>>(
      `/intake/field-library-entries`, { params: { includeDeleted } }),

  createFieldLibraryEntry: (body: FieldLibraryEntryCreateRequest) =>
    api.post<ApiResponse<FieldLibraryEntryCreateResponse>>(
      `/intake/field-library-entries`, body),

  getFieldLibraryEntry: (id: string) =>
    api.get<ApiResponse<FieldLibraryEntryGetResponse>>(
      `/intake/field-library-entries/${id}`),

  updateFieldLibraryEntry: (id: string, body: FieldLibraryEntryUpdateRequest) =>
    api.put<ApiResponse<FieldLibraryEntryUpdateResponse>>(
      `/intake/field-library-entries/${id}`, body),

  deleteFieldLibraryEntry: (id: string) =>
    api.delete<ApiResponse<{ fieldLibraryEntryId: string; deletedAt: string }>>(
      `/intake/field-library-entries/${id}`),

  restoreFieldLibraryEntry: (id: string) =>
    api.post<ApiResponse<{ fieldLibraryEntryId: string; modifiedAt: string }>>(
      `/intake/field-library-entries/${id}/restore`),

  // GenerationRule firm-library CRUD (Phase 4 + 8e backend).
  listGenerationRules: (includeDeleted = false) =>
    api.get<ApiResponse<GenerationRuleListResponse>>(
      `/intake/generation-rules`, { params: { includeDeleted } }),

  createGenerationRule: (body: GenerationRuleCreateRequest) =>
    api.post<ApiResponse<GenerationRuleCreateResponse>>(
      `/intake/generation-rules`, body),

  getGenerationRule: (id: string) =>
    api.get<ApiResponse<GenerationRuleGetResponse>>(
      `/intake/generation-rules/${id}`),

  updateGenerationRule: (id: string, body: GenerationRuleUpdateRequest) =>
    api.put<ApiResponse<GenerationRuleUpdateResponse>>(
      `/intake/generation-rules/${id}`, body),

  deleteGenerationRule: (id: string) =>
    api.delete<ApiResponse<{ generationRuleId: string; deletedAt: string }>>(
      `/intake/generation-rules/${id}`),

  restoreGenerationRule: (id: string) =>
    api.post<ApiResponse<{ generationRuleId: string; modifiedAt: string }>>(
      `/intake/generation-rules/${id}/restore`),
}

// ---- GenerationRule shapes (Phase 4 + 8e backend) ----

export interface GenerationRuleListItem {
  generationRuleId: string
  name: string
  ruleJson: string
  includeDocumentTemplateIdsCsv: string
  isFirmLibrary: boolean
  groupId: string | null
  createdByUserId: string
  createdAt: string
  modifiedAt: string
  deletedAt: string | null
}

export interface GenerationRuleListResponse {
  items: GenerationRuleListItem[]
}

export interface GenerationRuleCreateRequest {
  name: string
  ruleJson: string
  includeDocumentTemplateIdsCsv?: string
}

export interface GenerationRuleCreateResponse {
  generationRuleId: string
  name: string
  createdAt: string
}

export interface GenerationRuleGetResponse {
  generationRuleId: string
  name: string
  ruleJson: string
  includeDocumentTemplateIdsCsv: string
  isFirmLibrary: boolean
  groupId: string | null
  createdByUserId: string
  createdAt: string
  modifiedAt: string
  deletedAt: string | null
}

export interface GenerationRuleUpdateRequest {
  name: string
  ruleJson: string
  includeDocumentTemplateIdsCsv?: string
}

export interface GenerationRuleUpdateResponse {
  generationRuleId: string
  name: string
  modifiedAt: string
}

// ---- FieldLibraryEntry shapes (Phase 8d backend) ----

export interface FieldLibraryEntryListItem {
  fieldLibraryEntryId: string
  name: string
  category: string
  createdByUserId: string
  createdAt: string
  modifiedAt: string
  deletedAt: string | null
}

export interface FieldLibraryEntryListResponse {
  items: FieldLibraryEntryListItem[]
}

export interface FieldLibraryEntryCreateRequest {
  name: string
  category?: string
  definitionJson: string
}

export interface FieldLibraryEntryCreateResponse {
  fieldLibraryEntryId: string
  name: string
  category: string
  createdAt: string
}

export interface FieldLibraryEntryGetResponse {
  fieldLibraryEntryId: string
  name: string
  category: string
  definitionJson: string
  createdByUserId: string
  createdAt: string
  modifiedAt: string
  deletedAt: string | null
}

export interface FieldLibraryEntryUpdateRequest {
  name: string
  category?: string
  definitionJson: string
}

export interface FieldLibraryEntryUpdateResponse {
  fieldLibraryEntryId: string
  name: string
  category: string
  modifiedAt: string
}

// ---- TextTemplate shapes (Phase 8c backend) ----

export interface TextTemplateListItem {
  textTemplateId: string
  name: string
  isFirmLibrary: boolean
  groupId: string | null
  createdByUserId: string
  createdAt: string
  modifiedAt: string
  deletedAt: string | null
}

export interface TextTemplateListResponse {
  items: TextTemplateListItem[]
}

export interface TextTemplateCreateRequest {
  name: string
  bodyJson: string
}

export interface TextTemplateCreateResponse {
  textTemplateId: string
  name: string
  createdAt: string
}

export interface TextTemplateGetResponse {
  textTemplateId: string
  name: string
  bodyJson: string
  isFirmLibrary: boolean
  groupId: string | null
  createdByUserId: string
  createdAt: string
  modifiedAt: string
  deletedAt: string | null
}

export interface TextTemplateUpdateRequest {
  name: string
  bodyJson: string
}

export interface TextTemplateUpdateResponse {
  textTemplateId: string
  name: string
  modifiedAt: string
}

// ---- DocumentTemplate shapes (Phase 8a + 8b backend) ----

export type DocumentStrategy = 'Generate' | 'Overlay' | 'AcroFormFill' | 'Canvas'

export interface DocumentTemplateListItem {
  documentTemplateId: string
  name: string
  strategy: DocumentStrategy
  baseAssetRef: string | null
  isFirmLibrary: boolean
  groupId: string | null
  createdByUserId: string
  createdAt: string
  modifiedAt: string
  deletedAt: string | null
}

export interface DocumentTemplateListResponse {
  items: DocumentTemplateListItem[]
}

export interface DocumentTemplateCreateRequest {
  name: string
  strategy: DocumentStrategy
  baseAssetRef?: string | null
  mappingJson?: string
}

export interface DocumentTemplateCreateResponse {
  documentTemplateId: string
  name: string
  strategy: DocumentStrategy
  baseAssetRef: string | null
  createdAt: string
}

export interface DocumentTemplateGetResponse {
  documentTemplateId: string
  name: string
  strategy: DocumentStrategy
  baseAssetRef: string | null
  mappingJson: string
  isFirmLibrary: boolean
  groupId: string | null
  createdByUserId: string
  createdAt: string
  modifiedAt: string
  deletedAt: string | null
}

export interface DocumentTemplateUpdateRequest {
  name: string
  strategy: DocumentStrategy
  baseAssetRef?: string | null
  mappingJson?: string
}

export interface DocumentTemplateUpdateResponse {
  documentTemplateId: string
  name: string
  strategy: DocumentStrategy
  baseAssetRef: string | null
  modifiedAt: string
}

export interface IntakeResponseSubmitResponse {
  intakeResponseId: string
  lifecycleState: string
  submittedAt: string
  submittedByUserId: string
  questionnaireVersionHash: string
}

export interface IntakeResponseContactLink {
  intakeResponseContactLinkId: string
  intakeResponseId: string
  contactId: string
  partyKey: string
  createdAt: string
  deletedAt: string | null
}

export interface IntakeResponseContactLinksListResponse {
  intakeResponseId: string
  items: IntakeResponseContactLink[]
}

export interface IntakeResponseContactLinkCreateRequest {
  contactId: string
  partyKey: string
}

export interface IntakeResponseContactLinkCreateResponse {
  intakeResponseContactLinkId: string
  intakeResponseId: string
  contactId: string
  partyKey: string
  createdAt: string
}

export interface IntakeResponseGetRecipient {
  recipientType: 'User' | 'Group' | 'SaEscrow' | 'Classification'
  recipientUserId?: string | null
  recipientGroupId?: string | null
  groupKeyVersion?: number | null
  kemCiphertext: string
  encryptedPayload: string
}

export interface IntakeResponseGetResponse {
  intakeResponseId: string
  intakeInstanceId: string
  lifecycleState: string
  submittedAt: string | null
  submittedByUserId: string | null
  questionnaireVersionHash: string | null
  signature: string | null         // base64 of 64-byte Ed25519 signature
  signedKeyVersion: number | null
  createdByUserId: string
  createdAt: string
  modifiedAt: string
  deletedAt: string | null
  recipients: IntakeResponseGetRecipient[]
}

export interface IntakeResponseRecipientInput {
  recipientType: 'User' | 'Group' | 'SaEscrow' | 'Classification'
  recipientUserId?: string
  recipientGroupId?: string
  groupKeyVersion?: number
  kemCiphertext: string
  encryptedPayload: string
}

export interface IntakeResponseCreateRequest {
  recipients: IntakeResponseRecipientInput[]
  signature?: string         // base64 of 64-byte Ed25519 signature
  signedKeyVersion?: number  // matching UserSigningKey.KeyVersion
}

export interface IntakeResponseCreateResponse {
  intakeResponseId: string
  intakeInstanceId: string
  lifecycleState: string
  createdAt: string
}

// ---- IntakeResponse shapes (Phase 3c backend) ----

export interface IntakeResponseListItem {
  intakeResponseId: string
  intakeInstanceId: string
  lifecycleState: string
  submittedAt: string | null
  submittedByUserId: string | null
  questionnaireVersionHash: string | null
  createdByUserId: string
  createdAt: string
  modifiedAt: string
  deletedAt: string | null
}

export interface IntakeResponseListResponse {
  intakeInstanceId: string
  items: IntakeResponseListItem[]
}

// ---- IntakeInstance shapes (Phase 3a backend) ----

export interface IntakeInstanceListItem {
  intakeInstanceId: string
  groupId: string
  questionnaireTemplateId: string | null
  hasInlineDefinition: boolean
  outputProfileJson: string
  classification: string
  createdByUserId: string
  createdAt: string
  modifiedAt: string
  deletedAt: string | null
}

export interface IntakeInstanceListResponse {
  groupId: string
  items: IntakeInstanceListItem[]
}

export interface IntakeLinkedTaskRef {
  linkId: string
  taskItemId: string
  status: string
  dueDate?: string | null
}

export interface IntakeLinkedFromResponse {
  tasks: IntakeLinkedTaskRef[]
}

export interface IntakeInstanceCreateRequest {
  questionnaireTemplateId?: string | null
  inlineDefinitionJson?: string | null
  outputProfileJson?: string
  classification: string
}

export interface IntakeInstanceCreateResponse {
  intakeInstanceId: string
  groupId: string
  classification: string
  createdAt: string
}
