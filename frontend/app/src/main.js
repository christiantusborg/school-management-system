import { createApp } from 'vue'
import './style.css'
import App from './App.vue'
import router from './router/index.js'
import { startVersionWatch } from './utils/versionCheck.js'

createApp(App).use(router).mount('#app')

// Detect newer deploys: auto-reload to the latest build on page load, and
// surface a "new version" banner if one ships while the tab is open.
startVersionWatch()
// Thu Apr 16 12:29:50 AM CEST 2026
