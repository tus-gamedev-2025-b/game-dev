import { OpenAPIHono } from "@hono/zod-openapi"
import { Scalar } from "@scalar/hono-api-reference"
import authRefresh from "./auth-refresh.ts"
import matches from "./matches.ts"
import rankings from "./rankings.ts"
import users from "./users.ts"
import usersId from "./users-id.ts"
import usersIdName from "./users-id-name.ts"

const app = new OpenAPIHono()

// Security scheme を登録
app.openAPIRegistry.registerComponent("securitySchemes", "Bearer", {
  type: "http",
  scheme: "bearer",
  bearerFormat: "UUID",
  description: "Access token obtained from POST /users or POST /users/login",
})

// ユーザー関連ルート
app.route("/users", users)
app.route("/users", usersId)
app.route("/users", usersIdName)

// 認証関連ルート
app.route("/auth/refresh", authRefresh)

// 対戦関連ルート
app.route("/matches", matches)

// ランキング関連ルート
app.route("/rankings", rankings)

/** OpenAPI JSON エンドポイント */
app.doc("/doc", {
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

// Scalar API Reference UI
app.get(
  "/reference",
  Scalar({
    url: "/api/doc",
    theme: "purple",
    authentication: {
      preferredSecurityScheme: "Bearer",
      securitySchemes: {
        Bearer: {
          token: "",
        },
      },
    },
  }),
)

export default app
