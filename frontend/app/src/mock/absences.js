import { reactive } from 'vue'

export const absences = reactive([])

let _aid = 1
export const nextAbsenceId = () => _aid++
