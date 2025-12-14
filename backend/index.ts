import { config } from "./app/config.ts"
import app from "./app/index.ts"
import {
  handleUpgrade,
  type WsData,
  websocketHandlers,
} from "./app/routes/ws/index.ts"

const port = Number(process.env.PORT) || 3000

console.log(`Starting server on port ${port}...`)

export default {
  port,
  async fetch(req: Request, server: Bun.Server<WsData>) {
    const url = new URL(req.url)

    // WebSocketパスの場合はアップグレード処理
    if (url.pathname === config.pvp.websocket.path) {
      const response = await handleUpgrade(req, server)
      if (response) return response
      // アップグレード成功時はundefinedが返る
      return new Response(null, { status: 101 })
    }

    // 通常のHTTPリクエストはHonoアプリに委譲
    return app.fetch(req, server)
  },
  websocket: websocketHandlers,
}
