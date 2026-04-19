import { reactive } from 'vue'

export const announcements = reactive([])

let _anid = 1
export const nextAnnouncementId = () => _anid++
