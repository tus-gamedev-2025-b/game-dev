import { eq } from "drizzle-orm"
import type { Match } from "../../domain/match/entity.ts"
import type { MatchRepository } from "../../domain/match/repository.ts"
import type { DrizzleDB } from "../../libs/db/client.ts"
import { matches } from "../../libs/db/schema.ts"

type MatchRow = typeof matches.$inferSelect

const toMatchEntity = (row: MatchRow): Match => ({
  id: row.id,
  winnerId: row.winnerId,
  loserId: row.loserId,
  playedAt: new Date(row.playedAt),
})

export type CreateMatchRepository = (db: DrizzleDB) => MatchRepository

export const createMatchRepository: CreateMatchRepository = (
  db: DrizzleDB,
): MatchRepository => ({
  create: async (winnerId: number, loserId: number): Promise<Match> => {
    const now = new Date().toISOString()
    const result = await db
      .insert(matches)
      .values({
        winnerId,
        loserId,
        playedAt: now,
      })
      .returning()
    const created = result[0]
    if (!created) {
      throw new Error("Failed to create match")
    }
    return toMatchEntity(created)
  },

  findById: async (id: number): Promise<Match | null> => {
    const result = await db.select().from(matches).where(eq(matches.id, id))
    return result[0] ? toMatchEntity(result[0]) : null
  },
})
