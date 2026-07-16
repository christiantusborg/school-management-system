// ADR-0039 §3 PDF "Generate" strategy. Takes a DocumentTemplate
// MappingJson + a decrypted answers map, renders a fresh PDF with
// jsPDF (already approved for invoice rendering, ADR-0031), and
// returns the resulting Blob. Runs entirely in the browser AFTER the
// IntakeResponse has been decrypted; the server never sees the
// plaintext answers OR the rendered bytes.
//
// MappingJson v1 shape (intentionally narrow — the visual mapper UI
// will produce richer JSON later but every field is optional):
//
//   {
//     "title":       "Optional H1 heading at the top of the page",
//     "subtitle":    "Optional smaller line below the title",
//     "fields":      [{ "label": "Client name", "valueFrom": "client.name" }, ...],
//     "blocks":      [
//                      { "type": "heading",   "text": "Petitioner info" },
//                      { "type": "paragraph", "text": "I hereby certify …" },
//                      { "type": "field",     "label": "DOB", "valueFrom": "petitioner.dob" },
//                      { "type": "divider" }
//                    ]
//   }
//
// Both `fields` (simple flat list) and `blocks` (richer ordered
// sequence) are supported; `blocks` wins when present.

import jsPDF from 'jspdf'

export interface IntakeMappingField {
  label?: string
  valueFrom?: string
  literal?: string
}

export interface IntakeMappingBlock {
  type: 'heading' | 'paragraph' | 'field' | 'divider'
  text?: string
  label?: string
  valueFrom?: string
  literal?: string
}

export interface IntakeMapping {
  title?: string
  subtitle?: string
  fields?: IntakeMappingField[]
  blocks?: IntakeMappingBlock[]
}

/** Resolve a dotted path like "client.address.city" against the
 *  decrypted answer map. Returns "" for any miss so rendering stays
 *  deterministic even when an expected field is absent. */
export function lookupAnswer(answers: Record<string, unknown> | null | undefined, path: string): string {
  if (!answers || !path) {
    return ''
  }
  const parts = path.split('.')
  let cursor: unknown = answers
  for (const p of parts) {
    if (cursor && typeof cursor === 'object' && p in (cursor as Record<string, unknown>)) {
      cursor = (cursor as Record<string, unknown>)[p]
    } else {
      return ''
    }
  }
  if (cursor === null || cursor === undefined) {
    return ''
  }
  if (Array.isArray(cursor)) {
    return cursor.map(v => String(v)).join(', ')
  }
  if (typeof cursor === 'object') {
    return JSON.stringify(cursor)
  }
  return String(cursor)
}

export function parseMapping(raw: string): IntakeMapping {
  if (!raw) {
    return {}
  }
  try {
    const parsed = JSON.parse(raw)
    return (parsed && typeof parsed === 'object') ? parsed as IntakeMapping : {}
  } catch {
    return {}
  }
}

interface RenderInput {
  templateName: string
  mapping: IntakeMapping
  answers: Record<string, unknown> | null
}

export function renderGenerateStrategyPdf(input: RenderInput): Blob {
  const doc = new jsPDF({ unit: 'pt', format: 'a4' })
  const margin = 48
  const pageWidth = doc.internal.pageSize.getWidth()
  const pageHeight = doc.internal.pageSize.getHeight()
  const usableWidth = pageWidth - 2 * margin
  let cursorY = margin + 8

  function ensureSpace(needed: number) {
    if (cursorY + needed > pageHeight - margin) {
      doc.addPage()
      cursorY = margin + 8
    }
  }

  function drawTitle(t: string) {
    doc.setFont('helvetica', 'bold')
    doc.setFontSize(20)
    const lines = doc.splitTextToSize(t, usableWidth) as string[]
    for (const line of lines) {
      ensureSpace(26)
      doc.text(line, margin, cursorY)
      cursorY += 24
    }
    cursorY += 4
  }

  function drawSubtitle(t: string) {
    doc.setFont('helvetica', 'normal')
    doc.setFontSize(12)
    doc.setTextColor(110, 110, 110)
    const lines = doc.splitTextToSize(t, usableWidth) as string[]
    for (const line of lines) {
      ensureSpace(16)
      doc.text(line, margin, cursorY)
      cursorY += 14
    }
    doc.setTextColor(0, 0, 0)
    cursorY += 6
  }

  function drawHeading(t: string) {
    doc.setFont('helvetica', 'bold')
    doc.setFontSize(14)
    const lines = doc.splitTextToSize(t, usableWidth) as string[]
    for (const line of lines) {
      ensureSpace(18)
      doc.text(line, margin, cursorY)
      cursorY += 16
    }
    cursorY += 2
  }

  function drawParagraph(t: string) {
    doc.setFont('helvetica', 'normal')
    doc.setFontSize(11)
    const lines = doc.splitTextToSize(t, usableWidth) as string[]
    for (const line of lines) {
      ensureSpace(14)
      doc.text(line, margin, cursorY)
      cursorY += 13
    }
    cursorY += 4
  }

  function drawField(label: string, value: string) {
    doc.setFont('helvetica', 'bold')
    doc.setFontSize(10)
    ensureSpace(12)
    doc.text(label + ':', margin, cursorY)
    doc.setFont('helvetica', 'normal')
    doc.setFontSize(11)
    const lines = doc.splitTextToSize(value || '—', usableWidth - 110) as string[]
    let firstLine = true
    for (const line of lines) {
      if (!firstLine) {
        ensureSpace(13)
      }
      doc.text(line, margin + 110, cursorY)
      cursorY += 13
      firstLine = false
    }
    cursorY += 2
  }

  function drawDivider() {
    ensureSpace(10)
    doc.setDrawColor(220, 220, 220)
    doc.line(margin, cursorY, pageWidth - margin, cursorY)
    cursorY += 12
  }

  if (input.mapping.title) {
    drawTitle(input.mapping.title)
  } else {
    drawTitle(input.templateName)
  }
  if (input.mapping.subtitle) {
    drawSubtitle(input.mapping.subtitle)
  }

  if (input.mapping.blocks && input.mapping.blocks.length > 0) {
    for (const b of input.mapping.blocks) {
      if (b.type === 'heading') {
        drawHeading(b.text ?? '')
      } else if (b.type === 'paragraph') {
        drawParagraph(b.text ?? '')
      } else if (b.type === 'field') {
        const v = b.literal !== undefined ? b.literal : lookupAnswer(input.answers, b.valueFrom ?? '')
        drawField(b.label ?? (b.valueFrom ?? ''), v)
      } else if (b.type === 'divider') {
        drawDivider()
      }
    }
  } else if (input.mapping.fields && input.mapping.fields.length > 0) {
    for (const f of input.mapping.fields) {
      const v = f.literal !== undefined ? f.literal : lookupAnswer(input.answers, f.valueFrom ?? '')
      drawField(f.label ?? (f.valueFrom ?? ''), v)
    }
  } else {
    drawParagraph('(This document template has no fields or blocks defined; '
      + 'add a MappingJson with a "fields" or "blocks" array to render content.)')
  }

  return doc.output('blob') as Blob
}

export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  URL.revokeObjectURL(url)
}
