import { describe, expect, test } from "bun:test"
import { initializeDatabase } from "../../libs/db/client.ts"
import { createUser } from "./create-user.ts"
import { getUser } from "./get-user.ts"

// Initialize database tables
initializeDatabase()

describe("getUser usecase", () => {
  test("returns user by ID", async () => {
    const { user: created } = await createUser()

    const result = await getUser(created.id)

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.user.id).toBe(created.id)
      expect(result.user.name).toBe(created.name)
      expect(result.user.wins).toBe(0)
      expect(result.user.losses).toBe(0)
    }
  })

  test("returns USER_NOT_FOUND for non-existent user", async () => {
    const result = await getUser(99999)

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.code).toBe("USER_NOT_FOUND")
    }
  })

  test("returns USER_NOT_FOUND for negative ID", async () => {
    const result = await getUser(-1)

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.code).toBe("USER_NOT_FOUND")
    }
  })

  test("returns USER_NOT_FOUND for zero ID", async () => {
    const result = await getUser(0)

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.code).toBe("USER_NOT_FOUND")
    }
  })

  test("can get multiple different users", async () => {
    const user1 = await createUser()
    const user2 = await createUser()
    const user3 = await createUser()

    const result1 = await getUser(user1.user.id)
    const result2 = await getUser(user2.user.id)
    const result3 = await getUser(user3.user.id)

    expect(result1.success).toBe(true)
    expect(result2.success).toBe(true)
    expect(result3.success).toBe(true)

    if (result1.success && result2.success && result3.success) {
      expect(result1.user.id).toBe(user1.user.id)
      expect(result2.user.id).toBe(user2.user.id)
      expect(result3.user.id).toBe(user3.user.id)
    }
  })
})
