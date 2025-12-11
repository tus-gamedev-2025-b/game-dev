import { describe, expect, test } from "bun:test"
import { validateMatchRequest } from "./validator.ts"

describe("validateMatchRequest", () => {
  test("accepts valid match between different users", () => {
    const result = validateMatchRequest(1, 2)
    expect(result.success).toBe(true)
  })

  test("rejects self match", () => {
    const result = validateMatchRequest(1, 1)
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.code).toBe("SELF_MATCH_NOT_ALLOWED")
    }
  })

  test("accepts match with large user IDs", () => {
    const result = validateMatchRequest(99999, 88888)
    expect(result.success).toBe(true)
  })
})
