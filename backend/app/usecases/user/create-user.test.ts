import { describe, expect, test } from "bun:test"
import { initializeDatabase } from "../../libs/db/client.ts"
import { createUser } from "./create-user.ts"

// Initialize database tables
initializeDatabase()

describe("createUser usecase", () => {
  test("creates a new user with default name", async () => {
    const result = await createUser()

    expect(result.user).toBeDefined()
    expect(result.user.id).toBeNumber()
    expect(result.user.name).toBe("NoName")
    expect(result.user.wins).toBe(0)
    expect(result.user.losses).toBe(0)
    expect(result.user.createdAt).toBeInstanceOf(Date)
    expect(result.user.updatedAt).toBeInstanceOf(Date)
  })

  test("generates valid token pair", async () => {
    const result = await createUser()

    expect(result.tokenPair).toBeDefined()
    expect(result.tokenPair.accessToken).toBeString()
    expect(result.tokenPair.refreshToken).toBeString()
    expect(result.tokenPair.accessToken).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
    )
    expect(result.tokenPair.refreshToken).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
    )
  })

  test("sets correct token expiration times", async () => {
    const before = Date.now()
    const result = await createUser()
    const after = Date.now()

    const accessExpiry = result.tokenPair.accessTokenExpiresAt.getTime()
    const refreshExpiry = result.tokenPair.refreshTokenExpiresAt.getTime()

    // Access token expires in ~1 hour
    const oneHour = 60 * 60 * 1000
    expect(accessExpiry).toBeGreaterThanOrEqual(before + oneHour - 1000)
    expect(accessExpiry).toBeLessThanOrEqual(after + oneHour + 1000)

    // Refresh token expires in ~30 days
    const thirtyDays = 30 * 24 * 60 * 60 * 1000
    expect(refreshExpiry).toBeGreaterThanOrEqual(before + thirtyDays - 1000)
    expect(refreshExpiry).toBeLessThanOrEqual(after + thirtyDays + 1000)
  })

  test("creates unique user IDs", async () => {
    const result1 = await createUser()
    const result2 = await createUser()
    const result3 = await createUser()

    expect(result1.user.id).not.toBe(result2.user.id)
    expect(result2.user.id).not.toBe(result3.user.id)
    expect(result1.user.id).not.toBe(result3.user.id)
  })

  test("creates unique tokens for each user", async () => {
    const result1 = await createUser()
    const result2 = await createUser()

    expect(result1.tokenPair.accessToken).not.toBe(
      result2.tokenPair.accessToken,
    )
    expect(result1.tokenPair.refreshToken).not.toBe(
      result2.tokenPair.refreshToken,
    )
  })
})
