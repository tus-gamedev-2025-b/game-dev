import { createRoute, OpenAPIHono } from "@hono/zod-openapi"
import type { TokenPair, User } from "../../domain/user/entity.ts"
import {
  AuthResponseSchema,
  CreateUserRequestSchema,
  LoginRequestSchema,
} from "../../schemas/auth.ts"
import {
  createErrorResponse,
  type ErrorCodeType,
  ErrorResponseSchema,
} from "../../schemas/error.ts"
import { createUser } from "../../usecases/user/create-user.ts"
import { login } from "../../usecases/user/login.ts"

const app = new OpenAPIHono()

const toUserResponse = (user: User) => ({
  id: user.id,
  name: user.name,
  wins: user.wins,
  losses: user.losses,
  createdAt: user.createdAt.toISOString(),
  updatedAt: user.updatedAt.toISOString(),
})

const toAuthResponse = (user: User, tokenPair: TokenPair) => ({
  user: toUserResponse(user),
  accessToken: tokenPair.accessToken,
  refreshToken: tokenPair.refreshToken,
  accessTokenExpiresAt: tokenPair.accessTokenExpiresAt.toISOString(),
  refreshTokenExpiresAt: tokenPair.refreshTokenExpiresAt.toISOString(),
})

// POST /api/users - Create new user
const createUserRoute = createRoute({
  method: "post",
  path: "/",
  tags: ["Users"],
  summary: "Create new user",
  description: "Creates a new user and returns authentication tokens",
  request: {
    body: {
      content: {
        "application/json": {
          schema: CreateUserRequestSchema.openapi("CreateUserRequest"),
        },
      },
      required: false,
    },
  },
  responses: {
    201: {
      description: "User created successfully",
      content: {
        "application/json": {
          schema: AuthResponseSchema.openapi("AuthResponse"),
        },
      },
    },
  },
})

app.openapi(createUserRoute, async (c) => {
  const body = c.req.valid("json")
  const result = await createUser(body?.name)
  return c.json(toAuthResponse(result.user, result.tokenPair), 201)
})

// POST /api/users/login - Login
const loginRoute = createRoute({
  method: "post",
  path: "/login",
  tags: ["Users"],
  summary: "User login",
  description: "Authenticate user with userId and refresh token",
  request: {
    body: {
      content: {
        "application/json": {
          schema: LoginRequestSchema.openapi("LoginRequest"),
        },
      },
    },
  },
  responses: {
    200: {
      description: "Login successful",
      content: {
        "application/json": {
          schema: AuthResponseSchema.openapi("AuthResponse"),
        },
      },
    },
    401: {
      description: "Invalid refresh token",
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

app.openapi(loginRoute, async (c) => {
  const { userId, refreshToken } = c.req.valid("json")

  const result = await login(userId, refreshToken)

  if (!result.success) {
    if (result.code === "USER_NOT_FOUND") {
      return c.json(createErrorResponse(result.code), 404)
    }
    return c.json(createErrorResponse(result.code as ErrorCodeType), 401)
  }

  return c.json(toAuthResponse(result.user, result.tokenPair), 200)
})

export default app
