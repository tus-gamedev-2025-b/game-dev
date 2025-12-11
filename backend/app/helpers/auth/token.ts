import { config } from "../../config.ts"
import type { TokenPair } from "../../domain/user/entity.ts"

export const generateTokenPair = (): TokenPair => {
  const now = Date.now()

  return {
    accessToken: crypto.randomUUID(),
    refreshToken: crypto.randomUUID(),
    accessTokenExpiresAt: new Date(now + config.auth.accessTokenExpiresIn),
    refreshTokenExpiresAt: new Date(now + config.auth.refreshTokenExpiresIn),
  }
}

export const isTokenExpired = (expiresAt: Date): boolean => {
  return new Date() > expiresAt
}
