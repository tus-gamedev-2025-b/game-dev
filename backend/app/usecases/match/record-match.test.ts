import { describe, expect, test } from "bun:test"
import { initializeDatabase } from "../../libs/db/client.ts"
import { createUser } from "../user/create-user.ts"
import { getUser } from "../user/get-user.ts"
import { recordMatch } from "./record-match.ts"

// Initialize database tables
initializeDatabase()

describe("recordMatch usecase", () => {
  test("records match with home user winning", async () => {
    const home = await createUser()
    const visitor = await createUser()

    const result = await recordMatch(home.user.id, {
      visitorId: visitor.user.id,
      visitorScore: 1,
      homeScore: 3,
    })

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.match.winnerId).toBe(home.user.id)
      expect(result.match.loserId).toBe(visitor.user.id)
      expect(result.updatedStats.wins).toBe(1)
      expect(result.updatedStats.losses).toBe(0)
      expect(result.updatedStats.totalMatches).toBe(1)
    }
  })

  test("records match with visitor winning", async () => {
    const home = await createUser()
    const visitor = await createUser()

    const result = await recordMatch(home.user.id, {
      visitorId: visitor.user.id,
      visitorScore: 5,
      homeScore: 2,
    })

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.match.winnerId).toBe(visitor.user.id)
      expect(result.match.loserId).toBe(home.user.id)
      expect(result.updatedStats.wins).toBe(0)
      expect(result.updatedStats.losses).toBe(1)
    }
  })

  test("updates both users stats", async () => {
    const home = await createUser()
    const visitor = await createUser()

    await recordMatch(home.user.id, {
      visitorId: visitor.user.id,
      visitorScore: 1,
      homeScore: 3,
    })

    const homeResult = await getUser(home.user.id)
    const visitorResult = await getUser(visitor.user.id)

    expect(homeResult.success).toBe(true)
    expect(visitorResult.success).toBe(true)

    if (homeResult.success && visitorResult.success) {
      expect(homeResult.user.wins).toBe(1)
      expect(homeResult.user.losses).toBe(0)
      expect(visitorResult.user.wins).toBe(0)
      expect(visitorResult.user.losses).toBe(1)
    }
  })

  test("accumulates stats over multiple matches", async () => {
    const home = await createUser()
    const visitor = await createUser()

    // Home wins
    await recordMatch(home.user.id, {
      visitorId: visitor.user.id,
      visitorScore: 1,
      homeScore: 3,
    })

    // Visitor wins
    await recordMatch(home.user.id, {
      visitorId: visitor.user.id,
      visitorScore: 5,
      homeScore: 2,
    })

    // Home wins again
    const result = await recordMatch(home.user.id, {
      visitorId: visitor.user.id,
      visitorScore: 0,
      homeScore: 1,
    })

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.updatedStats.wins).toBe(2)
      expect(result.updatedStats.losses).toBe(1)
      expect(result.updatedStats.totalMatches).toBe(3)
    }
  })

  test("fails with self match", async () => {
    const home = await createUser()

    const result = await recordMatch(home.user.id, {
      visitorId: home.user.id,
      visitorScore: 1,
      homeScore: 3,
    })

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.code).toBe("SELF_MATCH_NOT_ALLOWED")
    }
  })

  test("fails with non-existent visitor", async () => {
    const home = await createUser()

    const result = await recordMatch(home.user.id, {
      visitorId: 99999,
      visitorScore: 1,
      homeScore: 3,
    })

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.code).toBe("VISITOR_NOT_FOUND")
    }
  })

  test("handles tie-breaker (visitor wins on equal score)", async () => {
    const home = await createUser()
    const visitor = await createUser()

    // Equal score - visitor wins by default (homeScore > visitorScore check fails)
    const result = await recordMatch(home.user.id, {
      visitorId: visitor.user.id,
      visitorScore: 3,
      homeScore: 3,
    })

    expect(result.success).toBe(true)
    if (result.success) {
      // Since homeScore is NOT > visitorScore, visitor wins
      expect(result.match.winnerId).toBe(visitor.user.id)
      expect(result.match.loserId).toBe(home.user.id)
    }
  })
})
