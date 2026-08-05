<template>
  <div class="pmv">
    <p v-if="error" class="err-banner">{{ error }}</p>
    <p v-if="loading" class="muted">Loading…</p>
    <template v-else>
      <div class="pmv-layout">
        <div class="pmv-list">
          <div v-for="m in items" :key="m.mailMessageId"
               :class="['pmv-row', { active: open?.mailMessageId === m.mailMessageId }]" @click="open = m">
            <div class="pmv-row-top">
              <strong>{{ m.isOutbound ? `MGW → you` : (m.fromName || m.fromAddress) }}</strong>
              <span class="pmv-date">{{ fmt(m.sentAt) }}</span>
            </div>
            <div class="pmv-subject">{{ m.subject || '(no subject)' }}</div>
            <span class="pmv-acct">{{ m.accountName }}</span>
          </div>
          <p v-if="!items.length" class="muted" style="padding:.6rem;">No mail linked yet.</p>
        </div>
        <div class="pmv-reader">
          <template v-if="open">
            <h3>{{ open.subject || '(no subject)' }}</h3>
            <p class="muted">
              {{ open.isOutbound ? 'From MGW' : `From ${open.fromName || open.fromAddress}` }}
              · via {{ open.accountName }} · {{ fmt(open.sentAt) }}
            </p>
            <pre class="pmv-body">{{ open.bodyText || '(no text content)' }}</pre>
          </template>
          <p v-else class="muted" style="padding:.8rem;">Select a message.</p>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '../../api/client.js'

const props = defineProps({
  // '/v1/partner/mail' for the partner portal; '/v1/student/me/mail' for students.
  endpoint: { type: String, required: true },
})
const items = ref([])
const open = ref(null)
const loading = ref(true)
const error = ref('')
const fmt = d => d ? new Date(d).toLocaleString('en-GB', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' }) : ''

onMounted(async () => {
  try { items.value = (await api.get(props.endpoint)).data.items ?? [] }
  catch (e) { error.value = e.response?.data?.error ?? e.message }
  finally { loading.value = false }
})
</script>

<style scoped>
.err-banner { background: #fde7e7; color: #8a1515; padding: .5rem .8rem; border-radius: 6px; font-size: .84rem; }
.muted { color: #5f6e85; font-size: .8rem; }
.pmv-layout { display: flex; gap: .6rem; height: calc(100vh - 260px); min-height: 420px; }
.pmv-list { width: 360px; flex-shrink: 0; overflow-y: auto; background: #fff; border: 1px solid #e0e6ee; border-radius: 8px; }
.pmv-row { padding: .5rem .65rem; border-bottom: 1px solid #f0f3f7; cursor: pointer; }
.pmv-row:hover { background: #f8fafd; }
.pmv-row.active { background: #eef3fb; }
.pmv-row-top { display: flex; justify-content: space-between; gap: .5rem; font-size: .8rem; color: #16324f; }
.pmv-date { font-size: .68rem; color: #8a93a4; flex-shrink: 0; }
.pmv-subject { font-size: .78rem; color: #445; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.pmv-acct { font-size: .64rem; background: #eef3fb; color: #1a4d8c; border-radius: 8px; padding: 0 7px; font-weight: 700; }
.pmv-reader { flex: 1; min-width: 0; overflow-y: auto; background: #fff; border: 1px solid #e0e6ee; border-radius: 8px; padding: .8rem 1rem; }
.pmv-reader h3 { margin: 0 0 .25rem; font-size: 1rem; color: #0b2e59; }
.pmv-body { white-space: pre-wrap; font-family: inherit; font-size: .86rem; margin-top: .6rem; }
</style>
