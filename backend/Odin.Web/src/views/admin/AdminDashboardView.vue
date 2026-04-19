<script setup lang="ts">
import { onMounted } from 'vue'
import { useAdminStore } from '@/stores/admin'
import { computed } from 'vue'
import LoadingSpinner from '@/components/LoadingSpinner.vue'

const admin = useAdminStore()

onMounted(() => {
  admin.fetchUsers(1, 1000)
})

const stats = computed(() => {
  if (!admin.users) return null
  const total = admin.users.total
  const enabled = admin.users.items.filter(u => u.isEnabled).length
  const admins = admin.users.items.filter(u => u.roles.includes('Admin')).length
  return { total, enabled, disabled: total - enabled, admins }
})
</script>

<template>
  <div class="page">
    <h1>Admin Dashboard</h1>

    <LoadingSpinner v-if="admin.loading" />

    <div v-else-if="stats" class="stats-grid">
      <div class="stat-card">
        <div class="stat-value">{{ stats.total }}</div>
        <div class="stat-label">Total Users</div>
      </div>
      <div class="stat-card">
        <div class="stat-value">{{ stats.enabled }}</div>
        <div class="stat-label">Active Users</div>
      </div>
      <div class="stat-card">
        <div class="stat-value">{{ stats.disabled }}</div>
        <div class="stat-label">Disabled Users</div>
      </div>
      <div class="stat-card">
        <div class="stat-value">{{ stats.admins }}</div>
        <div class="stat-label">Admins</div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.page h1 { margin-bottom: 1.5rem; }

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 1rem;
}

.stat-card {
  background: white;
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  text-align: center;
}

.stat-value {
  font-size: 2rem;
  font-weight: 700;
  color: #667eea;
}

.stat-label {
  font-size: 0.9rem;
  color: #6b7280;
  margin-top: 0.25rem;
}
</style>
