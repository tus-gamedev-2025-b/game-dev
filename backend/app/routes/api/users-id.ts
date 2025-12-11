import { createRoute, OpenAPIHono, z } from "@hono/zod-openapi"
import type { User } from "../../domain/user/entity.ts"
import { authMiddleware } from "../../helpers/auth/middleware.ts"
import {
  createErrorResponse,
  ErrorCode,
  ErrorResponseSchema,
} from "../../schemas/error.ts"
import { UserSchema } from "../../schemas/user.ts"
import { getUser } from "../../usecases/user/get-user.ts"

type Variables = {
  userId: number
}

const app = new OpenAPIHono<{ Variables: Variables }>()

const toUserResponse = (user: User) => ({
  id: user.id,
  name: user.name,
  wins: user.wins,
  losses: user.losses,
  createdAt: user.createdAt.toISOString(),
  updatedAt: user.updatedAt.toISOString(),
})

const UserResponseSchema = z.object({
  user: UserSchema,
})

const ParamsSchema = z.object({
  id: z.string().openapi({
    param: {
      name: "id",
      in: "path",
    },
    example: "1",
  }),
})

// GET /api/users/:id - Get user info
const getUserRoute = createRoute({
  method: "get",
  path: "/{id}",
  tags: ["Users"],
  summary: "Get user information",
  description: "Retrieve user information by user ID",
  security: [{ Bearer: [] }],
  request: {
    params: ParamsSchema,
  },
  responses: {
    200: {
      description: "User information retrieved successfully",
      content: {
        "application/json": {
          schema: UserResponseSchema.openapi("UserResponse"),
        },
      },
    },
    400: {
      description: "Validation error",
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
      description: "User not found",
      content: {
        "application/json": {
          schema: ErrorResponseSchema.openapi("ErrorResponse"),
        },
      },
    },
  },
})

app.use("/:id", authMiddleware)

app.openapi(getUserRoute, async (c) => {
  const { id } = c.req.valid("param")
  const numericId = Number(id)

  if (Number.isNaN(numericId)) {
    return c.json(createErrorResponse(ErrorCode.VALIDATION_ERROR), 400)
  }

  const result = await getUser(numericId)

  if (!result.success) {
    return c.json(createErrorResponse(ErrorCode.USER_NOT_FOUND), 404)
  }

  return c.json({ user: toUserResponse(result.user) }, 200)
})

export default app
