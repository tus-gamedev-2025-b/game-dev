import { and, eq, lt, sql } from "drizzle-orm"
import { config } from "../../config.ts"
import type { AuthToken, TokenPair, User } from "../../domain/user/entity.ts"
import type {
  AuthTokenRepository,
  UserRepository,
} from "../../domain/user/repository.ts"
import type { DrizzleDB } from "../../libs/db/client.ts"
import { authTokens, users } from "../../libs/db/schema.ts"

type UserRow = typeof users.$inferSelect
type AuthTokenRow = typeof authTokens.$inferSelect

const toUserEntity = (row: UserRow): User => ({
  id: row.id,
  name: row.name,
  wins: row.wins,
  losses: row.losses,
  createdAt: new Date(row.createdAt),
  updatedAt: new Date(row.updatedAt),
})

const toAuthTokenEntity = (row: AuthTokenRow): AuthToken => ({
  id: row.id,
  userId: row.userId,
  accessToken: row.accessToken,
  refreshToken: row.refreshToken,
  accessTokenExpiresAt: new Date(row.accessTokenExpiresAt),
  refreshTokenExpiresAt: new Date(row.refreshTokenExpiresAt),
  createdAt: new Date(row.createdAt),
})

export type CreateUserRepository = (db: DrizzleDB) => UserRepository
export type CreateAuthTokenRepository = (db: DrizzleDB) => AuthTokenRepository

export const createUserRepository: CreateUserRepository = (
  db: DrizzleDB,
): UserRepository => ({
  create: async (name: string): Promise<User> => {
    const now = new Date().toISOString()
    const result = await db
      .insert(users)
      .values({
        name: name || config.user.defaultName,
        createdAt: now,
        updatedAt: now,
      })
      .returning()
    const created = result[0]
    if (!created) {
      throw new Error("Failed to create user")
    }
    return toUserEntity(created)
  },

  findById: async (id: number): Promise<User | null> => {
    const result = await db.select().from(users).where(eq(users.id, id))
    return result[0] ? toUserEntity(result[0]) : null
  },

  updateName: async (id: number, name: string): Promise<User | null> => {
    const now = new Date().toISOString()
    const result = await db
      .update(users)
      .set({ name, updatedAt: now })
      .where(eq(users.id, id))
      .returning()
    return result[0] ? toUserEntity(result[0]) : null
  },

  updateStats: async (
    id: number,
    wins: number,
    losses: number,
  ): Promise<User | null> => {
    const now = new Date().toISOString()
    const result = await db
      .update(users)
      .set({ wins, losses, updatedAt: now })
      .where(eq(users.id, id))
      .returning()
    return result[0] ? toUserEntity(result[0]) : null
  },

  incrementWins: async (id: number): Promise<void> => {
    const now = new Date().toISOString()
    await db
      .update(users)
      .set({
        wins: sql`${users.wins} + 1`,
        updatedAt: now,
      })
      .where(eq(users.id, id))
  },

  incrementLosses: async (id: number): Promise<void> => {
    const now = new Date().toISOString()
    await db
      .update(users)
      .set({
        losses: sql`${users.losses} + 1`,
        updatedAt: now,
      })
      .where(eq(users.id, id))
  },
})

export const createAuthTokenRepository: CreateAuthTokenRepository = (
  db: DrizzleDB,
): AuthTokenRepository => ({
  create: async (userId: number, tokenPair: TokenPair): Promise<AuthToken> => {
    const now = new Date().toISOString()
    const result = await db
      .insert(authTokens)
      .values({
        userId,
        accessToken: tokenPair.accessToken,
        refreshToken: tokenPair.refreshToken,
        accessTokenExpiresAt: tokenPair.accessTokenExpiresAt.toISOString(),
        refreshTokenExpiresAt: tokenPair.refreshTokenExpiresAt.toISOString(),
        createdAt: now,
      })
      .returning()
    const created = result[0]
    if (!created) {
      throw new Error("Failed to create auth token")
    }
    return toAuthTokenEntity(created)
  },

  findByAccessToken: async (accessToken: string): Promise<AuthToken | null> => {
    const result = await db
      .select()
      .from(authTokens)
      .where(eq(authTokens.accessToken, accessToken))
    return result[0] ? toAuthTokenEntity(result[0]) : null
  },

  findByRefreshToken: async (
    refreshToken: string,
  ): Promise<AuthToken | null> => {
    const result = await db
      .select()
      .from(authTokens)
      .where(eq(authTokens.refreshToken, refreshToken))
    return result[0] ? toAuthTokenEntity(result[0]) : null
  },

  findByUserIdAndRefreshToken: async (
    userId: number,
    refreshToken: string,
  ): Promise<AuthToken | null> => {
    const result = await db
      .select()
      .from(authTokens)
      .where(
        and(
          eq(authTokens.userId, userId),
          eq(authTokens.refreshToken, refreshToken),
        ),
      )
    return result[0] ? toAuthTokenEntity(result[0]) : null
  },

  deleteByUserId: async (userId: number): Promise<void> => {
    await db.delete(authTokens).where(eq(authTokens.userId, userId))
  },

  deleteExpiredTokens: async (): Promise<void> => {
    const now = new Date().toISOString()
    await db.delete(authTokens).where(lt(authTokens.refreshTokenExpiresAt, now))
  },
})
