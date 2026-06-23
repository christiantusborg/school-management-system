import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { writeFileSync } from 'fs'
import { resolve } from 'path'

const ts = Date.now()
const buildDate = new Date(ts).toISOString().slice(0, 16).replace('T', ' ')

// Emit a tiny version.json into the build output so the running app can detect
// when a newer build has been deployed (see src/utils/versionCheck.js). Served
// un-hashed at /version.json and fetched with a cache-busting query param.
function emitVersionFile() {
  return {
    name: 'emit-version-json',
    closeBundle() {
      writeFileSync(
        resolve(__dirname, 'dist/version.json'),
        JSON.stringify({ buildId: ts, buildDate: `${buildDate} UTC` }),
      )
    },
  }
}

export default defineConfig({
  plugins: [vue(), emitVersionFile()],
  server: {
    cors: false,
    proxy: {
      '/v1': {
        target: 'http://localhost:5103',
        changeOrigin: true,
      },
    },
  },
  define: {
    __APP_VERSION__: JSON.stringify(`build ${buildDate} UTC`),
    __BUILD_ID__: JSON.stringify(ts),
  },
  build: {
    rollupOptions: {
      output: {
        entryFileNames: `assets/[name]-[hash]-${ts}.js`,
        chunkFileNames: `assets/[name]-[hash]-${ts}.js`,
        assetFileNames: `assets/[name]-[hash]-${ts}.[ext]`,
      },
    },
  },
})
