<template>
  <div v-if="open" class="rt-backdrop" @click.self="$emit('close')">
    <div class="rt-modal">
      <div class="rt-header">
        <h4>Rich text</h4>
        <button class="rt-close" @click="$emit('close')">✕</button>
      </div>
      <div class="rt-toolbar">
        <button type="button" class="rt-btn" :class="{ active: editor?.isActive('bold') }" @click="editor.chain().focus().toggleBold().run()" title="Bold"><strong>B</strong></button>
        <button type="button" class="rt-btn" :class="{ active: editor?.isActive('italic') }" @click="editor.chain().focus().toggleItalic().run()" title="Italic"><em>I</em></button>
        <button type="button" class="rt-btn" :class="{ active: editor?.isActive('underline') }" @click="editor.chain().focus().toggleUnderline().run()" title="Underline"><u>U</u></button>
        <button type="button" class="rt-btn" :class="{ active: editor?.isActive('strike') }" @click="editor.chain().focus().toggleStrike().run()" title="Strikethrough"><s>S</s></button>
        <span class="rt-sep" />
        <button type="button" class="rt-btn" :class="{ active: editor?.isActive('heading', { level: 1 }) }" @click="editor.chain().focus().toggleHeading({ level: 1 }).run()" title="Heading 1">H1</button>
        <button type="button" class="rt-btn" :class="{ active: editor?.isActive('heading', { level: 2 }) }" @click="editor.chain().focus().toggleHeading({ level: 2 }).run()" title="Heading 2">H2</button>
        <button type="button" class="rt-btn" :class="{ active: editor?.isActive('heading', { level: 3 }) }" @click="editor.chain().focus().toggleHeading({ level: 3 }).run()" title="Heading 3">H3</button>
        <span class="rt-sep" />
        <button type="button" class="rt-btn" :class="{ active: editor?.isActive('bulletList') }" @click="editor.chain().focus().toggleBulletList().run()" title="Bulleted list">•</button>
        <button type="button" class="rt-btn" :class="{ active: editor?.isActive('orderedList') }" @click="editor.chain().focus().toggleOrderedList().run()" title="Numbered list">1.</button>
        <span class="rt-sep" />
        <button type="button" class="rt-btn" :class="{ active: editor?.isActive({ textAlign: 'left' }) }" @click="editor.chain().focus().setTextAlign('left').run()" title="Align left">⯇</button>
        <button type="button" class="rt-btn" :class="{ active: editor?.isActive({ textAlign: 'center' }) }" @click="editor.chain().focus().setTextAlign('center').run()" title="Align center">≡</button>
        <button type="button" class="rt-btn" :class="{ active: editor?.isActive({ textAlign: 'right' }) }" @click="editor.chain().focus().setTextAlign('right').run()" title="Align right">⯈</button>
        <button type="button" class="rt-btn" :class="{ active: editor?.isActive({ textAlign: 'justify' }) }" @click="editor.chain().focus().setTextAlign('justify').run()" title="Justify">▤</button>
        <span class="rt-sep" />
        <label class="rt-color" title="Text color">
          <span :style="{ background: currentColor }" class="rt-color-swatch" />
          <input type="color" :value="currentColor" @input="setColor($event.target.value)" />
        </label>
        <button type="button" class="rt-btn" @click="editor.chain().focus().unsetColor().run()" title="Reset color">×</button>
        <span class="rt-sep" />
        <label class="rt-color" title="Highlight color">
          <span :style="{ background: currentHighlight }" class="rt-color-swatch" />
          <input type="color" :value="currentHighlight" @input="setHighlight($event.target.value)" />
        </label>
        <button type="button" class="rt-btn" @click="editor.chain().focus().unsetHighlight().run()" title="Remove highlight">×</button>
        <span class="rt-sep" />
        <button type="button" class="rt-btn" :disabled="!editor?.can().undo()" @click="editor.chain().focus().undo().run()" title="Undo">↶</button>
        <button type="button" class="rt-btn" :disabled="!editor?.can().redo()" @click="editor.chain().focus().redo().run()" title="Redo">↷</button>
        <button type="button" class="rt-btn" @click="editor.chain().focus().setHorizontalRule().run()" title="Horizontal rule">―</button>
        <span class="rt-sep" />
        <div class="rt-table-wrap" @keydown.escape="tableMenuOpen = false">
          <button type="button" class="rt-btn" :class="{ active: editor?.isActive('table') }" @click="tableMenuOpen = !tableMenuOpen" title="Table">▦ Table ▾</button>
          <div v-if="tableMenuOpen" class="rt-popover" @click.stop>
            <button type="button" class="rt-pop-btn" @click="insertTable">+ Insert table (3×3)</button>
            <div class="rt-pop-sep" />
            <button type="button" class="rt-pop-btn" :disabled="!editor?.can().addRowBefore()" @click="addRowAbove">↑ Row above</button>
            <button type="button" class="rt-pop-btn" :disabled="!editor?.can().addRowAfter()" @click="addRowBelow">↓ Row below</button>
            <button type="button" class="rt-pop-btn" :disabled="!editor?.can().addColumnBefore()" @click="addColLeft">← Column left</button>
            <button type="button" class="rt-pop-btn" :disabled="!editor?.can().addColumnAfter()" @click="addColRight">→ Column right</button>
            <div class="rt-pop-sep" />
            <button type="button" class="rt-pop-btn" :disabled="!editor?.can().deleteRow()" @click="deleteRow">✕ Delete row</button>
            <button type="button" class="rt-pop-btn" :disabled="!editor?.can().deleteColumn()" @click="deleteCol">✕ Delete column</button>
            <button type="button" class="rt-pop-btn" :disabled="!editor?.can().toggleHeaderRow()" @click="toggleHeaderRow">⇅ Toggle header row</button>
            <div class="rt-pop-sep" />
            <button type="button" class="rt-pop-btn rt-pop-danger" :disabled="!editor?.can().deleteTable()" @click="deleteTable">🗑 Delete table</button>
          </div>
        </div>
        <span class="rt-sep" />
        <button type="button" class="rt-btn" @click="clearFormatting" title="Clear all formatting">Clear</button>
      </div>
      <editor-content :editor="editor" class="rt-area" />
      <div class="rt-actions">
        <button class="rt-ghost" @click="$emit('close')">Cancel</button>
        <button class="rt-primary" @click="onApply">Apply</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onBeforeUnmount } from 'vue'
import { Editor, EditorContent } from '@tiptap/vue-3'
import StarterKit from '@tiptap/starter-kit'
import Underline from '@tiptap/extension-underline'
import { TextStyle } from '@tiptap/extension-text-style'
import { Color } from '@tiptap/extension-color'
import TextAlign from '@tiptap/extension-text-align'
import { Highlight } from '@tiptap/extension-highlight'
import { Table } from '@tiptap/extension-table'
import { TableRow } from '@tiptap/extension-table-row'
import { TableCell } from '@tiptap/extension-table-cell'
import { TableHeader } from '@tiptap/extension-table-header'

const props = defineProps({
  open: { type: Boolean, required: true },
  initialHtml: { type: String, default: '' },
  initialPlain: { type: String, default: '' },
})
const emit = defineEmits(['close', 'apply'])

const editor = ref(null)

function ensureEditor(content) {
  if (editor.value) {
    editor.value.commands.setContent(content || '<p></p>', false)
  } else {
    editor.value = new Editor({
      extensions: [
        StarterKit,
        Underline,
        TextStyle,
        Color.configure({ types: ['textStyle'] }),
        TextAlign.configure({ types: ['heading', 'paragraph'] }),
        Highlight.configure({ multicolor: true }),
        Table.configure({ resizable: true }),
        TableRow,
        TableHeader,
        TableCell,
      ],
      content: content || '<p></p>',
    })
  }
}

const currentColor = computed(() => editor.value?.getAttributes('textStyle')?.color || '#000000')
const currentHighlight = computed(() => editor.value?.getAttributes('highlight')?.color || '#fff59d')
const tableMenuOpen = ref(false)

function setColor(c) {
  editor.value?.chain().focus().setColor(c).run()
}
function setHighlight(c) {
  editor.value?.chain().focus().setHighlight({ color: c }).run()
}
function clearFormatting() {
  editor.value?.chain().focus().clearNodes().unsetAllMarks().run()
}

function tableCmd(fn) {
  return () => {
    const e = editor.value; if (!e) return
    fn(e.chain().focus()).run()
    tableMenuOpen.value = false
  }
}
const insertTable     = tableCmd((c) => c.insertTable({ rows: 3, cols: 3, withHeaderRow: true }))
const addRowAbove     = tableCmd((c) => c.addRowBefore())
const addRowBelow     = tableCmd((c) => c.addRowAfter())
const addColLeft      = tableCmd((c) => c.addColumnBefore())
const addColRight     = tableCmd((c) => c.addColumnAfter())
const deleteRow       = tableCmd((c) => c.deleteRow())
const deleteCol       = tableCmd((c) => c.deleteColumn())
const deleteTable     = tableCmd((c) => c.deleteTable())
const toggleHeaderRow = tableCmd((c) => c.toggleHeaderRow())

function onApply() {
  if (!editor.value) return
  const html = editor.value.getHTML()
  const plain = editor.value.getText()
  emit('apply', { html, plain })
  emit('close')
}

watch(() => props.open, (open) => {
  if (!open) return
  // Prefer provided HTML; fall back to plain text wrapped in a paragraph so
  // the editor has something to start with.
  const seed = props.initialHtml && props.initialHtml.length > 0
    ? props.initialHtml
    : `<p>${(props.initialPlain || '').replace(/&/g, '&amp;').replace(/</g, '&lt;')}</p>`
  ensureEditor(seed)
})

onBeforeUnmount(() => {
  if (editor.value) {
    editor.value.destroy()
    editor.value = null
  }
})
</script>

<style scoped>
.rt-backdrop { position: fixed; inset: 0; background: rgba(20, 30, 50, 0.6); z-index: 2000; display: flex; align-items: center; justify-content: center; padding: 1rem; }
.rt-modal { background: #fff; border-radius: 8px; width: min(720px, 100%); max-height: 80vh; display: flex; flex-direction: column; box-shadow: 0 10px 40px rgba(0,0,0,0.3); overflow: hidden; }
.rt-header { display: flex; justify-content: space-between; align-items: center; padding: .65rem .9rem; border-bottom: 1px solid #e6ebf2; }
.rt-header h4 { margin: 0; font-size: .95rem; color: #1a2d4f; }
.rt-close { border: none; background: transparent; font-size: 1rem; cursor: pointer; color: #6b7888; }
.rt-toolbar { display: flex; gap: .3rem; align-items: center; padding: .5rem .9rem; border-bottom: 1px solid #e6ebf2; flex-wrap: wrap; }
.rt-btn { border: 1px solid #cfd7e3; background: #fff; padding: .25rem .55rem; border-radius: 4px; font-size: .82rem; cursor: pointer; min-width: 2rem; }
.rt-btn.active { background: #1a4d8c; color: #fff; border-color: #1a4d8c; }
.rt-color { display: inline-flex; align-items: center; gap: .25rem; cursor: pointer; border: 1px solid #cfd7e3; padding: .25rem .35rem; border-radius: 4px; }
.rt-color input[type="color"] { display: none; }
.rt-color-swatch { display: inline-block; width: 16px; height: 16px; border-radius: 3px; border: 1px solid #cfd7e3; }
.rt-sep { width: 1px; height: 20px; background: #cfd7e3; margin: 0 .25rem; }
.rt-area { flex: 1 1 auto; min-height: 200px; max-height: 400px; padding: .8rem 1rem; overflow-y: auto; font-size: .9rem; line-height: 1.5; }
.rt-area :deep(.ProseMirror) { outline: none; min-height: 180px; }
.rt-area :deep(.ProseMirror p) { margin: 0 0 .65em; }
.rt-area :deep(.ProseMirror h2) { font-size: 1.2rem; margin: 0 0 .55em; color: #1a2d4f; }
.rt-area :deep(.ProseMirror h3) { font-size: 1.05rem; margin: 0 0 .55em; color: #1a2d4f; }
.rt-area :deep(.ProseMirror ul), .rt-area :deep(.ProseMirror ol) { padding-left: 1.4rem; margin: 0 0 .65em; }
.rt-area :deep(.ProseMirror h1) { font-size: 1.5rem; margin: 0 0 .55em; color: #1a2d4f; }
.rt-area :deep(.ProseMirror s) { text-decoration: line-through; }
.rt-area :deep(.ProseMirror u) { text-decoration: underline; }
.rt-area :deep(.ProseMirror table) { border-collapse: collapse; margin: .5em 0; width: 100%; table-layout: fixed; }
.rt-area :deep(.ProseMirror td), .rt-area :deep(.ProseMirror th) {
  border: 1px solid #cfd7e3; padding: 4px 6px; vertical-align: top; min-width: 40px; position: relative;
}
.rt-area :deep(.ProseMirror th) { background: #f6f9fd; font-weight: 700; }
.rt-area :deep(.ProseMirror hr) { border: 0; border-top: 1px solid #cfd7e3; margin: .8em 0; }
.rt-area :deep(.ProseMirror mark) { padding: 0 2px; }
.rt-area :deep(.ProseMirror .selectedCell:after) { background: rgba(26,77,140,0.12); content: ''; left: 0; right: 0; top: 0; bottom: 0; pointer-events: none; position: absolute; z-index: 2; }

/* Table popover */
.rt-table-wrap { position: relative; }
.rt-popover { position: absolute; top: calc(100% + 4px); left: 0; background: #fff; border: 1px solid #cfd7e3; border-radius: 6px; box-shadow: 0 4px 14px rgba(0,0,0,0.12); padding: .35rem; z-index: 30; min-width: 180px; display: flex; flex-direction: column; gap: 2px; }
.rt-pop-btn { text-align: left; border: none; background: transparent; padding: .35rem .55rem; border-radius: 4px; font-size: .82rem; cursor: pointer; color: #1a2d4f; }
.rt-pop-btn:hover:not(:disabled) { background: #eef3fb; }
.rt-pop-btn:disabled { color: #b3bcc9; cursor: not-allowed; }
.rt-pop-btn.rt-pop-danger { color: #b63329; }
.rt-pop-btn.rt-pop-danger:hover:not(:disabled) { background: #fde7e7; }
.rt-pop-sep { height: 1px; background: #e6ebf2; margin: .15rem 0; }
.rt-actions { display: flex; justify-content: flex-end; gap: .5rem; padding: .65rem .9rem; border-top: 1px solid #e6ebf2; }
.rt-ghost { border: 1px solid #cfd7e3; background: #fff; color: #1a2d4f; padding: .35rem .8rem; border-radius: 5px; cursor: pointer; }
.rt-primary { border: 1px solid #1a4d8c; background: #1a4d8c; color: #fff; padding: .35rem .9rem; border-radius: 5px; cursor: pointer; font-weight: 700; }
</style>
