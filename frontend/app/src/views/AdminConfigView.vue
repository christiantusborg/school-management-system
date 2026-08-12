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
      <button
        v-for="t in visibleEntities"
        :key="t.key"
        :class="['tab-btn', { active: activeTab === t.key }]"
        @click="activeTab = t.key"
      >
        {{ t.label }}
      </button>
    </div>

    <div class="container">
      <template v-for="t in visibleEntities" :key="t.key">
        <EmailSettingsPanel v-if="t.key === 'email'" v-show="activeTab === t.key" />
        <PartnerDocumentTypesTab v-else-if="t.key === 'partnerDocs'" v-show="activeTab === t.key" />
        <FacultyProfileConfigTab v-else-if="t.key === 'facultyProfile'" v-show="activeTab === t.key" />
        <ModuleCohortConfigTab v-else-if="t.key === 'moduleCohorts'" v-show="activeTab === t.key" />
        <RubricConfigTab v-else-if="t.key === 'rubrics'" v-show="activeTab === t.key" />
        <SchoolsManager v-else-if="t.key === 'schools'" v-show="activeTab === t.key" />
        <CurrenciesManager v-else-if="t.key === 'currencies'" v-show="activeTab === t.key" />
        <div v-else-if="t.key === 'contactMethods'" v-show="activeTab === t.key">
          <SimpleListManager
            title="Contact Methods" singular="Contact Method"
            endpoint="/v1/school/contact-methods" id-key="contactMethodTypeId" />
          <SimpleListManager
            title="Contact Types" singular="Contact Type"
            endpoint="/v1/school/contact-types" id-key="partnerContactTypeId" />
        </div>
        <SimpleListManager
          v-else-if="t.key === 'positionFunctions'" v-show="activeTab === t.key"
          title="Position Functions" singular="Position Function"
          endpoint="/v1/school/position-functions" id-key="positionFunctionId" />
        <SimpleListManager
          v-else-if="t.key === 'employmentIndustries'" v-show="activeTab === t.key"
          title="Employment Industries" singular="Employment Industry"
          endpoint="/v1/school/employment-industries" id-key="employmentIndustryId" />
        <PathwayManager v-else-if="t.key === 'pathways'" v-show="activeTab === t.key" />
        <LetterTypesConfigTab v-else-if="t.key === 'letterTypes'" v-show="activeTab === t.key" />
        <RolesPermissionsManager v-else-if="t.key === 'rolesPermissions'" v-show="activeTab === t.key" />
        <CrudManager v-else v-show="activeTab === t.key" :config="t.config" />
      </template>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { auth } from '../store/auth.js'
import CrudManager from '../components/crud/CrudManager.vue'
import PathwayManager from '../components/admin/PathwayManager.vue'
import EmailSettingsPanel from '../components/admin/EmailSettingsPanel.vue'
import SchoolsManager from '../components/admin/SchoolsManager.vue'
import CurrenciesManager from '../components/admin/CurrenciesManager.vue'
import SimpleListManager from '../components/admin/SimpleListManager.vue'
import PartnerDocumentTypesTab from '../components/admin/PartnerDocumentTypesTab.vue'
import FacultyProfileConfigTab from '../components/admin/FacultyProfileConfigTab.vue'
import ModuleCohortConfigTab from '../components/admin/ModuleCohortConfigTab.vue'
import RubricConfigTab from '../components/admin/RubricConfigTab.vue'
import LetterTypesConfigTab from '../components/admin/LetterTypesConfigTab.vue'
import RolesPermissionsManager from '../components/admin/RolesPermissionsManager.vue'

const router = useRouter()

function logout() {
  auth.logout()
  router.push('/login')
}

// Letter Types is SuperAdministrator-only (backend enforces writes too).
const entities = [
  { key: 'documentTypes',   label: 'Document Types',   config: { title: 'Document Types',   endpoint: '/v1/school/system-config/document-types' } },
  // Roles & Permissions stays SuperAdmin-only (it governs the matrix itself).
  ...(auth.adminLevel === 'SuperAdministrator' ? [{ key: 'rolesPermissions', label: 'Roles & Permissions' }] : []),
  // Letter Types now follows the access matrix.
  ...(auth.can('letter_types.manage') ? [{ key: 'letterTypes', label: 'Letter Types' }] : []),
  { key: 'educationLevels', label: 'Education Levels', config: { title: 'Education Levels', endpoint: '/v1/school/system-config/education-levels' } },
  { key: 'modesOfStudy',    label: 'Modes of Study',   config: { title: 'Modes of Study',   endpoint: '/v1/school/system-config/modes-of-study' } },
  { key: 'pathways',        label: 'Entry Requirements',         config: { title: 'Entry Requirements',         endpoint: '/v1/school/system-config/pathways' } },
  { key: 'partnerDocs',     label: 'Partnership Documents' },
  { key: 'facultyProfile',  label: 'Faculty Profile Information' },
  { key: 'moduleCohorts',   label: 'Module Cohorts' },
  { key: 'rubrics',         label: 'Grading Rubrics' },
  { key: 'email',           label: 'Email' },
  { key: 'schools',         label: 'Schools' },
  { key: 'currencies',      label: 'Currencies' },
  { key: 'contactMethods',       label: 'Contact Methods' },
  { key: 'positionFunctions',    label: 'Position Functions' },
  { key: 'employmentIndustries', label: 'Employment Industries' },
]

// Access-matrix key for each config tab. Tabs without a mapped key
// (letterTypes, rolesPermissions, email) keep their existing gating.
const ENTITY_ACCESS_KEYS = {
  partnerDocs: 'config.partner_doc_types',
  facultyProfile: 'config.faculty_structure',
  moduleCohorts: 'config.cohort_types',
  rubrics: 'config.rubrics',
  documentTypes: 'config.lists',
  educationLevels: 'config.lists',
  modesOfStudy: 'config.lists',
  pathways: 'config.lists',
  schools: 'config.lists',
  currencies: 'config.lists',
  contactMethods: 'config.lists',
  positionFunctions: 'config.lists',
  employmentIndustries: 'config.lists',
}

const visibleEntities = computed(() => entities.filter(t => {
  const k = ENTITY_ACCESS_KEYS[t.key]
  return !k || auth.access(k) > 0
}))

const activeTab = ref(visibleEntities.value[0]?.key ?? entities[0].key)
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
