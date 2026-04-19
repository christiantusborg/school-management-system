import { reactive } from 'vue'

export const tickets = reactive([])

let _tid = 1
export const nextTicketId = () => _tid++
