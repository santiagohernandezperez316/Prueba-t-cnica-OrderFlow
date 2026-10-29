import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import flowbiteReact from 'flowbite-react/plugin/vite'
import { loadEnv } from 'vite'
import { defineConfig } from 'vitest/config'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  return {
    plugins: [react(), tailwindcss(), ...(mode === 'test' ? [] : [flowbiteReact()])],
    server: {
      proxy: {
        '/api': {
          target: env.API_PROXY_TARGET,
          changeOrigin: true,
          rewrite: (path) => path.replace(/^\/api/, ''),
        },
        '/inventory-api': {
          target: env.INVENTORY_PROXY_TARGET,
          changeOrigin: true,
          rewrite: (path) => path.replace(/^\/inventory-api/, ''),
        },
        '/hubs': {
          target: env.API_PROXY_TARGET,
          ws: true,
        },
      },
    },
    test: {
      environment: 'jsdom',
      setupFiles: './src/test/setup.ts',
    },
  }
})
