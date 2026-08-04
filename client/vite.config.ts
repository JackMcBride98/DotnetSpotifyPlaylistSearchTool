import react from "@vitejs/plugin-react";
import path from "path";
import { defineConfig } from "vite";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    strictPort: true,
    proxy: {
      "/api": { target: "https://localhost:5030", secure: false },
    },
  },
  resolve: {
    alias: {
      "@api": path.resolve(__dirname, "./src/api"),
      "@helpers": path.resolve(__dirname, "./src/helpers"),
      "@components": path.resolve(__dirname, "./src/components"),
      "@icons": path.resolve(__dirname, "./src/icons"),
      "@assets": path.resolve(__dirname, "./src/assets"),
      "@features": path.resolve(__dirname, "./src/features"),
    },
  },
});
