import { beforeAll, beforeEach, describe, expect, test } from "bun:test"
import { Hono } from "hono"
import {
  createAuthTokenRepository,
  createUserRepository,
} from "../../infra/repositories/user.ts"
import { db, initializeDatabase } from "../../libs/db/client.ts"
import { authTokens, users } from "../../libs/db/schema.ts"
import { authMiddleware } from "./middleware.ts"

type ProtectedResponse = {
  userId?: number
  error?: { code: string; message: string }
}

beforeAll(() => {
  initializeDatabase()
})

beforeEach(async () => {
  await db.delete(authTokens)
  await db.delete(users)
})

describe("authMiddleware", () => {
  const userRepository = createUserRepository(db)
  const authTokenRepository = createAuthTokenRepository(db)

  const createTestApp = () => {
    const app = new Hono<{ Variables: { userId: number } }>()
    app.use("/*", authMiddleware)
    app.get("/protected", (c) => {
      const userId = c.get("userId")
      return c.json({ userId })
    })
    return app
  }

  const createValidToken = async () => {
    const user = await userRepository.create("TestUser")
    const tokenPair = {
      accessToken: `valid_access_${Date.now()}`,
      refreshToken: `valid_refresh_${Date.now()}`,
      accessTokenExpiresAt: new Date(Date.now() + 60 * 60 * 1000), // 1 hour
      refreshTokenExpiresAt: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000), // 30 days
    }
    await authTokenRepository.create(user.id, tokenPair)
    return { user, tokenPair }
  }

  const createExpiredToken = async () => {
    const user = await userRepository.create("ExpiredUser")
    const tokenPair = {
      accessToken: `expired_access_${Date.now()}`,
      refreshToken: `expired_refresh_${Date.now()}`,
      accessTokenExpiresAt: new Date(Date.now() - 1000), // Already expired
      refreshTokenExpiresAt: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000),
    }
    await authTokenRepository.create(user.id, tokenPair)
    return { user, tokenPair }
  }

  test("allows request with valid token", async () => {
    const app = createTestApp()
    const { user, tokenPair } = await createValidToken()

    const res = await app.request("/protected", {
      headers: {
        Authorization: `Bearer ${tokenPair.accessToken}`,
      },
    })

    expect(res.status).toBe(200)
    const body = (await res.json()) as ProtectedResponse
    expect(body.userId).toBe(user.id)
  })

  test("rejects request without Authorization header", async () => {
    const app = createTestApp()

    const res = await app.request("/protected")

    expect(res.status).toBe(401)
    const body = (await res.json()) as ProtectedResponse
    expect(body.error?.code).toBe("UNAUTHORIZED")
    expect(body.error?.message).toContain("Missing or invalid Authorization")
  })

  test("rejects request with empty Authorization header", async () => {
    const app = createTestApp()

    const res = await app.request("/protected", {
      headers: {
        Authorization: "",
      },
    })

    expect(res.status).toBe(401)
  })

  test("rejects request without Bearer prefix", async () => {
    const app = createTestApp()
    const { tokenPair } = await createValidToken()

    const res = await app.request("/protected", {
      headers: {
        Authorization: tokenPair.accessToken, // Missing "Bearer " prefix
      },
    })

    expect(res.status).toBe(401)
    const body = (await res.json()) as ProtectedResponse
    expect(body.error?.code).toBe("UNAUTHORIZED")
  })

  test("rejects request with invalid token", async () => {
    const app = createTestApp()

    const res = await app.request("/protected", {
      headers: {
        Authorization: "Bearer invalid_token_12345",
      },
    })

    expect(res.status).toBe(401)
    const body = (await res.json()) as ProtectedResponse
    expect(body.error?.code).toBe("UNAUTHORIZED")
    expect(body.error?.message).toContain("Invalid access token")
  })

  test("rejects request with expired token", async () => {
    const app = createTestApp()
    const { tokenPair } = await createExpiredToken()

    const res = await app.request("/protected", {
      headers: {
        Authorization: `Bearer ${tokenPair.accessToken}`,
      },
    })

    expect(res.status).toBe(401)
    const body = (await res.json()) as ProtectedResponse
    expect(body.error?.code).toBe("TOKEN_EXPIRED")
    expect(body.error?.message).toContain("expired")
  })

  test("sets userId in context for valid token", async () => {
    const app = createTestApp()
    const { user, tokenPair } = await createValidToken()

    const res = await app.request("/protected", {
      headers: {
        Authorization: `Bearer ${tokenPair.accessToken}`,
      },
    })

    const body = (await res.json()) as ProtectedResponse
    expect(body.userId).toBe(user.id)
  })

  test("handles Basic auth format (should reject)", async () => {
    const app = createTestApp()

    const res = await app.request("/protected", {
      headers: {
        Authorization: "Basic dXNlcm5hbWU6cGFzc3dvcmQ=",
      },
    })

    expect(res.status).toBe(401)
  })
})
