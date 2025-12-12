import { beforeEach, describe, expect, test } from "bun:test"
import type { RankedUser } from "../../domain/ranking/entity.ts"
import { RankingCache, rankingCache } from "./ranking-cache.ts"

describe("RankingCache", () => {
  const createTestRankings = (): RankedUser[] => [
    {
      rank: 1,
      userId: 1,
      userName: "Player1",
      winRate: 80,
      wins: 8,
      losses: 2,
      totalMatches: 10,
    },
    {
      rank: 2,
      userId: 2,
      userName: "Player2",
      winRate: 70,
      wins: 7,
      losses: 3,
      totalMatches: 10,
    },
  ]

  describe("with default TTL", () => {
    beforeEach(() => {
      rankingCache.invalidate()
    })

    test("returns null when cache is empty", () => {
      const result = rankingCache.get()

      expect(result).toBeNull()
    })

    test("returns data after set", () => {
      const rankings = createTestRankings()

      rankingCache.set(rankings)
      const result = rankingCache.get()

      expect(result).toEqual(rankings)
    })

    test("invalidate clears the cache", () => {
      const rankings = createTestRankings()
      rankingCache.set(rankings)

      rankingCache.invalidate()
      const result = rankingCache.get()

      expect(result).toBeNull()
    })

    test("set overwrites existing cache", () => {
      const rankings1 = createTestRankings()
      const rankings2: RankedUser[] = [
        {
          rank: 1,
          userId: 3,
          userName: "NewPlayer",
          winRate: 90,
          wins: 9,
          losses: 1,
          totalMatches: 10,
        },
      ]

      rankingCache.set(rankings1)
      rankingCache.set(rankings2)
      const result = rankingCache.get()

      expect(result).toEqual(rankings2)
    })
  })

  describe("with custom TTL", () => {
    test("returns data within TTL", () => {
      const cache = new RankingCache(1000) // 1 second TTL
      const rankings = createTestRankings()

      cache.set(rankings)
      const result = cache.get()

      expect(result).toEqual(rankings)
    })

    test("returns null after TTL expires", async () => {
      const cache = new RankingCache(50) // 50ms TTL
      const rankings = createTestRankings()

      cache.set(rankings)

      // Wait for cache to expire
      await new Promise((resolve) => setTimeout(resolve, 60))

      const result = cache.get()

      expect(result).toBeNull()
    })

    test("cache is cleared when accessed after expiry", async () => {
      const cache = new RankingCache(50) // 50ms TTL
      const rankings = createTestRankings()

      cache.set(rankings)

      // Wait for cache to expire
      await new Promise((resolve) => setTimeout(resolve, 60))

      // First access clears the cache
      cache.get()

      // Second access should still be null
      const result = cache.get()

      expect(result).toBeNull()
    })

    test("refreshing cache resets TTL", async () => {
      const cache = new RankingCache(100) // 100ms TTL
      const rankings = createTestRankings()

      cache.set(rankings)

      // Wait 50ms (half of TTL)
      await new Promise((resolve) => setTimeout(resolve, 50))

      // Refresh the cache
      cache.set(rankings)

      // Wait another 70ms (total 120ms from first set, but only 70ms from refresh)
      await new Promise((resolve) => setTimeout(resolve, 70))

      const result = cache.get()

      // Should still be valid since we refreshed
      expect(result).toEqual(rankings)
    })

    test("handles empty array", () => {
      const cache = new RankingCache(1000)

      cache.set([])
      const result = cache.get()

      expect(result).toEqual([])
    })

    test("invalidate works regardless of TTL", () => {
      const cache = new RankingCache(60000) // 1 minute TTL
      const rankings = createTestRankings()

      cache.set(rankings)
      cache.invalidate()
      const result = cache.get()

      expect(result).toBeNull()
    })
  })

  describe("edge cases", () => {
    test("handles very short TTL", async () => {
      const cache = new RankingCache(1) // 1ms TTL
      const rankings = createTestRankings()

      cache.set(rankings)

      await new Promise((resolve) => setTimeout(resolve, 5))

      const result = cache.get()
      expect(result).toBeNull()
    })

    test("handles large dataset", () => {
      const cache = new RankingCache(1000)
      const largeRankings: RankedUser[] = Array.from(
        { length: 1000 },
        (_, i) => ({
          rank: i + 1,
          userId: i + 1,
          userName: `Player${i + 1}`,
          winRate: Math.random() * 100,
          wins: Math.floor(Math.random() * 100),
          losses: Math.floor(Math.random() * 100),
          totalMatches: 100,
        }),
      )

      cache.set(largeRankings)
      const result = cache.get()

      expect(result).toHaveLength(1000)
      expect(result?.[0]?.rank).toBe(1)
      expect(result?.[999]?.rank).toBe(1000)
    })

    test("multiple get calls return same data", () => {
      const cache = new RankingCache(1000)
      const rankings = createTestRankings()

      cache.set(rankings)

      const result1 = cache.get()
      const result2 = cache.get()
      const result3 = cache.get()

      expect(result1).toEqual(result2)
      expect(result2).toEqual(result3)
    })
  })
})
