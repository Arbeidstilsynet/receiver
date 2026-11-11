import { defineConfig } from "@hey-api/openapi-ts";

// Shared by every consumer; each one creates its own client with its own base URL.
export default defineConfig({
  input: "./src/generated/openApi.json",
  output: {
    path: "./src/generated/api-client",
    // Explicit extensions so the compiled npm package resolves under Node ESM.
    importFileExtension: ".js",
  },
  plugins: [
    // exportFromIndex exposes createClient/createConfig for per-consumer clients.
    { name: "@hey-api/client-fetch", exportFromIndex: true },
    "@hey-api/typescript",
    "@hey-api/sdk",
  ],
});
