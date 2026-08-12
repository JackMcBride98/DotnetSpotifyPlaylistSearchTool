import react from "@vitejs/plugin-react";
import path from "path";
import { defineConfig } from "vite";
import { VitePWA } from "vite-plugin-pwa";

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: "autoUpdate",
      devOptions: { enabled: true },
      manifest: {
        name: "Playlist Search Tool",
        short_name: "PlaylistSearchTool",
        description:
          "A tool for finding out which of your owned Spotify playlists contain a given artist or song. Search your owned Spotify playlists by artist or song.",
        theme_color: "#7f22fe",
        background_color: "#000000",
        display: "standalone",
        icons: [
          {
            src: "/magnifying-glass-solid192x192.png",
            sizes: "192x192",
            type: "image/png",
          },
          {
            src: "/magnifying-glass-solid512x512.png",
            sizes: "512x512",
            type: "image/png",
          },
        ],
      },
    }),
  ],
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
