// ADR-0039 §1 GenerationRule expression engine. Each rule carries a
// LogicExpression in its RuleJson; the runtime evaluates the
// expression against the decrypted IntakeResponse answers and, when
// it evaluates true, emits the rule's IncludeDocumentTemplateIdsCsv.
//
// The supported operator vocabulary intentionally matches the
// QuestionnaireRenderer's ConditionalVisibility operator set (see
// types/questionnaire.ts) so an admin's mental model is the same
// whether they're writing a render-time visibility rule or a
// generation-time selection rule.

import { lookupAnswer } from './intakePdfGenerate'

export type LogicOperator =
  | 'equals' | 'notEquals' | 'contains' | 'notContains'
  | 'startsWith' | 'endsWith'
  | 'greaterThan' | 'lessThan' | 'greaterOrEqual' | 'lessOrEqual'
  | 'isEmpty' | 'isNotEmpty'
  | 'and' | 'or' | 'not'

export interface LogicExpression {
  type: LogicOperator
  field?: string
  value?: unknown
  operands?: LogicExpression[]
}

/** Returns the raw value at a dotted path. Differs from
 *  lookupAnswer (utils/intakePdfGenerate.ts) which always coerces to
 *  string for rendering; for comparisons we need the original type. */
function rawAt(answers: Record<string, unknown> | null | undefined, path: string): unknown {
  if (!answers || !path) {
    return undefined
  }
  const parts = path.split('.')
  let cursor: unknown = answers
  for (const p of parts) {
    if (cursor && typeof cursor === 'object' && p in (cursor as Record<string, unknown>)) {
      cursor = (cursor as Record<string, unknown>)[p]
    } else {
      return undefined
    }
  }
  return cursor
}

function toNumber(v: unknown): number | null {
  if (typeof v === 'number' && !Number.isNaN(v)) {
    return v
  }
  if (typeof v === 'string') {
    const n = Number(v)
    return Number.isFinite(n) ? n : null
  }
  return null
}

export function evaluateExpression(
  expr: LogicExpression | null | undefined,
  answers: Record<string, unknown> | null,
): boolean {
  if (!expr || typeof expr.type !== 'string') {
    return false
  }
  switch (expr.type) {
    case 'and':
      return (expr.operands ?? []).every(o => evaluateExpression(o, answers))
    case 'or':
      return (expr.operands ?? []).some(o => evaluateExpression(o, answers))
    case 'not':
      return !evaluateExpression((expr.operands ?? [])[0], answers)
    default: {
      const left = rawAt(answers, expr.field ?? '')
      const right = expr.value
      switch (expr.type) {
        case 'equals':         return left === right
        case 'notEquals':      return left !== right
        case 'contains':       return typeof left === 'string' && typeof right === 'string' && left.includes(right)
        case 'notContains':    return !(typeof left === 'string' && typeof right === 'string' && left.includes(right))
        case 'startsWith':     return typeof left === 'string' && typeof right === 'string' && left.startsWith(right)
        case 'endsWith':       return typeof left === 'string' && typeof right === 'string' && left.endsWith(right)
        case 'isEmpty':        return left === undefined || left === null || left === ''
                                || (Array.isArray(left) && left.length === 0)
        case 'isNotEmpty':     return !(left === undefined || left === null || left === ''
                                || (Array.isArray(left) && left.length === 0))
        case 'greaterThan':    { const l = toNumber(left), r = toNumber(right); return l !== null && r !== null && l > r }
        case 'lessThan':       { const l = toNumber(left), r = toNumber(right); return l !== null && r !== null && l < r }
        case 'greaterOrEqual': { const l = toNumber(left), r = toNumber(right); return l !== null && r !== null && l >= r }
        case 'lessOrEqual':    { const l = toNumber(left), r = toNumber(right); return l !== null && r !== null && l <= r }
        default: return false
      }
    }
  }
}

export interface RuleLike {
  generationRuleId: string
  name: string
  ruleJson: string
  includeDocumentTemplateIdsCsv: string
  deletedAt: string | null
}

export interface RuleMatch {
  ruleId: string
  ruleName: string
  documentTemplateIds: string[]
}

/** Evaluate every non-deleted rule, return the matches in declaration
 *  order plus the deduped union of all matched DocumentTemplate ids. */
export function evaluateRules(
  rules: ReadonlyArray<RuleLike>,
  answers: Record<string, unknown> | null,
): { matches: RuleMatch[]; matchedTemplateIds: string[] } {
  const matches: RuleMatch[] = []
  const seen = new Set<string>()
  for (const r of rules) {
    if (r.deletedAt) {
      continue
    }
    let expr: LogicExpression | null = null
    try {
      expr = JSON.parse(r.ruleJson) as LogicExpression
    } catch {
      continue
    }
    if (!evaluateExpression(expr, answers)) {
      continue
    }
    const ids = (r.includeDocumentTemplateIdsCsv ?? '')
      .split(',').map(s => s.trim()).filter(s => s.length > 0)
    matches.push({ ruleId: r.generationRuleId, ruleName: r.name, documentTemplateIds: ids })
    for (const id of ids) {
      seen.add(id)
    }
  }
  return { matches, matchedTemplateIds: [...seen] }
}

// Re-export so callers don't need a second import.
export { lookupAnswer }
