import tailwindcss from '@tailwindcss/vite'
import path from 'node:path'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  server: {
    port: 3000,
    strictPort: true,
  },

  plugins: [
  react({
  }),
  tailwindcss(),
],

resolve: {
  alias: {
    '@': path.resolve(import.meta.dirname, './src'),
  },
},
})
