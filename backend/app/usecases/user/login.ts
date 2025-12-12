import type { TokenPair, User } from "../../domain/user/entity.ts"
import { generateTokenPair, isTokenExpired } from "../../helpers/auth/token.ts"
import {
  authTokenRepository,
  userRepository,
} from "../../infra/repositories/index.ts"

export type LoginResult =
  | {
      success: true
      user: User
      tokenPair: TokenPair
    }
  | {
      success: false
      code: "USER_NOT_FOUND" | "INVALID_REFRESH_TOKEN"
    }

export const login = async (
  userId: number,
  refreshToken: string,
): Promise<LoginResult> => {
  // Find user
  const user = await userRepository.findById(userId)
  if (!user) {
    return { success: false, code: "USER_NOT_FOUND" }
  }

  // Verify refresh token
  const authToken = await authTokenRepository.findByUserIdAndRefreshToken(
    userId,
    refreshToken,
  )

  if (!authToken) {
    return { success: false, code: "INVALID_REFRESH_TOKEN" }
  }

  if (isTokenExpired(authToken.refreshTokenExpiresAt)) {
    return { success: false, code: "INVALID_REFRESH_TOKEN" }
  }

  // Delete old tokens
  await authTokenRepository.deleteByUserId(userId)

  // Generate new tokens
  const tokenPair = generateTokenPair()

  // Save new tokens
  await authTokenRepository.create(userId, tokenPair)

  return { success: true, user, tokenPair }
}
