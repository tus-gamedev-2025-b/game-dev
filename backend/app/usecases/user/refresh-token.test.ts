import { describe, expect, test } from "bun:test"
import { initializeDatabase } from "../../libs/db/client.ts"
import { createUser } from "./create-user.ts"
import { refreshToken } from "./refresh-token.ts"

// Initialize database tables
initializeDatabase()

describe("refreshToken usecase", () => {
  test("refreshes tokens with valid refresh token", async () => {
    const { tokenPair: oldTokens } = await createUser()

    const result = await refreshToken(oldTokens.refreshToken)

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.tokenPair.accessToken).toBeString()
      expect(result.tokenPair.refreshToken).toBeString()
    }
  })

  test("returns new tokens different from old ones", async () => {
    const { tokenPair: oldTokens } = await createUser()

    const result = await refreshToken(oldTokens.refreshToken)

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.tokenPair.accessToken).not.toBe(oldTokens.accessToken)
      expect(result.tokenPair.refreshToken).not.toBe(oldTokens.refreshToken)
    }
  })

  test("invalidates old refresh token after use", async () => {
    const { tokenPair: oldTokens } = await createUser()

    // First refresh succeeds
    const result1 = await refreshToken(oldTokens.refreshToken)
    expect(result1.success).toBe(true)

    // Second refresh with old token fails
    const result2 = await refreshToken(oldTokens.refreshToken)
    expect(result2.success).toBe(false)
    if (!result2.success) {
      expect(result2.code).toBe("INVALID_REFRESH_TOKEN")
    }
  })

  test("can chain multiple refreshes", async () => {
    const { tokenPair } = await createUser()

    const result1 = await refreshToken(tokenPair.refreshToken)
    expect(result1.success).toBe(true)

    if (result1.success) {
      const result2 = await refreshToken(result1.tokenPair.refreshToken)
      expect(result2.success).toBe(true)

      if (result2.success) {
        const result3 = await refreshToken(result2.tokenPair.refreshToken)
        expect(result3.success).toBe(true)
      }
    }
  })

  test("fails with non-existent refresh token", async () => {
    const result = await refreshToken("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.code).toBe("INVALID_REFRESH_TOKEN")
    }
  })

  test("fails with empty refresh token", async () => {
    const result = await refreshToken("")

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.code).toBe("INVALID_REFRESH_TOKEN")
    }
  })

  test("fails with access token instead of refresh token", async () => {
    const { tokenPair } = await createUser()

    // Try to use access token as refresh token
    const result = await refreshToken(tokenPair.accessToken)

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.code).toBe("INVALID_REFRESH_TOKEN")
    }
  })

  test("sets correct expiration times on new tokens", async () => {
    const { tokenPair: oldTokens } = await createUser()

    const before = Date.now()
    const result = await refreshToken(oldTokens.refreshToken)
    const after = Date.now()

    expect(result.success).toBe(true)
    if (result.success) {
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
    }
  })

  test("different users can refresh independently", async () => {
    const user1 = await createUser()
    const user2 = await createUser()

    const result1 = await refreshToken(user1.tokenPair.refreshToken)
    const result2 = await refreshToken(user2.tokenPair.refreshToken)

    expect(result1.success).toBe(true)
    expect(result2.success).toBe(true)

    if (result1.success && result2.success) {
      expect(result1.tokenPair.accessToken).not.toBe(
        result2.tokenPair.accessToken,
      )
      expect(result1.tokenPair.refreshToken).not.toBe(
        result2.tokenPair.refreshToken,
      )
    }
  })
})
