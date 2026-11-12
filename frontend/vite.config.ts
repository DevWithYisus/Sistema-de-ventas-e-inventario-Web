import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

// En desarrollo, Vite reenvía /api y /health a la API .NET: el navegador ve un solo origen
// y no hace falta CORS. Cambia el puerto si tu API corre en otro.
const API_LOCAL = "http://localhost:5248";

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      "/api": { target: API_LOCAL, changeOrigin: true },
      "/health": { target: API_LOCAL, changeOrigin: true },
    },
  },
});
