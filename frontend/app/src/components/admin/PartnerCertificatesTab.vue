<template>
  <div>
    <div class="pc-head">
      <div>
        <div class="manage-section-title">Partner certificates</div>
        <p class="pc-sub">Cooperation certificates for this partner — one per school. Design them like any
          other certificate; Download always renders the latest design.</p>
      </div>
    </div>

    <div v-if="error" class="err-banner">{{ error }}</div>
    <div v-if="loading" class="loading-row">Loading…</div>

    <table v-else-if="items.length" class="data-table" style="margin-bottom:.75rem">
      <thead><tr><th>School</th><th>Title</th><th style="width:280px">Actions</th></tr></thead>
      <tbody>
        <tr v-for="c in items" :key="c.partnerCertificateId" class="data-row">
          <td>{{ c.schoolName || '—' }}</td>
          <td>
            <input v-model="c.title" class="pc-title-inp" @keyup.enter="renameCert(c)" />
            <button v-if="c.title !== c.savedTitle" class="btn-sm" :disabled="c.renaming" @click="renameCert(c)">
              {{ c.renaming ? 'Saving…' : 'Save' }}
            </button>
          </td>
          <td class="actions-cell">
            <button class="btn-sm" @click="openDesigner(c)">✎ Design</button>
            <button class="btn-sm" :disabled="c.downloading" @click="downloadCert(c)">
              {{ c.downloading ? '…' : '⤓ Download' }}
            </button>
            <button class="btn-sm btn-danger" @click="removeCert(c)">✕</button>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-else-if="!loading" class="pc-sub" style="margin:.5rem 0;">No certificates yet — add one below.</p>

    <div v-if="schoolsAvailable.length" class="pc-add-row">
      <select v-model="addSchoolId" class="pc-add-select">
        <option value="">— pick a school —</option>
        <option v-for="s in schoolsAvailable" :key="s.schoolId" :value="s.schoolId">{{ s.name }}</option>
      </select>
      <input v-model="addTitle" class="pc-title-inp" placeholder="Certificate of Partnership" style="flex:1" />
      <button class="btn-primary-sm" :disabled="!addSchoolId || adding" @click="addCert">
        {{ adding ? 'Adding…' : '+ Add certificate' }}
      </button>
    </div>
    <p v-else-if="!loading && items.length" class="pc-sub" style="margin:.4rem 0;">
      Every school already has a certificate for this partner.
    </p>

    <CertificateEditorModal
      :open="designerOpen"
      :partner-certificate-id="designerCertId"
      :letter-type="designerTitle"
      :programme-name="partnerName"
      @close="designerOpen = false"
      @saved="load" />
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import api from '../../api/client.js'
import CertificateEditorModal from '../letters/CertificateEditorModal.vue'

const props = defineProps({
  partnerId: { type: String, required: true },
  partnerName: { type: String, default: '' },
})

const items = ref([])
const schoolsAvailable = ref([])
const loading = ref(false)
const error = ref('')
const addSchoolId = ref('')
const addTitle = ref('')
const adding = ref(false)

const designerOpen = ref(false)
const designerCertId = ref('')
const designerTitle = ref('Partner Certificate')

async function load() {
  if (!props.partnerId) return
  loading.value = true
  error.value = ''
  try {
    const res = await api.get(`/v1/admin/partners/${props.partnerId}/certificates`)
    items.value = (res.data.items ?? []).map(c => ({
      ...c, savedTitle: c.title, renaming: false, downloading: false,
    }))
    schoolsAvailable.value = res.data.schoolsAvailable ?? []
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to load certificates'
  } finally {
    loading.value = false
  }
}

async function addCert() {
  if (!addSchoolId.value || adding.value) return
  adding.value = true
  error.value = ''
  try {
    await api.post(`/v1/admin/partners/${props.partnerId}/certificates`, {
      schoolId: addSchoolId.value,
      title: addTitle.value.trim() || null,
    })
    addSchoolId.value = ''
    addTitle.value = ''
    await load()
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Failed to add certificate'
  } finally {
    adding.value = false
  }
}

async function renameCert(c) {
  if (c.renaming || !c.title.trim()) return
  c.renaming = true
  error.value = ''
  try {
    const res = await api.patch(`/v1/admin/partner-certificates/${c.partnerCertificateId}`, { title: c.title.trim() })
    c.title = res.data.title
    c.savedTitle = res.data.title
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Rename failed'
  } finally {
    c.renaming = false
  }
}

async function removeCert(c) {
  if (!confirm(`Remove the "${c.title}" certificate for ${c.schoolName}?`)) return
  error.value = ''
  try {
    await api.delete(`/v1/admin/partner-certificates/${c.partnerCertificateId}`)
    await load()
  } catch (e) {
    error.value = e.response?.data?.error ?? e.message ?? 'Remove failed'
  }
}

async function downloadCert(c) {
  if (c.downloading) return
  c.downloading = true
  error.value = ''
  try {
    const res = await api.get(`/v1/admin/partner-certificates/${c.partnerCertificateId}/download`, { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    window.open(url, '_blank')
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch (e) {
    error.value = 'Download failed'
  } finally {
    c.downloading = false
  }
}

function openDesigner(c) {
  designerCertId.value = c.partnerCertificateId
  designerTitle.value = c.title || 'Partner Certificate'
  designerOpen.value = true
}

watch(() => props.partnerId, load, { immediate: true })
</script>

<style scoped>
/* AdminView's styles are scoped, so this tab carries its own copies of the
   shared table/button looks. */
.pc-head { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: .4rem; }
.pc-sub { font-size: .78rem; color: #6b7888; margin: .15rem 0 .5rem; }
.pc-title-inp { padding: .3rem .5rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .85rem; min-width: 240px; }
.pc-add-row { display: flex; gap: .5rem; align-items: center; margin-top: .5rem; }
.pc-add-select { padding: .35rem .5rem; border: 1px solid #cfd7e3; border-radius: 5px; font-size: .85rem; background: #fff; min-width: 220px; }
.manage-section-title { font-size: .95rem; font-weight: 700; color: #003366; }
.data-table { width: 100%; border-collapse: collapse; font-size: .85rem; }
.data-table th { text-align: left; padding: .45rem .6rem; color: #6b7888; font-size: .75rem; text-transform: uppercase; letter-spacing: .03em; border-bottom: 1.5px solid #e8edf4; }
.data-table td { padding: .45rem .6rem; border-bottom: 1px solid #eef1f5; vertical-align: middle; }
.actions-cell { display: flex; gap: .35rem; flex-wrap: wrap; }
.btn-sm { background: #f2f5f9; border: 1px solid #cfd7e3; border-radius: 5px; padding: .28rem .6rem; font-size: .78rem; font-weight: 600; color: #2c3e50; cursor: pointer; }
.btn-sm:hover { background: #e8eef6; }
.btn-danger { color: #b3261e; border-color: #e2b8b5; background: #fdf3f2; }
.btn-primary-sm { background: #003366; color: #fff; border: none; border-radius: 5px; padding: .35rem .8rem; font-size: .8rem; font-weight: 600; cursor: pointer; }
.btn-primary-sm:disabled { opacity: .5; cursor: default; }
.err-banner { background: #fdf3f2; border: 1px solid #e2b8b5; color: #b3261e; padding: .45rem .7rem; border-radius: 6px; font-size: .8rem; margin-bottom: .5rem; }
.loading-row { color: #6b7888; font-size: .85rem; padding: .5rem 0; }
</style>
