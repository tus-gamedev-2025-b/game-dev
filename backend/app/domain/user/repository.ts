import type { AuthToken, TokenPair, User } from "./entity.ts"

export type UserRepository = {
  create: (name: string) => Promise<User>
  findById: (id: number) => Promise<User | null>
  updateName: (id: number, name: string) => Promise<User | null>
  updateStats: (
    id: number,
    wins: number,
    losses: number,
  ) => Promise<User | null>
  /** Atomically increment wins by 1 */
  incrementWins: (id: number) => Promise<void>
  /** Atomically increment losses by 1 */
  incrementLosses: (id: number) => Promise<void>
}

export type AuthTokenRepository = {
  create: (userId: number, tokenPair: TokenPair) => Promise<AuthToken>
  findByAccessToken: (accessToken: string) => Promise<AuthToken | null>
  findByRefreshToken: (refreshToken: string) => Promise<AuthToken | null>
  findByUserIdAndRefreshToken: (
    userId: number,
    refreshToken: string,
  ) => Promise<AuthToken | null>
  deleteByUserId: (userId: number) => Promise<void>
  deleteExpiredTokens: () => Promise<void>
}
