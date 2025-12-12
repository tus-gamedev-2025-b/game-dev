import { createMiddleware } from "hono/factory"
import { HTTPException } from "hono/http-exception"
import { authTokenRepository } from "../../infra/repositories/index.ts"
import { isTokenExpired } from "./token.ts"

type AuthVariables = {
  userId: number
}

export const authMiddleware = createMiddleware<{
  Variables: AuthVariables
}>(async (c, next) => {
  const authHeader = c.req.header("Authorization")

  if (!authHeader || !authHeader.startsWith("Bearer ")) {
    throw new HTTPException(401, {
      message: JSON.stringify({
        error: {
          code: "UNAUTHORIZED",
          message: "Missing or invalid Authorization header",
        },
      }),
    })
  }

  const token = authHeader.slice(7)

  const authToken = await authTokenRepository.findByAccessToken(token)

  if (!authToken) {
    throw new HTTPException(401, {
      message: JSON.stringify({
        error: {
          code: "UNAUTHORIZED",
          message: "Invalid access token",
        },
      }),
    })
  }

  if (isTokenExpired(authToken.accessTokenExpiresAt)) {
    throw new HTTPException(401, {
      message: JSON.stringify({
        error: {
          code: "TOKEN_EXPIRED",
          message: "Access token has expired",
        },
      }),
    })
  }

  c.set("userId", authToken.userId)
  await next()
})
