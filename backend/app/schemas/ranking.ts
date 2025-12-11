import { z } from "zod"

export const RankedUserSchema = z.object({
  rank: z.number().nullable(),
  userId: z.number(),
  userName: z.string(),
  winRate: z.number(),
  wins: z.number(),
  losses: z.number(),
  totalMatches: z.number(),
})

export const RankingResponseSchema = z.object({
  rankings: z.array(RankedUserSchema),
  currentUser: RankedUserSchema,
})

export type RankedUserResponse = z.infer<typeof RankedUserSchema>
export type RankingApiResponse = z.infer<typeof RankingResponseSchema>
