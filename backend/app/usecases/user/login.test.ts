import { describe, expect, test } from "bun:test"
import { initializeDatabase } from "../../libs/db/client.ts"
import { createUser } from "./create-user.ts"
import { login } from "./login.ts"

// Initialize database tables
initializeDatabase()

describe("login usecase", () => {
  test("logs in with valid credentials", async () => {
    const { user, tokenPair } = await createUser()

    const result = await login(user.id, tokenPair.refreshToken)

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.user.id).toBe(user.id)
      expect(result.user.name).toBe(user.name)
    }
  })

  test("returns new tokens on login", async () => {
    const { user, tokenPair: oldTokens } = await createUser()

    const result = await login(user.id, oldTokens.refreshToken)

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.tokenPair.accessToken).not.toBe(oldTokens.accessToken)
      expect(result.tokenPair.refreshToken).not.toBe(oldTokens.refreshToken)
    }
  })

  test("invalidates old tokens after login", async () => {
    const { user, tokenPair: oldTokens } = await createUser()

    // First login succeeds
    const result1 = await login(user.id, oldTokens.refreshToken)
    expect(result1.success).toBe(true)

    // Second login with old token fails
    const result2 = await login(user.id, oldTokens.refreshToken)
    expect(result2.success).toBe(false)
    if (!result2.success) {
      expect(result2.code).toBe("INVALID_REFRESH_TOKEN")
    }
  })

  test("fails with non-existent user ID", async () => {
    const result = await login(99999, "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.code).toBe("USER_NOT_FOUND")
    }
  })

  test("fails with invalid refresh token", async () => {
    const { user } = await createUser()

    const result = await login(user.id, "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.code).toBe("INVALID_REFRESH_TOKEN")
    }
  })

  test("fails with mismatched user ID and token", async () => {
    const user1 = await createUser()
    const user2 = await createUser()

    // Try to login as user1 with user2's token
    const result = await login(user1.user.id, user2.tokenPair.refreshToken)

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.code).toBe("INVALID_REFRESH_TOKEN")
    }
  })

  test("can login multiple times with new tokens", async () => {
    const { user, tokenPair } = await createUser()

    // First login
    const result1 = await login(user.id, tokenPair.refreshToken)
    expect(result1.success).toBe(true)

    if (result1.success) {
      // Second login with new token
      const result2 = await login(user.id, result1.tokenPair.refreshToken)
      expect(result2.success).toBe(true)

      if (result2.success) {
        // Third login with newest token
        const result3 = await login(user.id, result2.tokenPair.refreshToken)
        expect(result3.success).toBe(true)
      }
    }
  })
})
