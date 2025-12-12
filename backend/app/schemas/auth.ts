import { z } from "zod"
import { UserSchema } from "./user.ts"

// ユーザー名バリデーション: 3文字以上15文字以内、記号禁止（マルチバイト文字・スペースは可）
// ドメインバリデータ（validator.ts）と同じルールを適用
const userNameSchema = z
  .string()
  .min(3, "ユーザー名は3文字以上である必要があります")
  .max(15, "ユーザー名は15文字以内である必要があります")
  .regex(
    /^[\p{Script=Hiragana}\p{Script=Katakana}\p{Script=Han}a-zA-Z0-9\uFF21-\uFF3A\uFF41-\uFF5A\uFF10-\uFF19\s\u3000\u30FC\u30FB]+$/u,
    "ユーザー名に記号は使用できません",
  )

export const CreateUserRequestSchema = z.object({
  name: userNameSchema.optional(),
})

export const LoginRequestSchema = z.object({
  userId: z.number().int().positive(),
  refreshToken: z.string().uuid(),
})

export const RefreshTokenRequestSchema = z.object({
  refreshToken: z.string().uuid(),
})

export const AuthResponseSchema = z.object({
  user: UserSchema,
  accessToken: z.string(),
  refreshToken: z.string(),
  accessTokenExpiresAt: z.string(),
  refreshTokenExpiresAt: z.string(),
})

export const TokenResponseSchema = z.object({
  accessToken: z.string(),
  refreshToken: z.string(),
  accessTokenExpiresAt: z.string(),
  refreshTokenExpiresAt: z.string(),
})

export const UpdateNameRequestSchema = z.object({
  name: userNameSchema,
})

export type CreateUserRequest = z.infer<typeof CreateUserRequestSchema>
export type LoginRequest = z.infer<typeof LoginRequestSchema>
export type RefreshTokenRequest = z.infer<typeof RefreshTokenRequestSchema>
export type AuthResponse = z.infer<typeof AuthResponseSchema>
export type TokenResponse = z.infer<typeof TokenResponseSchema>
export type UpdateNameRequest = z.infer<typeof UpdateNameRequestSchema>
