import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

const ts = Date.now()
const buildDate = new Date(ts).toISOString().slice(0, 16).replace('T', ' ')

export default defineConfig({
  plugins: [vue()],
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
