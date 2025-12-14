/**
 * WebSocket認証ヘルパー
 */
import {
  authTokenRepository,
  userRepository,
} from "../../infra/repositories/index.ts"
import { isTokenExpired } from "./token.ts"

export interface WsAuthResult {
  success: true
  userId: number
  userName: string
}

export interface WsAuthError {
  success: false
  message: string
}

/**
 * WebSocket接続のトークンを検証する
 */
export async function authenticateWsToken(
  token: string,
): Promise<WsAuthResult | WsAuthError> {
  // トークンを検証
  const authToken = await authTokenRepository.findByAccessToken(token)
  if (!authToken) {
    return { success: false, message: "Invalid token" }
  }

  // トークンの有効期限を確認
  if (isTokenExpired(authToken.accessTokenExpiresAt)) {
    return { success: false, message: "Token expired" }
  }

  // ユーザー情報を取得
  const user = await userRepository.findById(authToken.userId)
  if (!user) {
    return { success: false, message: "User not found" }
  }

  return {
    success: true,
    userId: user.id,
    userName: user.name,
  }
}
