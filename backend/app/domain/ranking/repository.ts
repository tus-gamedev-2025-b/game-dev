import type { RankedUser } from "./entity.ts"

export type RankingRepository = {
  getTop10: () => Promise<RankedUser[]>
  getUserRank: (userId: number) => Promise<RankedUser | null>
}
