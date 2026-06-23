<template>
  <div v-if="updateAvailable" class="vc-banner">
    <span>A new version of the portal is available.</span>
    <button class="vc-reload" @click="manualReload">Reload now</button>
  </div>
  <RouterView />
</template>

<script setup>
import { RouterView } from 'vue-router'
import { updateAvailable, manualReload } from './utils/versionCheck.js'
</script>

<style scoped>
.vc-banner {
  position: fixed; top: 0; left: 0; right: 0; z-index: 4000;
  display: flex; align-items: center; justify-content: center; gap: .8rem;
  background: #1a4d8c; color: #fff; padding: .5rem 1rem; font-size: .85rem;
  box-shadow: 0 2px 8px rgba(0,0,0,.2);
}
.vc-reload {
  border: 1px solid #fff; background: #fff; color: #1a4d8c; border-radius: 5px;
  padding: .25rem .8rem; font-weight: 700; cursor: pointer; font-size: .82rem;
}
.vc-reload:hover { background: #eef3fb; }
</style>
