<script setup lang="ts">
import { RouterView } from 'vue-router'
</script>

<template>
  <div class="auth-layout">
    <div class="auth-card">
      <h1 class="auth-title">Odin</h1>
      <RouterView />
    </div>
  </div>
</template>

<style scoped>
:root {
  --c-unsafe: #e02424;
  --c-vuln: #ef6c00;
  --c-decent: #f2c037;
  --c-robust: #1e88e5;
  --c-green: #2e7d32;
}

.auth-layout {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #0b1220;
  font-family: system-ui, Segoe UI, Roboto, Arial, sans-serif;
}

.auth-card {
  background: #121a2a;
  border-radius: 16px;
  padding: 2rem 2.5rem 2.5rem;
  width: 100%;
  max-width: 520px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.4);
  color: #e9eef7;
}

.auth-title {
  text-align: center;
  margin: 0 0 1.5rem;
  font-size: 1.75rem;
  color: #e9eef7;
  letter-spacing: 0.05em;
}
</style>
