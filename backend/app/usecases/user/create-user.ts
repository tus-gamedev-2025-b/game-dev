import { config } from "../../config.ts"
import type { TokenPair, User } from "../../domain/user/entity.ts"
import { generateTokenPair } from "../../helpers/auth/token.ts"
import {
  authTokenRepository,
  userRepository,
} from "../../infra/repositories/index.ts"

export type CreateUserResult = {
  user: User
  tokenPair: TokenPair
}

export const createUser = async (name?: string): Promise<CreateUserResult> => {
  // Create user with default name
  const user = await userRepository.create(name ?? config.user.defaultName)

  // Generate tokens
  const tokenPair = generateTokenPair()

  // Save tokens
  await authTokenRepository.create(user.id, tokenPair)

  return { user, tokenPair }
}
