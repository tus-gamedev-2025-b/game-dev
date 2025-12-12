import { describe, expect, test } from "bun:test"
import app from "../../app/index.ts"
import { initializeDatabase } from "../../app/libs/db/client.ts"

// Initialize database tables
initializeDatabase()

type AuthResponse = {
  user: { id: number; name: string }
  accessToken: string
  refreshToken: string
}

type MatchResponse = {
  match: {
    id: number
    winnerId: number
    loserId: number
    playedAt: string
  }
  updatedStats: {
    wins: number
    losses: number
    totalMatches: number
  }
}

type ErrorResponse = {
  error: { code: string; message: string }
}

const createTestUser = async (): Promise<AuthResponse> => {
  const res = await app.request("/api/users", { method: "POST" })
  return (await res.json()) as AuthResponse
}

describe("Match API", () => {
  describe("POST /api/matches", () => {
    test("records match successfully", async () => {
      const home = await createTestUser()
      const visitor = await createTestUser()

      const res = await app.request("/api/matches", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${home.accessToken}`,
        },
        body: JSON.stringify({
          visitorId: visitor.user.id,
          visitorScore: 1,
          homeScore: 3,
        }),
      })

      expect(res.status).toBe(201)

      const body = (await res.json()) as MatchResponse
      expect(body.match).toBeDefined()
      expect(body.match.winnerId).toBe(home.user.id)
      expect(body.match.loserId).toBe(visitor.user.id)
      expect(body.updatedStats.wins).toBe(1)
      expect(body.updatedStats.losses).toBe(0)
      expect(body.updatedStats.totalMatches).toBe(1)
    })

    test("returns 401 without authorization", async () => {
      const visitor = await createTestUser()

      const res = await app.request("/api/matches", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          visitorId: visitor.user.id,
          visitorScore: 1,
          homeScore: 3,
        }),
      })

      expect(res.status).toBe(401)
    })

    test("returns 400 for self match", async () => {
      const home = await createTestUser()

      const res = await app.request("/api/matches", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${home.accessToken}`,
        },
        body: JSON.stringify({
          visitorId: home.user.id,
          visitorScore: 1,
          homeScore: 3,
        }),
      })

      expect(res.status).toBe(400)

      const body = (await res.json()) as ErrorResponse
      expect(body.error.code).toBe("SELF_MATCH_NOT_ALLOWED")
    })

    test("returns 404 for non-existent visitor", async () => {
      const home = await createTestUser()

      const res = await app.request("/api/matches", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${home.accessToken}`,
        },
        body: JSON.stringify({
          visitorId: 99999,
          visitorScore: 1,
          homeScore: 3,
        }),
      })

      expect(res.status).toBe(404)

      const body = (await res.json()) as ErrorResponse
      expect(body.error.code).toBe("VISITOR_NOT_FOUND")
    })

    test("records visitor as winner when visitor score is higher", async () => {
      const home = await createTestUser()
      const visitor = await createTestUser()

      const res = await app.request("/api/matches", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${home.accessToken}`,
        },
        body: JSON.stringify({
          visitorId: visitor.user.id,
          visitorScore: 5,
          homeScore: 2,
        }),
      })

      expect(res.status).toBe(201)

      const body = (await res.json()) as MatchResponse
      expect(body.match.winnerId).toBe(visitor.user.id)
      expect(body.match.loserId).toBe(home.user.id)
      expect(body.updatedStats.wins).toBe(0)
      expect(body.updatedStats.losses).toBe(1)
    })
  })
})
