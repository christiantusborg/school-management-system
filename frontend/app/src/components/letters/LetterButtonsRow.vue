<template>
  <div class="letters-row">
    <span class="letters-label">Letters:</span>
    <span v-for="t in TYPES" :key="t.code" class="lbtn-group">
      <button class="lbtn" :class="badgeClass(t.code)"
              @click="open(t.code)"
              :title="published[t.code] ? 'Published — releases are live' : 'Draft — no releases until you save'">
        <span class="lbtn-dot">{{ published[t.code] ? '🟢' : '🟠' }}</span>
        {{ t.label }}
      </button>
      <button v-if="EMAILABLE.includes(t.code)" class="lbtn lbtn-email"
              @click="openEmail(t.code)" title="Edit the email sent with this letter">✉</button>
    </span>
    <!-- Config-created letter types (System Config → Letter Types). One chip
         per type editing the English default; language mini-buttons edit the
         per-language versions. -->
    <span v-for="d in dynamicTypes" :key="d.letterTypeDefinitionId" class="lbtn-group">
      <button class="lbtn" :class="dynPublished(d.letterTypeDefinitionId, '') ? 'lbtn-pub' : 'lbtn-draft'"
              @click="openDynamic(d, '')"
              :title="dynPublished(d.letterTypeDefinitionId, '') ? 'Published — releases are live' : 'Draft — no releases until you save'">
        <span class="lbtn-dot">{{ dynPublished(d.letterTypeDefinitionId, '') ? '🟢' : '🟠' }}</span>
        {{ d.name }}
      </button>
      <button v-for="l in languages" :key="l.letterLanguageId" class="lbtn lbtn-lang"
              :class="dynPublished(d.letterTypeDefinitionId, l.name) ? 'lbtn-pub' : 'lbtn-draft'"
              :title="`${d.name} — ${l.name} version (falls back to English until saved)`"
              @click="openDynamic(d, l.name)">{{ langCode(l.name) }}</button>
      <button v-if="d.emailOnRelease" class="lbtn lbtn-email"
              @click="openDynamicEmail(d)" title="Edit the email sent with this letter">✉</button>
    </span>
    <CertificateEditorModal
      :open="modalOpen"
      :programme-id="programmeId"
      :programme-name="programmeName"
      :partner-id="partnerId"
      :letter-type="activeType"
      :letter-name="activeName"
      :language="activeLanguage"
      @close="modalOpen = false"
      @saved="onSaved"
    />
    <LetterEmailEditorModal
      :open="emailModalOpen"
      :programme-id="programmeId"
      :programme-name="programmeName"
      :partner-id="partnerId"
      :letter-type="activeType"
      :letter-name="activeName"
      @close="emailModalOpen = false"
      @saved="emit('saved')"
    />
  </div>
</template>

<script setup>
import { ref, watch, computed } from 'vue'
import apiClient from '../../api/client.js'
import CertificateEditorModal from './CertificateEditorModal.vue'
import LetterEmailEditorModal from './LetterEmailEditorModal.vue'

const props = defineProps({
  programmeId: { type: String, required: true },
  programmeName: { type: String, default: '' },
  partnerId: { type: String, default: '' },
  // Programme's IssueDigitalStudentCard toggle: shows the card editor button.
  showStudentCard: { type: Boolean, default: false },
})
const emit = defineEmits(['saved'])

const ALL_TYPES = [
  { code: 'OfferLetter',            label: 'Offer Letter' },
  { code: 'AdmissionLetter',        label: 'Admission Letter' },
  { code: 'Transcript',             label: 'Digital Transcript' },
  { code: 'PrintableTranscript',    label: 'Printable Transcript' },
  { code: 'Certificate',            label: 'Digital Certificate' },
  { code: 'ProvisionalCertificate', label: 'Printable Cert' },
  { code: 'StudentIdCard',          label: 'Student ID Card', requiresCard: true },
  { code: 'FinalProposalApproval', label: 'Proposal Approval' },
  { code: 'FinalProjectApproval',  label: 'Project Approval' },
]
const TYPES = computed(() =>
  ALL_TYPES.filter(t => !t.requiresCard || props.showStudentCard))

const EMAILABLE = ['OfferLetter', 'AdmissionLetter']
const modalOpen = ref(false)
const emailModalOpen = ref(false)
const activeType = ref('')
const activeName = ref('')
const activeLanguage = ref('')
const published = ref(Object.fromEntries(ALL_TYPES.map(t => [t.code, false])))

// Config-created letter types + language list + per-(type, language) publish
// state, keyed `${definitionId}:${language}` ('' = English default).
const dynamicTypes = ref([])
const languages = ref([])
const dynPublishedMap = ref({})
function dynPublished(defId, lang) { return !!dynPublishedMap.value[`${defId}:${lang || ''}`] }
function langCode(name) { return (name || '').slice(0, 2).toUpperCase() }

async function loadDynamicTypes() {
  try {
    const [defs, langs] = await Promise.all([
      apiClient.get('/v1/admin/letter-type-definitions'),
      apiClient.get('/v1/admin/letter-languages'),
    ])
    dynamicTypes.value = defs.data.items ?? []
    languages.value = langs.data.items ?? []
  } catch { dynamicTypes.value = []; languages.value = [] }
}
loadDynamicTypes()

function openDynamic(d, lang) {
  activeType.value = d.letterTypeDefinitionId
  activeName.value = d.name
  activeLanguage.value = lang || ''
  modalOpen.value = true
}

function openEmail(type) {
  activeType.value = type
  activeName.value = ''
  emailModalOpen.value = true
}

function openDynamicEmail(d) {
  activeType.value = d.letterTypeDefinitionId
  activeName.value = d.name
  emailModalOpen.value = true
}

function badgeClass(code) {
  return published.value[code] ? 'lbtn-pub' : 'lbtn-draft'
}

async function loadPublishStatus() {
  if (!props.programmeId || !props.partnerId) return
  try {
    const r = await apiClient.get(`/v1/admin/programmes/${props.programmeId}/letter-templates`, {
      params: { partnerId: props.partnerId },
    })
    const next = Object.fromEntries(ALL_TYPES.map(t => [t.code, false]))
    const dynNext = {}
    for (const row of (r.data.items ?? [])) {
      if (row.letterTypeDefinitionId)
        dynNext[`${row.letterTypeDefinitionId}:${row.language ?? ''}`] = !!row.isPublished
      else if (row.letterType in next) next[row.letterType] = !!row.isPublished
    }
    published.value = next
    dynPublishedMap.value = dynNext
  } catch { /* leave defaults; fetched again on next save */ }
}

function open(type) {
  activeType.value = type
  activeName.value = ''
  activeLanguage.value = ''
  modalOpen.value = true
}
function onSaved() {
  // The save endpoint flips IsPublished true; refresh local state so the
  // badge flips immediately without waiting for a parent reload.
  loadPublishStatus()
  emit('saved')
}

watch(() => [props.programmeId, props.partnerId], loadPublishStatus, { immediate: true })
</script>

<style scoped>
.letters-row { display: flex; align-items: center; gap: .4rem; flex-wrap: wrap; padding: .5rem 0; }
.letters-label { font-size: .72rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.05em; color: #6b7888; margin-right: .25rem; }
.lbtn { display: inline-flex; align-items: center; gap: .35rem; border: 1px solid #1a4d8c; background: #fff; color: #1a4d8c; padding: .25rem .65rem; border-radius: 14px; font-size: .78rem; cursor: pointer; font-weight: 600; }
.lbtn:hover { background: #eef3fb; }
.lbtn-dot { font-size: .65rem; line-height: 1; }
.lbtn-pub   { border-color: #1c7a4a; color: #1c7a4a; }
.lbtn-pub:hover { background: #eaf6ec; }
.lbtn-draft { border-color: #b66a00; color: #b66a00; }
.lbtn-draft:hover { background: #fff4e6; }
.lbtn-group { display: inline-flex; align-items: center; gap: .15rem; }
.lbtn-email { padding: .25rem .5rem; border-color: #6b4ea3; color: #6b4ea3; }
.lbtn-email:hover { background: #f1ecf9; }
.lbtn-lang { padding: .25rem .45rem; font-size: .68rem; }
</style>
