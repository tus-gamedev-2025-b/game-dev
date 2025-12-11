import { createRoute, OpenAPIHono } from "@hono/zod-openapi"
import type { RankedUser } from "../../domain/ranking/entity.ts"
import { authMiddleware } from "../../helpers/auth/middleware.ts"
import {
  createErrorResponse,
  ErrorCode,
  ErrorResponseSchema,
} from "../../schemas/error.ts"
import { RankingResponseSchema } from "../../schemas/ranking.ts"
import { getRanking } from "../../usecases/ranking/get-ranking.ts"

type Variables = {
  userId: number
}

const app = new OpenAPIHono<{ Variables: Variables }>()

const toRankedUserResponse = (user: RankedUser) => ({
  rank: user.rank,
  userId: user.userId,
  userName: user.userName,
  winRate: user.winRate,
  wins: user.wins,
  losses: user.losses,
  totalMatches: user.totalMatches,
})

// GET /api/rankings - Get rankings
const getRankingsRoute = createRoute({
  method: "get",
  path: "/",
  tags: ["Rankings"],
  summary: "Get rankings",
  description: "Retrieve TOP10 rankings and current user's rank",
  security: [{ Bearer: [] }],
  responses: {
    200: {
      description: "Rankings retrieved successfully",
      content: {
        "application/json": {
          schema: RankingResponseSchema.openapi("RankingResponse"),
        },
      },
    },
    401: {
      description: "Unauthorized",
      content: {
        "application/json": {
          schema: ErrorResponseSchema.openapi("ErrorResponse"),
        },
      },
    },
    404: {
      description: "User not found",
      content: {
        "application/json": {
          schema: ErrorResponseSchema.openapi("ErrorResponse"),
        },
      },
    },
  },
})

app.use("/", authMiddleware)

app.openapi(getRankingsRoute, async (c) => {
  const currentUserId = c.get("userId")

  const result = await getRanking(currentUserId)

  if (!result.success) {
    return c.json(createErrorResponse(ErrorCode.USER_NOT_FOUND), 404)
  }

  return c.json(
    {
      rankings: result.data.rankings.map(toRankedUserResponse),
      currentUser: toRankedUserResponse(result.data.currentUser),
    },
    200,
  )
})

export default app
