import { describe, expect, test } from "bun:test"
import { initializeDatabase } from "../../libs/db/client.ts"
import { createUser } from "./create-user.ts"
import { getUser } from "./get-user.ts"
import { updateName } from "./update-name.ts"

// Initialize database tables
initializeDatabase()

describe("updateName usecase", () => {
  test("updates own name successfully", async () => {
    const { user } = await createUser()

    const result = await updateName(user.id, user.id, "NewName")

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.user.name).toBe("NewName")
      expect(result.user.id).toBe(user.id)
    }
  })

  test("persists name change", async () => {
    const { user } = await createUser()

    await updateName(user.id, user.id, "ChangedName")

    const getResult = await getUser(user.id)
    expect(getResult.success).toBe(true)
    if (getResult.success) {
      expect(getResult.user.name).toBe("ChangedName")
    }
  })

  test("updates updatedAt timestamp", async () => {
    const { user } = await createUser()
    const originalUpdatedAt = user.updatedAt

    // Wait a bit to ensure timestamp difference
    await new Promise((resolve) => setTimeout(resolve, 10))

    const result = await updateName(user.id, user.id, "NewName")

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.user.updatedAt.getTime()).toBeGreaterThan(
        originalUpdatedAt.getTime(),
      )
    }
  })

  test("fails when updating other users name", async () => {
    const user1 = await createUser()
    const user2 = await createUser()

    const result = await updateName(user1.user.id, user2.user.id, "HackedName")

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.code).toBe("FORBIDDEN")
    }
  })

  test("original name unchanged when forbidden", async () => {
    const user1 = await createUser()
    const user2 = await createUser()

    await updateName(user1.user.id, user2.user.id, "HackedName")

    const getResult = await getUser(user1.user.id)
    expect(getResult.success).toBe(true)
    if (getResult.success) {
      expect(getResult.user.name).toBe("NoName")
    }
  })

  test("fails with name too short", async () => {
    const { user } = await createUser()

    const result = await updateName(user.id, user.id, "ab")

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.code).toBe("INVALID_NAME_LENGTH")
    }
  })

  test("fails with name too long", async () => {
    const { user } = await createUser()

    const result = await updateName(user.id, user.id, "a".repeat(16))

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.code).toBe("INVALID_NAME_LENGTH")
    }
  })

  test("fails with invalid characters", async () => {
    const { user } = await createUser()

    const result = await updateName(user.id, user.id, "Name@123")

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.code).toBe("INVALID_NAME_CHARACTERS")
    }
  })

  test("accepts Japanese name", async () => {
    const { user } = await createUser()

    const result = await updateName(user.id, user.id, "田中太郎")

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.user.name).toBe("田中太郎")
    }
  })

  test("accepts mixed Japanese and English", async () => {
    const { user } = await createUser()

    const result = await updateName(user.id, user.id, "Player田中")

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.user.name).toBe("Player田中")
    }
  })

  test("fails for non-existent user", async () => {
    const result = await updateName(99999, 99999, "NewName")

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.code).toBe("USER_NOT_FOUND")
    }
  })

  test("can update name multiple times", async () => {
    const { user } = await createUser()

    const result1 = await updateName(user.id, user.id, "Name1")
    expect(result1.success).toBe(true)

    const result2 = await updateName(user.id, user.id, "Name2")
    expect(result2.success).toBe(true)

    const result3 = await updateName(user.id, user.id, "Name3")
    expect(result3.success).toBe(true)

    if (result3.success) {
      expect(result3.user.name).toBe("Name3")
    }
  })
})
