import { describe, expect, test } from "bun:test"
import { rankingCache } from "../../libs/cache/ranking-cache.ts"
import { initializeDatabase } from "../../libs/db/client.ts"
import { recordMatch } from "../match/record-match.ts"
import { createUser } from "../user/create-user.ts"
import { getRanking } from "./get-ranking.ts"

// Initialize database tables
initializeDatabase()

describe("getRanking usecase", () => {
  test("returns current user info for new user", async () => {
    rankingCache.invalidate()
    const user = await createUser()

    const result = await getRanking(user.user.id)

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.data.rankings).toBeArray()
      expect(result.data.currentUser.rank).toBeNull()
      expect(result.data.currentUser.userId).toBe(user.user.id)
      expect(result.data.currentUser.totalMatches).toBe(0)
    }
  })

  test("returns current user with null rank when under 10 matches", async () => {
    const user = await createUser()
    const opponent = await createUser()

    // Play 5 matches (under 10)
    for (let i = 0; i < 5; i++) {
      await recordMatch(user.user.id, {
        visitorId: opponent.user.id,
        visitorScore: 0,
        homeScore: 1,
      })
    }

    const result = await getRanking(user.user.id)

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.data.currentUser.rank).toBeNull()
      expect(result.data.currentUser.wins).toBe(5)
      expect(result.data.currentUser.totalMatches).toBe(5)
    }
  })

  test("returns rank when user has 10+ matches", async () => {
    rankingCache.invalidate()
    const user = await createUser()
    const opponent = await createUser()

    // Play 10 matches
    for (let i = 0; i < 10; i++) {
      await recordMatch(user.user.id, {
        visitorId: opponent.user.id,
        visitorScore: 0,
        homeScore: 1,
      })
    }

    const result = await getRanking(user.user.id)

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.data.currentUser.rank).toBeNumber()
      expect(result.data.currentUser.wins).toBe(10)
      expect(result.data.currentUser.totalMatches).toBe(10)
      expect(result.data.currentUser.winRate).toBe(100)
    }
  })

  test("calculates win rate correctly", async () => {
    rankingCache.invalidate()
    const user = await createUser()
    const opponent = await createUser()

    // Win 7, lose 3 = 70% win rate
    for (let i = 0; i < 7; i++) {
      await recordMatch(user.user.id, {
        visitorId: opponent.user.id,
        visitorScore: 0,
        homeScore: 1,
      })
    }
    for (let i = 0; i < 3; i++) {
      await recordMatch(user.user.id, {
        visitorId: opponent.user.id,
        visitorScore: 1,
        homeScore: 0,
      })
    }

    const result = await getRanking(user.user.id)

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.data.currentUser.winRate).toBe(70)
      expect(result.data.currentUser.wins).toBe(7)
      expect(result.data.currentUser.losses).toBe(3)
    }
  })

  test("fails for non-existent user", async () => {
    const result = await getRanking(99999)

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.code).toBe("USER_NOT_FOUND")
    }
  })

  test("uses cache for subsequent requests", async () => {
    rankingCache.invalidate()
    const user = await createUser()
    const opponent = await createUser()

    // Create qualified user
    for (let i = 0; i < 10; i++) {
      await recordMatch(user.user.id, {
        visitorId: opponent.user.id,
        visitorScore: 0,
        homeScore: 1,
      })
    }

    // First request - should hit DB
    const result1 = await getRanking(user.user.id)
    expect(result1.success).toBe(true)

    // Second request - should use cache
    const result2 = await getRanking(user.user.id)
    expect(result2.success).toBe(true)

    if (result1.success && result2.success) {
      expect(result1.data.rankings.length).toBe(result2.data.rankings.length)
    }
  })
})
