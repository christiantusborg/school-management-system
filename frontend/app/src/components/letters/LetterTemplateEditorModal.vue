<template>
  <div v-if="open" class="modal-backdrop" @click.self="$emit('close')">
    <div class="modal">
      <div class="modal-header">
        <h3>{{ titleFor(letterType) }} <span class="muted">— {{ programmeName || 'Programme' }}</span></h3>
        <button class="btn-close" @click="$emit('close')">✕</button>
      </div>

      <div v-if="loading" class="modal-loading">Loading…</div>
      <div v-else-if="loadError" class="modal-err">{{ loadError }}</div>
      <template v-else>
        <div class="page-strip">
          <span class="page-strip-label">Pages:</span>
          <button
            v-for="(_, i) in pages"
            :key="i"
            type="button"
            class="pgbtn"
            :class="{ active: activePage === i }"
            @click="switchPage(i)"
          >{{ i + 1 }}</button>
          <button v-if="pages.length < 5" type="button" class="pgbtn pgbtn-add" @click="addPage" title="Add page">+</button>
          <button v-if="pages.length > 1" type="button" class="pgbtn pgbtn-del" @click="removeCurrentPage" title="Remove this page">−</button>
        </div>

        <div class="toolbar">
          <button type="button" class="tb-btn" :class="{ active: editor?.isActive('bold') }" @click="editor.chain().focus().toggleBold().run()"><strong>B</strong></button>
          <button type="button" class="tb-btn" :class="{ active: editor?.isActive('italic') }" @click="editor.chain().focus().toggleItalic().run()"><em>I</em></button>
          <button type="button" class="tb-btn" :class="{ active: editor?.isActive('heading', { level: 1 }) }" @click="editor.chain().focus().toggleHeading({ level: 1 }).run()">H1</button>
          <button type="button" class="tb-btn" :class="{ active: editor?.isActive('heading', { level: 2 }) }" @click="editor.chain().focus().toggleHeading({ level: 2 }).run()">H2</button>
          <button type="button" class="tb-btn" :class="{ active: editor?.isActive('bulletList') }" @click="editor.chain().focus().toggleBulletList().run()">• List</button>
          <button type="button" class="tb-btn" :class="{ active: editor?.isActive('orderedList') }" @click="editor.chain().focus().toggleOrderedList().run()">1. List</button>
          <span class="tb-sep" />
          <select class="tag-select" @change="onTagInsert($event)">
            <option value="">+ Insert tag…</option>
            <option v-for="t in tags" :key="t.token" :value="t.token">{{ t.token }}</option>
          </select>
          <button type="button" class="tb-btn" @click="pickerOpen = true" title="Insert image from archive">🖼 Image</button>
        </div>

        <editor-content :editor="editor" class="editor-area" />

        <div v-if="pickerOpen" class="picker-backdrop" @click.self="pickerOpen = false">
          <div class="picker">
            <div class="picker-head">
              <h4>Image archive</h4>
              <label class="upload-label">
                <input type="file" accept="image/*" @change="onUpload" />
                <span class="upload-fake-btn">{{ uploading ? 'Uploading…' : '+ Upload new' }}</span>
              </label>
              <button class="btn-close" @click="pickerOpen = false">✕</button>
            </div>
            <div v-if="pickerError" class="modal-err">{{ pickerError }}</div>
            <div v-if="loadingAssets" class="picker-empty">Loading…</div>
            <div v-else-if="assets.length === 0" class="picker-empty">No images yet — upload one to get started.</div>
            <div v-else class="picker-grid">
              <div v-for="a in assets" :key="a.letterAssetId" class="picker-item">
                <button type="button" class="picker-pick" :title="`Insert ${a.name}`" @click="insertAsset(a)">
                  <img v-if="assetUrls[a.letterAssetId]" :src="assetUrls[a.letterAssetId]" :alt="a.name" />
                  <span v-else class="picker-loading">…</span>
                  <span class="picker-name">{{ a.name }}</span>
                </button>
                <button type="button" class="picker-del" :title="`Delete ${a.name}`" @click.stop="deleteAsset(a)">✕</button>
              </div>
            </div>
          </div>
        </div>

        <div v-if="saveError" class="modal-err">{{ saveError }}</div>

        <div class="modal-actions">
          <button class="btn-ghost" @click="$emit('close')">Cancel</button>
          <button class="btn-primary" :disabled="saving" @click="onSave">{{ saving ? 'Saving…' : 'Save' }}</button>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup>
import { ref, watch, onBeforeUnmount } from 'vue'
import { Editor, EditorContent, VueNodeViewRenderer } from '@tiptap/vue-3'
import StarterKit from '@tiptap/starter-kit'
import Image from '@tiptap/extension-image'
import apiClient from '../../api/client.js'
import { fetchAssetBlobUrl, invalidateAssetBlobUrl } from './useAssetUrl.js'
import ResizableImageView from './ResizableImageView.vue'

/// Custom Image extension that:
///   - preserves `data-asset-id` (so the PDF renderer can resolve the asset
///     server-side), and
///   - persists a `width` attribute as an inline style so the user's resize
///     survives the HTML round-trip, and
///   - renders via a Vue NodeView with a corner drag-handle for resizing.
const AssetImage = Image.extend({
  addAttributes() {
    return {
      ...this.parent?.(),
      'data-asset-id': {
        default: null,
        parseHTML: el => el.getAttribute('data-asset-id'),
        renderHTML: attrs => attrs['data-asset-id']
          ? { 'data-asset-id': attrs['data-asset-id'] }
          : {},
      },
      width: {
        default: null,
        parseHTML: (el) => {
          // Pick up width from either an explicit attribute or inline style.
          const fromAttr = el.getAttribute('width')
          if (fromAttr) return parseInt(fromAttr, 10)
          const fromStyle = el.style?.width
          if (fromStyle && fromStyle.endsWith('px')) return parseInt(fromStyle, 10)
          return null
        },
        renderHTML: (attrs) => {
          if (!attrs.width) return {}
          return { style: `width: ${attrs.width}px; height: auto;` }
        },
      },
    }
  },
  addNodeView() {
    return VueNodeViewRenderer(ResizableImageView)
  },
})

const props = defineProps({
  open: { type: Boolean, required: true },
  programmeId: { type: String, default: '' },
  programmeName: { type: String, default: '' },
  letterType: { type: String, default: '' },
})
const emit = defineEmits(['close', 'saved'])

const editor = ref(null)
const tags = ref([])
const loading = ref(false)
const loadError = ref('')
const saving = ref(false)
const saveError = ref('')

const pages = ref([])     // string[] — raw HTML for each page (pre-rehydration on save)
const activePage = ref(0)

const pickerOpen = ref(false)
const assets = ref([])
const assetUrls = ref({}) // assetId → blob URL
const loadingAssets = ref(false)
const pickerError = ref('')
const uploading = ref(false)

function titleFor(t) {
  switch (t) {
    case 'OfferLetter': return 'Offer Letter'
    case 'AdmissionLetter': return 'Admission Letter'
    case 'Transcript': return 'Transcript'
    case 'Certificate': return 'Certificate'
    default: return t || 'Letter'
  }
}

/// Replaces every &lt;img&gt; src with a same-id blob URL fetched via axios.
/// The DB stores `/v1/admin/letter-assets/.../file` URLs that the browser
/// can't load directly (no Bearer token on &lt;img&gt; requests), so we swap
/// them client-side before injecting into the editor.
async function rehydrateImageSrcs(html) {
  if (!html || !html.includes('<img')) return html
  const parser = new DOMParser()
  const doc = parser.parseFromString(`<root>${html}</root>`, 'text/html')
  const imgs = doc.querySelectorAll('img[data-asset-id]')
  await Promise.all(Array.from(imgs).map(async (img) => {
    const id = img.getAttribute('data-asset-id')
    try {
      img.setAttribute('src', await fetchAssetBlobUrl(id))
    } catch {
      img.removeAttribute('src')
    }
  }))
  const root = doc.querySelector('root')
  return root ? root.innerHTML : html
}

/// Replaces blob URLs with canonical /v1/... refs before saving so the row
/// stays valid across editor sessions and PDF renders.
function dehydrateImageSrcs(html) {
  if (!html || !html.includes('<img')) return html
  const parser = new DOMParser()
  const doc = parser.parseFromString(`<root>${html}</root>`, 'text/html')
  const imgs = doc.querySelectorAll('img[data-asset-id]')
  imgs.forEach((img) => {
    const id = img.getAttribute('data-asset-id')
    if (id) img.setAttribute('src', `/v1/admin/letter-assets/${id}/file`)
  })
  const root = doc.querySelector('root')
  return root ? root.innerHTML : html
}

function parseStoredPages(raw) {
  if (!raw) return ['<p></p>']
  try {
    const parsed = JSON.parse(raw)
    if (Array.isArray(parsed) && parsed.every(p => typeof p === 'string')) {
      return parsed.length > 0 ? parsed : ['<p></p>']
    }
  } catch { /* not JSON, treat as single-page legacy */ }
  return [String(raw)]
}

async function load() {
  if (!props.open || !props.programmeId || !props.letterType) return
  loading.value = true
  loadError.value = ''
  saveError.value = ''
  try {
    const [tplRes, tagRes] = await Promise.all([
      apiClient.get(`/v1/admin/programmes/${props.programmeId}/letter-templates`),
      apiClient.get('/v1/admin/letter-tags'),
    ])
    tags.value = tagRes.data.items ?? []
    const existing = (tplRes.data.items ?? []).find(t => t.letterType === props.letterType)
    pages.value = parseStoredPages(existing?.bodyHtml).slice(0, 5)
    activePage.value = 0
    const html = await rehydrateImageSrcs(pages.value[0])
    if (editor.value) {
      editor.value.commands.setContent(html, false)
    } else {
      editor.value = new Editor({ extensions: [StarterKit, AssetImage], content: html })
    }
  } catch (e) {
    loadError.value = e.response?.data?.message ?? e.message ?? 'Failed to load template'
  } finally {
    loading.value = false
  }
}

async function switchPage(idx) {
  if (idx === activePage.value || idx < 0 || idx >= pages.value.length) return
  if (editor.value) pages.value[activePage.value] = editor.value.getHTML()
  activePage.value = idx
  if (editor.value) {
    const html = await rehydrateImageSrcs(pages.value[idx])
    editor.value.commands.setContent(html, false)
  }
}

async function addPage() {
  if (pages.value.length >= 5) return
  if (editor.value) pages.value[activePage.value] = editor.value.getHTML()
  pages.value.push('<p></p>')
  activePage.value = pages.value.length - 1
  if (editor.value) editor.value.commands.setContent('<p></p>', false)
}

function removeCurrentPage() {
  if (pages.value.length <= 1) return
  if (!confirm(`Delete page ${activePage.value + 1}? This can't be undone after Save.`)) return
  pages.value.splice(activePage.value, 1)
  const next = Math.max(0, Math.min(activePage.value, pages.value.length - 1))
  activePage.value = next
  if (editor.value) {
    rehydrateImageSrcs(pages.value[next]).then(html => editor.value.commands.setContent(html, false))
  }
}

function onTagInsert(event) {
  const token = event.target.value
  event.target.value = ''
  if (!token || !editor.value) return
  editor.value.chain().focus().insertContent(token + ' ').run()
}

async function loadAssets() {
  loadingAssets.value = true
  pickerError.value = ''
  try {
    const r = await apiClient.get('/v1/admin/letter-assets')
    assets.value = r.data.items ?? []
    // Prefetch blob URLs for thumbnails — done in parallel so the picker
    // pops up populated rather than each tile flashing in turn.
    const next = { ...assetUrls.value }
    await Promise.all(assets.value.map(async (a) => {
      try { next[a.letterAssetId] = await fetchAssetBlobUrl(a.letterAssetId) }
      catch { next[a.letterAssetId] = '' }
    }))
    assetUrls.value = next
  } catch (e) {
    pickerError.value = e.response?.data?.message ?? e.message ?? 'Failed to load assets'
  } finally {
    loadingAssets.value = false
  }
}

async function deleteAsset(asset) {
  if (!confirm(`Delete "${asset.name}"? It will be hidden from the archive (soft delete).`)) return
  try {
    await apiClient.delete(`/v1/admin/letter-assets/${asset.letterAssetId}`)
    invalidateAssetBlobUrl(asset.letterAssetId)
    await loadAssets()
  } catch (e) {
    pickerError.value = e.response?.data?.message ?? e.message ?? 'Delete failed'
  }
}

async function onUpload(event) {
  const file = event.target.files?.[0]
  event.target.value = ''
  if (!file) return
  uploading.value = true
  pickerError.value = ''
  try {
    const fd = new FormData()
    fd.append('file', file)
    fd.append('name', file.name.replace(/\.[^.]+$/, ''))
    await apiClient.post('/v1/admin/letter-assets', fd, { headers: { 'Content-Type': 'multipart/form-data' } })
    await loadAssets()
  } catch (e) {
    pickerError.value = e.response?.data?.message ?? e.message ?? 'Upload failed'
  } finally {
    uploading.value = false
  }
}

async function insertAsset(asset) {
  if (!editor.value) return
  const blobUrl = await fetchAssetBlobUrl(asset.letterAssetId).catch(() => '')
  // Insert as a TipTap Image node so the editor renders the picture instead
  // of stuffing literal HTML into a text node. data-asset-id survives the
  // round-trip via the AssetImage extension and is what the PDF renderer
  // keys off of.
  editor.value.chain().focus().insertContent({
    type: 'image',
    attrs: {
      src: blobUrl,
      alt: asset.name,
      'data-asset-id': asset.letterAssetId,
    },
  }).run()
  pickerOpen.value = false
}

watch(pickerOpen, (open) => { if (open) loadAssets() })

async function onSave() {
  if (!editor.value) return
  saving.value = true
  saveError.value = ''
  try {
    // Capture the in-progress page first, then dehydrate every page's images
    // back to their canonical /v1/... URLs before stuffing into the JSON array.
    pages.value[activePage.value] = editor.value.getHTML()
    const dehydrated = pages.value.map(p => dehydrateImageSrcs(p || ''))
    const bodyHtml = JSON.stringify(dehydrated)
    await apiClient.put(`/v1/admin/programmes/${props.programmeId}/letter-templates/${props.letterType}`, { bodyHtml })
    emit('saved')
    emit('close')
  } catch (e) {
    saveError.value = e.response?.data?.message ?? e.message ?? 'Save failed'
  } finally {
    saving.value = false
  }
}

watch(() => [props.open, props.programmeId, props.letterType], () => {
  if (props.open) load()
}, { immediate: true })

onBeforeUnmount(() => {
  if (editor.value) {
    editor.value.destroy()
    editor.value = null
  }
})
</script>

<style scoped>
.modal-backdrop { position: fixed; inset: 0; background: rgba(20, 30, 50, 0.55); z-index: 1000; display: flex; align-items: center; justify-content: center; padding: 1rem; }
.modal { background: #fff; border-radius: 8px; width: min(900px, 100%); max-height: 90vh; display: flex; flex-direction: column; box-shadow: 0 10px 40px rgba(0,0,0,0.2); }
.modal-header { display: flex; justify-content: space-between; align-items: center; padding: 0.85rem 1.1rem; border-bottom: 1px solid #e6ebf2; }
.modal-header h3 { margin: 0; font-size: 1rem; color: #1a2d4f; }
.muted { color: #6b7888; font-weight: 400; font-size: .9rem; }
.btn-close { border: none; background: transparent; font-size: 1.1rem; cursor: pointer; color: #6b7888; }
.modal-loading, .modal-err { padding: 1rem 1.1rem; }
.modal-err { background: #fde7e7; color: #8a1515; border-radius: 6px; margin: .5rem 1.1rem; padding: .55rem .8rem; font-size: .85rem; }
.page-strip { display: flex; align-items: center; gap: .3rem; padding: .35rem 1rem; border-bottom: 1px solid #e6ebf2; background: #f6f9fd; }
.page-strip-label { font-size: .72rem; font-weight: 700; color: #5f6e85; text-transform: uppercase; letter-spacing: .04em; margin-right: .25rem; }
.pgbtn { border: 1px solid #cfd7e3; background: #fff; padding: .2rem .55rem; border-radius: 4px; font-size: .78rem; cursor: pointer; min-width: 1.85rem; }
.pgbtn:hover { background: #eef3fb; }
.pgbtn.active { background: #1a4d8c; color: #fff; border-color: #1a4d8c; }
.pgbtn-add { color: #1c7a4a; border-color: #b3dac0; }
.pgbtn-del { color: #b63329; border-color: #d4a4a4; }
.toolbar { display: flex; align-items: center; gap: .35rem; padding: .55rem 1rem; border-bottom: 1px solid #e6ebf2; flex-wrap: wrap; }
.tb-btn { border: 1px solid #cfd7e3; background: #fff; padding: .25rem .55rem; border-radius: 4px; font-size: .82rem; cursor: pointer; min-width: 2rem; }
.tb-btn.active { background: #1a4d8c; color: #fff; border-color: #1a4d8c; }
.tb-sep { width: 1px; height: 20px; background: #cfd7e3; margin: 0 .25rem; }
.tag-select { padding: .25rem .5rem; border: 1px solid #cfd7e3; border-radius: 4px; font-size: .82rem; background: #fff; cursor: pointer; }
.editor-area { padding: 1rem 1.2rem; min-height: 320px; max-height: 60vh; overflow-y: auto; font-size: .9rem; line-height: 1.5; }
.editor-area :deep(.ProseMirror) { outline: none; min-height: 300px; }
.editor-area :deep(.ProseMirror p) { margin: 0 0 .65em; }
.editor-area :deep(.ProseMirror h1) { font-size: 1.4rem; margin: 0 0 .55em; color: #1a2d4f; }
.editor-area :deep(.ProseMirror h2) { font-size: 1.15rem; margin: 0 0 .55em; color: #1a2d4f; }
.editor-area :deep(.ProseMirror ul), .editor-area :deep(.ProseMirror ol) { padding-left: 1.4rem; margin: 0 0 .65em; }
.modal-actions { display: flex; justify-content: flex-end; gap: .5rem; padding: .85rem 1.1rem; border-top: 1px solid #e6ebf2; }
.btn-ghost { border: 1px solid #cfd7e3; background: #fff; color: #1a2d4f; padding: .42rem .9rem; border-radius: 5px; cursor: pointer; }
.btn-primary { border: 1px solid #1a4d8c; background: #1a4d8c; color: #fff; padding: .42rem 1rem; border-radius: 5px; cursor: pointer; font-weight: 700; }
.btn-primary:disabled { opacity: .55; cursor: default; }

.editor-area :deep(.ProseMirror img) { max-width: 100%; height: auto; }
.editor-area :deep(.ProseMirror .ri-wrap) { display: inline-block; max-width: 100%; }

.picker-backdrop { position: absolute; inset: 0; background: rgba(20, 30, 50, 0.55); display: flex; align-items: center; justify-content: center; padding: 1rem; z-index: 10; }
.picker { background: #fff; border-radius: 8px; width: min(700px, 100%); max-height: 80%; display: flex; flex-direction: column; overflow: hidden; box-shadow: 0 8px 30px rgba(0,0,0,0.25); }
.picker-head { display: flex; align-items: center; gap: .65rem; padding: .65rem .9rem; border-bottom: 1px solid #e6ebf2; }
.picker-head h4 { margin: 0; flex: 1; font-size: .95rem; color: #1a2d4f; }
.upload-label { display: inline-block; cursor: pointer; }
.upload-label input[type="file"] { display: none; }
.upload-fake-btn { display: inline-block; padding: .35rem .75rem; border: 1px solid #1a4d8c; background: #fff; color: #1a4d8c; border-radius: 5px; font-size: .8rem; font-weight: 600; }
.upload-fake-btn:hover { background: #eef3fb; }
.picker-empty { padding: 1.4rem; text-align: center; color: #6b7888; font-size: .9rem; }
.picker-grid { padding: .8rem; display: grid; grid-template-columns: repeat(auto-fill, minmax(140px, 1fr)); gap: .75rem; overflow-y: auto; }
.picker-item { position: relative; background: #fff; border: 1px solid #cfd7e3; border-radius: 6px; }
.picker-item:hover { border-color: #1a4d8c; box-shadow: 0 2px 6px rgba(26,77,140,0.15); }
.picker-pick { width: 100%; background: transparent; border: none; padding: .35rem; cursor: pointer; display: flex; flex-direction: column; align-items: center; gap: .35rem; }
.picker-pick img { max-width: 100%; max-height: 90px; object-fit: contain; }
.picker-loading { display: flex; align-items: center; justify-content: center; height: 90px; color: #b3bcc9; font-size: 1.2rem; }
.picker-name { font-size: .72rem; color: #5f6e85; word-break: break-word; text-align: center; }
.picker-del { position: absolute; top: 4px; right: 4px; width: 22px; height: 22px; padding: 0; border-radius: 50%; border: 1px solid #d4a4a4; background: rgba(255,255,255,0.9); color: #b63329; font-size: .8rem; cursor: pointer; line-height: 1; }
.picker-del:hover { background: #fde7e7; border-color: #b63329; }
</style>
