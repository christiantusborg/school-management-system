<script setup lang="ts">
// ADR-0039 Canvas strategy designer. Single-page WYSIWYG with
// positioned text + image fields over a background image. The layout
// JSON is the DocumentTemplate.MappingJson payload; at PDF-render
// time the browser embeds the background + each field via pdf-lib.
//
// Deferred (additive — none of these require schema changes):
//   - multiple pages (start with 1)
//   - zoom controls + fit-to-width (fixed 60% in v1)
//   - HTML rich-text overlays (basic text only for v1)

import { ref, computed, watch, onMounted } from 'vue'

export interface CanvasField {
  id: string
  kind: 'text' | 'image'
  x: number
  y: number
  width: number
  height: number
  // text-only
  text?: string
  fontSize?: number
  color?: string
  // image-only
  imageDataUrl?: string
  /** ADR-0039 image bank reference. When set, the field was inserted
   *  via the shared <ImageAssetBank> picker and may be re-resolved
   *  through the bank by id. The inline imageDataUrl is still kept
   *  so the canvas renderer can draw without a bank lookup. */
  imageAssetId?: string
}

export interface CanvasLayout {
  width: number
  height: number
  backgroundDataUrl: string | null
  fields: CanvasField[]
}

const props = defineProps<{ modelValue: CanvasLayout | null }>()
const emit = defineEmits<{ (e: 'update:modelValue', value: CanvasLayout): void }>()

const STAGE_W = 1240
const STAGE_H = 877

function emptyLayout(): CanvasLayout {
  return { width: STAGE_W, height: STAGE_H, backgroundDataUrl: null, fields: [] }
}

const layout = ref<CanvasLayout>(props.modelValue ?? emptyLayout())
const selectedFieldId = ref<string | null>(null)

watch(() => props.modelValue, v => {
  if (v) {
    layout.value = v
  }
}, { deep: true })

watch(layout, v => emit('update:modelValue', v), { deep: true })

const selectedField = computed<CanvasField | null>(() =>
  layout.value.fields.find(f => f.id === selectedFieldId.value) ?? null)

function uid(): string { return crypto.randomUUID() }

function addTextField() {
  const f: CanvasField = {
    id: uid(),
    kind: 'text',
    x: 100, y: 100, width: 320, height: 48,
    text: '{{fieldName}}',
    fontSize: 24,
    color: '#000000',
  }
  layout.value.fields.push(f)
  selectedFieldId.value = f.id
}

function addImageField() {
  const f: CanvasField = {
    id: uid(),
    kind: 'image',
    x: 100, y: 100, width: 200, height: 200,
    imageDataUrl: undefined,
  }
  layout.value.fields.push(f)
  selectedFieldId.value = f.id
}

function deleteSelected() {
  if (!selectedFieldId.value) {
    return
  }
  layout.value.fields = layout.value.fields.filter(f => f.id !== selectedFieldId.value)
  selectedFieldId.value = null
}

function selectField(id: string) {
  selectedFieldId.value = id
}

function onBackgroundUpload(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (!file) {
    return
  }
  const reader = new FileReader()
  reader.onload = () => { layout.value.backgroundDataUrl = String(reader.result) }
  reader.readAsDataURL(file)
}

function onFieldImageUpload(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (!file || !selectedField.value) {
    return
  }
  const reader = new FileReader()
  reader.onload = () => {
    if (selectedField.value) {
      selectedField.value.imageDataUrl = String(reader.result)
    }
  }
  reader.readAsDataURL(file)
}

// ---------------------------------------------------------------
// Drag handling. Konva supports draggable on the field group but we
// keep the geometry in the layout state — onDragEnd writes x/y back.
// ---------------------------------------------------------------

interface KonvaDragEvent { target: { x(): number; y(): number; id(): string } }

function onDragEnd(e: KonvaDragEvent, field: CanvasField) {
  field.x = Math.round(e.target.x())
  field.y = Math.round(e.target.y())
}

// Stage scale for fit-on-screen — we shrink the visible stage to a
// fixed width so the editor renders inside the form rather than
// requiring its own modal.
const stageScale = ref(0.6)
const stageW = computed(() => STAGE_W * stageScale.value)
const stageH = computed(() => STAGE_H * stageScale.value)

// Konva needs HTMLImageElement objects, not data URLs, for KImage
// configs. Maintain a per-id image element cache that re-loads when
// the data URL changes.
const fieldImageEls = ref<Record<string, HTMLImageElement | null>>({})
const backgroundEl = ref<HTMLImageElement | null>(null)

function rebuildImage(key: string, dataUrl: string | null | undefined, sink: (img: HTMLImageElement | null) => void) {
  if (!dataUrl) {
    sink(null)
    return
  }
  const img = new Image()
  img.src = dataUrl
  img.onload = () => { sink(img) }
  void key
}

watch(() => layout.value.backgroundDataUrl, v => rebuildImage('bg', v, img => { backgroundEl.value = img }), { immediate: true })
watch(() => layout.value.fields.map(f => `${f.id}:${f.imageDataUrl ?? ''}`).join('|'),
  () => {
    for (const f of layout.value.fields) {
      if (f.kind === 'image' && f.imageDataUrl) {
        if (!fieldImageEls.value[f.id]) {
          rebuildImage(f.id, f.imageDataUrl, img => { fieldImageEls.value[f.id] = img })
        }
      }
    }
  }, { immediate: true })

onMounted(() => {
  if (!props.modelValue) {
    emit('update:modelValue', layout.value)
  }
})
</script>

<template>
  <div class="canvas-designer">
    <div class="cd-toolbar">
      <button type="button" class="tb-btn" @click="addTextField">+ Text</button>
      <button type="button" class="tb-btn" @click="addImageField">+ Image</button>
      <button type="button" class="tb-btn" :disabled="!selectedField" @click="deleteSelected">Delete</button>
      <label class="tb-btn tb-file">
        Background
        <input type="file" accept="image/*" @change="onBackgroundUpload" hidden />
      </label>
      <span class="tb-info">Stage: {{ STAGE_W }}×{{ STAGE_H }} · zoom {{ Math.round(stageScale * 100) }}%</span>
    </div>

    <div class="cd-body">
      <div class="cd-stage" :style="{ width: stageW + 'px', height: stageH + 'px' }">
        <v-stage :config="{ width: stageW, height: stageH, scaleX: stageScale, scaleY: stageScale }">
          <v-layer>
            <v-image v-if="backgroundEl" :config="{ image: backgroundEl, width: STAGE_W, height: STAGE_H }" />
            <v-group v-for="f in layout.fields" :key="f.id"
              :config="{ x: f.x, y: f.y, draggable: true, id: f.id }"
              @click="selectField(f.id)" @tap="selectField(f.id)"
              @dragend="onDragEnd($event, f)">
              <v-rect :config="{ x: 0, y: 0, width: f.width, height: f.height,
                fill: selectedFieldId === f.id ? 'rgba(102,126,234,0.15)' : 'rgba(0,0,0,0)',
                stroke: selectedFieldId === f.id ? '#667eea' : 'rgba(0,0,0,0.15)',
                strokeWidth: 1, dash: [4, 4] }" />
              <v-text v-if="f.kind === 'text'"
                :config="{ x: 4, y: 4, width: f.width - 8, height: f.height - 8,
                  text: f.text ?? '', fontSize: f.fontSize ?? 24,
                  fill: f.color ?? '#000000', listening: false }" />
              <v-image v-else-if="fieldImageEls[f.id]"
                :config="{ x: 0, y: 0, width: f.width, height: f.height, image: fieldImageEls[f.id], listening: false }" />
            </v-group>
          </v-layer>
        </v-stage>
      </div>

      <aside class="cd-props">
        <p v-if="!selectedField" class="muted small">
          Click <strong>+ Text</strong> or <strong>+ Image</strong>
          to add a field. Drag a field on the stage to position it.
          Click <strong>Background</strong> to upload the background
          image drawn behind every field.
        </p>
        <template v-else>
          <h4>{{ selectedField.kind === 'text' ? 'Text field' : 'Image field' }}</h4>
          <label class="prop-row">
            <span>X</span>
            <input type="number" v-model.number="selectedField.x" class="text-input small" />
          </label>
          <label class="prop-row">
            <span>Y</span>
            <input type="number" v-model.number="selectedField.y" class="text-input small" />
          </label>
          <label class="prop-row">
            <span>Width</span>
            <input type="number" v-model.number="selectedField.width" class="text-input small" />
          </label>
          <label class="prop-row">
            <span>Height</span>
            <input type="number" v-model.number="selectedField.height" class="text-input small" />
          </label>
          <template v-if="selectedField.kind === 'text'">
            <label class="prop-row">
              <span>Text</span>
              <textarea v-model="selectedField.text" rows="3" class="text-input"
                placeholder="Literal or {{fieldName}} placeholder" />
            </label>
            <label class="prop-row">
              <span>Font size (px)</span>
              <input type="number" v-model.number="selectedField.fontSize" min="8" max="200" class="text-input small" />
            </label>
            <label class="prop-row">
              <span>Color</span>
              <input type="color" v-model="selectedField.color" class="text-input small" />
            </label>
          </template>
          <template v-else>
            <label class="prop-row">
              <span>Image</span>
              <input type="file" accept="image/*" @change="onFieldImageUpload" />
            </label>
            <p v-if="selectedField.imageDataUrl" class="muted small">
              Image embedded ({{ Math.round(selectedField.imageDataUrl.length / 1024) }} KB base64).
            </p>
          </template>
        </template>
      </aside>
    </div>
  </div>
</template>

<style scoped>
.canvas-designer { display: flex; flex-direction: column; gap: 0.5rem; }
.cd-toolbar { display: flex; align-items: center; gap: 0.4rem; flex-wrap: wrap; padding: 0.4rem; background: var(--surface-3); border-radius: 6px; }
.tb-btn { padding: 0.3rem 0.6rem; border: 1px solid var(--border-strong); background: white; border-radius: 4px; font-size: 0.82rem; cursor: pointer; }
.tb-btn:hover:not([disabled]) { background: var(--surface-2); }
.tb-btn[disabled] { opacity: 0.55; cursor: not-allowed; }
.tb-file { display: inline-flex; align-items: center; gap: 0.3rem; cursor: pointer; }
.tb-info { margin-left: auto; font-size: 0.74rem; color: var(--text-muted); }

.cd-body { display: grid; grid-template-columns: 1fr 280px; gap: 0.5rem; align-items: start; }
.cd-stage { background: repeating-conic-gradient(var(--surface-3) 0 25%, #fff 0 50%) 50% / 16px 16px; border: 1px solid var(--border-strong); overflow: hidden; }
.cd-props { padding: 0.5rem; border: 1px solid var(--border); border-radius: 6px; background: #fafafa; }
.cd-props h4 { margin: 0 0 0.5rem; font-size: 0.9rem; }
.prop-row { display: flex; flex-direction: column; gap: 0.2rem; font-size: 0.8rem; color: #374151; margin-bottom: 0.45rem; }
.prop-row > span { font-weight: 500; color: #4b5563; }
.text-input { padding: 0.3rem 0.45rem; border: 1px solid var(--border-strong); border-radius: 4px; font-size: 0.82rem; width: 100%; }
.text-input.small { width: auto; }
.muted { color: var(--text-muted); }
.small { font-size: 0.82rem; }
</style>
