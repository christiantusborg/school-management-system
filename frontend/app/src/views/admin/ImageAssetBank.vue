<script setup lang="ts">
// Firm-wide image bank for ADR-0039 Document Templates. Backed by the
// DocumentTemplateImage API (list / upload / get file / delete). The
// component owns its list: it loads on mount, uploads/deletes through
// the API, and emits 'pick' when the user clicks Insert. A template's
// MappingJson stores image references by id only (no inline base64).
//
// Why a self-managed list (no v-model): the bank is firm-wide, not
// per-template, so making the parent persist a list inside its form
// state was strictly wrong — every template would have ended up with
// its own private copy of the bank in MappingJson.

import { nextTick, onMounted, ref } from 'vue'
import { intakeApi } from '@quvian/shared/api/intakeApi'
import { useNotificationStore } from '@/stores/notification'

export interface ImageAsset {
  id: string
  name: string
  /** data:<mime>;base64,<bytes> — populated by fetching the file
   *  endpoint once per asset on load. Inline so the picker can use
   *  it directly in <img src> and pdf-lib can embed it. */
  dataUrl: string
}

const props = defineProps<{
  /** When true the component renders as a modal dialog with a
   *  backdrop + close button. When false (default) it renders inline
   *  inside the page flow. */
  asDialog?: boolean
  open?: boolean
}>()
const emit = defineEmits<{
  (e: 'pick', asset: ImageAsset): void
  (e: 'close'): void
}>()

const assets = ref<ImageAsset[]>([])
const loading = ref(false)
const uploading = ref(false)
const fileInput = ref<HTMLInputElement | null>(null)
const notify = useNotificationStore()

async function loadAll() {
  loading.value = true
  try {
    const res = await intakeApi.listDocumentTemplateImages(false)
    if (res.data.success && res.data.data) {
      const rows = res.data.data.items
      // Fetch bytes for each asset in parallel so the picker can render
      // thumbnails and so the consumer's insert path has a data URL
      // ready without an extra hop.
      const hydrated = await Promise.all(rows.map(async r => {
        try {
          const f = await intakeApi.getDocumentTemplateImageFile(r.documentTemplateImageId)
          if (f.data.success && f.data.data) {
            return {
              id: r.documentTemplateImageId,
              name: r.name,
              dataUrl: `data:${f.data.data.mimeType};base64,${f.data.data.bytesBase64}`,
            } as ImageAsset
          }
        } catch {
          // Skip the row if its file fetch failed; the thumbnail
          // would render empty anyway.
        }
        return null
      }))
      assets.value = hydrated.filter((a): a is ImageAsset => a !== null)
    }
  } finally {
    loading.value = false
  }
}

function readFileAsBase64(file: File): Promise<{ bytesBase64: string; mimeType: string } | null> {
  return new Promise(resolve => {
    const reader = new FileReader()
    reader.onload = () => {
      const dataUrl = String(reader.result)
      if (!dataUrl.startsWith('data:')) {
        resolve(null)
        return
      }
      const comma = dataUrl.indexOf(',')
      const meta = dataUrl.slice(5, comma)
      const semi = meta.indexOf(';')
      const mimeType = semi >= 0 ? meta.slice(0, semi) : meta
      resolve({ bytesBase64: dataUrl.slice(comma + 1), mimeType })
    }
    reader.onerror = () => resolve(null)
    reader.readAsDataURL(file)
  })
}

async function onUpload(e: Event) {
  const input = e.target as HTMLInputElement
  const files = Array.from(input.files ?? [])
  input.value = ''
  if (files.length === 0) {
    return
  }
  uploading.value = true
  let uploaded = 0
  for (const file of files) {
    const parsed = await readFileAsBase64(file)
    if (!parsed || !parsed.mimeType.startsWith('image/')) {
      notify.error(`Skipped "${file.name}": not an image.`)
      continue
    }
    try {
      const res = await intakeApi.uploadDocumentTemplateImage({
        name: file.name || 'image',
        mimeType: parsed.mimeType,
        bytesBase64: parsed.bytesBase64,
      })
      if (res.data.success && res.data.data) {
        assets.value = [
          {
            id: res.data.data.documentTemplateImageId,
            name: res.data.data.name,
            dataUrl: `data:${parsed.mimeType};base64,${parsed.bytesBase64}`,
          },
          ...assets.value,
        ]
        uploaded += 1
      }
    } catch {
      notify.error(`Upload failed for "${file.name}".`)
    }
  }
  uploading.value = false
  if (uploaded > 0) {
    notify.success(`Uploaded ${uploaded} image${uploaded === 1 ? '' : 's'}.`)
  }
}

async function removeAsset(id: string) {
  const target = assets.value.find(a => a.id === id)
  if (!target) {
    return
  }
  if (!confirm(`Delete "${target.name}" from the firm image bank?`)) {
    return
  }
  try {
    const res = await intakeApi.deleteDocumentTemplateImage(id)
    if (res.data.success) {
      assets.value = assets.value.filter(a => a.id !== id)
    }
  } catch {
    notify.error(`Failed to delete "${target.name}".`)
  }
}

async function pick(asset: ImageAsset) {
  // Close the dialog FIRST so the modal teardown finishes before the
  // parent focuses Quill — otherwise getSelection() races with the
  // modal still owning focus and the insert ends up at position 0
  // (or no-ops if Quill silently fails to take focus).
  if (props.asDialog) {
    emit('close')
    await nextTick()
  }
  emit('pick', asset)
}

defineExpose({ reload: loadAll })

onMounted(loadAll)
</script>

<template>
  <!-- Inline rendering (default — used by callers that want the
       bank visible alongside other form rows). -->
  <section v-if="!asDialog" class="image-bank">
    <header class="bank-head">
      <strong>Images</strong>
      <span class="muted small">{{ assets.length }} uploaded</span>
      <span class="grow" />
      <button type="button" class="link-btn" :disabled="uploading"
        @click="fileInput?.click()">+ Upload</button>
      <input ref="fileInput" type="file" accept="image/*" multiple hidden @change="onUpload" />
    </header>
    <p v-if="loading" class="muted small empty">Loading firm bank…</p>
    <p v-else-if="assets.length === 0" class="muted small empty">
      Upload PNG / JPG / SVG images, then insert them from inside any
      strategy's editor (Generate body, Canvas field, …).
    </p>
    <ul v-else class="bank-grid">
      <li v-for="a in assets" :key="a.id" class="bank-tile">
        <img :src="a.dataUrl" :alt="a.name" />
        <span class="bank-name" :title="a.name">{{ a.name }}</span>
        <div class="bank-actions">
          <button type="button" class="link-btn" @click="pick(a)">Insert</button>
          <button type="button" class="link-btn link-danger" @click="removeAsset(a.id)">Delete</button>
        </div>
      </li>
    </ul>
  </section>

  <!-- Dialog rendering (opened from any strategy's "Insert image"
       toolbar button). Click backdrop or × to close. -->
  <div v-else-if="open" class="dlg-backdrop" @click.self="emit('close')">
    <div class="dlg">
      <header class="dlg-head">
        <h3>Firm image bank</h3>
        <label class="upload-fake">
          <span>{{ uploading ? 'Uploading…' : '+ Upload new' }}</span>
          <input ref="fileInput" type="file" accept="image/*" multiple hidden @change="onUpload" />
        </label>
        <button type="button" class="dlg-close" @click="emit('close')">✕</button>
      </header>
      <p v-if="loading" class="muted small empty">Loading firm bank…</p>
      <p v-else-if="assets.length === 0" class="muted small empty">
        No images yet — upload one to get started.
      </p>
      <ul v-else class="bank-grid">
        <li v-for="a in assets" :key="a.id" class="bank-tile">
          <button type="button" class="tile-pick" :title="`Insert ${a.name}`" @click="pick(a)">
            <img :src="a.dataUrl" :alt="a.name" />
            <span class="tile-name">{{ a.name }}</span>
          </button>
          <button type="button" class="tile-del" :title="`Delete ${a.name}`"
            @click.stop="removeAsset(a.id)">✕</button>
        </li>
      </ul>
    </div>
  </div>
</template>

<style scoped>
.image-bank {
  border: 1px solid var(--border);
  border-radius: 6px;
  background: var(--surface-2);
  padding: 0.5rem;
  margin-bottom: 0.75rem;
}
.bank-head { display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.35rem; }
.grow { flex: 1; }
.empty { margin: 0.25rem 0; }
.bank-grid {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(110px, 1fr));
  gap: 0.5rem;
}
.bank-tile {
  border: 1px solid var(--border-strong);
  border-radius: 4px;
  padding: 0.3rem;
  background: white;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}
.bank-tile img {
  width: 100%;
  height: 80px;
  object-fit: contain;
  background: repeating-conic-gradient(var(--surface-3) 0 25%, #fff 0 50%) 50% / 12px 12px;
  border-radius: 3px;
}
.bank-name {
  display: block;
  font-size: 0.74rem;
  color: #374151;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.bank-actions { display: flex; justify-content: space-between; font-size: 0.74rem; }
.link-btn { background: none; border: none; color: var(--accent); cursor: pointer; font-size: 0.78rem; padding: 0; }
.link-btn:hover { text-decoration: underline; }
.link-btn:disabled { opacity: 0.4; cursor: not-allowed; }
.link-danger { color: #b91c1c; }
.muted { color: var(--text-muted); }
.small { font-size: 0.82rem; }

/* Dialog mode -------------------------------------------------- */
.dlg-backdrop {
  position: fixed; inset: 0;
  background: rgba(15, 23, 42, 0.45);
  display: flex; align-items: center; justify-content: center;
  z-index: 4000;
}
.dlg {
  width: min(880px, 92vw);
  max-height: 84vh;
  display: flex; flex-direction: column;
  background: white;
  border-radius: 10px;
  box-shadow: 0 24px 60px rgba(15, 23, 42, 0.25);
  overflow: hidden;
}
.dlg-head {
  display: flex; align-items: center; gap: 0.75rem;
  padding: 0.65rem 0.85rem;
  border-bottom: 1px solid var(--border);
}
.dlg-head h3 { margin: 0; font-size: 1rem; }
.dlg-head .upload-fake {
  margin-left: auto;
  padding: 0.4rem 0.75rem;
  border: 1px solid var(--border-strong);
  background: var(--surface-2);
  border-radius: 6px;
  cursor: pointer;
  font-size: 0.85rem;
  color: #374151;
}
.dlg-head .upload-fake:hover { background: var(--surface-3); }
.dlg-close {
  border: none; background: transparent; cursor: pointer;
  font-size: 1.1rem; color: var(--text-muted); padding: 0.2rem 0.4rem; border-radius: 4px;
}
.dlg-close:hover { background: var(--surface-3); color: #111827; }
.dlg .empty { padding: 1.25rem 0.85rem; }
.dlg .bank-grid {
  margin: 0; padding: 0.85rem;
  overflow-y: auto;
  grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
  gap: 0.75rem;
}
.tile-pick {
  display: flex; flex-direction: column; gap: 0.3rem;
  width: 100%;
  padding: 0.4rem;
  border: 1px solid var(--border-strong); border-radius: 6px;
  background: white; cursor: pointer; font-family: inherit; text-align: left;
}
.tile-pick:hover { border-color: var(--accent); background: #f5f7ff; }
.tile-pick img {
  width: 100%; height: 110px; object-fit: contain;
  background: repeating-conic-gradient(var(--surface-3) 0 25%, #fff 0 50%) 50% / 14px 14px;
  border-radius: 4px;
}
.tile-name { font-size: 0.78rem; color: #374151; word-break: break-word; }
.tile-del {
  position: absolute;
  border: 1px solid #fecaca; background: white; color: #b91c1c;
  width: 22px; height: 22px; border-radius: 50%;
  display: inline-flex; align-items: center; justify-content: center;
  font-size: 0.74rem; cursor: pointer;
}
.bank-tile { position: relative; padding: 0; border: none; background: transparent; }
.dlg .bank-tile .tile-del { right: -4px; top: -4px; }
</style>
