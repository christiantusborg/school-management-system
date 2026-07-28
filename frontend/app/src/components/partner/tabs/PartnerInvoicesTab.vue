<template>
  <div>
    <!-- Partner: pick open items and combine -->
    <template v-if="mode === 'partner'">
      <h2 class="inv-title">Open payment items</h2>
      <p class="inv-sub">Every unpaid installment and additional invoice across your students.
        Tick any number and generate ONE combined invoice to pay on their behalf.
        Items already on a combined invoice are not listed.</p>
      <div v-if="itemsError" class="err-banner">{{ itemsError }}</div>
      <div v-if="itemsLoading" class="loading-row">Loading…</div>
      <table v-else-if="items.length" class="data-table">
        <thead><tr>
          <th style="width:34px"><input type="checkbox" :checked="allChecked" @change="toggleAll($event.target.checked)" /></th>
          <th>Student</th><th>Student #</th><th>Programme</th><th>Item</th><th>Due date</th><th>Amount</th>
        </tr></thead>
        <tbody>
          <tr v-for="it in items" :key="itemKey(it)">
            <td><input type="checkbox" v-model="it.checked" /></td>
            <td>{{ it.student }}</td>
            <td class="mono">{{ it.studentNumber }}</td>
            <td>{{ it.programmeCode }}</td>
            <td>{{ it.label }}</td>
            <td>{{ (it.dueDate ?? '').slice(0, 10) || '—' }}</td>
            <td><strong>{{ money(it.amount) }} {{ it.currency }}</strong></td>
          </tr>
        </tbody>
      </table>
      <p v-else class="inv-sub">No open payment items — everything is paid or already combined. 🎉</p>

      <div v-if="selected.length" class="inv-genbar">
        <strong>{{ selected.length }}</strong> item(s) selected ·
        <strong v-for="t in selectedTotals" :key="t.currency" style="margin-right:.6rem">
          {{ money(t.amount) }} {{ t.currency }}</strong>
        <button class="btn-primary-sm" :disabled="generating" @click="generate">
          {{ generating ? 'Generating…' : '🧾 Generate combined invoice' }}</button>
      </div>
    </template>

    <!-- Both: the combined invoices -->
    <h2 class="inv-title" :style="mode === 'partner' ? 'margin-top:1.4rem' : ''">Combined invoices</h2>
    <p v-if="mode === 'admin'" class="inv-sub">Invoices this partner generated to pay on their students'
      behalf. Marking one paid marks every underlying student installment/invoice paid.</p>
    <div v-if="listError" class="err-banner">{{ listError }}</div>
    <div v-if="listLoading" class="loading-row">Loading…</div>
    <table v-else-if="invoices.length" class="data-table">
      <thead><tr><th></th><th>Number</th><th>Created</th><th>Items</th><th>Total</th><th>Status</th><th style="width:220px"></th></tr></thead>
      <tbody>
        <template v-for="inv in invoices" :key="inv.id">
          <tr class="inv-row" @click="openInv[inv.id] = !openInv[inv.id]">
            <td>{{ openInv[inv.id] ? '▾' : '▸' }}</td>
            <td class="mono" style="font-weight:700">{{ inv.number }}</td>
            <td>{{ fmt(inv.createdAt) }}</td>
            <td>{{ inv.lines.length }}</td>
            <td><strong v-for="t in inv.totals" :key="t.currency" style="margin-right:.5rem">
              {{ money(t.amount) }} {{ t.currency }}</strong></td>
            <td>
              <span :class="['inv-chip', inv.status === 'Paid' ? 'inv-paid' : 'inv-open']">
                {{ inv.status }}<template v-if="inv.paidAt"> · {{ fmt(inv.paidAt) }}</template></span>
            </td>
            <td @click.stop>
              <button class="btn-sm" :disabled="downloadingId === inv.id" @click="downloadPdf(inv)">
                {{ downloadingId === inv.id ? '…' : '📄 PDF' }}</button>
              <button v-if="mode === 'admin' && inv.status !== 'Paid' && canMarkPaid" class="btn-sm inv-markpaid"
                      :disabled="markingId === inv.id" @click="markPaid(inv)">
                {{ markingId === inv.id ? 'Marking…' : '✓ Mark paid' }}</button>
            </td>
          </tr>
          <tr v-if="openInv[inv.id]">
            <td colspan="7" class="inv-lines">
              <table class="data-table" style="margin:.2rem 0">
                <thead><tr><th>Student</th><th>Student #</th><th>Programme</th><th>Item</th><th>Amount</th></tr></thead>
                <tbody>
                  <tr v-for="(l, i) in inv.lines" :key="i">
                    <td>{{ l.student }}</td>
                    <td class="mono">{{ l.studentNumber }}</td>
                    <td>{{ l.programmeCode }}</td>
                    <td>{{ l.label }}</td>
                    <td><strong>{{ money(l.amount) }} {{ l.currency }}</strong></td>
                  </tr>
                </tbody>
              </table>
            </td>
          </tr>
        </template>
      </tbody>
    </table>
    <p v-else class="inv-sub">No combined invoices yet.</p>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import api from '../../../api/client.js'
import { auth } from '../../../store/auth.js'

const props = defineProps({
  mode: { type: String, default: 'partner' },   // 'partner' | 'admin'
  partnerId: { type: String, default: '' },     // admin mode
})

const items = ref([])
const itemsLoading = ref(false)
const itemsError = ref('')
const invoices = ref([])
const listLoading = ref(false)
const listError = ref('')
const generating = ref(false)
const downloadingId = ref('')
const markingId = ref('')
const openInv = reactive({})

const canMarkPaid = computed(() => auth.adminLevel !== 'Sales' && auth.adminLevel !== null)
const selected = computed(() => items.value.filter(i => i.checked))
const allChecked = computed(() => items.value.length > 0 && items.value.every(i => i.checked))
const selectedTotals = computed(() => {
  const map = {}
  for (const i of selected.value) map[i.currency] = (map[i.currency] ?? 0) + Number(i.amount || 0)
  return Object.entries(map).map(([currency, amount]) => ({ currency, amount }))
})

function itemKey(it) { return it.installmentId ?? it.invoiceId }
function toggleAll(on) { items.value.forEach(i => { i.checked = on }) }
function money(v) { return Number(v ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2 }) }
function fmt(iso) { return iso ? String(iso).slice(0, 10) : '—' }

const listUrl = computed(() => props.mode === 'admin'
  ? `/v1/admin/partners/${props.partnerId}/invoices`
  : '/v1/partner/my/invoices')

async function loadItems() {
  if (props.mode !== 'partner') return
  itemsLoading.value = true
  itemsError.value = ''
  try {
    items.value = ((await api.get('/v1/partner/my/invoices/items')).data.items ?? [])
      .map(i => ({ ...i, checked: false }))
  } catch (e) {
    itemsError.value = e.response?.data?.error ?? e.message ?? 'Failed to load items'
  } finally {
    itemsLoading.value = false
  }
}
async function loadInvoices() {
  listLoading.value = true
  listError.value = ''
  try {
    invoices.value = (await api.get(listUrl.value)).data.items ?? []
  } catch (e) {
    listError.value = e.response?.data?.error ?? e.message ?? 'Failed to load invoices'
  } finally {
    listLoading.value = false
  }
}
async function generate() {
  if (generating.value || !selected.value.length) return
  generating.value = true
  itemsError.value = ''
  try {
    await api.post('/v1/partner/my/invoices', {
      installmentIds: selected.value.filter(i => i.installmentId).map(i => i.installmentId),
      invoiceIds: selected.value.filter(i => i.invoiceId).map(i => i.invoiceId),
    })
    await Promise.all([loadItems(), loadInvoices()])
  } catch (e) {
    itemsError.value = e.response?.data?.error ?? e.message ?? 'Generate failed'
  } finally {
    generating.value = false
  }
}
async function downloadPdf(inv) {
  downloadingId.value = inv.id
  try {
    const url = props.mode === 'admin'
      ? `/v1/admin/partners/${props.partnerId}/invoices/${inv.id}/pdf`
      : `/v1/partner/my/invoices/${inv.id}/pdf`
    const res = await api.get(url, { responseType: 'blob' })
    const blobUrl = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = blobUrl
    a.download = `${inv.number}.pdf`
    document.body.appendChild(a)
    a.click()
    setTimeout(() => { document.body.removeChild(a); URL.revokeObjectURL(blobUrl) }, 60_000)
  } catch (e) {
    listError.value = e.response?.data?.error ?? e.message ?? 'Download failed'
  } finally {
    downloadingId.value = ''
  }
}
async function markPaid(inv) {
  if (!confirm(`Mark ${inv.number} as PAID? Every underlying student installment/invoice will be marked paid.`)) return
  markingId.value = inv.id
  listError.value = ''
  try {
    await api.post(`/v1/admin/partners/${props.partnerId}/invoices/${inv.id}/mark-paid`)
    await loadInvoices()
  } catch (e) {
    listError.value = e.response?.data?.error ?? e.message ?? 'Mark paid failed'
  } finally {
    markingId.value = ''
  }
}

onMounted(() => { loadItems(); loadInvoices() })
</script>

<style scoped>
.inv-title { margin: 0 0 .2rem; font-size: 1.15rem; color: #003366; }
.inv-sub { font-size: .8rem; color: #6b7888; margin: .2rem 0 .7rem; max-width: 46rem; }
.inv-genbar { position: sticky; bottom: 0; display: flex; align-items: center; gap: .6rem; background: #fff; border: 1px solid #d5deea; border-radius: 8px; padding: .6rem .9rem; margin-top: .6rem; box-shadow: 0 -2px 12px rgba(0,0,0,.06); font-size: .9rem; }
.inv-row { cursor: pointer; }
.inv-row:hover td { background: #f0f4fa; }
.inv-lines { background: #f8fafd; padding: .5rem .9rem !important; }
.inv-chip { display: inline-block; border-radius: 10px; padding: .1rem .55rem; font-size: .74rem; font-weight: 700; }
.inv-open { background: #fdf3e5; color: #a8641e; border: 1px solid #ecc9a0; }
.inv-paid { background: #e7f0e9; color: #1d7a3e; border: 1px solid #9dc4a8; }
.inv-markpaid { color: #1d7a3e; margin-left: .3rem; }
.mono { font-family: ui-monospace, monospace; font-size: .82rem; }
.data-table { width: 100%; border-collapse: collapse; font-size: .85rem; background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,.06); }
.data-table th { text-align: left; padding: .5rem .7rem; color: #6b7888; font-size: .74rem; text-transform: uppercase; letter-spacing: .03em; border-bottom: 1.5px solid #e8edf4; background: #fafbfd; }
.data-table td { padding: .45rem .7rem; border-bottom: 1px solid #eef1f5; }
.btn-sm { background: #f2f5f9; border: 1px solid #cfd7e3; border-radius: 5px; padding: .3rem .6rem; font-size: .78rem; font-weight: 600; color: #2c3e50; cursor: pointer; }
.btn-primary-sm { background: #003366; border: 1px solid #003366; color: #fff; border-radius: 5px; padding: .4rem .9rem; font-size: .82rem; font-weight: 600; cursor: pointer; }
.btn-primary-sm:disabled { opacity: .5; cursor: default; }
.err-banner { background: #fdf3f2; border: 1px solid #e2b8b5; color: #b3261e; padding: .45rem .7rem; border-radius: 6px; font-size: .8rem; margin-bottom: .5rem; }
.loading-row { color: #6b7888; font-size: .85rem; padding: .5rem 0; }
</style>
