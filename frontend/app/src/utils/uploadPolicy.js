// Shared upload constraints for student documents. Mirror of the
// backend Odin.Api.Base.Documents.DocumentUploadPolicy — keep in sync.

export const ACCEPTED_DOC_MIMES = [
  'application/pdf',
  'image/jpeg',
  'image/png',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
]

// `accept` attribute for <input type="file"> — include both the MIME
// types and the .docx extension so OS file pickers that key off the
// extension (rather than the sniffed MIME) still expose Word docs.
export const ACCEPTED_DOC_ACCEPT_ATTR =
  [...ACCEPTED_DOC_MIMES, '.docx'].join(',')

export const ACCEPTED_DOC_HUMAN = 'PDF, JPG, PNG, or DOCX'
