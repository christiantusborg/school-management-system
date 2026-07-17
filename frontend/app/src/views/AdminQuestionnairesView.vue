<template>
  <div class="page-wrapper">
    <nav class="navbar">
      <span class="brand-text">MGW Admin Portal</span>
      <div class="nav-links">
        <RouterLink to="/admin" class="nav-link">Dashboard</RouterLink>
        <RouterLink to="/admin/academic" class="nav-link">Academic</RouterLink>
        <RouterLink to="/admin/questionnaires" class="nav-link">Questionnaires</RouterLink>
        <RouterLink to="/admin/config" class="nav-link">System Config</RouterLink>
      </div>
      <div class="nav-right">
        <span class="nav-user">{{ auth.user?.displayName }}</span>
        <button class="btn-logout" @click="logout">Log out</button>
      </div>
    </nav>

    <div class="tab-bar">
      <button :class="['tab-btn', { active: tab === 'builder' }]" @click="tab = 'builder'">Builder</button>
      <button :class="['tab-btn', { active: tab === 'templates' }]" @click="tab = 'templates'">Templates</button>
      <button :class="['tab-btn', { active: tab === 'assignments' }]" @click="tab = 'assignments'">Assignments &amp; Responses</button>
      <button :class="['tab-btn', { active: tab === 'texts' }]" @click="tab = 'texts'">Text templates</button>
      <button :class="['tab-btn', { active: tab === 'rules' }]" @click="tab = 'rules'">Rules</button>
      <button :class="['tab-btn', { active: tab === 'documents' }]" @click="tab = 'documents'">Documents</button>
      <button :class="['tab-btn', { active: tab === 'publicforms' }]" @click="tab = 'publicforms'">Public forms</button>
      <button :class="['tab-btn', { active: tab === 'stats' }]" @click="tab = 'stats'">Statistics</button>
    </div>

    <!-- The ported QuVian builder is Vuetify-based; v-app scopes Vuetify's
         theme/overlay machinery to this section only. -->
    <v-app class="qb-vapp">
      <v-main>
        <div class="qb-content">
          <QuestionnaireBuilderView v-if="tab === 'builder'" />
          <IntakeTemplatesView v-else-if="tab === 'templates'" />
          <TextTemplatesView v-else-if="tab === 'texts'" />
          <GenerationRulesAdmin v-else-if="tab === 'rules'" />
          <DocumentTemplatesView v-else-if="tab === 'documents'" />
          <PublicFormsAdmin v-else-if="tab === 'publicforms'" />
          <QuestionnaireStatsAdmin v-else-if="tab === 'stats'" />
          <IntakeInstancesAdmin v-else />
        </div>
      </v-main>
    </v-app>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { auth } from '../store/auth.js'
import QuestionnaireBuilderView from './admin/QuestionnaireBuilderView.vue'
import IntakeTemplatesView from './admin/IntakeTemplatesView.vue'
import IntakeInstancesAdmin from '../components/intake/IntakeInstancesAdmin.vue'
import TextTemplatesView from './admin/TextTemplatesView.vue'
import GenerationRulesAdmin from '../components/intake/GenerationRulesAdmin.vue'
import DocumentTemplatesView from './admin/DocumentTemplatesView.vue'
import PublicFormsAdmin from '../components/intake/PublicFormsAdmin.vue'
import QuestionnaireStatsAdmin from '../components/intake/QuestionnaireStatsAdmin.vue'

const router = useRouter()
const tab = ref('builder')

function logout() {
  auth.logout()
  router.push('/login')
}
</script>

<style scoped>
.page-wrapper { min-height: 100vh; display: flex; flex-direction: column; background: #f2f5f9; }
.navbar {
  background: #0b2e59;
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
  font-size: 0.85rem;
  cursor: pointer;
}
.btn-logout:hover { background: rgba(255,255,255,0.22); }
.tab-bar { display: flex; gap: 0.25rem; background: #fff; padding: 0.4rem 2rem 0; border-bottom: 2px solid #e8edf4; }
.tab-btn {
  background: none; border: none; padding: 0.6rem 1.1rem; font-size: 0.9rem;
  color: #555; cursor: pointer; border-bottom: 2.5px solid transparent; margin-bottom: -2px;
}
.tab-btn.active { color: #0b2e59; font-weight: 700; border-bottom-color: #0b2e59; }
/* Keep the embedded Vuetify app from claiming the whole viewport. */
.qb-vapp { flex: 1; }
.qb-vapp :deep(.v-application__wrap) { min-height: 0; }
.qb-content { padding: 1.25rem 2rem; }
</style>
