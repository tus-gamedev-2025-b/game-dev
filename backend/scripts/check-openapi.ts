/**
 * OpenAPI スキーマが最新かどうかをチェックするスクリプト
 * CI で使用して、スキーマの更新忘れを検知する
 *
 * Usage: bun run openapi:check
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
const generated = JSON.stringify(spec, null, 2)

const file = Bun.file(outputPath)

if (!(await file.exists())) {
  console.error(`❌ OpenAPI schema file not found: ${outputPath}`)
  console.error("Run 'bun run openapi:gen' to generate it.")
  process.exit(1)
}

const existing = await file.text()

if (generated !== existing) {
  console.error("❌ OpenAPI schema is out of date!")
  console.error("Run 'bun run openapi:gen' to update it.")
  process.exit(1)
}

console.log("✅ OpenAPI schema is up to date.")
