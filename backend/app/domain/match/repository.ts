import type { Match } from "./entity.ts"

export type MatchRepository = {
  create: (winnerId: number, loserId: number) => Promise<Match>
  findById: (id: number) => Promise<Match | null>
}
