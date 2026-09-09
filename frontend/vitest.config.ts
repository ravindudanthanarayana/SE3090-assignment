import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

/** Test config is kept separate so vite.config.ts stays a plain Vite config. */
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': new URL('./src', import.meta.url).pathname },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test/setup.ts'],
    css: false,
    include: ['src/**/*.test.{ts,tsx}'],
  },
});
