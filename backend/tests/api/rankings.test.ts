import { describe, expect, test } from "bun:test"
import app from "../../app/index.ts"
import { rankingCache } from "../../app/libs/cache/ranking-cache.ts"
import { initializeDatabase } from "../../app/libs/db/client.ts"

// Initialize database tables
initializeDatabase()

type AuthResponse = {
  user: { id: number; name: string }
  accessToken: string
  refreshToken: string
}

type RankedUser = {
  rank: number | null
  userId: number
  userName: string
  winRate: number
  wins: number
  losses: number
  totalMatches: number
}

type RankingResponse = {
  rankings: RankedUser[]
  currentUser: RankedUser
}

const createTestUser = async (): Promise<AuthResponse> => {
  const res = await app.request("/api/users", { method: "POST" })
  return (await res.json()) as AuthResponse
}

const recordMatch = async (
  accessToken: string,
  visitorId: number,
  homeScore: number,
  visitorScore: number,
) => {
  await app.request("/api/matches", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify({ visitorId, visitorScore, homeScore }),
  })
}

describe("Ranking API", () => {
  describe("GET /api/rankings", () => {
    test("returns rankings successfully", async () => {
      const user = await createTestUser()

      const res = await app.request("/api/rankings", {
        headers: {
          Authorization: `Bearer ${user.accessToken}`,
        },
      })

      expect(res.status).toBe(200)

      const body = (await res.json()) as RankingResponse
      expect(body.rankings).toBeArray()
      expect(body.currentUser).toBeDefined()
      expect(body.currentUser.userId).toBe(user.user.id)
    })

    test("returns 401 without authorization", async () => {
      const res = await app.request("/api/rankings")

      expect(res.status).toBe(401)
    })

    test("returns null rank for user with less than 10 matches", async () => {
      const user = await createTestUser()
      const opponent = await createTestUser()

      // Play 5 matches
      for (let i = 0; i < 5; i++) {
        await recordMatch(user.accessToken, opponent.user.id, 1, 0)
      }

      const res = await app.request("/api/rankings", {
        headers: {
          Authorization: `Bearer ${user.accessToken}`,
        },
      })

      expect(res.status).toBe(200)

      const body = (await res.json()) as RankingResponse
      expect(body.currentUser.rank).toBeNull()
      expect(body.currentUser.wins).toBe(5)
      expect(body.currentUser.totalMatches).toBe(5)
    })

    test("returns rank for user with 10+ matches", async () => {
      rankingCache.invalidate()
      const user = await createTestUser()
      const opponent = await createTestUser()

      // Play 10 matches
      for (let i = 0; i < 10; i++) {
        await recordMatch(user.accessToken, opponent.user.id, 1, 0)
      }

      const res = await app.request("/api/rankings", {
        headers: {
          Authorization: `Bearer ${user.accessToken}`,
        },
      })

      expect(res.status).toBe(200)

      const body = (await res.json()) as RankingResponse
      expect(body.currentUser.rank).toBeNumber()
      expect(body.currentUser.wins).toBe(10)
      expect(body.currentUser.winRate).toBe(100)
    })

    test("includes user in rankings when qualified", async () => {
      rankingCache.invalidate()
      const user = await createTestUser()

      // Create multiple opponents and win many matches to ensure top ranking
      const opponents: AuthResponse[] = []
      for (let i = 0; i < 5; i++) {
        opponents.push(await createTestUser())
      }

      // Play 50 matches (10 vs each opponent) - all wins = 100% win rate with 50 wins
      for (const opponent of opponents) {
        for (let i = 0; i < 10; i++) {
          await recordMatch(user.accessToken, opponent.user.id, 1, 0)
        }
      }

      const res = await app.request("/api/rankings", {
        headers: {
          Authorization: `Bearer ${user.accessToken}`,
        },
      })

      expect(res.status).toBe(200)

      const body = (await res.json()) as RankingResponse
      const userInRankings = body.rankings.find(
        (r) => r.userId === user.user.id,
      )
      expect(userInRankings).toBeDefined()
    })
  })
})
