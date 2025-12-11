import { beforeAll, beforeEach, describe, expect, test } from "bun:test"
import { db, initializeDatabase } from "../../libs/db/client.ts"
import { authTokens, users } from "../../libs/db/schema.ts"
import { createAuthTokenRepository, createUserRepository } from "./user.ts"

beforeAll(() => {
  initializeDatabase()
})

beforeEach(async () => {
  await db.delete(authTokens)
  await db.delete(users)
})

describe("UserRepository", () => {
  const userRepository = createUserRepository(db)

  describe("create", () => {
    test("creates a user with given name", async () => {
      const user = await userRepository.create("TestUser")

      expect(user.id).toBeNumber()
      expect(user.name).toBe("TestUser")
      expect(user.wins).toBe(0)
      expect(user.losses).toBe(0)
      expect(user.createdAt).toBeInstanceOf(Date)
      expect(user.updatedAt).toBeInstanceOf(Date)
    })

    test("creates a user with default name when empty string provided", async () => {
      const user = await userRepository.create("")

      expect(user.name).toBe("NoName")
    })
  })

  describe("findById", () => {
    test("returns user when found", async () => {
      const created = await userRepository.create("FindMe")
      const found = await userRepository.findById(created.id)

      expect(found).not.toBeNull()
      expect(found?.id).toBe(created.id)
      expect(found?.name).toBe("FindMe")
    })

    test("returns null when user not found", async () => {
      const found = await userRepository.findById(99999)

      expect(found).toBeNull()
    })
  })

  describe("updateName", () => {
    test("updates user name and returns updated user", async () => {
      const created = await userRepository.create("OldName")
      const updated = await userRepository.updateName(created.id, "NewName")

      expect(updated).not.toBeNull()
      expect(updated?.name).toBe("NewName")
      expect(updated?.updatedAt.getTime()).toBeGreaterThanOrEqual(
        created.updatedAt.getTime(),
      )
    })

    test("returns null when user not found", async () => {
      const updated = await userRepository.updateName(99999, "NewName")

      expect(updated).toBeNull()
    })
  })

  describe("updateStats", () => {
    test("updates user stats and returns updated user", async () => {
      const created = await userRepository.create("StatsUser")
      const updated = await userRepository.updateStats(created.id, 5, 3)

      expect(updated).not.toBeNull()
      expect(updated?.wins).toBe(5)
      expect(updated?.losses).toBe(3)
    })

    test("returns null when user not found", async () => {
      const updated = await userRepository.updateStats(99999, 5, 3)

      expect(updated).toBeNull()
    })
  })
})

describe("AuthTokenRepository", () => {
  const userRepository = createUserRepository(db)
  const authTokenRepository = createAuthTokenRepository(db)

  const createTokenPair = () => ({
    accessToken: `access_${Date.now()}_${Math.random()}`,
    refreshToken: `refresh_${Date.now()}_${Math.random()}`,
    accessTokenExpiresAt: new Date(Date.now() + 60 * 60 * 1000),
    refreshTokenExpiresAt: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000),
  })

  describe("create", () => {
    test("creates auth token for user", async () => {
      const user = await userRepository.create("TokenUser")
      const tokenPair = createTokenPair()

      const authToken = await authTokenRepository.create(user.id, tokenPair)

      expect(authToken.id).toBeNumber()
      expect(authToken.userId).toBe(user.id)
      expect(authToken.accessToken).toBe(tokenPair.accessToken)
      expect(authToken.refreshToken).toBe(tokenPair.refreshToken)
      expect(authToken.accessTokenExpiresAt).toBeInstanceOf(Date)
      expect(authToken.refreshTokenExpiresAt).toBeInstanceOf(Date)
      expect(authToken.createdAt).toBeInstanceOf(Date)
    })
  })

  describe("findByAccessToken", () => {
    test("returns auth token when found", async () => {
      const user = await userRepository.create("AccessUser")
      const tokenPair = createTokenPair()
      await authTokenRepository.create(user.id, tokenPair)

      const found = await authTokenRepository.findByAccessToken(
        tokenPair.accessToken,
      )

      expect(found).not.toBeNull()
      expect(found?.accessToken).toBe(tokenPair.accessToken)
    })

    test("returns null when not found", async () => {
      const found = await authTokenRepository.findByAccessToken("nonexistent")

      expect(found).toBeNull()
    })
  })

  describe("findByRefreshToken", () => {
    test("returns auth token when found", async () => {
      const user = await userRepository.create("RefreshUser")
      const tokenPair = createTokenPair()
      await authTokenRepository.create(user.id, tokenPair)

      const found = await authTokenRepository.findByRefreshToken(
        tokenPair.refreshToken,
      )

      expect(found).not.toBeNull()
      expect(found?.refreshToken).toBe(tokenPair.refreshToken)
    })

    test("returns null when not found", async () => {
      const found = await authTokenRepository.findByRefreshToken("nonexistent")

      expect(found).toBeNull()
    })
  })

  describe("findByUserIdAndRefreshToken", () => {
    test("returns auth token when both match", async () => {
      const user = await userRepository.create("ComboUser")
      const tokenPair = createTokenPair()
      await authTokenRepository.create(user.id, tokenPair)

      const found = await authTokenRepository.findByUserIdAndRefreshToken(
        user.id,
        tokenPair.refreshToken,
      )

      expect(found).not.toBeNull()
      expect(found?.userId).toBe(user.id)
      expect(found?.refreshToken).toBe(tokenPair.refreshToken)
    })

    test("returns null when user id does not match", async () => {
      const user = await userRepository.create("MismatchUser")
      const tokenPair = createTokenPair()
      await authTokenRepository.create(user.id, tokenPair)

      const found = await authTokenRepository.findByUserIdAndRefreshToken(
        99999,
        tokenPair.refreshToken,
      )

      expect(found).toBeNull()
    })

    test("returns null when refresh token does not match", async () => {
      const user = await userRepository.create("MismatchUser2")
      const tokenPair = createTokenPair()
      await authTokenRepository.create(user.id, tokenPair)

      const found = await authTokenRepository.findByUserIdAndRefreshToken(
        user.id,
        "wrong_token",
      )

      expect(found).toBeNull()
    })
  })

  describe("deleteByUserId", () => {
    test("deletes all tokens for user", async () => {
      const user = await userRepository.create("DeleteUser")
      const tokenPair1 = createTokenPair()
      const tokenPair2 = createTokenPair()
      await authTokenRepository.create(user.id, tokenPair1)
      await authTokenRepository.create(user.id, tokenPair2)

      await authTokenRepository.deleteByUserId(user.id)

      const found1 = await authTokenRepository.findByAccessToken(
        tokenPair1.accessToken,
      )
      const found2 = await authTokenRepository.findByAccessToken(
        tokenPair2.accessToken,
      )

      expect(found1).toBeNull()
      expect(found2).toBeNull()
    })
  })

  describe("deleteExpiredTokens", () => {
    test("deletes tokens with expired refresh token", async () => {
      const user = await userRepository.create("ExpiredUser")
      const expiredTokenPair = {
        accessToken: `expired_access_${Date.now()}`,
        refreshToken: `expired_refresh_${Date.now()}`,
        accessTokenExpiresAt: new Date(Date.now() - 1000),
        refreshTokenExpiresAt: new Date(Date.now() - 1000), // Already expired
      }
      const validTokenPair = createTokenPair()

      await authTokenRepository.create(user.id, expiredTokenPair)
      await authTokenRepository.create(user.id, validTokenPair)

      await authTokenRepository.deleteExpiredTokens()

      const expiredFound = await authTokenRepository.findByAccessToken(
        expiredTokenPair.accessToken,
      )
      const validFound = await authTokenRepository.findByAccessToken(
        validTokenPair.accessToken,
      )

      expect(expiredFound).toBeNull()
      expect(validFound).not.toBeNull()
    })
  })
})
