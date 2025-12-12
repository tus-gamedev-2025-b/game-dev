export type RankedUser = {
  rank: number | null // nullは圏外（10戦未満）
  userId: number
  userName: string
  winRate: number // パーセント表記（例: 75.5）
  wins: number
  losses: number
  totalMatches: number
}

export type RankingResponse = {
  rankings: RankedUser[] // TOP10（最大10件）
  currentUser: RankedUser
}
