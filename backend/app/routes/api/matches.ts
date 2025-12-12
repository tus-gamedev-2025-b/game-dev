import { createRoute, OpenAPIHono } from "@hono/zod-openapi"
import type { Match } from "../../domain/match/entity.ts"
import { authMiddleware } from "../../helpers/auth/middleware.ts"
import {
  createErrorResponse,
  type ErrorCodeType,
  ErrorResponseSchema,
} from "../../schemas/error.ts"
import {
  MatchResponseSchema,
  RecordMatchRequestSchema,
} from "../../schemas/match.ts"
import { recordMatch } from "../../usecases/match/record-match.ts"

type Variables = {
  userId: number
}

const app = new OpenAPIHono<{ Variables: Variables }>()

const toMatchResponse = (
  match: Match,
  updatedStats: { wins: number; losses: number; totalMatches: number },
) => ({
  match: {
    id: match.id,
    winnerId: match.winnerId,
    loserId: match.loserId,
    playedAt: match.playedAt.toISOString(),
  },
  updatedStats,
})

// POST /api/matches - Record match result
const recordMatchRoute = createRoute({
  method: "post",
  path: "/",
  tags: ["Matches"],
  summary: "Record match result",
  description: "Record the result of a match and update player statistics",
  security: [{ Bearer: [] }],
  request: {
    body: {
      content: {
        "application/json": {
          schema: RecordMatchRequestSchema.openapi("RecordMatchRequest"),
        },
      },
    },
  },
  responses: {
    201: {
      description: "Match recorded successfully",
      content: {
        "application/json": {
          schema: MatchResponseSchema.openapi("MatchResponse"),
        },
      },
    },
    400: {
      description: "Validation error or self-match not allowed",
      content: {
        "application/json": {
          schema: ErrorResponseSchema.openapi("ErrorResponse"),
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
      description: "Visitor user not found",
      content: {
        "application/json": {
          schema: ErrorResponseSchema.openapi("ErrorResponse"),
        },
      },
    },
  },
})

app.use("/", authMiddleware)

app.openapi(recordMatchRoute, async (c) => {
  const homeUserId = c.get("userId")
  const { visitorId, visitorScore, homeScore } = c.req.valid("json")

  const result = await recordMatch(homeUserId, {
    visitorId,
    visitorScore,
    homeScore,
  })

  if (!result.success) {
    if (result.code === "VISITOR_NOT_FOUND") {
      return c.json(createErrorResponse(result.code), 404)
    }
    return c.json(createErrorResponse(result.code as ErrorCodeType), 400)
  }

  return c.json(toMatchResponse(result.match, result.updatedStats), 201)
})

export default app
