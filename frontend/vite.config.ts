import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    // `@` points at src, so components copied from shadcn/Aceternity resolve unchanged.
    alias: { '@': new URL('./src', import.meta.url).pathname },
  },
  server: {
    port: 5173,
  },
});
