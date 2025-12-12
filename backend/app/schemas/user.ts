import { z } from "zod"

export const UserSchema = z.object({
  id: z.number(),
  name: z.string(),
  wins: z.number(),
  losses: z.number(),
  createdAt: z.string(),
  updatedAt: z.string(),
})

export type UserResponse = z.infer<typeof UserSchema>
