export type Match = {
  id: number
  winnerId: number
  loserId: number
  playedAt: Date
}

export type MatchResult = {
  visitorId: number
  visitorScore: number
  homeScore: number
}
