import { createRoute, OpenAPIHono } from "@hono/zod-openapi"
import type { TokenPair } from "../../domain/user/entity.ts"
import {
  RefreshTokenRequestSchema,
  TokenResponseSchema,
} from "../../schemas/auth.ts"
import {
  createErrorResponse,
  ErrorCode,
  ErrorResponseSchema,
} from "../../schemas/error.ts"
import { refreshToken } from "../../usecases/user/refresh-token.ts"

const app = new OpenAPIHono()

const toTokenResponse = (tokenPair: TokenPair) => ({
  accessToken: tokenPair.accessToken,
  refreshToken: tokenPair.refreshToken,
  accessTokenExpiresAt: tokenPair.accessTokenExpiresAt.toISOString(),
  refreshTokenExpiresAt: tokenPair.refreshTokenExpiresAt.toISOString(),
})

// POST /api/auth/refresh - Refresh tokens
const refreshTokenRoute = createRoute({
  method: "post",
  path: "/",
  tags: ["Auth"],
  summary: "Refresh access token",
  description: "Exchange a refresh token for new access and refresh tokens",
  request: {
    body: {
      content: {
        "application/json": {
          schema: RefreshTokenRequestSchema.openapi("RefreshTokenRequest"),
        },
      },
    },
  },
  responses: {
    200: {
      description: "Tokens refreshed successfully",
      content: {
        "application/json": {
          schema: TokenResponseSchema.openapi("TokenResponse"),
        },
      },
    },
    401: {
      description: "Invalid or expired refresh token",
      content: {
        "application/json": {
          schema: ErrorResponseSchema.openapi("ErrorResponse"),
        },
      },
    },
  },
})

app.openapi(refreshTokenRoute, async (c) => {
  const { refreshToken: refreshTokenValue } = c.req.valid("json")

  const result = await refreshToken(refreshTokenValue)

  if (!result.success) {
    return c.json(createErrorResponse(ErrorCode.INVALID_REFRESH_TOKEN), 401)
  }

  return c.json(toTokenResponse(result.tokenPair), 200)
})

export default app
