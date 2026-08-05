<template>
  <div class="chg">
    <aside class="chg-list">
      <button v-for="e in CHANGELOG" :key="e.id"
              :class="['chg-item', { active: active?.id === e.id }]" @click="active = e">
        <span class="chg-date">{{ e.date }}</span>
        <strong class="chg-title">{{ e.title }}</strong>
        <span class="chg-summary">{{ e.summary }}</span>
      </button>
    </aside>
    <div class="chg-detail" v-if="active">
      <h2>{{ active.title }}</h2>
      <p class="chg-date-big">{{ active.date }}</p>
      <p class="chg-summary-big">{{ active.summary }}</p>
      <ul class="chg-bullets">
        <li v-for="(d, i) in active.details" :key="i">{{ d }}</li>
      </ul>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { CHANGELOG } from '../../data/changelog.js'
const active = ref(CHANGELOG[0] ?? null)
</script>

<style scoped>
.chg { display: flex; gap: .8rem; align-items: flex-start; }
.chg-list { width: 320px; flex-shrink: 0; display: flex; flex-direction: column; gap: .4rem; max-height: calc(100vh - 220px); overflow-y: auto; }
.chg-item { text-align: left; background: #fff; border: 1.5px solid #e0e6ee; border-radius: 8px; padding: .55rem .7rem; cursor: pointer; display: flex; flex-direction: column; gap: .1rem; }
.chg-item:hover { border-color: #a0b8d0; }
.chg-item.active { border-color: #0b2e59; background: #eef3fb; }
.chg-date { font-size: .68rem; color: #8a93a4; }
.chg-title { font-size: .86rem; color: #0b2e59; }
.chg-summary { font-size: .74rem; color: #5f6e85; }
.chg-detail { flex: 1; background: #fff; border: 1px solid #e0e6ee; border-radius: 8px; padding: 1rem 1.3rem; }
.chg-detail h2 { margin: 0 0 .2rem; font-size: 1.15rem; color: #0b2e59; }
.chg-date-big { color: #8a93a4; font-size: .78rem; margin: 0 0 .5rem; }
.chg-summary-big { font-size: .92rem; color: #334; font-weight: 600; }
.chg-bullets { padding-left: 1.2rem; }
.chg-bullets li { margin-bottom: .5rem; font-size: .88rem; color: #334; line-height: 1.45; }
</style>
