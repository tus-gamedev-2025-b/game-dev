import type { Match, MatchResult } from "../../domain/match/entity.ts"
import { validateMatchRequest } from "../../domain/match/validator.ts"
import {
  matchRepository,
  userRepository,
} from "../../infra/repositories/index.ts"

export type RecordMatchResult =
  | {
      success: true
      match: Match
      updatedStats: {
        wins: number
        losses: number
        totalMatches: number
      }
    }
  | {
      success: false
      code: "VISITOR_NOT_FOUND" | "SELF_MATCH_NOT_ALLOWED"
    }

const determineWinner = (
  homeUserId: number,
  result: MatchResult,
): { winnerId: number; loserId: number } => {
  if (result.homeScore > result.visitorScore) {
    return { winnerId: homeUserId, loserId: result.visitorId }
  } else {
    return { winnerId: result.visitorId, loserId: homeUserId }
  }
}

export const recordMatch = async (
  homeUserId: number,
  result: MatchResult,
): Promise<RecordMatchResult> => {
  // 自己対戦チェック
  const validation = validateMatchRequest(homeUserId, result.visitorId)
  if (!validation.success) {
    return { success: false, code: validation.code }
  }

  // 対戦相手の存在確認
  const visitor = await userRepository.findById(result.visitorId)
  if (!visitor) {
    return { success: false, code: "VISITOR_NOT_FOUND" }
  }

  // 勝敗判定
  const { winnerId, loserId } = determineWinner(homeUserId, result)

  // 対戦記録作成
  const match = await matchRepository.create(winnerId, loserId)

  // 戦績更新（アトミック操作で競合状態を防止）
  await Promise.all([
    userRepository.incrementWins(winnerId),
    userRepository.incrementLosses(loserId),
  ])

  // 更新後のhomeユーザー情報を取得
  const updatedHome = await userRepository.findById(homeUserId)

  return {
    success: true,
    match,
    updatedStats: {
      wins: updatedHome?.wins ?? 0,
      losses: updatedHome?.losses ?? 0,
      totalMatches: (updatedHome?.wins ?? 0) + (updatedHome?.losses ?? 0),
    },
  }
}
