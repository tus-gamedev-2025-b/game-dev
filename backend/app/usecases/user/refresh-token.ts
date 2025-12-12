import type { TokenPair } from "../../domain/user/entity.ts"
import { generateTokenPair, isTokenExpired } from "../../helpers/auth/token.ts"
import { authTokenRepository } from "../../infra/repositories/index.ts"

export type RefreshTokenResult =
  | {
      success: true
      tokenPair: TokenPair
    }
  | {
      success: false
      code: "INVALID_REFRESH_TOKEN"
    }

export const refreshToken = async (
  refreshTokenValue: string,
): Promise<RefreshTokenResult> => {
  // Find token
  const authToken =
    await authTokenRepository.findByRefreshToken(refreshTokenValue)

  if (!authToken) {
    return { success: false, code: "INVALID_REFRESH_TOKEN" }
  }

  if (isTokenExpired(authToken.refreshTokenExpiresAt)) {
    return { success: false, code: "INVALID_REFRESH_TOKEN" }
  }

  // Delete old tokens
  await authTokenRepository.deleteByUserId(authToken.userId)

  // Generate new tokens
  const tokenPair = generateTokenPair()

  // Save new tokens
  await authTokenRepository.create(authToken.userId, tokenPair)

  return { success: true, tokenPair }
}
