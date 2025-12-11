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

type TestUser = {
  id: number
  name: string
  accessToken: string
  refreshToken: string
  expectedWins: number
  expectedLosses: number
}

const createUser = async (name?: string): Promise<TestUser> => {
  const res = await app.request("/api/users", { method: "POST" })
  const body = (await res.json()) as AuthResponse

  const user: TestUser = {
    id: body.user.id,
    name: body.user.name,
    accessToken: body.accessToken,
    refreshToken: body.refreshToken,
    expectedWins: 0,
    expectedLosses: 0,
  }

  if (name) {
    await app.request(`/api/users/${user.id}/name`, {
      method: "PATCH",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${user.accessToken}`,
      },
      body: JSON.stringify({ name }),
    })
    user.name = name
  }

  return user
}

const recordMatch = async (
  home: TestUser,
  visitor: TestUser,
  homeScore: number,
  visitorScore: number,
): Promise<MatchResponse> => {
  const res = await app.request("/api/matches", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${home.accessToken}`,
    },
    body: JSON.stringify({
      visitorId: visitor.id,
      visitorScore,
      homeScore,
    }),
  })

  const body = (await res.json()) as MatchResponse

  // Track expected stats
  if (homeScore > visitorScore) {
    home.expectedWins++
    visitor.expectedLosses++
  } else {
    visitor.expectedWins++
    home.expectedLosses++
  }

  return body
}

const getRanking = async (user: TestUser): Promise<RankingResponse> => {
  const res = await app.request("/api/rankings", {
    headers: {
      Authorization: `Bearer ${user.accessToken}`,
    },
  })
  return (await res.json()) as RankingResponse
}

const getUser = async (
  userId: number,
  accessToken: string,
): Promise<UserResponse> => {
  const res = await app.request(`/api/users/${userId}`, {
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })
  return (await res.json()) as UserResponse
}

describe("Workflow Integration Test", () => {
  beforeAll(() => {
    rankingCache.invalidate()
  })

  describe("30 Users Scenario", () => {
    const users: TestUser[] = []

    test("creates 30 users with unique names", async () => {
      for (let i = 1; i <= 30; i++) {
        const user = await createUser(`Player${i.toString().padStart(2, "0")}`)
        users.push(user)
      }

      expect(users.length).toBe(30)

      // Verify all users have unique IDs
      const ids = new Set(users.map((u) => u.id))
      expect(ids.size).toBe(30)

      // Verify names are set correctly
      for (let i = 0; i < 30; i++) {
        expect(users[i]!.name).toBe(
          `Player${(i + 1).toString().padStart(2, "0")}`,
        )
      }
    })

    test("records matches between users with varied results", async () => {
      // Create a realistic match distribution
      // Top players (1-5) win most matches
      // Middle players (6-20) have mixed results
      // Bottom players (21-30) lose most matches

      const matchResults: Array<{
        home: number
        visitor: number
        homeScore: number
        visitorScore: number
      }> = []

      // Top players beat everyone else consistently
      for (let topIdx = 0; topIdx < 5; topIdx++) {
        for (let otherIdx = 5; otherIdx < 30; otherIdx++) {
          // Top player wins against non-top players
          matchResults.push({
            home: topIdx,
            visitor: otherIdx,
            homeScore: 3,
            visitorScore: 1,
          })
        }
      }

      // Top players play each other (varied results)
      for (let i = 0; i < 5; i++) {
        for (let j = i + 1; j < 5; j++) {
          // Higher ranked top player wins
          matchResults.push({
            home: i,
            visitor: j,
            homeScore: 2,
            visitorScore: 1,
          })
        }
      }

      // Middle players play each other
      for (let i = 5; i < 20; i++) {
        for (let j = i + 1; j < 20; j++) {
          // Alternate wins
          const homeWins = (i + j) % 2 === 0
          matchResults.push({
            home: i,
            visitor: j,
            homeScore: homeWins ? 2 : 1,
            visitorScore: homeWins ? 1 : 2,
          })
        }
      }

      // Bottom players play each other
      for (let i = 20; i < 30; i++) {
        for (let j = i + 1; j < 30; j++) {
          matchResults.push({
            home: i,
            visitor: j,
            homeScore: 1,
            visitorScore: 2,
          })
        }
      }

      // Middle vs Bottom
      for (let midIdx = 5; midIdx < 20; midIdx++) {
        for (let botIdx = 20; botIdx < 30; botIdx++) {
          matchResults.push({
            home: midIdx,
            visitor: botIdx,
            homeScore: 2,
            visitorScore: 1,
          })
        }
      }

      // Record all matches
      for (const match of matchResults) {
        await recordMatch(
          users[match.home]!,
          users[match.visitor]!,
          match.homeScore,
          match.visitorScore,
        )
      }

      // Verify match count
      expect(matchResults.length).toBeGreaterThan(100)
    })

    test("verifies user stats are correctly updated", async () => {
      // Verify each user's stats match expected values
      for (const user of users) {
        const response = await getUser(user.id, user.accessToken)
        expect(response.user.wins).toBe(user.expectedWins)
        expect(response.user.losses).toBe(user.expectedLosses)
      }
    })

    test("verifies ranking accuracy based on win rate", async () => {
      rankingCache.invalidate()

      const response = await getRanking(users[0]!)
      const rankings = response.rankings

      // Calculate expected win rates for qualified users from our test
      const qualifiedTestUsers = users
        .filter((u) => u.expectedWins + u.expectedLosses >= 10)
        .map((u) => ({
          userId: u.id,
          userName: u.name,
          wins: u.expectedWins,
          losses: u.expectedLosses,
          totalMatches: u.expectedWins + u.expectedLosses,
          winRate:
            Math.round(
              (u.expectedWins / (u.expectedWins + u.expectedLosses)) * 10000,
            ) / 100,
        }))
        .sort((a, b) => {
          // Sort by win rate desc, then wins desc, then userId asc
          if (b.winRate !== a.winRate) return b.winRate - a.winRate
          if (b.wins !== a.wins) return b.wins - a.wins
          return a.userId - b.userId
        })

      // Rankings should contain up to 10 users
      expect(rankings.length).toBeLessThanOrEqual(10)
      expect(rankings.length).toBeGreaterThan(0)

      // Verify rankings are sorted correctly (by rank)
      for (let i = 0; i < rankings.length; i++) {
        const ranking = rankings[i]!
        expect(ranking.rank).toBe(i + 1)

        // Verify win rate calculation is correct for users in ranking
        if (ranking.totalMatches > 0) {
          const expectedWinRate =
            Math.round((ranking.wins / ranking.totalMatches) * 10000) / 100
          expect(ranking.winRate).toBeCloseTo(expectedWinRate, 1)
        }
      }

      // Verify that users are ranked by win rate (descending)
      for (let i = 1; i < rankings.length; i++) {
        const prev = rankings[i - 1]!
        const curr = rankings[i]!
        // Win rate should be descending (or equal with tie-breaker)
        expect(prev.winRate).toBeGreaterThanOrEqual(curr.winRate)
      }

      // Verify our top test user (Player01) has highest win rate among test users
      const topTestUser = qualifiedTestUsers[0]!
      const player01InRanking = rankings.find(
        (r) => r.userId === topTestUser.userId,
      )
      expect(player01InRanking).toBeDefined()
      expect(player01InRanking!.winRate).toBeCloseTo(topTestUser.winRate, 1)
    })

    test("verifies unqualified users have null rank", async () => {
      // Create a new user with few matches
      const newUser = await createUser("NewPlayer")
      const opponent = users[0]!

      // Play only 5 matches
      for (let i = 0; i < 5; i++) {
        await recordMatch(newUser, opponent, 1, 2) // newUser loses
      }

      rankingCache.invalidate()
      const response = await getRanking(newUser)

      expect(response.currentUser.rank).toBeNull()
      expect(response.currentUser.totalMatches).toBe(5)
    })

    test("verifies ranking updates after new matches", async () => {
      rankingCache.invalidate()

      // Get initial ranking
      const initialRanking = await getRanking(users[0]!)
      const initialTop = initialRanking.rankings[0]

      // Have a lower-ranked player win many matches
      const challenger = users[15]! // Middle player
      const weakOpponents = users.slice(25, 30) // Bottom players

      // Challenger wins 20 more matches
      for (const opponent of weakOpponents) {
        for (let i = 0; i < 4; i++) {
          await recordMatch(challenger, opponent, 3, 0)
        }
      }

      rankingCache.invalidate()

      // Get updated ranking
      const updatedRanking = await getRanking(challenger)

      // Verify challenger stats increased
      expect(updatedRanking.currentUser.wins).toBeGreaterThan(0)

      // Top player should still be top (they have more total wins)
      expect(updatedRanking.rankings[0]!.userId).toBe(initialTop!.userId)
    })

    test("verifies cache consistency", async () => {
      // Make multiple ranking requests and verify consistency
      const responses: RankingResponse[] = []

      for (let i = 0; i < 5; i++) {
        const response = await getRanking(users[i]!)
        responses.push(response)
      }

      // All responses should have the same rankings
      const firstRankings = JSON.stringify(responses[0]!.rankings)
      for (let i = 1; i < responses.length; i++) {
        expect(JSON.stringify(responses[i]!.rankings)).toBe(firstRankings)
      }
    })

    test("verifies currentUser is correct for each user", async () => {
      rankingCache.invalidate()

      // Check a few users to verify currentUser is correctly populated
      for (let i = 0; i < 5; i++) {
        const user = users[i]!
        const response = await getRanking(user)

        expect(response.currentUser.userId).toBe(user.id)
        expect(response.currentUser.userName).toBe(user.name)
        expect(response.currentUser.wins).toBe(user.expectedWins)
        expect(response.currentUser.losses).toBe(user.expectedLosses)
      }
    })
  })

  describe("User Workflow Scenarios", () => {
    test("complete user journey: create, play, rank", async () => {
      rankingCache.invalidate()

      // New user signs up
      const newUser = await createUser("JourneyUser")
      expect(newUser.id).toBeNumber()

      // Check initial stats
      let userInfo = await getUser(newUser.id, newUser.accessToken)
      expect(userInfo.user.wins).toBe(0)
      expect(userInfo.user.losses).toBe(0)

      // Create opponents
      const opponents: TestUser[] = []
      for (let i = 0; i < 3; i++) {
        opponents.push(await createUser(`Opponent${i}`))
      }

      // Play 12 matches (4 vs each opponent) - win most
      for (const opponent of opponents) {
        await recordMatch(newUser, opponent, 3, 1) // win
        await recordMatch(newUser, opponent, 3, 2) // win
        await recordMatch(newUser, opponent, 2, 1) // win
        await recordMatch(newUser, opponent, 0, 3) // lose
      }

      // Verify stats (9 wins, 3 losses)
      userInfo = await getUser(newUser.id, newUser.accessToken)
      expect(userInfo.user.wins).toBe(9)
      expect(userInfo.user.losses).toBe(3)

      // Check ranking
      rankingCache.invalidate()
      const ranking = await getRanking(newUser)

      // Should have a rank (12 matches >= 10 minimum)
      expect(ranking.currentUser.rank).toBeNumber()
      expect(ranking.currentUser.totalMatches).toBe(12)
      expect(ranking.currentUser.winRate).toBe(75) // 9/12 = 75%
    })

    test("login flow preserves user data", async () => {
      const user = await createUser("LoginTest")

      // Create opponent and play matches
      const opponent = await createUser("LoginOpp")
      await recordMatch(user, opponent, 3, 1)
      await recordMatch(user, opponent, 2, 3)

      // Login with refresh token
      const loginRes = await app.request("/api/users/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          userId: user.id,
          refreshToken: user.refreshToken,
        }),
      })

      expect(loginRes.status).toBe(200)

      const loginBody = (await loginRes.json()) as AuthResponse
      expect(loginBody.user.id).toBe(user.id)
      expect(loginBody.user.name).toBe("LoginTest")
      expect(loginBody.user.wins).toBe(1)
      expect(loginBody.user.losses).toBe(1)

      // Can use new token
      const newToken = loginBody.accessToken
      const userInfo = await getUser(user.id, newToken)
      expect(userInfo.user.id).toBe(user.id)
    })

    test("token refresh maintains session", async () => {
      const user = await createUser("RefreshTest")

      // Refresh token
      const refreshRes = await app.request("/api/auth/refresh", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken: user.refreshToken }),
      })

      expect(refreshRes.status).toBe(200)

      const refreshBody = (await refreshRes.json()) as {
        accessToken: string
        refreshToken: string
      }

      // New tokens should be different
      expect(refreshBody.accessToken).not.toBe(user.accessToken)
      expect(refreshBody.refreshToken).not.toBe(user.refreshToken)

      // Can use new access token
      const userInfo = await getUser(user.id, refreshBody.accessToken)
      expect(userInfo.user.id).toBe(user.id)
    })
  })
})
