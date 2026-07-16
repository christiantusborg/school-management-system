import { createApp } from 'vue'
import { createPinia } from 'pinia'
import './style.css'
import App from './App.vue'
import router from './router/index.js'
import { startVersionWatch } from './utils/versionCheck.js'
// Vuetify + Pinia power the ported questionnaire builder (Questionnaires
// section); the rest of the portal stays hand-rolled CSS.
import 'vuetify/styles'
import '@mdi/font/css/materialdesignicons.css'
import vuetify from './plugins/vuetify.ts'

createApp(App).use(router).use(createPinia()).use(vuetify).mount('#app')

// Detect newer deploys: auto-reload to the latest build on page load, and
// surface a "new version" banner if one ships while the tab is open.
startVersionWatch()
// Thu Apr 16 12:29:50 AM CEST 2026
