import { reactive } from 'vue'

// grades[studentId][moduleName] = { ibssGrade, ukGrade, ectsGrade, ectsPoints, remark, gradePoints }
export const gradesStore = reactive({})

export function saveGrades(studentId, gradeMap) {
  gradesStore[studentId] = { ...gradeMap }
}

export function getStudentGrades(studentId) {
  return gradesStore[studentId] ?? {}
}

export function isGraded(studentId) {
  const g = gradesStore[studentId]
  return g && Object.keys(g).length > 0
}
