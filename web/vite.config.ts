
import tailwindcss from '@tailwindcss/vite'
import path from 'node:path'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
  ],

  server: {
    port: 3000,
    strictPort: true,
  },

  resolve: {
    alias: {
      '@': path.resolve(import.meta.dirname, './src'),
    },

    dedupe: [
      'react',
      'react-dom',
    ],
  },

  optimizeDeps: {
    include: [
      'react',
      'react-dom',
      'react-dom/client',
      'react/jsx-runtime',
      'react/jsx-dev-runtime',
      '@tanstack/react-query',
    ],
  },
})
