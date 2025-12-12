import { z } from "zod"

export const RecordMatchRequestSchema = z.object({
  visitorId: z.number().int().positive(),
  visitorScore: z.number().int().min(0),
  homeScore: z.number().int().min(0),
})

export const MatchSchema = z.object({
  id: z.number(),
  winnerId: z.number(),
  loserId: z.number(),
  playedAt: z.string(),
})

export const MatchResponseSchema = z.object({
  match: MatchSchema,
  updatedStats: z.object({
    wins: z.number(),
    losses: z.number(),
    totalMatches: z.number(),
  }),
})

export type RecordMatchRequest = z.infer<typeof RecordMatchRequestSchema>
export type MatchResponse = z.infer<typeof MatchResponseSchema>
