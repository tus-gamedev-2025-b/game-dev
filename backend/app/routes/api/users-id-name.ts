import { createRoute, OpenAPIHono, z } from "@hono/zod-openapi"
import type { User } from "../../domain/user/entity.ts"
import { authMiddleware } from "../../helpers/auth/middleware.ts"
import { UpdateNameRequestSchema } from "../../schemas/auth.ts"
import {
  createErrorResponse,
  ErrorCode,
  type ErrorCodeType,
  ErrorResponseSchema,
} from "../../schemas/error.ts"
import { UserSchema } from "../../schemas/user.ts"
import { updateName } from "../../usecases/user/update-name.ts"

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

// PATCH /api/users/:id/name - Update user name
const updateNameRoute = createRoute({
  method: "patch",
  path: "/{id}/name",
  tags: ["Users"],
  summary: "Update user name",
  description: "Update the name of a user",
  security: [{ Bearer: [] }],
  request: {
    params: ParamsSchema,
    body: {
      content: {
        "application/json": {
          schema: UpdateNameRequestSchema.openapi("UpdateNameRequest"),
        },
      },
    },
  },
  responses: {
    200: {
      description: "User name updated successfully",
      content: {
        "application/json": {
          schema: UserResponseSchema.openapi("UserResponse"),
        },
      },
    },
    400: {
      description: "Validation error or invalid name",
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
    403: {
      description: "Forbidden - cannot update other user's name",
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

app.use("/:id/name", authMiddleware)

app.openapi(updateNameRoute, async (c) => {
  const { id } = c.req.valid("param")
  const targetId = Number(id)
  const requesterId = c.get("userId")
  const { name } = c.req.valid("json")

  if (Number.isNaN(targetId)) {
    return c.json(createErrorResponse(ErrorCode.VALIDATION_ERROR), 400)
  }

  const result = await updateName(targetId, requesterId, name)

  if (!result.success) {
    if (result.code === "FORBIDDEN") {
      return c.json(createErrorResponse(result.code), 403)
    }
    if (result.code === "USER_NOT_FOUND") {
      return c.json(createErrorResponse(result.code), 404)
    }
    return c.json(createErrorResponse(result.code as ErrorCodeType), 400)
  }

  return c.json({ user: toUserResponse(result.user) }, 200)
})

export default app
