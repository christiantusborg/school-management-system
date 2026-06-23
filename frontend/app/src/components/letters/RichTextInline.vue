<template>
  <div class="rti">
    <div class="rti-toolbar">
      <button type="button" class="rti-btn" :class="{ active: editor?.isActive('bold') }" @click="editor.chain().focus().toggleBold().run()" title="Bold"><strong>B</strong></button>
      <button type="button" class="rti-btn" :class="{ active: editor?.isActive('italic') }" @click="editor.chain().focus().toggleItalic().run()" title="Italic"><em>I</em></button>
      <button type="button" class="rti-btn" :class="{ active: editor?.isActive('underline') }" @click="editor.chain().focus().toggleUnderline().run()" title="Underline"><u>U</u></button>
      <button type="button" class="rti-btn" :class="{ active: editor?.isActive('strike') }" @click="editor.chain().focus().toggleStrike().run()" title="Strikethrough"><s>S</s></button>
      <span class="rti-sep" />
      <button type="button" class="rti-btn" :class="{ active: editor?.isActive('heading', { level: 2 }) }" @click="editor.chain().focus().toggleHeading({ level: 2 }).run()" title="Heading">H</button>
      <button type="button" class="rti-btn" :class="{ active: editor?.isActive('bulletList') }" @click="editor.chain().focus().toggleBulletList().run()" title="Bulleted list">•</button>
      <button type="button" class="rti-btn" :class="{ active: editor?.isActive('orderedList') }" @click="editor.chain().focus().toggleOrderedList().run()" title="Numbered list">1.</button>
      <span class="rti-sep" />
      <button type="button" class="rti-btn" :class="{ active: editor?.isActive({ textAlign: 'left' }) }" @click="editor.chain().focus().setTextAlign('left').run()" title="Align left">⯇</button>
      <button type="button" class="rti-btn" :class="{ active: editor?.isActive({ textAlign: 'center' }) }" @click="editor.chain().focus().setTextAlign('center').run()" title="Align center">≡</button>
      <button type="button" class="rti-btn" :class="{ active: editor?.isActive({ textAlign: 'right' }) }" @click="editor.chain().focus().setTextAlign('right').run()" title="Align right">⯈</button>
      <span class="rti-sep" />
      <label class="rti-color" title="Text color">
        <span :style="{ background: currentColor }" class="rti-color-swatch" />
        <input type="color" :value="currentColor" @input="setColor($event.target.value)" />
      </label>
      <button type="button" class="rti-btn" @click="editor.chain().focus().unsetColor().run()" title="Reset color">×</button>
      <span class="rti-sep" />
      <button type="button" class="rti-btn" :disabled="!editor?.can().undo()" @click="editor.chain().focus().undo().run()" title="Undo">↶</button>
      <button type="button" class="rti-btn" :disabled="!editor?.can().redo()" @click="editor.chain().focus().redo().run()" title="Redo">↷</button>
      <button type="button" class="rti-btn" @click="editor.chain().focus().setHorizontalRule().run()" title="Horizontal rule">―</button>
    </div>
    <editor-content :editor="editor" class="rti-area" />
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, onBeforeUnmount } from 'vue'
import { Editor, EditorContent } from '@tiptap/vue-3'
import StarterKit from '@tiptap/starter-kit'
import Underline from '@tiptap/extension-underline'
import { TextStyle } from '@tiptap/extension-text-style'
import { Color } from '@tiptap/extension-color'
import TextAlign from '@tiptap/extension-text-align'

const props = defineProps({
  modelValue: { type: String, default: '' },
})
const emit = defineEmits(['update:modelValue'])

const editor = ref(null)

// Older templates may hold plain text (newlines, no tags) rather than HTML.
// Detect that and convert to paragraphs/line breaks so it renders correctly;
// real HTML passes through untouched.
function seedFrom(val) {
  const s = val || ''
  if (!s.trim()) return '<p></p>'
  const looksHtml = /<\/?[a-z][\s\S]*>/i.test(s)
  if (looksHtml) return s
  const esc = s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
  return esc
    .split(/\n{2,}/)
    .map(p => `<p>${p.replace(/\n/g, '<br>')}</p>`)
    .join('')
}

onMounted(() => {
  editor.value = new Editor({
    extensions: [
      StarterKit,
      Underline,
      TextStyle,
      Color.configure({ types: ['textStyle'] }),
      TextAlign.configure({ types: ['heading', 'paragraph'] }),
    ],
    content: seedFrom(props.modelValue),
    onUpdate: ({ editor }) => emit('update:modelValue', editor.getHTML()),
  })
})

// Keep the editor in sync when the bound value is replaced wholesale (e.g. a
// different template loads). Guard against the echo from our own onUpdate.
watch(() => props.modelValue, (val) => {
  if (!editor.value) return
  if (val !== editor.value.getHTML()) {
    editor.value.commands.setContent(seedFrom(val), false)
  }
})

const currentColor = computed(() => editor.value?.getAttributes('textStyle')?.color || '#000000')
function setColor(c) { editor.value?.chain().focus().setColor(c).run() }

// Insert a literal string (e.g. a [tag] token) at the cursor. Exposed so the
// parent's tag picker can drop tokens into the rich body at the caret.
function insertText(text) {
  editor.value?.chain().focus().insertContent(text).run()
}
defineExpose({ insertText })

onBeforeUnmount(() => {
  if (editor.value) { editor.value.destroy(); editor.value = null }
})
</script>

<style scoped>
.rti { border: 1px solid #d8dde5; border-radius: 6px; overflow: hidden; background: #fff; }
.rti-toolbar { display: flex; gap: .25rem; align-items: center; padding: .4rem .5rem; border-bottom: 1px solid #e6ebf2; flex-wrap: wrap; background: #f8fafc; }
.rti-btn { border: 1px solid #cfd7e3; background: #fff; padding: .18rem .45rem; border-radius: 4px; font-size: .8rem; cursor: pointer; min-width: 1.8rem; line-height: 1.2; }
.rti-btn.active { background: #1a4d8c; color: #fff; border-color: #1a4d8c; }
.rti-btn:disabled { opacity: .5; cursor: not-allowed; }
.rti-color { display: inline-flex; align-items: center; gap: .2rem; cursor: pointer; border: 1px solid #cfd7e3; padding: .18rem .3rem; border-radius: 4px; }
.rti-color input[type="color"] { display: none; }
.rti-color-swatch { display: inline-block; width: 14px; height: 14px; border-radius: 3px; border: 1px solid #cfd7e3; }
.rti-sep { width: 1px; height: 18px; background: #cfd7e3; margin: 0 .2rem; }
.rti-area { min-height: 200px; max-height: 360px; overflow-y: auto; padding: .7rem .9rem; font-size: .88rem; line-height: 1.5; }
.rti-area :deep(.ProseMirror) { outline: none; min-height: 180px; }
.rti-area :deep(.ProseMirror p) { margin: 0 0 .6em; }
.rti-area :deep(.ProseMirror h2) { font-size: 1.15rem; margin: 0 0 .5em; color: #1a2d4f; }
.rti-area :deep(.ProseMirror ul), .rti-area :deep(.ProseMirror ol) { padding-left: 1.4rem; margin: 0 0 .6em; }
.rti-area :deep(.ProseMirror hr) { border: 0; border-top: 1px solid #cfd7e3; margin: .7em 0; }
.rti-area :deep(.ProseMirror s) { text-decoration: line-through; }
.rti-area :deep(.ProseMirror u) { text-decoration: underline; }
</style>
