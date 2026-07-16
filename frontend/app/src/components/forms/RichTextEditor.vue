<template>
  <div class="rich-text-editor">
    <!-- Mode toggle + Insert variable + Show-in-live switch -->
    <div v-if="isBuilderMode" class="editor-header d-flex align-center justify-space-between pa-2 bg-grey-lighten-4">
      <div class="d-flex align-center gap-2">
        <v-btn-toggle v-model="activeMode" mandatory density="compact" variant="outlined">
          <v-btn value="edit" size="small"><v-icon>mdi-pencil</v-icon>Edit</v-btn>
          <v-btn value="html" size="small"><v-icon>mdi-code-tags</v-icon>HTML</v-btn>
          <v-btn value="preview" size="small"><v-icon>mdi-eye</v-icon>Preview</v-btn>
        </v-btn-toggle>
        <v-btn
          v-if="activeMode === 'edit' && showVariableInsert !== false"
          size="small"
          variant="outlined"
          prepend-icon="mdi-code-brackets"
          @click="showInsertVariableDialog = true"
        >
          Insert Variable
        </v-btn>
      </div>
      <v-switch
        v-if="showLiveToggle !== false"
        v-model="showInLive"
        label="Show in Live"
        color="primary"
        density="compact"
        hide-details
        @update:model-value="$emit('update:showInLive', $event ?? false)"
      />
    </div>

    <!-- Edit mode: TipTap. Kept mounted via v-show so the HTML / Preview
         toggle doesn't tear down the editor state. -->
    <div v-show="isBuilderMode && activeMode === 'edit'" class="tiptap-editor-container">
      <div v-if="editor" class="tt-toolbar">
        <select :value="headingValue" @change="setHeading(($event.target as HTMLSelectElement).value)">
          <option value="0">Normal</option>
          <option value="1">H1</option>
          <option value="2">H2</option>
          <option value="3">H3</option>
          <option value="4">H4</option>
          <option value="5">H5</option>
          <option value="6">H6</option>
        </select>
        <select :value="currentFont" @change="setFont(($event.target as HTMLSelectElement).value)">
          <option value="">Default</option>
          <option value="Arial, sans-serif">Arial</option>
          <option value="Georgia, serif">Georgia</option>
          <option value="Tahoma, sans-serif">Tahoma</option>
          <option value="'Times New Roman', Times, serif">Times New Roman</option>
          <option value="Verdana, sans-serif">Verdana</option>
          <option value="'Courier New', Courier, monospace">Courier New</option>
          <option value="'Comic Sans MS', cursive">Comic Sans</option>
          <option value="'Trebuchet MS', sans-serif">Trebuchet MS</option>
        </select>
        <select :value="currentSize" @change="setFontSize(($event.target as HTMLSelectElement).value)">
          <option value="">Size</option>
          <option v-for="s in fontSizes" :key="s" :value="s">{{ s }}</option>
        </select>
        <button type="button" :class="['btn-tool', { active: editor.isActive('bold') }]" @click="editor.chain().focus().toggleBold().run()" title="Bold"><b>B</b></button>
        <button type="button" :class="['btn-tool', { active: editor.isActive('italic') }]" @click="editor.chain().focus().toggleItalic().run()" title="Italic"><i>I</i></button>
        <button type="button" :class="['btn-tool', { active: editor.isActive('underline') }]" @click="editor.chain().focus().toggleUnderline().run()" title="Underline"><u>U</u></button>
        <button type="button" :class="['btn-tool', { active: editor.isActive('strike') }]" @click="editor.chain().focus().toggleStrike().run()" title="Strikethrough"><s>S</s></button>
        <label class="btn-tool color-swatch" title="Text color">
          <span class="dot" :style="{ background: currentColor || '#111827' }" />
          <input type="color" :value="currentColor || '#111827'" @input="setColor(($event.target as HTMLInputElement).value)" />
        </label>
        <label class="btn-tool color-swatch" title="Highlight">
          <span class="dot dot-hl" :style="{ background: currentHighlight || '#fef08a' }" />
          <input type="color" :value="currentHighlight || '#fef08a'" @input="setHighlight(($event.target as HTMLInputElement).value)" />
        </label>
        <button type="button" :class="['btn-tool', { active: editor.isActive('subscript') }]" @click="editor.chain().focus().toggleSubscript().run()" title="Subscript">x₂</button>
        <button type="button" :class="['btn-tool', { active: editor.isActive('superscript') }]" @click="editor.chain().focus().toggleSuperscript().run()" title="Superscript">x²</button>
        <span class="sep" />
        <button type="button" :class="['btn-tool', { active: editor.isActive('bulletList') }]" @click="editor.chain().focus().toggleBulletList().run()" title="Bullet list">•</button>
        <button type="button" :class="['btn-tool', { active: editor.isActive('orderedList') }]" @click="editor.chain().focus().toggleOrderedList().run()" title="Numbered list">1.</button>
        <button type="button" :class="['btn-tool', { active: editor.isActive('taskList') }]" @click="editor.chain().focus().toggleTaskList().run()" title="Task list">☑</button>
        <button type="button" class="btn-tool" @click="editor.chain().focus().sinkListItem('listItem').run()" title="Indent">→</button>
        <button type="button" class="btn-tool" @click="editor.chain().focus().liftListItem('listItem').run()" title="Outdent">←</button>
        <span class="sep" />
        <button type="button" :class="['btn-tool', { active: editor.isActive({ textAlign: 'left' }) }]" @click="editor.chain().focus().setTextAlign('left').run()" title="Align left">⯇</button>
        <button type="button" :class="['btn-tool', { active: editor.isActive({ textAlign: 'center' }) }]" @click="editor.chain().focus().setTextAlign('center').run()" title="Center">≡</button>
        <button type="button" :class="['btn-tool', { active: editor.isActive({ textAlign: 'right' }) }]" @click="editor.chain().focus().setTextAlign('right').run()" title="Align right">⯈</button>
        <button type="button" :class="['btn-tool', { active: editor.isActive({ textAlign: 'justify' }) }]" @click="editor.chain().focus().setTextAlign('justify').run()" title="Justify">≣</button>
        <span class="sep" />
        <button type="button" :class="['btn-tool', { active: editor.isActive('blockquote') }]" @click="editor.chain().focus().toggleBlockquote().run()" title="Blockquote">❝</button>
        <button type="button" :class="['btn-tool', { active: editor.isActive('codeBlock') }]" @click="editor.chain().focus().toggleCodeBlock().run()" title="Code block">{}</button>
        <button type="button" class="btn-tool" @click="addLink" title="Insert link">🔗</button>
        <button type="button" class="btn-tool" @click="editor.chain().focus().setHorizontalRule().run()" title="Horizontal rule">—</button>
        <span class="sep" />
        <button type="button" class="btn-tool" @click="insertTable" title="Insert table">▦</button>
        <button v-if="editor.isActive('table')" type="button" class="btn-tool" @click="editor.chain().focus().addRowAfter().run()" title="Add row below">+R</button>
        <button v-if="editor.isActive('table')" type="button" class="btn-tool" @click="editor.chain().focus().addColumnAfter().run()" title="Add column right">+C</button>
        <button v-if="editor.isActive('table')" type="button" class="btn-tool" @click="editor.chain().focus().deleteRow().run()" title="Delete row">-R</button>
        <button v-if="editor.isActive('table')" type="button" class="btn-tool" @click="editor.chain().focus().deleteColumn().run()" title="Delete column">-C</button>
        <button v-if="editor.isActive('table')" type="button" class="btn-tool" @click="editor.chain().focus().deleteTable().run()" title="Delete table">✕▦</button>
        <span class="sep" />
        <button type="button" class="btn-tool" @click="editor.chain().focus().undo().run()" :disabled="!editor.can().undo()" title="Undo">↶</button>
        <button type="button" class="btn-tool" @click="editor.chain().focus().redo().run()" :disabled="!editor.can().redo()" title="Redo">↷</button>
        <button type="button" class="btn-tool" @click="editor.chain().focus().unsetAllMarks().clearNodes().run()" title="Clear formatting">⌫</button>
      </div>
      <EditorContent :editor="editor" class="tiptap-surface" />

      <!-- Floating image control: appears when an <img> inside the
           editor is clicked. Width presets + alignment + delete +
           4 corner-drag handles for free resize. -->
      <div
        v-if="selectedImg && imgPopupPos"
        class="img-popup"
        :style="{ left: `${imgPopupPos.left}px`, top: `${imgPopupPos.top}px` }"
        @mousedown.stop
      >
        <button type="button" @click="setImgWidth('25%')">25%</button>
        <button type="button" @click="setImgWidth('50%')">50%</button>
        <button type="button" @click="setImgWidth('75%')">75%</button>
        <button type="button" @click="setImgWidth('100%')">100%</button>
        <span class="sep" />
        <button type="button" @click="setImgAlign('left')" title="Align left">◧</button>
        <button type="button" @click="setImgAlign('center')" title="Center">▣</button>
        <button type="button" @click="setImgAlign('right')" title="Align right">◨</button>
        <span class="sep" />
        <button type="button" class="danger" @click="deleteSelectedImg">✕ Delete</button>
      </div>
      <template v-if="selectedImg && imgHandlePos">
        <div
          v-for="corner in (['tl', 'tr', 'bl', 'br'] as const)"
          :key="corner"
          class="img-handle"
          :style="handleStyle(corner)"
          @mousedown.prevent="startResize(corner, $event)"
        />
      </template>
    </div>

    <!-- HTML source view (raw textarea bound to v-model) -->
    <div v-if="isBuilderMode && activeMode === 'html'" class="html-editor-container">
      <textarea
        class="html-textarea"
        spellcheck="false"
        :value="modelValue"
        @input="onHtmlInput"
      />
    </div>

    <!-- Preview with {{fieldName}} substitution -->
    <div v-if="isBuilderMode && activeMode === 'preview'" class="preview-container pa-4 bg-white border">
      <div v-if="!showInLive" class="hidden-indicator mb-3">
        <v-alert type="info" variant="tonal" density="compact" prepend-icon="mdi-eye-off">
          This content is hidden in live questionnaire
        </v-alert>
      </div>
      <div class="preview-content" v-html="previewContent"></div>
    </div>

    <!-- Live mode (parent passed mode="live" or mode="preview") -->
    <div v-if="isLiveMode" class="live-content pa-4 bg-white">
      <div class="live-text-content" v-html="modelValue || '<p>No content</p>'"></div>
    </div>

    <InsertVariableDialog
      v-model="showInsertVariableDialog"
      :selected-questionnaire="selectedQuestionnaire"
      :available-fields="(availableFields as unknown as Array<{ id: string; label: string; type: ComponentType; required: boolean }>)"
      @insert-variable="insertVariable"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, ref, shallowRef, watch } from 'vue'
import type { ComponentType } from '@quvian/shared/types/questionnaire'
import { EditorContent, useEditor } from '@tiptap/vue-3'
import { StarterKit } from '@tiptap/starter-kit'
import { Underline } from '@tiptap/extension-underline'
import { TextAlign } from '@tiptap/extension-text-align'
// TipTap v3 consolidated text-style, color, font-family, font-size,
// and background color under extension-text-style. We pull all five
// from one package and the standalone extension-* packages stay
// installed but unused.
import { TextStyle, Color, FontFamily, FontSize } from '@tiptap/extension-text-style'
import { Highlight } from '@tiptap/extension-highlight'
import { Link } from '@tiptap/extension-link'
import { Image } from '@tiptap/extension-image'
import { Subscript } from '@tiptap/extension-subscript'
import { Superscript } from '@tiptap/extension-superscript'
import { Table } from '@tiptap/extension-table'
import { TableRow } from '@tiptap/extension-table-row'
import { TableCell } from '@tiptap/extension-table-cell'
import { TableHeader } from '@tiptap/extension-table-header'
import { TaskList } from '@tiptap/extension-task-list'
import { TaskItem } from '@tiptap/extension-task-item'
import InsertVariableDialog from './InsertVariableDialog.vue'

const props = defineProps<{
  modelValue?: string
  showInLive?: boolean
  availableFields?: Array<{
    id: string
    label: string
    type: string
    required?: boolean
  }>
  fieldAnswers?: Record<string, unknown>
  mode?: 'builder' | 'live' | 'preview'
  disabled?: boolean
  selectedQuestionnaire?: unknown
  // When false, hide the questionnaire-builder-specific header controls so the same editor can be
  // reused standalone (e.g. CRM Message Templates, which has its own merge-token buttons). Omitted
  // (undefined) keeps them visible, so existing callers are unchanged.
  showVariableInsert?: boolean
  showLiveToggle?: boolean
}>()

const emit = defineEmits<{
  'update:modelValue': [value: string]
  'update:showInLive': [value: boolean]
}>()

const activeMode = ref<'edit' | 'html' | 'preview'>('edit')
const showInLive = ref(props.showInLive ?? true)
const showInsertVariableDialog = ref(false)
const fontSizes = ['8px', '10px', '12px', '14px', '16px', '18px', '20px', '24px', '28px', '32px', '36px', '48px', '64px']

const isBuilderMode = computed(() => props.mode === 'builder' || props.mode === undefined)
const isLiveMode = computed(() => props.mode === 'live' || props.mode === 'preview')

const editor = useEditor({
  content: props.modelValue ?? '',
  extensions: [
    StarterKit.configure({
      heading: { levels: [1, 2, 3, 4, 5, 6] },
    }),
    Underline,
    TextStyle,
    Color,
    Highlight.configure({ multicolor: true }),
    FontFamily.configure({ types: ['textStyle'] }),
    FontSize,
    TextAlign.configure({ types: ['heading', 'paragraph'] }),
    Link.configure({ openOnClick: false, autolink: true }),
    Image.configure({ inline: false, allowBase64: true }),
    Subscript,
    Superscript,
    Table.configure({ resizable: true }),
    TableRow,
    TableHeader,
    TableCell,
    TaskList,
    TaskItem.configure({ nested: true }),
  ],
  onUpdate: ({ editor: e }) => {
    emit('update:modelValue', e.getHTML())
  },
})

// Watch parent v-model so HTML-mode edits or image-bank inserts
// (which append directly to formGenerateHtml) flow back into the
// TipTap document.
watch(() => props.modelValue, v => {
  const e = editor.value
  if (!e) {
    return
  }
  if (v !== e.getHTML()) {
    e.commands.setContent(v ?? '', { emitUpdate: false })
  }
})

watch(() => props.showInLive, v => { showInLive.value = v ?? true })

// Toolbar state derived from the editor's current selection. Calls
// through `editor.value` so the dropdowns update as the caret moves.
const headingValue = computed(() => {
  const e = editor.value
  if (!e) {
    return '0'
  }
  for (let lvl = 1; lvl <= 6; lvl++) {
    if (e.isActive('heading', { level: lvl })) {
      return String(lvl)
    }
  }
  return '0'
})
const currentFont = computed(() => editor.value?.getAttributes('textStyle').fontFamily ?? '')
const currentSize = computed(() => editor.value?.getAttributes('textStyle').fontSize ?? '')
const currentColor = computed(() => editor.value?.getAttributes('textStyle').color ?? '')
const currentHighlight = computed(() => editor.value?.getAttributes('highlight').color ?? '')

function setHeading(level: string) {
  const e = editor.value
  if (!e) {
    return
  }
  const n = Number(level)
  if (n === 0) {
    e.chain().focus().setParagraph().run()
  } else {
    e.chain().focus().toggleHeading({ level: n as 1 | 2 | 3 | 4 | 5 | 6 }).run()
  }
}
function setFont(family: string) {
  const e = editor.value
  if (!e) {
    return
  }
  if (family === '') {
    e.chain().focus().unsetFontFamily().run()
  } else {
    e.chain().focus().setFontFamily(family).run()
  }
}
function setFontSize(size: string) {
  const e = editor.value
  if (!e) {
    return
  }
  if (size === '') {
    e.chain().focus().unsetFontSize().run()
  } else {
    e.chain().focus().setFontSize(size).run()
  }
}
function setColor(color: string) {
  editor.value?.chain().focus().setColor(color).run()
}
function setHighlight(color: string) {
  editor.value?.chain().focus().toggleHighlight({ color }).run()
}
function addLink() {
  const e = editor.value
  if (!e) {
    return
  }
  const prev = e.getAttributes('link').href ?? ''
  const url = prompt('URL?', prev)
  if (url === null) {
    return
  }
  if (url === '') {
    e.chain().focus().unsetLink().run()
  } else {
    e.chain().focus().extendMarkRange('link').setLink({ href: url }).run()
  }
}
function insertTable() {
  const e = editor.value
  if (!e) {
    return
  }
  const cols = Math.max(1, Math.min(20, Number(prompt('Columns?', '3')) || 3))
  const rows = Math.max(1, Math.min(50, Number(prompt('Rows?', '3')) || 3))
  e.chain().focus().insertTable({ rows, cols, withHeaderRow: true }).run()
}

function onHtmlInput(e: Event) {
  emit('update:modelValue', (e.target as HTMLTextAreaElement).value)
}

/**
 * Insert arbitrary HTML at the current selection. Used by the image
 * bank picker (writes <img src=data:…>) and by external callers via
 * defineExpose.
 */
async function insertHtml(html: string) {
  const e = editor.value
  if (!e) {
    emit('update:modelValue', (props.modelValue ?? '') + html)
    return
  }
  await nextTick()
  e.chain().focus().insertContent(html).run()
}

function insertVariable(variable: string) {
  editor.value?.chain().focus().insertContent(variable).run()
}

// Preview tab — substitute {{fieldName}} placeholders with a tag so
// the user sees where variables will land at render time. Same logic
// as the prior Quill version.
const previewContent = computed(() => {
  if (!props.modelValue) {
    return '<p>No content</p>'
  }
  let content = props.modelValue
  const variableRegex = /\{\{([^}]+)\}\}|\[([^\]]+)\]/g
  content = content.replace(variableRegex, (match) => `<span class="variable-placeholder">${match}</span>`)
  return content
})

// === Image click popup + corner-drag resize ===
// Listens at the EditorContent DOM level. The popup floats over the
// editor surface in container-relative coordinates so it survives
// scroll inside the editor wrapper.
const selectedImg = shallowRef<HTMLImageElement | null>(null)
const imgPopupPos = ref<{ left: number; top: number } | null>(null)
const imgHandlePos = ref<{ left: number; top: number; width: number; height: number } | null>(null)

function refreshImgRects() {
  const img = selectedImg.value
  const surface = document.querySelector('.tiptap-editor-container .tiptap-surface')
  if (!img || !surface) {
    imgPopupPos.value = null
    imgHandlePos.value = null
    return
  }
  const cRect = (surface.parentElement as HTMLElement).getBoundingClientRect()
  const iRect = img.getBoundingClientRect()
  const left = iRect.left - cRect.left
  const top = iRect.top - cRect.top
  imgHandlePos.value = { left, top, width: iRect.width, height: iRect.height }
  const popupTop = top - 36 < 4 ? top + iRect.height + 8 : top - 36
  imgPopupPos.value = { left, top: popupTop }
}

function selectImg(target: HTMLImageElement | null) {
  selectedImg.value = target
  if (target) {
    refreshImgRects()
  } else {
    imgPopupPos.value = null
    imgHandlePos.value = null
  }
}

function syncModelFromEditor() {
  const e = editor.value
  if (e) {
    emit('update:modelValue', e.getHTML())
  }
}

function setImgWidth(width: string) {
  const img = selectedImg.value
  if (!img) {
    return
  }
  img.style.width = width
  img.style.height = 'auto'
  refreshImgRects()
  syncModelFromEditor()
}

function setImgAlign(side: 'left' | 'center' | 'right') {
  const img = selectedImg.value
  if (!img) {
    return
  }
  img.style.display = 'block'
  if (side === 'center') {
    img.style.marginLeft = 'auto'
    img.style.marginRight = 'auto'
  } else if (side === 'left') {
    img.style.marginLeft = '0'
    img.style.marginRight = 'auto'
  } else {
    img.style.marginLeft = 'auto'
    img.style.marginRight = '0'
  }
  refreshImgRects()
  syncModelFromEditor()
}

function deleteSelectedImg() {
  const img = selectedImg.value
  if (!img) {
    return
  }
  img.remove()
  selectImg(null)
  syncModelFromEditor()
}

function startResize(corner: 'tl' | 'tr' | 'bl' | 'br', e: MouseEvent) {
  const img = selectedImg.value
  if (!img) {
    return
  }
  const startX = e.clientX
  const startY = e.clientY
  const startW = img.getBoundingClientRect().width
  const startH = img.getBoundingClientRect().height
  const aspect = startH > 0 ? startW / startH : 1

  function onMove(ev: MouseEvent) {
    const dx = ev.clientX - startX
    const dy = ev.clientY - startY
    const widthDelta = (corner === 'br' || corner === 'tr') ? dx : -dx
    let newW = Math.max(20, startW + widthDelta)
    const heightDelta = (corner === 'br' || corner === 'bl') ? dy : -dy
    const fromHeight = Math.max(20, startH + heightDelta) * aspect
    newW = Math.max(newW, fromHeight)
    img!.style.width = `${Math.round(newW)}px`
    img!.style.height = 'auto'
    refreshImgRects()
  }

  function onUp() {
    window.removeEventListener('mousemove', onMove)
    window.removeEventListener('mouseup', onUp)
    syncModelFromEditor()
  }

  window.addEventListener('mousemove', onMove)
  window.addEventListener('mouseup', onUp)
}

function handleStyle(corner: 'tl' | 'tr' | 'bl' | 'br') {
  const p = imgHandlePos.value
  if (!p) {
    return {}
  }
  const size = 10
  const half = size / 2
  const isTop = corner === 'tl' || corner === 'tr'
  const isLeft = corner === 'tl' || corner === 'bl'
  const cursor = (corner === 'tl' || corner === 'br') ? 'nwse-resize' : 'nesw-resize'
  return {
    left: `${(isLeft ? p.left : p.left + p.width) - half}px`,
    top: `${(isTop ? p.top : p.top + p.height) - half}px`,
    width: `${size}px`,
    height: `${size}px`,
    cursor,
  }
}

function onEditorClick(e: MouseEvent) {
  const target = e.target as HTMLElement
  if (target.tagName === 'IMG') {
    selectImg(target as HTMLImageElement)
    e.preventDefault()
  } else if (!target.closest('.img-popup') && !target.closest('.img-handle')) {
    selectImg(null)
  }
}

function onEditorScroll() {
  if (selectedImg.value) {
    refreshImgRects()
  }
}

watch(editor, e => {
  if (e) {
    e.view.dom.addEventListener('click', onEditorClick)
    e.view.dom.addEventListener('scroll', onEditorScroll)
  }
})

watch(activeMode, m => {
  if (m !== 'edit') {
    selectImg(null)
  }
})

// No onBeforeUnmount cleanup: useEditor() registers an unmount hook
// that destroys the editor (and frees its DOM) before ours would
// run, so manual removeEventListener on view.dom would throw
// (parentNode is null on a destroyed view). The click/scroll
// listeners live on the editor's own DOM and are GC'd along with
// it — no manual cleanup needed.

defineExpose({ insertHtml, insertVariable })
</script>

<style scoped>
.rich-text-editor {
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 4px;
  overflow: hidden;
}
.editor-header {
  border-bottom: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
}
.tiptap-editor-container {
  position: relative;
  min-height: 240px;
  background: white;
}
.tt-toolbar {
  display: flex;
  flex-wrap: wrap;
  gap: 2px;
  align-items: center;
  padding: 6px 8px;
  border-bottom: 1px solid var(--border);
  background: var(--surface-2);
}
.tt-toolbar select,
.tt-toolbar .btn-tool {
  border: 1px solid var(--border-strong);
  background: white;
  padding: 2px 6px;
  border-radius: 4px;
  font-size: 0.82rem;
  cursor: pointer;
  color: #111827;
  min-width: 24px;
  height: 26px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}
.tt-toolbar .btn-tool:hover { background: #eef2ff; border-color: #a5b4fc; }
.tt-toolbar .btn-tool.active { background: #c7d2fe; border-color: #6366f1; }
.tt-toolbar .btn-tool:disabled { opacity: 0.4; cursor: not-allowed; }
.tt-toolbar .color-swatch { position: relative; padding: 0 4px; }
.tt-toolbar .color-swatch .dot { width: 14px; height: 14px; border-radius: 3px; border: 1px solid #94a3b8; }
.tt-toolbar .color-swatch .dot-hl { border-color: #facc15; }
.tt-toolbar .color-swatch input[type=color] { position: absolute; inset: 0; opacity: 0; cursor: pointer; }
.tt-toolbar .sep { width: 1px; height: 18px; background: var(--border-strong); margin: 0 4px; }

.tiptap-surface :deep(.ProseMirror) {
  min-height: 220px;
  padding: 12px 16px;
  outline: none;
}
.tiptap-surface :deep(.ProseMirror p) { margin: 0 0 0.5em; }
.tiptap-surface :deep(.ProseMirror blockquote) {
  border-left: 3px solid #94a3b8; padding-left: 0.75em; color: #475569; margin: 0.5em 0;
}
.tiptap-surface :deep(.ProseMirror pre) {
  background: #0f172a; color: #f8fafc; padding: 0.75em; border-radius: 6px; overflow-x: auto;
}
.tiptap-surface :deep(.ProseMirror table) {
  border-collapse: collapse; margin: 0.5em 0; table-layout: fixed; width: 100%;
}
.tiptap-surface :deep(.ProseMirror table td),
.tiptap-surface :deep(.ProseMirror table th) {
  border: 1px solid var(--border-strong); padding: 6px 8px; min-width: 40px; vertical-align: top;
}
.tiptap-surface :deep(.ProseMirror table th) { background: #f1f5f9; font-weight: 600; }
.tiptap-surface :deep(.ProseMirror img) { max-width: 100%; }
.tiptap-surface :deep(.ProseMirror ul[data-type="taskList"]) { list-style: none; padding-left: 0; }
.tiptap-surface :deep(.ProseMirror ul[data-type="taskList"] li) { display: flex; gap: 0.5em; }

.preview-container {
  min-height: 200px;
  border-top: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
}
.preview-content :deep(.variable-placeholder) {
  background-color: rgba(var(--v-theme-warning), 0.2);
  padding: 2px 4px;
  border-radius: 4px;
  font-style: italic;
  color: rgba(var(--v-theme-warning));
}
.hidden-indicator { opacity: 0.8; }

/* Image popup + resize handles */
.img-popup {
  position: absolute;
  z-index: 50;
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 4px 6px;
  background: white;
  border: 1px solid var(--border-strong);
  border-radius: 6px;
  box-shadow: 0 4px 12px rgba(15, 23, 42, 0.18);
  font-size: 0.78rem;
  user-select: none;
}
.img-popup button {
  border: 1px solid var(--border-strong);
  background: var(--surface-2);
  padding: 2px 8px;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.78rem;
  color: #374151;
}
.img-popup button:hover { background: #eef2ff; border-color: #a5b4fc; }
.img-popup button.danger { color: #b91c1c; border-color: #fecaca; background: #fef2f2; }
.img-popup button.danger:hover { background: #fee2e2; }
.img-popup .sep { width: 1px; height: 18px; background: var(--border); margin: 0 2px; }
.img-handle {
  position: absolute;
  z-index: 49;
  background: #4f46e5;
  border: 2px solid white;
  border-radius: 2px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.35);
}

.html-editor-container { min-height: 200px; display: flex; }
.html-textarea {
  flex: 1;
  min-height: 240px;
  border: none;
  outline: none;
  padding: 0.75rem 1rem;
  font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
  font-size: 0.82rem;
  line-height: 1.45;
  resize: vertical;
  background: #fafafa;
  color: #111827;
}
</style>
