import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import { fileURLToPath } from 'node:url';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    // `@` points at src, so components copied from shadcn/Aceternity resolve unchanged.
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  server: {
    port: 5173,
  },
});
