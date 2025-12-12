/**
 * WebSocketサーバー設定
 */
import type { ServerWebSocket } from "bun"
import { getConnectionRepository } from "../../domain/lobby/adapters.ts"
import { authenticateWsToken } from "../../helpers/auth/ws-auth.ts"
import { handleClose, handleMessage, type WsData } from "./handler.ts"

export type { WsData }

/** URLからトークンを取得 */
function getTokenFromUrl(url: string): string | null {
  try {
    const parsedUrl = new URL(url)
    return parsedUrl.searchParams.get("token")
  } catch {
    return null
  }
}

/** WebSocketアップグレードハンドラー */
export async function handleUpgrade(
  req: Request,
  server: Bun.Server<WsData>,
): Promise<Response | undefined> {
  // トークンを取得
  const token = getTokenFromUrl(req.url)
  if (!token) {
    return new Response("Unauthorized: Missing token", { status: 401 })
  }

  // トークンを検証
  const authResult = await authenticateWsToken(token)
  if (!authResult.success) {
    return new Response(`Unauthorized: ${authResult.message}`, { status: 401 })
  }

  // WebSocketにアップグレード
  const success = server.upgrade(req, {
    data: {
      userId: authResult.userId,
      userName: authResult.userName,
    } satisfies WsData,
  })

  if (success) {
    return undefined // アップグレード成功
  }

  return new Response("Failed to upgrade to WebSocket", { status: 500 })
}

/** WebSocketハンドラー定義 */
export const websocketHandlers = {
  open(ws: ServerWebSocket<WsData>) {
    const connectionRepo = getConnectionRepository()
    connectionRepo.add(ws, {
      userId: ws.data.userId,
      userName: ws.data.userName,
      roomCode: null,
    })
    console.log(`WebSocket connected: userId=${ws.data.userId}`)
  },

  message(ws: ServerWebSocket<WsData>, message: string | Buffer) {
    const messageStr =
      typeof message === "string" ? message : message.toString()
    handleMessage(ws, messageStr)
  },

  close(ws: ServerWebSocket<WsData>) {
    console.log(`WebSocket disconnected: userId=${ws.data.userId}`)
    handleClose(ws)
  },
}
