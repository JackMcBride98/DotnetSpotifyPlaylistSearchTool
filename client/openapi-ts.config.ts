// openapi-ts.config.ts
import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
    client: '@hey-api/client-fetch',          // Configures standard fetch engine
    input: '../client/api/swagger.json', // Path to your backend OpenAPI file
    output: 'src/api',                       // Destination folder for the client SDK
    plugins: [
        '@hey-api/client-fetch',               // Generates base SDK functions
        '@tanstack/react-query',               // 🚀 Core plugin for TanStack Query options
    ],
});