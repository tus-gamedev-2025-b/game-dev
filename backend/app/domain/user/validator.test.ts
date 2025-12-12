import { describe, expect, test } from "bun:test"
import { validateUserName } from "./validator.ts"

describe("validateUserName", () => {
  describe("valid names", () => {
    test("accepts 3 character name", () => {
      const result = validateUserName("abc")
      expect(result.success).toBe(true)
    })

    test("accepts 15 character name", () => {
      const result = validateUserName("abcdefghijklmno")
      expect(result.success).toBe(true)
    })

    test("accepts Japanese hiragana", () => {
      const result = validateUserName("たなか")
      expect(result.success).toBe(true)
    })

    test("accepts Japanese katakana", () => {
      const result = validateUserName("タナカ")
      expect(result.success).toBe(true)
    })

    test("accepts katakana with prolonged sound mark", () => {
      const result = validateUserName("ユーザー")
      expect(result.success).toBe(true)
    })

    test("accepts katakana with middle dot", () => {
      const result = validateUserName("タナカ・タロウ")
      expect(result.success).toBe(true)
    })

    test("accepts Japanese kanji", () => {
      const result = validateUserName("田中太郎")
      expect(result.success).toBe(true)
    })

    test("accepts mixed Japanese and English", () => {
      const result = validateUserName("田中Taro")
      expect(result.success).toBe(true)
    })

    test("accepts spaces", () => {
      const result = validateUserName("John Doe")
      expect(result.success).toBe(true)
    })

    test("accepts full-width space", () => {
      const result = validateUserName("田中　太郎")
      expect(result.success).toBe(true)
    })

    test("accepts full-width alphanumeric", () => {
      const result = validateUserName("ＡＢＣ１２３")
      expect(result.success).toBe(true)
    })
  })

  describe("invalid name length", () => {
    test("rejects empty string", () => {
      const result = validateUserName("")
      expect(result.success).toBe(false)
      if (!result.success) {
        expect(result.code).toBe("INVALID_NAME_LENGTH")
      }
    })

    test("rejects 2 character name", () => {
      const result = validateUserName("ab")
      expect(result.success).toBe(false)
      if (!result.success) {
        expect(result.code).toBe("INVALID_NAME_LENGTH")
      }
    })

    test("rejects 16 character name", () => {
      const result = validateUserName("abcdefghijklmnop")
      expect(result.success).toBe(false)
      if (!result.success) {
        expect(result.code).toBe("INVALID_NAME_LENGTH")
      }
    })

    test("counts Unicode characters correctly", () => {
      // 3 Japanese characters should be valid
      const result = validateUserName("あいう")
      expect(result.success).toBe(true)
    })
  })

  describe("invalid characters", () => {
    test("rejects special characters", () => {
      const result = validateUserName("user@name")
      expect(result.success).toBe(false)
      if (!result.success) {
        expect(result.code).toBe("INVALID_NAME_CHARACTERS")
      }
    })

    test("rejects emoji", () => {
      const result = validateUserName("user😀")
      expect(result.success).toBe(false)
      if (!result.success) {
        expect(result.code).toBe("INVALID_NAME_CHARACTERS")
      }
    })

    test("rejects punctuation", () => {
      const result = validateUserName("user.name")
      expect(result.success).toBe(false)
      if (!result.success) {
        expect(result.code).toBe("INVALID_NAME_CHARACTERS")
      }
    })
  })
})
