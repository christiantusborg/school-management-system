<script setup lang="ts">
import { useNotificationStore } from '@/stores/notification'

const store = useNotificationStore()
</script>

<template>
  <div class="toast-container">
    <div
      v-for="n in store.notifications"
      :key="n.id"
      :class="['toast', `toast-${n.type}`]"
    >
      {{ n.message }}
    </div>
  </div>
</template>

<style scoped>
.toast-container {
  position: fixed;
  top: 1rem;
  right: 1rem;
  z-index: 9999;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.toast {
  padding: 0.75rem 1.25rem;
  border-radius: 8px;
  color: white;
  font-size: 0.9rem;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  animation: slideIn 0.3s ease;
}

.toast-success { background: #22c55e; }
.toast-error { background: #ef4444; }
.toast-info { background: #3b82f6; }

@keyframes slideIn {
  from { transform: translateX(100%); opacity: 0; }
  to { transform: translateX(0); opacity: 1; }
}
</style>
