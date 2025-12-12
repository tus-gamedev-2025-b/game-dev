/**
 * OpenAPI スキーマを JSON ファイルとして出力するスクリプト
 *
 * Usage: bun run openapi:gen
 */
import api from "../app/routes/api/index.ts"

const spec = api.getOpenAPI31Document({
  openapi: "3.1.0",
  info: {
    title: "Game API",
    version: "1.0.0",
    description:
      "Unity game backend API for user management, authentication, matches, and rankings",
  },
  servers: [
    {
      url: "/api",
      description: "API server",
    },
  ],
  tags: [
    { name: "Users", description: "User management endpoints" },
    { name: "Auth", description: "Authentication endpoints" },
    { name: "Matches", description: "Match recording endpoints" },
    { name: "Rankings", description: "Ranking endpoints" },
  ],
})

const outputPath = "./docs/openapi.json"

await Bun.write(outputPath, JSON.stringify(spec, null, 2))

console.log(`OpenAPI schema generated: ${outputPath}`)
