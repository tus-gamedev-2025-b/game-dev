export type User = {
  id: number
  name: string
  wins: number
  losses: number
  createdAt: Date
  updatedAt: Date
}

export type AuthToken = {
  id: number
  userId: number
  accessToken: string
  refreshToken: string
  accessTokenExpiresAt: Date
  refreshTokenExpiresAt: Date
  createdAt: Date
}

export type TokenPair = {
  accessToken: string
  refreshToken: string
  accessTokenExpiresAt: Date
  refreshTokenExpiresAt: Date
}
