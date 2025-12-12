import { z } from "zod"

export const ErrorResponseSchema = z.object({
  error: z.object({
    code: z.string(),
    message: z.string(),
  }),
})

export type ErrorResponse = z.infer<typeof ErrorResponseSchema>

export const ErrorCode = {
  // 認証系
  UNAUTHORIZED: "UNAUTHORIZED",
  TOKEN_EXPIRED: "TOKEN_EXPIRED",
  FORBIDDEN: "FORBIDDEN",
  INVALID_REFRESH_TOKEN: "INVALID_REFRESH_TOKEN",

  // ユーザー系
  NOT_FOUND: "NOT_FOUND",
  USER_NOT_FOUND: "USER_NOT_FOUND",
  INVALID_NAME_LENGTH: "INVALID_NAME_LENGTH",
  INVALID_NAME_CHARACTERS: "INVALID_NAME_CHARACTERS",

  // 対戦系
  VISITOR_NOT_FOUND: "VISITOR_NOT_FOUND",
  SELF_MATCH_NOT_ALLOWED: "SELF_MATCH_NOT_ALLOWED",

  // 共通
  VALIDATION_ERROR: "VALIDATION_ERROR",
  INTERNAL_ERROR: "INTERNAL_ERROR",
} as const

export type ErrorCodeType = (typeof ErrorCode)[keyof typeof ErrorCode]

export const errorMessages: Record<ErrorCodeType, string> = {
  [ErrorCode.UNAUTHORIZED]: "Unauthorized",
  [ErrorCode.TOKEN_EXPIRED]: "Access token has expired",
  [ErrorCode.FORBIDDEN]: "Access forbidden",
  [ErrorCode.INVALID_REFRESH_TOKEN]: "Invalid or expired refresh token",
  [ErrorCode.NOT_FOUND]: "Resource not found",
  [ErrorCode.USER_NOT_FOUND]: "User not found",
  [ErrorCode.INVALID_NAME_LENGTH]: "Name must be between 3 and 15 characters",
  [ErrorCode.INVALID_NAME_CHARACTERS]: "Name contains invalid characters",
  [ErrorCode.VISITOR_NOT_FOUND]: "Visitor user not found",
  [ErrorCode.SELF_MATCH_NOT_ALLOWED]: "Cannot play against yourself",
  [ErrorCode.VALIDATION_ERROR]: "Validation error",
  [ErrorCode.INTERNAL_ERROR]: "Internal server error",
}

export const createErrorResponse = (code: ErrorCodeType): ErrorResponse => ({
  error: {
    code,
    message: errorMessages[code],
  },
})
