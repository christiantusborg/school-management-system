<script setup lang="ts">
import { ref, computed } from 'vue'
import { RouterView, RouterLink, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const route = useRoute()

const settingsRoutes = ['/profile', '/account', '/account/recovery-codes', '/account/two-factor']
const settingsOpen = ref(settingsRoutes.some(r => route.path.startsWith(r)))

const isSettingsActive = computed(() => settingsRoutes.some(r => route.path.startsWith(r)))
</script>

<template>
  <div class="app-layout">
    <aside class="sidebar">
      <div class="sidebar-header">
        <h2>Odin</h2>
      </div>
      <nav class="sidebar-nav">

        <!-- Settings accordion -->
        <button
          class="nav-group-btn"
          :class="{ active: isSettingsActive }"
          @click="settingsOpen = !settingsOpen"
        >
          <span class="nav-group-icon">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <circle cx="12" cy="12" r="3"/>
              <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/>
            </svg>
          </span>
          <span>Settings</span>
          <span class="chevron" :class="{ open: settingsOpen }">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
              <polyline points="6 9 12 15 18 9"/>
            </svg>
          </span>
        </button>

        <div class="submenu" :class="{ open: settingsOpen }">
          <RouterLink to="/profile" class="nav-sub-item">Profile</RouterLink>
          <RouterLink to="/account" class="nav-sub-item">Account</RouterLink>
          <RouterLink to="/account/recovery-codes" class="nav-sub-item">Recovery Codes</RouterLink>
          <RouterLink to="/account/two-factor" class="nav-sub-item">Two-Factor Auth</RouterLink>
        </div>

        <div class="nav-divider"></div>
        <RouterLink to="/teams" class="nav-item">Teams</RouterLink>
        <RouterLink to="/cases" class="nav-item">Cases</RouterLink>

        <template v-if="auth.isAdmin">
          <div class="nav-divider"></div>
          <RouterLink to="/admin" class="nav-item">Dashboard</RouterLink>
          <RouterLink to="/admin/users" class="nav-item">Users</RouterLink>
          <RouterLink to="/admin/invite-codes" class="nav-item">Invite Codes</RouterLink>
        </template>

      </nav>
      <div class="sidebar-footer">
        <span class="username">{{ auth.user?.username }}</span>
        <button class="logout-btn" @click="auth.logout()">Logout</button>
      </div>
    </aside>
    <main class="content">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.app-layout {
  display: flex;
  min-height: 100vh;
}

.sidebar {
  width: 240px;
  background: #1a1a2e;
  color: white;
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
}

.sidebar-header {
  padding: 1.5rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.sidebar-header h2 {
  margin: 0;
  font-size: 1.5rem;
}

.sidebar-nav {
  flex: 1;
  padding: 1rem 0;
}

/* Flat nav items (admin) */
.nav-item {
  display: block;
  padding: 0.75rem 1.5rem;
  color: rgba(255, 255, 255, 0.7);
  text-decoration: none;
  transition: all 0.2s;
}

.nav-item:hover,
.nav-item.router-link-active {
  background: rgba(255, 255, 255, 0.1);
  color: white;
}

/* Settings accordion trigger */
.nav-group-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  width: 100%;
  padding: 0.75rem 1.5rem;
  background: transparent;
  border: none;
  color: rgba(255, 255, 255, 0.7);
  cursor: pointer;
  font-size: 0.9rem;
  text-align: left;
  transition: all 0.2s;
}

.nav-group-btn:hover,
.nav-group-btn.active {
  background: rgba(255, 255, 255, 0.08);
  color: white;
}

.nav-group-icon {
  display: flex;
  align-items: center;
  opacity: 0.8;
}

.chevron {
  margin-left: auto;
  display: flex;
  align-items: center;
  transition: transform 0.2s ease;
}

.chevron.open {
  transform: rotate(180deg);
}

/* Accordion submenu */
.submenu {
  max-height: 0;
  overflow: hidden;
  transition: max-height 0.25s ease;
}

.submenu.open {
  max-height: 300px;
}

.nav-sub-item {
  display: block;
  padding: 0.6rem 1.5rem 0.6rem 2.75rem;
  color: rgba(255, 255, 255, 0.6);
  text-decoration: none;
  font-size: 0.875rem;
  transition: all 0.2s;
}

.nav-sub-item:hover,
.nav-sub-item.router-link-active {
  background: rgba(255, 255, 255, 0.08);
  color: white;
}

.nav-divider {
  height: 1px;
  background: rgba(255, 255, 255, 0.1);
  margin: 0.5rem 1.5rem;
}

.sidebar-footer {
  padding: 1rem 1.5rem;
  border-top: 1px solid rgba(255, 255, 255, 0.1);
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.username {
  font-size: 0.875rem;
  opacity: 0.8;
}

.logout-btn {
  background: transparent;
  border: 1px solid rgba(255, 255, 255, 0.3);
  color: white;
  padding: 0.375rem 0.75rem;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.8rem;
}

.logout-btn:hover {
  background: rgba(255, 255, 255, 0.1);
}

.content {
  flex: 1;
  padding: 2rem;
  overflow-y: auto;
}
</style>
