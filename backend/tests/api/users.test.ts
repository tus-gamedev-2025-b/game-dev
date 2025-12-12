import { afterAll, beforeAll, describe, expect, test } from "bun:test"
import { unlink } from "node:fs/promises"
import app from "../../app/index.ts"

const TEST_DB = "./data/test.db"

type UserResponse = {
  user: {
    id: number
    name: string
    wins: number
    losses: number
    createdAt: string
    updatedAt: string
  }
}

type AuthResponse = UserResponse & {
  accessToken: string
  refreshToken: string
  accessTokenExpiresAt: string
  refreshTokenExpiresAt: string
}

type TokenResponse = {
  accessToken: string
  refreshToken: string
  accessTokenExpiresAt: string
  refreshTokenExpiresAt: string
}

describe("User API", () => {
  let accessToken: string
  let refreshToken: string
  let userId: number

  beforeAll(async () => {
    // Set test database
    process.env.DATABASE_PATH = TEST_DB
  })

  afterAll(async () => {
    // Cleanup test database
    try {
      await unlink(TEST_DB)
      await unlink(`${TEST_DB}-wal`)
      await unlink(`${TEST_DB}-shm`)
    } catch {
      // Ignore if files don't exist
    }
  })

  describe("POST /api/users", () => {
    test("creates a new user with default name", async () => {
      const res = await app.request("/api/users", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({}),
      })

      expect(res.status).toBe(201)

      const body = (await res.json()) as AuthResponse
      expect(body.user).toBeDefined()
      expect(body.user.id).toBeNumber()
      expect(body.user.name).toBe("NoName")
      expect(body.user.wins).toBe(0)
      expect(body.user.losses).toBe(0)
      expect(body.accessToken).toBeString()
      expect(body.refreshToken).toBeString()
      expect(body.accessTokenExpiresAt).toBeString()
      expect(body.refreshTokenExpiresAt).toBeString()

      // Save for later tests
      accessToken = body.accessToken
      refreshToken = body.refreshToken
      userId = body.user.id
    })

    test("creates a new user with custom name", async () => {
      const res = await app.request("/api/users", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ name: "TestPlayer" }),
      })

      expect(res.status).toBe(201)

      const body = (await res.json()) as AuthResponse
      expect(body.user.name).toBe("TestPlayer")
    })

    test("creates a new user with Japanese name", async () => {
      const res = await app.request("/api/users", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ name: "テストユーザー" }),
      })

      expect(res.status).toBe(201)

      const body = (await res.json()) as AuthResponse
      expect(body.user.name).toBe("テストユーザー")
    })

    test("creates a new user with name containing space", async () => {
      const res = await app.request("/api/users", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ name: "John Doe" }),
      })

      expect(res.status).toBe(201)

      const body = (await res.json()) as AuthResponse
      expect(body.user.name).toBe("John Doe")
    })

    test("rejects name shorter than 3 characters", async () => {
      const res = await app.request("/api/users", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ name: "AB" }),
      })

      expect(res.status).toBe(400)
    })

    test("rejects name longer than 15 characters", async () => {
      const res = await app.request("/api/users", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ name: "1234567890123456" }),
      })

      expect(res.status).toBe(400)
    })

    test("rejects name with special characters", async () => {
      const res = await app.request("/api/users", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ name: "user@name" }),
      })

      expect(res.status).toBe(400)
    })

    test("rejects name with emoji", async () => {
      const res = await app.request("/api/users", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ name: "user😀name" }),
      })

      expect(res.status).toBe(400)
    })
  })

  describe("GET /api/users/:id", () => {
    test("returns user info with valid token", async () => {
      const res = await app.request(`/api/users/${userId}`, {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      })

      expect(res.status).toBe(200)

      const body = (await res.json()) as UserResponse
      expect(body.user.id).toBe(userId)
      expect(body.user.name).toBe("NoName")
    })

    test("returns 401 without authorization header", async () => {
      const res = await app.request(`/api/users/${userId}`)

      expect(res.status).toBe(401)
    })

    test("returns 401 with invalid token", async () => {
      const res = await app.request(`/api/users/${userId}`, {
        headers: {
          Authorization: "Bearer invalid-token",
        },
      })

      expect(res.status).toBe(401)
    })

    test("returns 404 for non-existent user", async () => {
      const res = await app.request("/api/users/99999", {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      })

      expect(res.status).toBe(404)
    })
  })

  describe("PATCH /api/users/:id/name", () => {
    test("updates user name", async () => {
      const res = await app.request(`/api/users/${userId}/name`, {
        method: "PATCH",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify({ name: "NewName" }),
      })

      expect(res.status).toBe(200)

      const body = (await res.json()) as UserResponse
      expect(body.user.name).toBe("NewName")
    })

    test("returns 403 when updating other user", async () => {
      const res = await app.request("/api/users/99999/name", {
        method: "PATCH",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify({ name: "NewName" }),
      })

      expect(res.status).toBe(403)
    })

    test("returns 400 for invalid name length", async () => {
      const res = await app.request(`/api/users/${userId}/name`, {
        method: "PATCH",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify({ name: "ab" }), // Too short
      })

      expect(res.status).toBe(400)
    })

    test("returns 400 for invalid characters", async () => {
      const res = await app.request(`/api/users/${userId}/name`, {
        method: "PATCH",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify({ name: "user@name" }),
      })

      expect(res.status).toBe(400)
    })
  })

  describe("POST /api/users/login", () => {
    test("logs in with valid credentials", async () => {
      const res = await app.request("/api/users/login", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          userId,
          refreshToken,
        }),
      })

      expect(res.status).toBe(200)

      const body = (await res.json()) as AuthResponse
      expect(body.user.id).toBe(userId)
      expect(body.accessToken).toBeString()
      expect(body.refreshToken).toBeString()

      // Update tokens for subsequent tests
      accessToken = body.accessToken
      refreshToken = body.refreshToken
    })

    test("returns 401 with invalid refresh token", async () => {
      const res = await app.request("/api/users/login", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          userId,
          refreshToken: "00000000-0000-0000-0000-000000000000",
        }),
      })

      expect(res.status).toBe(401)
    })

    test("returns 404 for non-existent user", async () => {
      const res = await app.request("/api/users/login", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          userId: 99999,
          refreshToken: "00000000-0000-0000-0000-000000000000",
        }),
      })

      expect(res.status).toBe(404)
    })
  })

  describe("POST /api/auth/refresh", () => {
    test("refreshes tokens", async () => {
      const res = await app.request("/api/auth/refresh", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          refreshToken,
        }),
      })

      expect(res.status).toBe(200)

      const body = (await res.json()) as TokenResponse
      expect(body.accessToken).toBeString()
      expect(body.refreshToken).toBeString()
      expect(body.accessToken).not.toBe(accessToken)
      expect(body.refreshToken).not.toBe(refreshToken)
    })

    test("returns 401 with invalid refresh token", async () => {
      const res = await app.request("/api/auth/refresh", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          refreshToken: "00000000-0000-0000-0000-000000000000",
        }),
      })

      expect(res.status).toBe(401)
    })
  })
})
