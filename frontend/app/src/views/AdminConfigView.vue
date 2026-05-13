<template>
  <div class="page-wrapper">
    <nav class="navbar">
      <span class="brand-text">IBSS Admin Portal</span>
      <div class="nav-links">
        <RouterLink to="/admin" class="nav-link">Dashboard</RouterLink>
        <RouterLink to="/admin/academic" class="nav-link">Academic</RouterLink>
        <RouterLink to="/admin/config" class="nav-link">System Config</RouterLink>
      </div>
      <div class="nav-right">
        <span class="nav-user">{{ auth.user?.displayName }}</span>
        <button class="btn-logout" @click="logout">Log out</button>
      </div>
    </nav>

    <div class="tab-bar">
      <button
        v-for="t in entities"
        :key="t.key"
        :class="['tab-btn', { active: activeTab === t.key }]"
        @click="activeTab = t.key"
      >
        {{ t.label }}
      </button>
    </div>

    <div class="container">
      <template v-for="t in entities" :key="t.key">
        <PathwayManager v-if="t.key === 'pathways'" v-show="activeTab === t.key" />
        <CrudManager v-else v-show="activeTab === t.key" :config="t.config" />
      </template>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { auth } from '../store/auth.js'
import CrudManager from '../components/crud/CrudManager.vue'
import PathwayManager from '../components/admin/PathwayManager.vue'

const router = useRouter()

function logout() {
  auth.logout()
  router.push('/login')
}

const entities = [
  { key: 'documentTypes',   label: 'Document Types',   config: { title: 'Document Types',   endpoint: '/v1/school/system-config/document-types' } },
  { key: 'educationLevels', label: 'Education Levels', config: { title: 'Education Levels', endpoint: '/v1/school/system-config/education-levels' } },
  { key: 'modesOfStudy',    label: 'Modes of Study',   config: { title: 'Modes of Study',   endpoint: '/v1/school/system-config/modes-of-study' } },
  { key: 'pathways',        label: 'Pathways',         config: { title: 'Pathways',         endpoint: '/v1/school/system-config/pathways' } },
]

const activeTab = ref(entities[0].key)
</script>

<style scoped>
.page-wrapper { min-height: 100vh; background: #f2f5f9; }

.navbar {
  background: #003366;
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.85rem 2rem;
  gap: 1rem;
}
.brand-text { font-size: 1.05rem; font-weight: 700; white-space: nowrap; }
.nav-links { display: flex; gap: 0.25rem; flex: 1; padding: 0 1rem; }
.nav-link {
  color: rgba(255,255,255,0.75);
  text-decoration: none;
  padding: 0.35rem 0.9rem;
  border-radius: 5px;
  font-size: 0.88rem;
  transition: background 0.15s, color 0.15s;
}
.nav-link:hover, .nav-link.router-link-active { background: rgba(255,255,255,0.15); color: #fff; }
.nav-right { display: flex; align-items: center; gap: 1rem; }
.nav-user { font-size: 0.88rem; opacity: 0.85; }
.btn-logout {
  background: rgba(255,255,255,0.12);
  border: 1px solid rgba(255,255,255,0.25);
  color: #fff;
  border-radius: 5px;
  padding: 0.3rem 0.8rem;
  font-size: 0.83rem;
  cursor: pointer;
}
.btn-logout:hover { background: rgba(255,255,255,0.22); }

.tab-bar {
  background: #fff;
  border-bottom: 2px solid #e8edf4;
  padding: 0 2rem;
  display: flex;
}
.tab-btn {
  background: none;
  border: none;
  padding: 0.85rem 1.25rem;
  font-size: 0.9rem;
  font-weight: 600;
  color: #888;
  cursor: pointer;
  border-bottom: 3px solid transparent;
  margin-bottom: -2px;
  transition: color 0.15s, border-color 0.15s;
}
.tab-btn.active { color: #003366; border-bottom-color: #003366; }
.tab-btn:hover:not(.active) { color: #333; }

.container { max-width: 1100px; margin: 2rem auto; padding: 0 1.5rem; }
</style>
