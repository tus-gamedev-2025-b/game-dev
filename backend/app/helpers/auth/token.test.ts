import { describe, expect, test } from "bun:test"
import { config } from "../../config.ts"
import { generateTokenPair, isTokenExpired } from "./token.ts"

describe("generateTokenPair", () => {
  test("generates unique access and refresh tokens", () => {
    const tokenPair = generateTokenPair()

    expect(tokenPair.accessToken).toBeString()
    expect(tokenPair.refreshToken).toBeString()
    expect(tokenPair.accessToken).not.toBe(tokenPair.refreshToken)
  })

  test("generates UUID format tokens", () => {
    const tokenPair = generateTokenPair()
    const uuidRegex =
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

    expect(tokenPair.accessToken).toMatch(uuidRegex)
    expect(tokenPair.refreshToken).toMatch(uuidRegex)
  })

  test("generates different tokens on each call", () => {
    const tokenPair1 = generateTokenPair()
    const tokenPair2 = generateTokenPair()

    expect(tokenPair1.accessToken).not.toBe(tokenPair2.accessToken)
    expect(tokenPair1.refreshToken).not.toBe(tokenPair2.refreshToken)
  })

  test("sets access token expiry based on config", () => {
    const before = Date.now()
    const tokenPair = generateTokenPair()
    const after = Date.now()

    const expectedMinExpiry = before + config.auth.accessTokenExpiresIn
    const expectedMaxExpiry = after + config.auth.accessTokenExpiresIn

    expect(tokenPair.accessTokenExpiresAt.getTime()).toBeGreaterThanOrEqual(
      expectedMinExpiry,
    )
    expect(tokenPair.accessTokenExpiresAt.getTime()).toBeLessThanOrEqual(
      expectedMaxExpiry,
    )
  })

  test("sets refresh token expiry based on config", () => {
    const before = Date.now()
    const tokenPair = generateTokenPair()
    const after = Date.now()

    const expectedMinExpiry = before + config.auth.refreshTokenExpiresIn
    const expectedMaxExpiry = after + config.auth.refreshTokenExpiresIn

    expect(tokenPair.refreshTokenExpiresAt.getTime()).toBeGreaterThanOrEqual(
      expectedMinExpiry,
    )
    expect(tokenPair.refreshTokenExpiresAt.getTime()).toBeLessThanOrEqual(
      expectedMaxExpiry,
    )
  })

  test("refresh token expires later than access token", () => {
    const tokenPair = generateTokenPair()

    expect(tokenPair.refreshTokenExpiresAt.getTime()).toBeGreaterThan(
      tokenPair.accessTokenExpiresAt.getTime(),
    )
  })
})

describe("isTokenExpired", () => {
  test("returns false for future date", () => {
    const futureDate = new Date(Date.now() + 60 * 60 * 1000) // 1 hour from now

    expect(isTokenExpired(futureDate)).toBe(false)
  })

  test("returns true for past date", () => {
    const pastDate = new Date(Date.now() - 1000) // 1 second ago

    expect(isTokenExpired(pastDate)).toBe(true)
  })

  test("returns true for current time (edge case)", () => {
    // Use a date slightly in the past to ensure it's expired
    const now = new Date(Date.now() - 1)

    expect(isTokenExpired(now)).toBe(true)
  })

  test("handles dates far in the future", () => {
    const farFuture = new Date(Date.now() + 365 * 24 * 60 * 60 * 1000) // 1 year

    expect(isTokenExpired(farFuture)).toBe(false)
  })

  test("handles dates far in the past", () => {
    const farPast = new Date(Date.now() - 365 * 24 * 60 * 60 * 1000) // 1 year ago

    expect(isTokenExpired(farPast)).toBe(true)
  })
})
