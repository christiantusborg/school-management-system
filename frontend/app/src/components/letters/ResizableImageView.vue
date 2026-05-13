<template>
  <node-view-wrapper class="ri-wrap" :class="{ selected }" :style="wrapStyle">
    <img
      :src="node.attrs.src"
      :alt="node.attrs.alt || ''"
      :data-asset-id="node.attrs['data-asset-id'] || null"
      class="ri-img"
      :style="imgStyle"
      @click="onImgClick"
    />
    <div v-if="selected" class="ri-handle" @mousedown="onHandleDown" @touchstart.prevent="onHandleDown"></div>
  </node-view-wrapper>
</template>

<script setup>
import { computed, ref, onBeforeUnmount } from 'vue'
import { NodeViewWrapper } from '@tiptap/vue-3'

const props = defineProps({
  node: { type: Object, required: true },
  updateAttributes: { type: Function, required: true },
  selected: { type: Boolean, default: false },
  editor: { type: Object, required: true },
  getPos: { type: Function, default: null },
})

const dragStart = ref(null)

const widthAttr = computed(() => {
  const w = props.node.attrs.width
  if (!w) return null
  return typeof w === 'number' ? `${w}px` : w
})
const wrapStyle = computed(() => widthAttr.value ? { width: widthAttr.value } : {})
const imgStyle = computed(() => widthAttr.value ? { width: '100%' } : {})

function onImgClick() {
  // Select this node so the resize handle appears.
  if (typeof props.getPos === 'function') {
    const pos = props.getPos()
    if (typeof pos === 'number') {
      props.editor.commands.setNodeSelection(pos)
    }
  }
}

function onHandleDown(e) {
  e.preventDefault()
  e.stopPropagation()
  const wrap = e.target.parentElement
  const startX = (e.touches?.[0]?.clientX ?? e.clientX)
  const startWidth = wrap.getBoundingClientRect().width
  dragStart.value = { startX, startWidth }
  document.addEventListener('mousemove', onHandleMove)
  document.addEventListener('mouseup', onHandleUp)
  document.addEventListener('touchmove', onHandleMove, { passive: false })
  document.addEventListener('touchend', onHandleUp)
}

function onHandleMove(e) {
  if (!dragStart.value) return
  if (e.cancelable) e.preventDefault()
  const cx = (e.touches?.[0]?.clientX ?? e.clientX)
  const dx = cx - dragStart.value.startX
  const next = Math.max(40, Math.round(dragStart.value.startWidth + dx))
  props.updateAttributes({ width: next })
}

function onHandleUp() {
  dragStart.value = null
  document.removeEventListener('mousemove', onHandleMove)
  document.removeEventListener('mouseup', onHandleUp)
  document.removeEventListener('touchmove', onHandleMove)
  document.removeEventListener('touchend', onHandleUp)
}

onBeforeUnmount(onHandleUp)
</script>

<style scoped>
.ri-wrap { position: relative; display: inline-block; max-width: 100%; line-height: 0; }
.ri-img { display: block; max-width: 100%; height: auto; cursor: pointer; }
.ri-wrap.selected .ri-img { outline: 2px solid #1a4d8c; outline-offset: -2px; }
.ri-handle { position: absolute; right: -6px; bottom: -6px; width: 14px; height: 14px; background: #1a4d8c; border: 2px solid #fff; border-radius: 50%; cursor: nwse-resize; box-shadow: 0 1px 3px rgba(0,0,0,0.25); }
</style>
