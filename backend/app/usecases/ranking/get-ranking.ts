import type { RankingResponse } from "../../domain/ranking/entity.ts"
import { rankingRepository } from "../../infra/repositories/index.ts"
import { rankingCache } from "../../libs/cache/ranking-cache.ts"

export type GetRankingResult =
  | {
      success: true
      data: RankingResponse
    }
  | {
      success: false
      code: "USER_NOT_FOUND"
    }

export const getRanking = async (
  currentUserId: number,
): Promise<GetRankingResult> => {
  // 1. TOP10をキャッシュから取得（または DB から取得してキャッシュ）
  let top10 = rankingCache.get()

  if (!top10) {
    top10 = await rankingRepository.getTop10()
    rankingCache.set(top10)
  }

  // 2. 自分の順位は常にDBから取得（キャッシュしない）
  const currentUser = await rankingRepository.getUserRank(currentUserId)

  if (!currentUser) {
    return { success: false, code: "USER_NOT_FOUND" }
  }

  // 3. 自分がTOP10に含まれていれば、TOP10内の自分の情報を最新に差し替え
  const rankings = top10.map((user) =>
    user.userId === currentUserId ? currentUser : user,
  )

  return {
    success: true,
    data: {
      rankings,
      currentUser,
    },
  }
}
