import { beforeAll, describe, expect, test } from "bun:test"
import app from "../../app/index.ts"
import { rankingCache } from "../../app/libs/cache/ranking-cache.ts"
import { initializeDatabase } from "../../app/libs/db/client.ts"

// Initialize database tables
initializeDatabase()

type AuthResponse = {
  user: { id: number; name: string; wins: number; losses: number }
  accessToken: string
  refreshToken: string
}

type UserResponse = {
  user: { id: number; name: string; wins: number; losses: number }
}

type RankingResponse = {
  rankings: Array<{
    rank: number | null
    userId: number
    userName: string
    winRate: number
    wins: number
    losses: number
    totalMatches: number
  }>
  currentUser: {
    rank: number | null
    userId: number
    wins: number
    losses: number
    totalMatches: number
  }
}

const createUser = async (): Promise<AuthResponse> => {
  const res = await app.request("/api/users", { method: "POST" })
  return (await res.json()) as AuthResponse
}

const getUser = async (
  userId: number,
  accessToken: string,
): Promise<UserResponse> => {
  const res = await app.request(`/api/users/${userId}`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  })
  return (await res.json()) as UserResponse
}

const recordMatch = async (
  accessToken: string,
  visitorId: number,
  homeScore: number,
  visitorScore: number,
): Promise<Response> => {
  return app.request("/api/matches", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify({ visitorId, visitorScore, homeScore }),
  })
}

const getRanking = async (accessToken: string): Promise<RankingResponse> => {
  const res = await app.request("/api/rankings", {
    headers: { Authorization: `Bearer ${accessToken}` },
  })
  return (await res.json()) as RankingResponse
}

describe("Concurrency Tests", () => {
  beforeAll(() => {
    rankingCache.invalidate()
  })

  describe("Concurrent User Creation", () => {
    test("creates multiple users simultaneously without ID collision", async () => {
      const concurrentCount = 20

      // Create 20 users simultaneously
      const createPromises = Array.from({ length: concurrentCount }, () =>
        createUser(),
      )

      const users = await Promise.all(createPromises)

      // Verify all users were created
      expect(users.length).toBe(concurrentCount)

      // Verify all IDs are unique
      const ids = users.map((u) => u.user.id)
      const uniqueIds = new Set(ids)
      expect(uniqueIds.size).toBe(concurrentCount)

      // Verify all tokens are unique
      const accessTokens = users.map((u) => u.accessToken)
      const uniqueTokens = new Set(accessTokens)
      expect(uniqueTokens.size).toBe(concurrentCount)
    })
  })

  describe("Concurrent Match Recording", () => {
    test("records multiple matches simultaneously with correct stats", async () => {
      // Create users for this test
      const home = await createUser()
      const visitors = await Promise.all(
        Array.from({ length: 10 }, () => createUser()),
      )

      // Record 10 matches simultaneously (home wins all)
      const matchPromises = visitors.map((visitor) =>
        recordMatch(home.accessToken, visitor.user.id, 3, 1),
      )

      const responses = await Promise.all(matchPromises)

      // All requests should succeed
      for (const res of responses) {
        expect(res.status).toBe(201)
      }

      // With atomic increments, all 10 wins should be recorded correctly
      const homeStats = await getUser(home.user.id, home.accessToken)
      expect(homeStats.user.wins).toBe(10)
      expect(homeStats.user.losses).toBe(0)
    })

    test("handles concurrent matches - sequential stats are accurate", async () => {
      const user1 = await createUser()
      const user2 = await createUser()

      // Record matches sequentially for accurate counting
      await recordMatch(user1.accessToken, user2.user.id, 3, 1) // user1 wins
      await recordMatch(user1.accessToken, user2.user.id, 2, 1) // user1 wins
      await recordMatch(user2.accessToken, user1.user.id, 3, 0) // user2 wins
      await recordMatch(user1.accessToken, user2.user.id, 1, 0) // user1 wins
      await recordMatch(user2.accessToken, user1.user.id, 2, 1) // user2 wins

      // Verify final stats are consistent
      const stats1 = await getUser(user1.user.id, user1.accessToken)
      const stats2 = await getUser(user2.user.id, user2.accessToken)

      // user1: 3 wins (as home winner), 2 losses (as visitor loser)
      // user2: 2 wins (as home winner), 3 losses (as visitor loser)
      expect(stats1.user.wins).toBe(3)
      expect(stats1.user.losses).toBe(2)
      expect(stats2.user.wins).toBe(2)
      expect(stats2.user.losses).toBe(3)

      // Total matches should be consistent
      const totalMatches =
        stats1.user.wins +
        stats1.user.losses +
        stats2.user.wins +
        stats2.user.losses
      expect(totalMatches).toBe(10) // 5 matches * 2 participants
    })

    test("concurrent matches - maintains data integrity", async () => {
      const players = await Promise.all(
        Array.from({ length: 5 }, () => createUser()),
      )

      // Create many concurrent matches between all players
      const matchPromises: Promise<Response>[] = []

      // Each player plays against every other player
      for (let i = 0; i < players.length; i++) {
        for (let j = 0; j < players.length; j++) {
          if (i !== j) {
            matchPromises.push(
              recordMatch(players[i]!.accessToken, players[j]!.user.id, 2, 1),
            )
          }
        }
      }

      // Execute all matches concurrently
      const responses = await Promise.all(matchPromises)

      // All should succeed (201 status)
      const successCount = responses.filter((r) => r.status === 201).length
      expect(successCount).toBe(matchPromises.length)

      // Get all player stats
      let totalWins = 0
      let totalLosses = 0

      for (const player of players) {
        const stats = await getUser(player.user.id, player.accessToken)
        totalWins += stats.user.wins
        totalLosses += stats.user.losses
      }

      // With atomic increments, total wins should equal total losses
      expect(totalWins).toBe(totalLosses)
      expect(totalWins).toBe(20) // 5 players * 4 opponents each
    })

    test("sequential matches maintain perfect data integrity", async () => {
      const players = await Promise.all(
        Array.from({ length: 3 }, () => createUser()),
      )

      // Record matches sequentially to verify correct implementation
      for (let i = 0; i < players.length; i++) {
        for (let j = 0; j < players.length; j++) {
          if (i !== j) {
            await recordMatch(
              players[i]!.accessToken,
              players[j]!.user.id,
              2,
              1,
            )
          }
        }
      }

      // Get all player stats
      let totalWins = 0
      let totalLosses = 0

      for (const player of players) {
        const stats = await getUser(player.user.id, player.accessToken)
        totalWins += stats.user.wins
        totalLosses += stats.user.losses
      }

      // Sequential execution should maintain perfect integrity
      expect(totalWins).toBe(totalLosses)
      expect(totalWins).toBe(6) // 3 players * 2 matches each as home
    })
  })

  describe("Concurrent Ranking Access", () => {
    test("returns consistent rankings under concurrent access", async () => {
      rankingCache.invalidate()

      // Create qualified users
      const user = await createUser()
      const opponents = await Promise.all(
        Array.from({ length: 3 }, () => createUser()),
      )

      // Play enough matches to qualify
      for (const opponent of opponents) {
        for (let i = 0; i < 4; i++) {
          await recordMatch(user.accessToken, opponent.user.id, 2, 1)
        }
      }

      rankingCache.invalidate()

      // Make 20 concurrent ranking requests
      const rankingPromises = Array.from({ length: 20 }, () =>
        getRanking(user.accessToken),
      )

      const rankings = await Promise.all(rankingPromises)

      // All responses should be identical
      const firstRanking = JSON.stringify(rankings[0]!.rankings)
      for (let i = 1; i < rankings.length; i++) {
        expect(JSON.stringify(rankings[i]!.rankings)).toBe(firstRanking)
      }
    })

    test("handles concurrent ranking requests during match recording", async () => {
      rankingCache.invalidate()

      const user = await createUser()
      const opponent = await createUser()

      // Mix of match recording and ranking requests
      const operations: Promise<unknown>[] = []

      for (let i = 0; i < 10; i++) {
        // Record a match
        operations.push(recordMatch(user.accessToken, opponent.user.id, 2, 1))
        // Get ranking
        operations.push(getRanking(user.accessToken))
      }

      // Execute all operations concurrently
      const results = await Promise.all(operations)

      // Verify all operations completed successfully
      for (let i = 0; i < results.length; i++) {
        const result = results[i]
        if (i % 2 === 0) {
          // Match response
          expect((result as Response).status).toBe(201)
        } else {
          // Ranking response - should have valid structure
          const ranking = result as RankingResponse
          expect(ranking.rankings).toBeArray()
          expect(ranking.currentUser).toBeDefined()
        }
      }
    })
  })

  describe("Concurrent Name Updates", () => {
    test("handles concurrent name update attempts for same user", async () => {
      const user = await createUser()

      // Try to update name concurrently with different values
      const names = ["NameA001", "NameB002", "NameC003", "NameD004", "NameE005"]
      const updatePromises = names.map((name) =>
        app.request(`/api/users/${user.user.id}/name`, {
          method: "PATCH",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${user.accessToken}`,
          },
          body: JSON.stringify({ name }),
        }),
      )

      const responses = await Promise.all(updatePromises)

      // All should succeed (last write wins)
      for (const res of responses) {
        expect(res.status).toBe(200)
      }

      // Final name should be one of the attempted names
      const finalUser = await getUser(user.user.id, user.accessToken)
      expect(names).toContain(finalUser.user.name)
    })

    test("handles concurrent name updates for different users", async () => {
      const users = await Promise.all(
        Array.from({ length: 10 }, () => createUser()),
      )

      // Update all users' names concurrently
      const updatePromises = users.map((user, i) =>
        app.request(`/api/users/${user.user.id}/name`, {
          method: "PATCH",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${user.accessToken}`,
          },
          body: JSON.stringify({
            name: `Concurrent${i.toString().padStart(2, "0")}`,
          }),
        }),
      )

      const responses = await Promise.all(updatePromises)

      // All should succeed
      for (const res of responses) {
        expect(res.status).toBe(200)
      }

      // Verify all names were updated correctly
      for (let i = 0; i < users.length; i++) {
        const user = users[i]!
        const updated = await getUser(user.user.id, user.accessToken)
        expect(updated.user.name).toBe(
          `Concurrent${i.toString().padStart(2, "0")}`,
        )
      }
    })
  })

  describe("Concurrent Token Operations", () => {
    test("handles concurrent token refresh requests", async () => {
      const user = await createUser()

      // Try to refresh token multiple times concurrently
      const refreshPromises = Array.from({ length: 5 }, () =>
        app.request("/api/auth/refresh", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ refreshToken: user.refreshToken }),
        }),
      )

      const responses = await Promise.all(refreshPromises)

      // At least one should succeed, others may fail due to token rotation
      const successCount = responses.filter((r) => r.status === 200).length
      const failCount = responses.filter((r) => r.status === 401).length

      // First request should succeed, subsequent may fail if token was rotated
      expect(successCount).toBeGreaterThanOrEqual(1)
      expect(successCount + failCount).toBe(5)
    })

    test("handles concurrent login and API requests", async () => {
      const user = await createUser()

      // Concurrent login and user info requests
      const operations = [
        // Login requests
        app.request("/api/users/login", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            userId: user.user.id,
            refreshToken: user.refreshToken,
          }),
        }),
        // User info requests
        getUser(user.user.id, user.accessToken),
        getUser(user.user.id, user.accessToken),
        // Another login
        app.request("/api/users/login", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            userId: user.user.id,
            refreshToken: user.refreshToken,
          }),
        }),
      ]

      const results = await Promise.all(operations)

      // User info requests should always succeed with valid token
      expect((results[1] as UserResponse).user.id).toBe(user.user.id)
      expect((results[2] as UserResponse).user.id).toBe(user.user.id)
    })
  })

  describe("Race Condition Prevention", () => {
    test("prevents double-counting in rapid match submissions", async () => {
      const home = await createUser()
      const visitor = await createUser()

      // Submit the exact same match data multiple times rapidly
      const matchData = {
        visitorId: visitor.user.id,
        visitorScore: 1,
        homeScore: 3,
      }

      const rapidSubmissions = Array.from({ length: 10 }, () =>
        app.request("/api/matches", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${home.accessToken}`,
          },
          body: JSON.stringify(matchData),
        }),
      )

      await Promise.all(rapidSubmissions)

      // Each submission creates a separate match (this is expected behavior)
      // But stats should be consistent
      const homeStats = await getUser(home.user.id, home.accessToken)
      const visitorStats = await getUser(visitor.user.id, visitor.accessToken)

      // Wins + Losses should equal for consistency
      expect(homeStats.user.wins).toBe(visitorStats.user.losses)
      expect(homeStats.user.losses).toBe(visitorStats.user.wins)
    })

    test("maintains ranking consistency during concurrent updates", async () => {
      rankingCache.invalidate()

      // Create multiple users and have them play matches concurrently
      const players = await Promise.all(
        Array.from({ length: 6 }, () => createUser()),
      )

      // Concurrent match recording
      const matchPromises: Promise<Response>[] = []
      for (let round = 0; round < 3; round++) {
        for (let i = 0; i < players.length; i++) {
          for (let j = i + 1; j < players.length; j++) {
            matchPromises.push(
              recordMatch(players[i]!.accessToken, players[j]!.user.id, 2, 1),
            )
          }
        }
      }

      await Promise.all(matchPromises)

      rankingCache.invalidate()

      // Get rankings from multiple users simultaneously
      const rankingPromises = players.map((p) => getRanking(p.accessToken))
      const rankings = await Promise.all(rankingPromises)

      // All rankings should show the same top 10
      const firstTopRankings = JSON.stringify(rankings[0]!.rankings)
      for (let i = 1; i < rankings.length; i++) {
        expect(JSON.stringify(rankings[i]!.rankings)).toBe(firstTopRankings)
      }

      // Rankings should be properly ordered by win rate
      const topRankings = rankings[0]!.rankings
      for (let i = 1; i < topRankings.length; i++) {
        const prev = topRankings[i - 1]!
        const curr = topRankings[i]!
        expect(prev.winRate).toBeGreaterThanOrEqual(curr.winRate)
      }
    })
  })
})
