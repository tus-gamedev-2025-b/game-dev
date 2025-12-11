import { Hono } from "hono"
import { cors } from "hono/cors"
import { HTTPException } from "hono/http-exception"
import { logger } from "hono/logger"
import { initializeDatabase } from "./libs/db/client.ts"
import api from "./routes/api/index.ts"

// Initialize database
initializeDatabase()

const app = new Hono()

// Middleware
app.use("*", logger())
app.use("*", cors())

// Error handling
app.onError((err, c) => {
  if (err instanceof HTTPException) {
    try {
      const errorBody = JSON.parse(err.message)
      return c.json(errorBody, err.status)
    } catch {
      return c.json(
        { error: { code: "INTERNAL_ERROR", message: err.message } },
        err.status,
      )
    }
  }

  console.error(err)
  return c.json(
    { error: { code: "INTERNAL_ERROR", message: "Internal server error" } },
    500,
  )
})

// Health check
app.get("/health", (c) => c.json({ status: "ok" }))

// API routes
app.route("/api", api)

export default app
export { app }
