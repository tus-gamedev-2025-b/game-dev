import { beforeAll, beforeEach, describe, expect, test } from "bun:test"
import { db, initializeDatabase } from "../../libs/db/client.ts"
import { matches, users } from "../../libs/db/schema.ts"
import { createMatchRepository } from "./match.ts"
import { createUserRepository } from "./user.ts"

beforeAll(() => {
  initializeDatabase()
})

beforeEach(async () => {
  await db.delete(matches)
  await db.delete(users)
})

describe("MatchRepository", () => {
  const userRepository = createUserRepository(db)
  const matchRepository = createMatchRepository(db)

  describe("create", () => {
    test("creates a match with winner and loser", async () => {
      const winner = await userRepository.create("Winner")
      const loser = await userRepository.create("Loser")

      const match = await matchRepository.create(winner.id, loser.id)

      expect(match.id).toBeNumber()
      expect(match.winnerId).toBe(winner.id)
      expect(match.loserId).toBe(loser.id)
      expect(match.playedAt).toBeInstanceOf(Date)
    })

    test("creates multiple matches between same users", async () => {
      const user1 = await userRepository.create("User1")
      const user2 = await userRepository.create("User2")

      const match1 = await matchRepository.create(user1.id, user2.id)
      const match2 = await matchRepository.create(user2.id, user1.id)
      const match3 = await matchRepository.create(user1.id, user2.id)

      expect(match1.id).not.toBe(match2.id)
      expect(match2.id).not.toBe(match3.id)
      expect(match1.winnerId).toBe(user1.id)
      expect(match2.winnerId).toBe(user2.id)
      expect(match3.winnerId).toBe(user1.id)
    })
  })

  describe("findById", () => {
    test("returns match when found", async () => {
      const winner = await userRepository.create("FindWinner")
      const loser = await userRepository.create("FindLoser")
      const created = await matchRepository.create(winner.id, loser.id)

      const found = await matchRepository.findById(created.id)

      expect(found).not.toBeNull()
      expect(found?.id).toBe(created.id)
      expect(found?.winnerId).toBe(winner.id)
      expect(found?.loserId).toBe(loser.id)
    })

    test("returns null when match not found", async () => {
      const found = await matchRepository.findById(99999)

      expect(found).toBeNull()
    })
  })
})
