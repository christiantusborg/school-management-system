<template>
  <div v-if="open" class="modal-backdrop" @click.self="$emit('close')">
    <div class="modal">
      <div class="modal-header">
        <h3>
          {{ titleFor(letterType) }}
          <span class="muted">— {{ programmeName || 'Programme' }}</span>
          <span v-if="!loading"
                :class="['pub-pill', isPublished ? 'pub-pill-on' : 'pub-pill-off']"
                :title="isPublished ? 'Releases are live for this programme + letter type.' : 'No PDFs will be released for this programme + letter type until you save.'">
            {{ isPublished ? '🟢 Published' : '🟠 Draft' }}
          </span>
        </h3>
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

        <div class="cert-toolbar">
          <button class="tb-btn" @click="addTextField">+ Text</button>
          <button class="tb-btn" @click="openImageFieldPicker">+ Image</button>
          <button class="tb-btn" :disabled="!selectedField" @click="removeSelected">Delete</button>
          <button class="tb-btn" @click="openBgPicker">🖼 Background</button>
          <button v-if="copySiblingLabel" class="tb-btn tb-copy" :disabled="copying" @click="copyFromSibling">
            {{ copying ? 'Copying…' : `📋 ${copySiblingLabel}` }}
          </button>
          <span class="tb-zoom">
            <button class="tb-btn tb-zoom-btn" @click="zoomOut" title="Zoom out">−</button>
            <span class="tb-zoom-val">{{ Math.round(zoom * 100) }}%</span>
            <button class="tb-btn tb-zoom-btn" @click="zoomIn" title="Zoom in">+</button>
            <button class="tb-btn tb-zoom-btn" @click="zoomFit" title="Fit to width">⤧</button>
          </span>
          <span class="tb-info">Stage: {{ layout.width }}×{{ layout.height }}</span>
        </div>

        <div class="cert-body">
          <div ref="stageWrap" class="cert-stage-wrap" @wheel="onWheel">
            <div v-if="stageW > 0" class="cert-stage-stack" :style="{ width: stageW + 'px', height: stageH + 'px' }">
              <KStage :config="{ width: stageW, height: stageH }" @click="onStageClick">
                <KLayer>
                  <KImage v-if="bgImage" :config="{ image: bgImage, width: stageW, height: stageH }" />
                  <KRect v-if="hasMargins" :config="{ x: marginGuide.x, y: marginGuide.y, width: marginGuide.w, height: marginGuide.h, stroke: '#1a4d8c', strokeWidth: 1, dash: [6, 4], listening: false }" />
                  <template v-for="(f, i) in currentPage.fields" :key="f.id || i">
                    <KGroup :config="groupConfig(f)" :draggable="true" @click="selectField(f)" @tap="selectField(f)" @dragend="onDragEnd($event, f)">
                      <KRect :config="hitRectConfig(f)" />
                      <KImage v-if="f.kind === 'image' && fieldImages[f.imageAssetId]" :config="imageConfig(f)" />
                      <template v-else-if="f.kind === 'transcriptTable' || f.kind === 'gradeStandardTable'">
                        <KRect :config="placeholderRectConfig(f)" />
                        <KText :config="placeholderLabelConfig(f)" />
                      </template>
                      <KText v-else-if="f.kind !== 'image' && !f.htmlText" :config="textConfig(f)" />
                    </KGroup>
                  </template>
                </KLayer>
              </KStage>
              <div class="cert-html-overlay">
                <div
                  v-for="f in currentPage.fields.filter(x => x.htmlText && x.kind !== 'image' && x.kind !== 'transcriptTable' && x.kind !== 'gradeStandardTable')"
                  :key="`html-${f.id}`"
                  class="cert-html-field"
                  :style="htmlFieldStyle(f)"
                  v-html="f.htmlText"
                />
              </div>
            </div>
            <div v-else class="cert-stage-placeholder">Loading canvas… (stage size {{ stageW }}×{{ stageH }})</div>
          </div>

          <div class="cert-side">
            <template v-if="!selectedField">
              <h4>Page setup</h4>
              <label>Page size</label>
              <select v-model="layout.pageSize" @change="applyPageSize">
                <option v-for="opt in PAGE_SIZE_OPTIONS" :key="opt" :value="opt">{{ opt }}</option>
              </select>
              <label>Orientation</label>
              <select v-model="layout.orientation" @change="applyPageSize">
                <option v-for="opt in ORIENTATION_OPTIONS" :key="opt" :value="opt">{{ opt }}</option>
              </select>
              <div class="row">
                <div><label>Canvas W</label><input type="number" v-model.number="layout.width" min="50" /></div>
                <div><label>Canvas H</label><input type="number" v-model.number="layout.height" min="50" /></div>
              </div>
              <label class="mt-row">Margins (canvas px)</label>
              <div class="row">
                <div><label>Top</label><input type="number" v-model.number="layout.marginTop" min="0" /></div>
                <div><label>Right</label><input type="number" v-model.number="layout.marginRight" min="0" /></div>
              </div>
              <div class="row">
                <div><label>Bottom</label><input type="number" v-model.number="layout.marginBottom" min="0" /></div>
                <div><label>Left</label><input type="number" v-model.number="layout.marginLeft" min="0" /></div>
              </div>
              <p class="cert-side-hint">Click a field on the canvas to edit it, or use + Text / + Image above.</p>

              <h4 class="mt-row">Copy from programme</h4>
              <select v-model="copyFromProgrammeId" class="copy-prog-select">
                <option value="">— pick a programme —</option>
                <option v-for="p in otherProgrammes" :key="p.programmeId" :value="p.programmeId">
                  {{ p.code ? p.code + ' — ' : '' }}{{ p.name }} ({{ p.ownerLabel }})
                </option>
              </select>
              <button type="button" class="tb-btn copy-prog-btn" :disabled="!copyFromProgrammeId || copying" @click="copyFromProgramme">
                {{ copying ? 'Copying…' : `📋 Copy ${titleFor(letterType)} from this programme` }}
              </button>
            </template>
            <template v-else>
              <h4>{{ selectedField.kind === 'image' ? 'Image field' : 'Text field' }}</h4>

              <template v-if="selectedField.kind === 'image'">
                <label>Image</label>
                <button type="button" class="tb-btn change-img-btn" @click="openImageFieldPicker">{{ selectedField.imageAssetId ? 'Change image' : 'Pick image' }}</button>
                <div v-if="fieldImages[selectedField.imageAssetId]" class="img-preview-thumb">
                  <img :src="fieldImages[selectedField.imageAssetId].src" />
                </div>
                <div class="row">
                  <div><label>X</label><input type="number" v-model.number="selectedField.x" /></div>
                  <div><label>Y</label><input type="number" v-model.number="selectedField.y" /></div>
                </div>
                <div class="row">
                  <div><label>Width</label><input type="number" :value="Math.round(selectedField.width)" min="10" @input="onImageDimChange('width', $event.target.value)" /></div>
                  <div><label>Height</label><input type="number" :value="Math.round(selectedField.height)" min="10" @input="onImageDimChange('height', $event.target.value)" /></div>
                </div>
                <div class="check"><label><input type="checkbox" v-model="selectedField.lockAspect" /> Keep image ratio</label></div>
              </template>

              <template v-else>
                <label>Tag (or leave blank for static)</label>
                <select v-model="selectedField.tag">
                  <option :value="null">— static text —</option>
                  <option v-for="t in tags" :key="t.token" :value="t.token">{{ t.token }}</option>
                </select>
                <label v-if="!selectedField.tag">Static text</label>
                <textarea v-if="!selectedField.tag" v-model="selectedField.text" rows="2"></textarea>
                <button v-if="!selectedField.tag" type="button" class="tb-btn rich-edit-btn" @click="openRichEditor">
                  ✎ Rich edit{{ selectedField.htmlText ? ' (rich applied)' : '' }}
                </button>
                <label>Prefix</label>
                <input v-model="selectedField.prefix" />
                <label>Suffix</label>
                <input v-model="selectedField.suffix" />
                <div class="row">
                  <div><label>X</label><input type="number" v-model.number="selectedField.x" /></div>
                  <div><label>Y</label><input type="number" v-model.number="selectedField.y" /></div>
                </div>
                <div class="row">
                  <div><label>Font size</label><input type="number" v-model.number="selectedField.fontSize" min="8" /></div>
                  <div><label>Align</label>
                    <select v-model="selectedField.align">
                      <option value="left">left</option>
                      <option value="center">center</option>
                      <option value="right">right</option>
                    </select>
                  </div>
                </div>
                <div class="row">
                  <div><label>Color</label><input type="color" v-model="selectedField.color" /></div>
                  <div class="check"><label><input type="checkbox" v-model="selectedField.bold" /> Bold</label></div>
                  <div class="check"><label><input type="checkbox" v-model="selectedField.italic" /> Italic</label></div>
                </div>
              </template>
            </template>
          </div>
        </div>

        <div v-if="saveError" class="modal-err">{{ saveError }}</div>
        <div class="modal-actions">
          <button class="btn-ghost" @click="$emit('close')">Cancel</button>
          <button class="btn-ghost btn-preview" :disabled="saving || previewing" @click="onPreview">
            {{ previewing ? 'Rendering…' : '👁 Preview' }}
          </button>
          <button class="btn-primary" :disabled="saving || previewing" @click="onSave">
            {{ saving ? 'Saving…' : (isPublished ? 'Save' : 'Save & Publish') }}
          </button>
        </div>

        <RichTextDialog
          :open="richOpen"
          :initial-html="selectedField?.htmlText || ''"
          :initial-plain="selectedField?.text || ''"
          @close="richOpen = false"
          @apply="onRichApply"
        />

        <div v-if="pickerOpen" class="picker-backdrop" @click.self="pickerOpen = false">
          <div class="picker">
            <div class="picker-head">
              <h4>{{ pickerMode === 'bg' ? 'Pick background' : 'Pick image' }}</h4>
              <label class="upload-label">
                <input type="file" accept="image/*" @change="onUpload" />
                <span class="upload-fake-btn">{{ uploading ? 'Uploading…' : '+ Upload new' }}</span>
              </label>
              <button class="btn-close" @click="pickerOpen = false">✕</button>
            </div>
            <div v-if="uploadError" class="modal-err">{{ uploadError }}</div>
            <div class="picker-grid">
              <div v-for="a in assets" :key="a.letterAssetId" class="picker-item">
                <button type="button" class="picker-pick" @click="onPick(a)">
                  <img v-if="assetUrls[a.letterAssetId]" :src="assetUrls[a.letterAssetId]" />
                  <span v-else class="picker-loading">…</span>
                  <span class="picker-name">{{ a.name }}</span>
                </button>
                <button type="button" class="picker-del" :title="`Delete ${a.name}`" @click.stop="deleteAsset(a)">✕</button>
              </div>
            </div>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, watch, nextTick, onBeforeUnmount } from 'vue'
import apiClient from '../../api/client.js'
import { fetchAssetBlobUrl, invalidateAssetBlobUrl } from './useAssetUrl.js'
import RichTextDialog from './RichTextDialog.vue'
// Pull vue-konva components in directly rather than relying on the global
// plugin registration (which uses a "V" prefix and goes through Vue's
// kebab-case→PascalCase resolution — flaky in some bundlers).
import {
  Stage as KStage,
  Layer as KLayer,
  Image as KImage,
  Group as KGroup,
  Text as KText,
  Rect as KRect,
} from 'vue-konva'

const props = defineProps({
  open: { type: Boolean, required: true },
  programmeId: { type: String, default: '' },
  programmeName: { type: String, default: '' },
  letterType: { type: String, default: 'Certificate' },
})
const emit = defineEmits(['close', 'saved'])

function titleFor(t) {
  switch (t) {
    case 'OfferLetter':            return 'Offer Letter'
    case 'AdmissionLetter':        return 'Admission Letter'
    case 'Transcript':             return 'Transcript'
    case 'Certificate':            return 'Certificate'
    case 'ProvisionalCertificate': return 'Provisional Certificate (no stamp/signature)'
    default: return t || 'Letter'
  }
}

const loading = ref(false)
const loadError = ref('')
const saving = ref(false)
const saveError = ref('')
const previewing = ref(false)
// Tracks whether THIS letter type is currently published for the programme.
// Seeded defaults are unpublished; first save flips it true server-side.
const isPublished = ref(false)

// Layout-level metadata; per-page state lives in `pages`.
const layout = reactive({
  width: 2000,
  height: 1414,
  pageSize: 'A4',          // 'A4' | 'A3' | 'Letter' | 'Custom'
  orientation: 'auto',     // 'portrait' | 'landscape' | 'auto'
  marginTop: 0,
  marginRight: 0,
  marginBottom: 0,
  marginLeft: 0,
})

const PAGE_SIZE_OPTIONS = ['A4', 'A3', 'Letter', 'Custom']
const ORIENTATION_OPTIONS = ['auto', 'portrait', 'landscape']

// Effective margin in canvas pixels — visualised as a dashed safe-zone box.
const marginGuide = computed(() => ({
  x: layout.marginLeft * scale.value,
  y: layout.marginTop * scale.value,
  w: Math.max(0, (layout.width - layout.marginLeft - layout.marginRight)) * scale.value,
  h: Math.max(0, (layout.height - layout.marginTop - layout.marginBottom)) * scale.value,
}))
const pages = ref([{ backgroundAssetId: null, fields: [] }])
const activePage = ref(0)
const currentPage = computed(() => pages.value[activePage.value])
const tags = ref([])
const assets = ref([])
const assetUrls = ref({}) // assetId → blob URL (used in picker thumbnails)
const fieldImages = ref({}) // assetId → HTMLImageElement (used in canvas)
const pickerOpen = ref(false)
const pickerMode = ref('bg') // 'bg' | 'fieldImage'
const uploading = ref(false)
const uploadError = ref('')
const zoom = ref(1)
const richOpen = ref(false)
const copying = ref(false)
const programmes = ref([])
const copyFromProgrammeId = ref('')
const partnerNames = ref({}) // partnerId → partner display name
const otherProgrammes = computed(() => {
  return programmes.value
    .filter(p => p.programmeId !== props.programmeId)
    .map(p => ({
      ...p,
      ownerLabel: p.ownerId ? (partnerNames.value[p.ownerId] ?? '?') : 'Core',
    }))
    .sort((a, b) => {
      // Core programmes first, then partners alphabetically.
      const ac = a.ownerId ? 1 : 0
      const bc = b.ownerId ? 1 : 0
      if (ac !== bc) return ac - bc
      return (a.name ?? '').localeCompare(b.name ?? '')
    })
})

// Pair Certificate with ProvisionalCertificate. The button only shows for
// these two; for everything else we return null and v-if hides it.
const siblingType = computed(() => {
  if (props.letterType === 'Certificate')            return 'ProvisionalCertificate'
  if (props.letterType === 'ProvisionalCertificate') return 'Certificate'
  return null
})
const copySiblingLabel = computed(() => {
  if (siblingType.value === 'ProvisionalCertificate') return 'Copy from Provisional Cert.'
  if (siblingType.value === 'Certificate')            return 'Copy from Certificate'
  return null
})

const stageWrap = ref(null)
const baseScale = ref(0) // fit-to-width scale (re-computed on resize)
const stageW = computed(() => layout.width * baseScale.value * zoom.value)
const stageH = computed(() => layout.height * baseScale.value * zoom.value)
const scale = computed(() => baseScale.value * zoom.value)

const bgImage = ref(null)
const selectedFieldId = ref(null)
const selectedField = computed(() => currentPage.value?.fields.find(f => f.id === selectedFieldId.value) || null)

function fitStage() {
  const wrap = stageWrap.value
  if (!wrap) return
  const w = wrap.clientWidth - 24 // padding allowance
  if (w <= 0) return
  baseScale.value = w / Math.max(1, layout.width)
}

// When admin picks a named page size or changes orientation, snap canvas
// dimensions to that page in points. Custom keeps the current dimensions
// (admin types W/H manually).
const NAMED_PAGE_SIZES = {
  A4:     { w: 595,  h: 842 },
  A3:     { w: 842,  h: 1191 },
  Letter: { w: 612,  h: 792 },
}
function applyPageSize() {
  const def = NAMED_PAGE_SIZES[layout.pageSize]
  if (!def) return // Custom — leave width/height alone
  let isLandscape
  if (layout.orientation === 'landscape')      isLandscape = true
  else if (layout.orientation === 'portrait')  isLandscape = false
  else /* auto */                              isLandscape = layout.width >= layout.height
  layout.width  = isLandscape ? def.h : def.w
  layout.height = isLandscape ? def.w : def.h
  nextTick(fitStage)
}

const hasMargins = computed(() =>
  (layout.marginTop || layout.marginRight || layout.marginBottom || layout.marginLeft) > 0)

function zoomIn()  { zoom.value = Math.min(4, +(zoom.value + 0.1).toFixed(2)) }
function zoomOut() { zoom.value = Math.max(0.25, +(zoom.value - 0.1).toFixed(2)) }
function zoomFit() { zoom.value = 1 }
function onWheel(e) {
  if (!e.ctrlKey && !e.metaKey) return
  e.preventDefault()
  if (e.deltaY < 0) zoomIn(); else zoomOut()
}

function selectField(f) {
  selectedFieldId.value = f.id
}
function onStageClick(e) {
  if (e.target === e.target.getStage()) selectedFieldId.value = null
}

function uid() { return Math.random().toString(36).slice(2, 10) }

function addTextField() {
  const f = {
    id: uid(),
    kind: 'text',
    tag: null,
    text: 'New field',
    prefix: '',
    suffix: '',
    x: layout.width / 2,
    y: 200,
    fontSize: 28,
    color: '#000000',
    align: 'center',
    bold: false,
    italic: false,
    imageAssetId: null,
    width: 0,
    height: 0,
  }
  currentPage.value.fields.push(f)
  selectedFieldId.value = f.id
}

function openBgPicker() { pickerMode.value = 'bg'; pickerOpen.value = true }
function openImageFieldPicker() { pickerMode.value = 'fieldImage'; pickerOpen.value = true }

async function onPick(asset) {
  if (pickerMode.value === 'bg') {
    await useBackground(asset)
  } else {
    await pickFieldImage(asset)
  }
}

async function pickFieldImage(asset) {
  await preloadFieldImage(asset.letterAssetId)
  const img = fieldImages.value[asset.letterAssetId]
  // Default to 30% page width, preserving aspect.
  const targetW = Math.round(layout.width * 0.3)
  const aspect = img && img.naturalHeight ? img.naturalHeight / img.naturalWidth : 0.5
  const targetH = Math.round(targetW * aspect)

  if (selectedField.value && selectedField.value.kind === 'image') {
    selectedField.value.imageAssetId = asset.letterAssetId
    if (selectedField.value.lockAspect && img) {
      // Re-snap dimensions to the new image's aspect to avoid distortion.
      selectedField.value.height = Math.round(selectedField.value.width * (img.naturalHeight / img.naturalWidth))
    }
    if (!selectedField.value.width) selectedField.value.width = targetW
    if (!selectedField.value.height) selectedField.value.height = targetH
  } else {
    const f = {
      id: uid(),
      kind: 'image',
      tag: null,
      text: '',
      prefix: '',
      suffix: '',
      x: Math.round(layout.width / 2 - targetW / 2),
      y: Math.round(layout.height / 2 - targetH / 2),
      fontSize: 24,
      color: '#000000',
      align: 'left',
      bold: false,
      italic: false,
      imageAssetId: asset.letterAssetId,
      width: targetW,
      height: targetH,
      lockAspect: true,
    }
    currentPage.value.fields.push(f)
    selectedFieldId.value = f.id
  }
  pickerOpen.value = false
}

function onImageDimChange(which, raw) {
  const f = selectedField.value
  if (!f) return
  const next = Number(raw)
  if (!Number.isFinite(next) || next < 1) return
  if (!f.lockAspect) {
    f[which] = Math.round(next)
    return
  }
  const img = fieldImages.value[f.imageAssetId]
  if (!img || !img.naturalWidth || !img.naturalHeight) {
    f[which] = Math.round(next)
    return
  }
  const aspect = img.naturalHeight / img.naturalWidth
  if (which === 'width') {
    f.width = Math.round(next)
    f.height = Math.round(next * aspect)
  } else {
    f.height = Math.round(next)
    f.width = Math.round(next / aspect)
  }
}

async function preloadFieldImage(assetId) {
  if (!assetId || fieldImages.value[assetId]) return
  let blobUrl
  try { blobUrl = await fetchAssetBlobUrl(assetId) } catch { return }
  await new Promise(resolve => {
    const img = new Image()
    img.onload = () => { fieldImages.value = { ...fieldImages.value, [assetId]: img }; resolve() }
    img.onerror = () => resolve()
    img.src = blobUrl
  })
}

function removeSelected() {
  if (!selectedField.value) return
  const arr = currentPage.value.fields
  const idx = arr.findIndex(f => f.id === selectedFieldId.value)
  if (idx >= 0) arr.splice(idx, 1)
  selectedFieldId.value = null
}

function fontStyle(f) {
  const parts = []
  if (f.italic) parts.push('italic')
  if (f.bold) parts.push('bold')
  return parts.join(' ') || 'normal'
}

function textConfig(f) {
  const sz = (f.fontSize || 24) * scale.value
  const cfg = {
    text: previewText(f),
    fontSize: sz,
    fill: f.color || '#000',
    fontStyle: fontStyle(f),
    align: f.align || 'left',
    listening: true,
  }
  // Two layout modes — must mirror the QuestPDF backend exactly:
  //  - Width > 0: field.X is the LEFT of a width-bounded box; Konva
  //    handles wrap via the `width` attr; align applies INSIDE the box.
  //  - Width == 0 (legacy): center spans the full page; right anchors at field.X.
  if (f.width && f.width > 0) {
    cfg.width = f.width * scale.value
  } else if (f.align === 'center') {
    cfg.width = layout.width * scale.value
    cfg.offsetX = cfg.width / 2
  } else if (f.align === 'right') {
    cfg.width = (f.x || 0) * scale.value
    cfg.offsetX = cfg.width
  }
  return cfg
}

function previewText(f) {
  const body = f.tag ? f.tag : (f.text || '')
  return `${f.prefix || ''}${body}${f.suffix || ''}`
}

function hitRectConfig(f) {
  // Invisible click-target so empty/short fields are still selectable.
  let w, h, x = 0
  if (f.kind === 'image' || f.kind === 'transcriptTable' || f.kind === 'gradeStandardTable') {
    w = (f.width || 200) * scale.value
    h = (f.height || 200) * scale.value
  } else if (f.width && f.width > 0) {
    // Width-bounded text field: hit-rect matches the wrap box width and
    // estimates the wrapped height from the unwrapped char count.
    const fs = (f.fontSize || 24) * scale.value
    const charsPerLine = Math.max(10, Math.round(f.width / Math.max(4, f.fontSize / 2)))
    const lines = Math.max(1, Math.ceil((previewText(f).length || 1) / charsPerLine))
    w = f.width * scale.value
    h = fs * 1.2 * lines
  } else {
    w = (f.fontSize || 24) * scale.value * 6
    h = (f.fontSize || 24) * scale.value * 1.2
    if (f.align === 'center') x = -w/2
    else if (f.align === 'right') x = -w
  }
  return {
    x, y: 0, width: w, height: h,
    fill: selectedField.value && selectedField.value.id === f.id ? 'rgba(26,77,140,0.15)' : 'rgba(0,0,0,0)',
    stroke: selectedField.value && selectedField.value.id === f.id ? '#1a4d8c' : 'transparent',
    strokeWidth: 1,
  }
}

// transcriptTable and gradeStandardTable fields render as a labelled
// placeholder rectangle in the editor. The actual table is generated by the
// PDF renderer at release time using the student's grade data.
function placeholderRectConfig(f) {
  return {
    x: 0, y: 0,
    width:  Math.max(50, f.width  || 400) * scale.value,
    height: Math.max(40, f.height || 200) * scale.value,
    fill: 'rgba(26,77,140,0.06)',
    stroke: '#1a4d8c',
    strokeWidth: 1,
    dash: [6, 4],
  }
}
function placeholderLabelConfig(f) {
  const label = f.kind === 'transcriptTable' ? 'Grades Table (auto-generated)' : 'Grade Standard Table'
  return {
    x: 8 * scale.value,
    y: 8 * scale.value,
    text: label,
    fontSize: Math.max(10, 12 * scale.value),
    fontStyle: 'bold',
    fill: '#1a4d8c',
    listening: false,
  }
}

// Inline CSS for the HTML overlay div. Position+size match the Konva field
// so the rich-text content appears exactly where the canvas placeholder is.
function htmlFieldStyle(f) {
  const x = (f.x || 0) * scale.value
  const y = (f.y || 0) * scale.value
  const w = (f.width && f.width > 0)
    ? f.width * scale.value
    : (f.align === 'center' ? layout.width * scale.value : 'auto')
  const fontSize = (f.fontSize || 14) * scale.value
  const transform = f.align === 'center' && (!f.width || f.width <= 0)
    ? `translateX(${-(layout.width * scale.value) / 2 + x}px)` : 'none'
  const style = {
    left: `${x}px`,
    top: `${y}px`,
    width: typeof w === 'number' ? `${w}px` : w,
    fontSize: `${fontSize}px`,
    color: f.color || '#000',
    textAlign: f.align || 'left',
    fontWeight: f.bold ? 700 : 400,
    fontStyle: f.italic ? 'italic' : 'normal',
  }
  return style
}

function imageConfig(f) {
  const img = fieldImages.value[f.imageAssetId]
  if (!img) return { width: 0, height: 0 }
  return {
    image: img,
    width: (f.width || img.naturalWidth) * scale.value,
    height: (f.height || img.naturalHeight) * scale.value,
  }
}

function groupConfig(f) {
  return {
    x: (f.x || 0) * scale.value,
    y: (f.y || 0) * scale.value,
  }
}

function onDragEnd(e, f) {
  const node = e.target
  f.x = node.x() / scale.value
  f.y = node.y() / scale.value
}

async function loadBackground(assetId) {
  if (!assetId) { bgImage.value = null; return }
  let blobUrl
  try { blobUrl = await fetchAssetBlobUrl(assetId) } catch { bgImage.value = null; return }
  await new Promise((resolve) => {
    const img = new Image()
    img.onload = () => { bgImage.value = img; resolve() }
    img.onerror = () => { bgImage.value = null; resolve() }
    img.src = blobUrl
  })
}

async function useBackground(a) {
  currentPage.value.backgroundAssetId = a.letterAssetId
  await loadBackground(a.letterAssetId)
  pickerOpen.value = false
}

async function switchPage(idx) {
  if (idx === activePage.value || idx < 0 || idx >= pages.value.length) return
  activePage.value = idx
  selectedFieldId.value = null
  await loadBackground(currentPage.value.backgroundAssetId)
  await Promise.all(currentPage.value.fields
    .filter(f => f.kind === 'image' && f.imageAssetId)
    .map(f => preloadFieldImage(f.imageAssetId)))
}

function addPage() {
  if (pages.value.length >= 5) return
  pages.value.push({ backgroundAssetId: null, fields: [] })
  activePage.value = pages.value.length - 1
  selectedFieldId.value = null
  bgImage.value = null
}

function removeCurrentPage() {
  if (pages.value.length <= 1) return
  if (!confirm(`Delete page ${activePage.value + 1}? This can't be undone after Save.`)) return
  pages.value.splice(activePage.value, 1)
  const next = Math.max(0, Math.min(activePage.value, pages.value.length - 1))
  activePage.value = next
  selectedFieldId.value = null
  loadBackground(currentPage.value.backgroundAssetId)
}

async function loadProgrammes() {
  try {
    // Load core + partner programmes + partner names in parallel so the
    // dropdown can show "(Core)" or "(<Partner>)" suffixes.
    const [coreRes, partnerRes, partnersRes] = await Promise.all([
      apiClient.get('/v1/school/programmes?ownership=core'),
      apiClient.get('/v1/school/programmes?ownership=partner').catch(() => ({ data: { items: [] } })),
      apiClient.get('/v1/admin/school/partners').catch(() => ({ data: { items: [] } })),
    ])
    const all = [
      ...((coreRes.data.items ?? []).filter(p => !p.deletedAt)),
      ...((partnerRes.data.items ?? []).filter(p => !p.deletedAt)),
    ]
    // De-dupe by programmeId in case the two endpoints overlap.
    const byId = new Map()
    for (const p of all) byId.set(p.programmeId, p)
    programmes.value = Array.from(byId.values())
    const map = {}
    for (const p of (partnersRes.data.items ?? [])) {
      if (p.partnerId) map[p.partnerId] = p.name ?? p.displayName ?? '(unknown)'
    }
    partnerNames.value = map
  } catch {
    programmes.value = []
    partnerNames.value = {}
  }
}

/// Replace the current editor state with the layout parsed out of a sibling's
/// `certificateLayoutJson`. Shared between the sibling copy (Cert ↔ Prov) and
/// the cross-programme copy (same letter type from a different programme).
async function applyParsedLayout(parsed) {
  layout.width  = parsed.width  ?? layout.width
  layout.height = parsed.height ?? layout.height
  layout.pageSize    = parsed.pageSize    ?? layout.pageSize
  layout.orientation = parsed.orientation ?? layout.orientation
  layout.marginTop    = parsed.marginTop    ?? 0
  layout.marginRight  = parsed.marginRight  ?? 0
  layout.marginBottom = parsed.marginBottom ?? 0
  layout.marginLeft   = parsed.marginLeft   ?? 0
  const rawPages = Array.isArray(parsed.pages) && parsed.pages.length > 0
    ? parsed.pages
    : [{ backgroundAssetId: parsed.backgroundAssetId ?? null, fields: parsed.fields ?? [] }]
  pages.value = rawPages.slice(0, 5).map(p => ({
    backgroundAssetId: p?.backgroundAssetId ?? null,
    fields: (p?.fields ?? []).map(f => ({ ...f, id: uid() })),
  }))
  activePage.value = 0
  selectedFieldId.value = null
  await loadBackground(currentPage.value.backgroundAssetId)
  await Promise.all(currentPage.value.fields
    .filter(f => f.kind === 'image' && f.imageAssetId)
    .map(f => preloadFieldImage(f.imageAssetId)))
}

async function copyFromProgramme() {
  if (!copyFromProgrammeId.value || copying.value) return
  const src = otherProgrammes.value.find(p => p.programmeId === copyFromProgrammeId.value)
  const srcLabel = src
    ? `${src.code ? src.code + ' — ' : ''}${src.name} (${src.ownerLabel})`
    : 'the selected programme'
  if (!confirm(`Replace this ${titleFor(props.letterType)} with the one from "${srcLabel}"? Unsaved changes will be lost.`)) return
  copying.value = true
  try {
    const r = await apiClient.get(`/v1/admin/programmes/${copyFromProgrammeId.value}/letter-templates`)
    const source = (r.data.items ?? []).find(t => t.letterType === props.letterType)
    if (!source || !source.certificateLayoutJson) {
      alert(`"${srcLabel}" has no saved ${titleFor(props.letterType)}.`)
      return
    }
    let parsed
    try { parsed = JSON.parse(source.certificateLayoutJson) } catch { parsed = null }
    if (!parsed) { alert('Source layout JSON is invalid.'); return }
    await applyParsedLayout(parsed)
  } catch (e) {
    saveError.value = e.response?.data?.message ?? e.message ?? 'Copy failed'
  } finally {
    copying.value = false
  }
}

async function copyFromSibling() {
  const other = siblingType.value
  if (!other || copying.value) return
  if (!confirm(`Replace this template with the ${other === 'Certificate' ? 'Certificate' : 'Provisional Certificate'} layout? Unsaved changes will be lost.`)) return
  copying.value = true
  try {
    const r = await apiClient.get(`/v1/admin/programmes/${props.programmeId}/letter-templates`)
    const sibling = (r.data.items ?? []).find(t => t.letterType === other)
    if (!sibling || !sibling.certificateLayoutJson) {
      alert(`No saved layout found for ${other}. Save the source template first.`)
      return
    }
    let parsed
    try { parsed = JSON.parse(sibling.certificateLayoutJson) } catch { parsed = null }
    if (!parsed) { alert('Source template JSON is invalid.'); return }
    await applyParsedLayout(parsed)
  } catch (e) {
    saveError.value = e.response?.data?.message ?? e.message ?? 'Copy failed'
  } finally {
    copying.value = false
  }
}

function openRichEditor() {
  if (!selectedField.value) return
  richOpen.value = true
}

function onRichApply({ html, plain }) {
  const f = selectedField.value
  if (!f) return
  // Store both: htmlText for the PDF renderer's inline-formatted output,
  // text as the plain-string fallback shown on the canvas (Konva can't
  // render HTML, so the editor preview is unformatted).
  f.htmlText = html && html !== '<p></p>' ? html : ''
  f.text = plain || ''
}

async function onUpload(event) {
  const file = event.target.files?.[0]
  event.target.value = ''
  if (!file) return
  uploading.value = true
  uploadError.value = ''
  try {
    const fd = new FormData()
    fd.append('file', file)
    fd.append('name', file.name.replace(/\.[^.]+$/, ''))
    await apiClient.post('/v1/admin/letter-assets', fd, { headers: { 'Content-Type': 'multipart/form-data' } })
    // Refresh the asset list and blob-url cache.
    const r = await apiClient.get('/v1/admin/letter-assets')
    assets.value = r.data.items ?? []
    const next = { ...assetUrls.value }
    await Promise.all(assets.value.map(async (a) => {
      if (next[a.letterAssetId]) return
      try { next[a.letterAssetId] = await fetchAssetBlobUrl(a.letterAssetId) }
      catch { next[a.letterAssetId] = '' }
    }))
    assetUrls.value = next
  } catch (e) {
    uploadError.value = e.response?.data?.message ?? e.message ?? 'Upload failed'
  } finally {
    uploading.value = false
  }
}

async function deleteAsset(asset) {
  if (!confirm(`Delete "${asset.name}"? It will be hidden from the archive (soft delete).`)) return
  try {
    await apiClient.delete(`/v1/admin/letter-assets/${asset.letterAssetId}`)
    invalidateAssetBlobUrl(asset.letterAssetId)
    if (currentPage.value.backgroundAssetId === asset.letterAssetId) {
      currentPage.value.backgroundAssetId = null
      bgImage.value = null
    }
    // Refresh list
    const r = await apiClient.get('/v1/admin/letter-assets')
    assets.value = r.data.items ?? []
  } catch (e) {
    saveError.value = e.response?.data?.message ?? e.message ?? 'Delete failed'
  }
}

async function load() {
  if (!props.open || !props.programmeId) return
  loading.value = true
  loadError.value = ''
  try {
    const [tplRes, tagRes, assetRes] = await Promise.all([
      apiClient.get(`/v1/admin/programmes/${props.programmeId}/letter-templates`),
      apiClient.get('/v1/admin/letter-tags'),
      apiClient.get('/v1/admin/letter-assets'),
    ])
    tags.value = tagRes.data.items ?? []
    assets.value = assetRes.data.items ?? []
    const urlMap = { ...assetUrls.value }
    await Promise.all(assets.value.map(async (a) => {
      try { urlMap[a.letterAssetId] = await fetchAssetBlobUrl(a.letterAssetId) }
      catch { urlMap[a.letterAssetId] = '' }
    }))
    assetUrls.value = urlMap
    const existing = (tplRes.data.items ?? []).find(t => t.letterType === props.letterType)
    isPublished.value = !!existing?.isPublished
    let next
    try { next = JSON.parse(existing?.certificateLayoutJson ?? 'null') } catch { next = null }
    layout.width = next?.width ?? 2000
    layout.height = next?.height ?? 1414
    layout.pageSize = next?.pageSize ?? 'A4'
    layout.orientation = next?.orientation ?? 'auto'
    layout.marginTop = next?.marginTop ?? 0
    layout.marginRight = next?.marginRight ?? 0
    layout.marginBottom = next?.marginBottom ?? 0
    layout.marginLeft = next?.marginLeft ?? 0
    // Multi-page (preferred) or legacy single-page layout.
    const rawPages = Array.isArray(next?.pages) && next.pages.length > 0
      ? next.pages
      : [{ backgroundAssetId: next?.backgroundAssetId ?? null, fields: next?.fields ?? [] }]
    pages.value = rawPages.slice(0, 5).map(p => ({
      backgroundAssetId: p?.backgroundAssetId ?? null,
      fields: (p?.fields ?? []).map(f => ({
        id: f.id || uid(),
        kind: f.kind || 'text',
        tag: f.tag ?? null,
        text: f.text ?? '',
        prefix: f.prefix ?? '',
        suffix: f.suffix ?? '',
        x: f.x ?? 0,
        y: f.y ?? 0,
        fontSize: f.fontSize ?? 24,
        color: f.color ?? '#000000',
        align: f.align ?? 'left',
        bold: !!f.bold,
        italic: !!f.italic,
        imageAssetId: f.imageAssetId ?? null,
        width: f.width ?? 0,
        height: f.height ?? 0,
        lockAspect: f.lockAspect !== false, // default ON for image fields
        htmlText: f.htmlText ?? '',
      })),
    }))
    activePage.value = 0
    await loadBackground(currentPage.value.backgroundAssetId)
    // Preload image-field images for the active page; other pages are loaded on switch.
    await Promise.all(currentPage.value.fields
      .filter(f => f.kind === 'image' && f.imageAssetId)
      .map(f => preloadFieldImage(f.imageAssetId)))
  } catch (e) {
    loadError.value = e.response?.data?.message ?? e.message ?? 'Failed to load template'
  } finally {
    loading.value = false
  }
  // Fit the stage after the v-else template has actually been mounted —
  // doing this inside the try block above runs while loading=true and
  // stageWrap is still null.
  await nextTick()
  fitStage()
}

// Snapshot of the current canvas — shared between Save and Preview so the
// admin sees the in-flight edits, not whatever was last persisted.
function buildLayoutPayload() {
  return {
    certificateLayoutJson: JSON.stringify({
      width: layout.width,
      height: layout.height,
      pageSize: layout.pageSize,
      orientation: layout.orientation,
      marginTop: layout.marginTop,
      marginRight: layout.marginRight,
      marginBottom: layout.marginBottom,
      marginLeft: layout.marginLeft,
      pages: pages.value.map(p => ({
        backgroundAssetId: p.backgroundAssetId,
        fields: p.fields,
      })),
    }),
  }
}

async function onPreview() {
  if (previewing.value) return
  previewing.value = true
  saveError.value = ''
  try {
    const res = await apiClient.post(
      `/v1/admin/programmes/${props.programmeId}/letter-templates/${props.letterType}/preview`,
      buildLayoutPayload(),
      { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    window.open(url, '_blank')
    // Revoke after the new tab has had time to load it. Don't revoke
    // immediately — Firefox/Chrome both need the URL alive while the tab loads.
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch (e) {
    let msg = 'Preview failed'
    if (e.response?.data instanceof Blob) {
      try { msg = JSON.parse(await e.response.data.text())?.error ?? msg } catch { /* keep generic */ }
    } else {
      msg = e.response?.data?.error ?? e.message ?? msg
    }
    saveError.value = msg
  } finally {
    previewing.value = false
  }
}

async function onSave() {
  saving.value = true
  saveError.value = ''
  try {
    const payload = buildLayoutPayload()
    await apiClient.put(`/v1/admin/programmes/${props.programmeId}/letter-templates/${props.letterType}`, payload)
    emit('saved')
    emit('close')
  } catch (e) {
    saveError.value = e.response?.data?.message ?? e.message ?? 'Save failed'
  } finally {
    saving.value = false
  }
}

let resizeHandler = null
watch(() => props.open, (open) => {
  if (open) {
    copyFromProgrammeId.value = ''
    loadProgrammes()
    load()
    resizeHandler = () => fitStage()
    window.addEventListener('resize', resizeHandler)
  } else if (resizeHandler) {
    window.removeEventListener('resize', resizeHandler)
    resizeHandler = null
  }
})

onBeforeUnmount(() => {
  if (resizeHandler) window.removeEventListener('resize', resizeHandler)
})
</script>

<style scoped>
.modal-backdrop { position: fixed; inset: 0; background: rgba(20, 30, 50, 0.55); z-index: 1000; display: flex; align-items: center; justify-content: center; padding: 1rem; }
.modal { background: #fff; border-radius: 8px; width: min(1280px, 100%); height: 90vh; max-height: 95vh; display: flex; flex-direction: column; box-shadow: 0 10px 40px rgba(0,0,0,0.2); overflow: hidden; }
.modal-header { display: flex; justify-content: space-between; align-items: center; padding: 0.85rem 1.1rem; border-bottom: 1px solid #e6ebf2; }
.modal-header h3 { margin: 0; font-size: 1rem; color: #1a2d4f; }
.muted { color: #6b7888; font-weight: 400; font-size: .9rem; }
.pub-pill { display: inline-block; margin-left: .5rem; padding: .1rem .55rem; border-radius: 12px; font-size: .7rem; font-weight: 700; vertical-align: middle; }
.pub-pill-on  { background: #e6f6ec; color: #1c7a4a; border: 1px solid #b9e1c7; }
.pub-pill-off { background: #fff4e6; color: #b66a00; border: 1px solid #f0d2a8; }
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
.cert-toolbar { display: flex; align-items: center; gap: .4rem; padding: .55rem 1rem; border-bottom: 1px solid #e6ebf2; }
.tb-btn { border: 1px solid #cfd7e3; background: #fff; padding: .3rem .65rem; border-radius: 5px; font-size: .82rem; cursor: pointer; }
.tb-btn:disabled { opacity: .5; cursor: not-allowed; }
.tb-zoom { display: flex; align-items: center; gap: .25rem; margin-left: 1rem; padding: 0 .5rem; border-left: 1px solid #e6ebf2; }
.tb-zoom-btn { padding: .15rem .55rem; }
.tb-zoom-val { font-size: .75rem; color: #5f6e85; min-width: 3rem; text-align: center; }
.tb-info { font-size: .75rem; color: #6b7888; margin-left: auto; }
.tb-copy { border-color: #1c7a4a; color: #1c7a4a; }
.tb-copy:hover:not(:disabled) { background: #eaf6ec; }
.copy-prog-select { width: 100%; padding: .35rem .5rem; border: 1px solid #cfd7e3; border-radius: 4px; font-size: .8rem; box-sizing: border-box; margin-top: .25rem; }
.copy-prog-btn { width: 100%; margin-top: .35rem; border-color: #1c7a4a; color: #1c7a4a; }
.copy-prog-btn:hover:not(:disabled) { background: #eaf6ec; }
.change-img-btn { width: 100%; margin-top: .25rem; }
.rich-edit-btn { width: 100%; margin-top: .25rem; }
.img-preview-thumb { margin-top: .35rem; padding: .25rem; border: 1px dashed #cfd7e3; border-radius: 4px; text-align: center; }
.img-preview-thumb img { max-width: 100%; max-height: 80px; }

.cert-body { display: flex; flex: 1 1 auto; min-height: 400px; }
.cert-stage-wrap { flex: 1 1 auto; padding: .6rem; overflow: auto; background: #f0f2f7; min-width: 0; }
.cert-stage-stack { position: relative; }
.cert-html-overlay { position: absolute; inset: 0; pointer-events: none; }
.cert-html-field { position: absolute; pointer-events: none; line-height: 1.25; }
.cert-html-field :deep(p) { margin: 0 0 .35em; }
.cert-html-field :deep(p:last-child) { margin-bottom: 0; }
.cert-html-field :deep(h1) { font-size: 1.4em; font-weight: 700; margin: 0 0 .35em; }
.cert-html-field :deep(h2) { font-size: 1.2em; font-weight: 700; margin: 0 0 .35em; }
.cert-html-field :deep(h3) { font-size: 1.1em; font-weight: 700; margin: 0 0 .35em; }
.cert-html-field :deep(ul), .cert-html-field :deep(ol) { padding-left: 1.4em; margin: 0 0 .35em; }
.cert-html-field :deep(u) { text-decoration: underline; }
.cert-html-field :deep(s) { text-decoration: line-through; }
.cert-html-field :deep(table) { border-collapse: collapse; margin: .35em 0; }
.cert-html-field :deep(td), .cert-html-field :deep(th) { border: 1px solid #1a2d4f; padding: 2px 4px; vertical-align: top; }
.cert-html-field :deep(th) { background: #eef3fb; font-weight: 700; }
.cert-html-field :deep(hr) { border: 0; border-top: 1px solid #1a2d4f; margin: .35em 0; }
.cert-html-field :deep(mark) { padding: 0 2px; }
.cert-stage-placeholder { padding: 2rem; color: #6b7888; font-size: .85rem; text-align: center; }
.cert-side { width: 280px; border-left: 1px solid #e6ebf2; padding: .8rem; overflow-y: auto; font-size: .82rem; }
.cert-side h4 { margin: 0 0 .5rem; color: #1a2d4f; font-size: .9rem; }
.cert-side label { display: block; margin: .55rem 0 .15rem; font-size: .72rem; color: #5f6e85; font-weight: 600; text-transform: uppercase; letter-spacing: .04em; }
.cert-side input[type="text"], .cert-side input:not([type]), .cert-side input[type="number"], .cert-side textarea, .cert-side select { width: 100%; padding: .35rem .5rem; border: 1px solid #cfd7e3; border-radius: 4px; font-size: .82rem; box-sizing: border-box; }
.cert-side input[type="color"] { width: 100%; height: 30px; padding: 2px; border: 1px solid #cfd7e3; border-radius: 4px; }
.cert-side .row { display: flex; gap: .4rem; }
.cert-side .row > div { flex: 1; }
.cert-side .check label { display: flex; gap: .3rem; align-items: center; text-transform: none; font-weight: normal; font-size: .82rem; letter-spacing: 0; color: #1a2d4f; }
.cert-side-empty { color: #8a93a4; font-size: .85rem; padding: .5rem 0; }
.cert-side-hint { color: #8a93a4; font-size: .78rem; padding: .5rem 0; margin: .5rem 0 0; }
.mt-row { margin-top: .65rem; }

.modal-actions { display: flex; justify-content: flex-end; gap: .5rem; padding: .85rem 1.1rem; border-top: 1px solid #e6ebf2; }
.btn-ghost { border: 1px solid #cfd7e3; background: #fff; color: #1a2d4f; padding: .42rem .9rem; border-radius: 5px; cursor: pointer; }
.btn-ghost:disabled { opacity: .55; cursor: default; }
.btn-preview { border-color: #1a4d8c; color: #1a4d8c; font-weight: 600; }
.btn-preview:hover:not(:disabled) { background: #eef3fb; }
.btn-primary { border: 1px solid #1a4d8c; background: #1a4d8c; color: #fff; padding: .42rem 1rem; border-radius: 5px; cursor: pointer; font-weight: 700; }
.btn-primary:disabled { opacity: .55; cursor: default; }

.picker-backdrop { position: absolute; inset: 0; background: rgba(20,30,50,0.55); display: flex; align-items: center; justify-content: center; z-index: 10; padding: 1rem; }
.picker { background: #fff; border-radius: 8px; width: min(700px, 100%); max-height: 80%; display: flex; flex-direction: column; overflow: hidden; }
.picker-head { display: flex; align-items: center; gap: .65rem; padding: .65rem .9rem; border-bottom: 1px solid #e6ebf2; }
.picker-head h4 { margin: 0; flex: 1; font-size: .95rem; color: #1a2d4f; }
.upload-label { display: inline-block; cursor: pointer; }
.upload-label input[type="file"] { display: none; }
.upload-fake-btn { display: inline-block; padding: .35rem .75rem; border: 1px solid #1a4d8c; background: #fff; color: #1a4d8c; border-radius: 5px; font-size: .8rem; font-weight: 600; }
.upload-fake-btn:hover { background: #eef3fb; }
.picker-grid { padding: .8rem; display: grid; grid-template-columns: repeat(auto-fill, minmax(140px,1fr)); gap: .75rem; overflow-y: auto; }
.picker-item { position: relative; background: #fff; border: 1px solid #cfd7e3; border-radius: 6px; }
.picker-item:hover { border-color: #1a4d8c; }
.picker-pick { width: 100%; background: transparent; border: none; padding: .35rem; cursor: pointer; display: flex; flex-direction: column; align-items: center; gap: .35rem; }
.picker-pick img { max-width: 100%; max-height: 90px; object-fit: contain; }
.picker-loading { display: flex; align-items: center; justify-content: center; height: 90px; color: #b3bcc9; font-size: 1.2rem; }
.picker-name { font-size: .72rem; color: #5f6e85; word-break: break-word; text-align: center; }
.picker-del { position: absolute; top: 4px; right: 4px; width: 22px; height: 22px; padding: 0; border-radius: 50%; border: 1px solid #d4a4a4; background: rgba(255,255,255,0.9); color: #b63329; font-size: .8rem; cursor: pointer; line-height: 1; }
.picker-del:hover { background: #fde7e7; border-color: #b63329; }
</style>
