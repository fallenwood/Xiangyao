import { defineConfig } from "vite";
import solid from "vite-plugin-solid";
import tailwindcss from "@tailwindcss/vite";
import { env }from "process";

function setupProxy() {
  const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
    env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'http://localhost:8080';

  const context = [
    "/api",
    "/health",
    "/swagger",
  ];

  const proxy = {} as any;

  for (const c of context) {
    proxy[c] = {
      forward: target,
      target,
    };
  }
  return proxy;
}

export default defineConfig({
  plugins: [solid(), tailwindcss()],
  server: {
    proxy: setupProxy(),
    host: "0.0.0.0"
  },
})
