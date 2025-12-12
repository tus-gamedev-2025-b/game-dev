import { sql } from "drizzle-orm"
import { config } from "../../config.ts"
import type { RankedUser } from "../../domain/ranking/entity.ts"
import type { RankingRepository } from "../../domain/ranking/repository.ts"
import type { DrizzleDB } from "../../libs/db/client.ts"

type RankedUserRow = {
  user_id: number
  user_name: string
  wins: number
  losses: number
  total_matches: number
  win_rate: number
  rank: number
}

const toRankedUser = (row: RankedUserRow): RankedUser => ({
  rank: row.rank,
  userId: row.user_id,
  userName: row.user_name,
  winRate: row.win_rate,
  wins: row.wins,
  losses: row.losses,
  totalMatches: row.total_matches,
})

export type CreateRankingRepository = (db: DrizzleDB) => RankingRepository

export const createRankingRepository: CreateRankingRepository = (
  db: DrizzleDB,
): RankingRepository => ({
  getTop10: async (): Promise<RankedUser[]> => {
    const minMatches = config.ranking.minMatchesForRanking
    const topCount = config.ranking.topRanksCount

    const result = await db.all<RankedUserRow>(sql`
      WITH ranked_users AS (
        SELECT
          id AS user_id,
          name AS user_name,
          wins,
          losses,
          (wins + losses) AS total_matches,
          CASE
            WHEN (wins + losses) = 0 THEN 0.0
            ELSE ROUND(CAST(wins AS REAL) / (wins + losses) * 100, 2)
          END AS win_rate,
          ROW_NUMBER() OVER (
            ORDER BY
              CAST(wins AS REAL) / NULLIF(wins + losses, 0) DESC,
              wins DESC,
              id ASC
          ) AS rank
        FROM users
        WHERE (wins + losses) >= ${minMatches}
      )
      SELECT * FROM ranked_users WHERE rank <= ${topCount}
    `)

    return result.map(toRankedUser)
  },

  getUserRank: async (userId: number): Promise<RankedUser | null> => {
    const minMatches = config.ranking.minMatchesForRanking

    // まずユーザーが10戦以上しているか確認
    const qualifiedResult = await db.all<RankedUserRow>(sql`
      WITH ranked_users AS (
        SELECT
          id AS user_id,
          name AS user_name,
          wins,
          losses,
          (wins + losses) AS total_matches,
          CASE
            WHEN (wins + losses) = 0 THEN 0.0
            ELSE ROUND(CAST(wins AS REAL) / (wins + losses) * 100, 2)
          END AS win_rate,
          ROW_NUMBER() OVER (
            ORDER BY
              CAST(wins AS REAL) / NULLIF(wins + losses, 0) DESC,
              wins DESC,
              id ASC
          ) AS rank
        FROM users
        WHERE (wins + losses) >= ${minMatches}
      )
      SELECT * FROM ranked_users WHERE user_id = ${userId}
    `)

    const qualifiedUser = qualifiedResult[0]
    if (qualifiedUser) {
      return toRankedUser(qualifiedUser)
    }

    // 10戦未満のユーザー情報を取得（圏外）
    const unqualifiedResult = await db.all<{
      user_id: number
      user_name: string
      wins: number
      losses: number
      total_matches: number
      win_rate: number
    }>(sql`
      SELECT
        id AS user_id,
        name AS user_name,
        wins,
        losses,
        (wins + losses) AS total_matches,
        CASE
          WHEN (wins + losses) = 0 THEN 0.0
          ELSE ROUND(CAST(wins AS REAL) / (wins + losses) * 100, 2)
        END AS win_rate
      FROM users
      WHERE id = ${userId}
    `)

    const row = unqualifiedResult[0]
    if (!row) {
      return null
    }

    return {
      rank: null, // 圏外
      userId: row.user_id,
      userName: row.user_name,
      winRate: row.win_rate,
      wins: row.wins,
      losses: row.losses,
      totalMatches: row.total_matches,
    }
  },
})
