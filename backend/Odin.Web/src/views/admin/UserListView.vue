<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAdminStore } from '@/stores/admin'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import PaginationControls from '@/components/PaginationControls.vue'

const admin = useAdminStore()
const router = useRouter()
const search = ref('')
const page = ref(1)

onMounted(() => admin.fetchUsers(1))

function handleSearch() {
  page.value = 1
  admin.fetchUsers(1, 20, search.value || undefined)
}

function handlePageChange(newPage: number) {
  page.value = newPage
  admin.fetchUsers(newPage, 20, search.value || undefined)
}
</script>

<template>
  <div class="page">
    <h1>Users</h1>

    <div class="toolbar">
      <input
        v-model="search"
        placeholder="Search users..."
        class="search-input"
        @keyup.enter="handleSearch"
      />
      <button class="btn-primary" @click="handleSearch">Search</button>
    </div>

    <LoadingSpinner v-if="admin.loading" />

    <table v-else-if="admin.users" class="data-table">
      <thead>
        <tr>
          <th>Username</th>
          <th>Email</th>
          <th>Roles</th>
          <th>Status</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="user in admin.users.items" :key="user.userId">
          <td>{{ user.username }}</td>
          <td>{{ user.email }}</td>
          <td>{{ user.roles.join(', ') }}</td>
          <td>
            <span :class="['badge', user.isEnabled ? 'badge-green' : 'badge-red']">
              {{ user.isEnabled ? 'Active' : 'Disabled' }}
            </span>
          </td>
          <td>
            <button class="btn-sm" @click="router.push(`/admin/users/${user.userId}`)">
              View
            </button>
          </td>
        </tr>
      </tbody>
    </table>

    <PaginationControls
      v-if="admin.users"
      :page="page"
      :page-size="20"
      :total-count="admin.users.total"
      @update:page="handlePageChange"
    />
  </div>
</template>

<style scoped>
.page h1 { margin-bottom: 1.5rem; }

.toolbar {
  display: flex;
  gap: 0.75rem;
  margin-bottom: 1.5rem;
}

.search-input {
  flex: 1;
  padding: 0.5rem 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 6px;
  font-size: 0.9rem;
}

.data-table {
  width: 100%;
  background: white;
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  border-collapse: collapse;
}

.data-table th,
.data-table td {
  padding: 0.75rem 1rem;
  text-align: left;
  border-bottom: 1px solid #e5e7eb;
}

.data-table th {
  background: #f9fafb;
  font-weight: 600;
  font-size: 0.85rem;
  color: #6b7280;
  text-transform: uppercase;
}

.badge {
  padding: 0.2rem 0.6rem;
  border-radius: 12px;
  font-size: 0.8rem;
  font-weight: 500;
}

.badge-green { background: #dcfce7; color: #166534; }
.badge-red { background: #fee2e2; color: #991b1b; }

.btn-primary {
  padding: 0.5rem 1rem;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
}

.btn-sm {
  padding: 0.3rem 0.7rem;
  background: #e5e7eb;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.85rem;
}

.btn-sm:hover { background: #d1d5db; }
</style>
