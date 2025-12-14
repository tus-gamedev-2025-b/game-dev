import {
  afterAll,
  beforeAll,
  beforeEach,
  describe,
  expect,
  test,
} from "bun:test"
import { config } from "../../app/config.ts"
import { resetLobbyRepositories } from "../../app/domain/lobby/adapters.ts"
import {
  createAuthTokenRepository,
  createUserRepository,
} from "../../app/infra/repositories/user.ts"
import { db, initializeDatabase } from "../../app/libs/db/client.ts"
import { authTokens, users } from "../../app/libs/db/schema.ts"
import server from "../../index.ts"

interface ServerMessage {
  type: string
  payload?: unknown
  error?: { code: string; message: string }
}

const PORT = 3456
const WS_URL = `ws://localhost:${PORT}${config.pvp.websocket.path}`

let bunServer: ReturnType<typeof Bun.serve>
let userRepository: ReturnType<typeof createUserRepository>
let authTokenRepository: ReturnType<typeof createAuthTokenRepository>

// テスト用ユーザー作成
async function createTestUser(name: string) {
  const user = await userRepository.create(name)
  const tokenPair = {
    accessToken: `test_access_${user.id}_${Date.now()}`,
    refreshToken: `test_refresh_${user.id}_${Date.now()}`,
    accessTokenExpiresAt: new Date(Date.now() + 60 * 60 * 1000),
    refreshTokenExpiresAt: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000),
  }
  await authTokenRepository.create(user.id, tokenPair)
  return { user, token: tokenPair.accessToken }
}

// WebSocket接続ヘルパー
function connectWs(token: string): Promise<WebSocket> {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(`${WS_URL}?token=${token}`)
    ws.onopen = () => resolve(ws)
    ws.onerror = (e) => reject(e)
  })
}

// メッセージ受信ヘルパー
function waitForMessage(ws: WebSocket, timeout = 1000): Promise<ServerMessage> {
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => {
      reject(new Error("Timeout waiting for message"))
    }, timeout)

    const handler = (event: MessageEvent) => {
      clearTimeout(timer)
      ws.removeEventListener("message", handler)
      resolve(JSON.parse(event.data))
    }

    ws.addEventListener("message", handler)
  })
}

// メッセージ送信ヘルパー
function send(ws: WebSocket, type: string, payload?: unknown) {
  ws.send(JSON.stringify({ type, payload }))
}

beforeAll(async () => {
  initializeDatabase()
  userRepository = createUserRepository(db)
  authTokenRepository = createAuthTokenRepository(db)

  bunServer = Bun.serve({
    port: PORT,
    fetch: server.fetch,
    websocket: server.websocket,
  })
})

afterAll(() => {
  bunServer.stop()
})

beforeEach(async () => {
  await db.delete(authTokens)
  await db.delete(users)
  resetLobbyRepositories()
})

describe("WebSocket Lobby Integration", () => {
  describe("認証", () => {
    test("有効なトークンで接続できる", async () => {
      const { token } = await createTestUser("TestUser")
      const ws = await connectWs(token)
      expect(ws.readyState).toBe(WebSocket.OPEN)
      ws.close()
    })

    test("無効なトークンで接続できない", async () => {
      const ws = new WebSocket(`${WS_URL}?token=invalid_token`)

      await new Promise<void>((resolve) => {
        ws.onerror = () => resolve()
        ws.onclose = () => resolve()
      })

      expect(ws.readyState).toBe(WebSocket.CLOSED)
    })

    test("トークンなしで接続できない", async () => {
      const ws = new WebSocket(WS_URL)

      await new Promise<void>((resolve) => {
        ws.onerror = () => resolve()
        ws.onclose = () => resolve()
      })

      expect(ws.readyState).toBe(WebSocket.CLOSED)
    })
  })

  describe("ルーム作成", () => {
    test("ルームを作成できる", async () => {
      const { token } = await createTestUser("Host")
      const ws = await connectWs(token)

      send(ws, "createRoom")
      const response = await waitForMessage(ws)

      expect(response.type).toBe("roomCreated")
      expect((response.payload as { roomCode: string }).roomCode).toHaveLength(
        6,
      )

      ws.close()
    })

    test("既にルームにいる場合は作成できない", async () => {
      const { token } = await createTestUser("Host")
      const ws = await connectWs(token)

      send(ws, "createRoom")
      await waitForMessage(ws)

      send(ws, "createRoom")
      const response = await waitForMessage(ws)

      expect(response.type).toBe("error")
      expect(response.error?.code).toBe("ALREADY_IN_ROOM")

      ws.close()
    })
  })

  describe("ルーム参加", () => {
    test("ルームに参加できる", async () => {
      const { token: hostToken } = await createTestUser("Host")
      const { token: guestToken } = await createTestUser("Guest")

      const hostWs = await connectWs(hostToken)
      const guestWs = await connectWs(guestToken)

      // ホストがルーム作成
      send(hostWs, "createRoom")
      const createResponse = await waitForMessage(hostWs)
      const roomCode = (createResponse.payload as { roomCode: string }).roomCode

      // ゲストがルーム参加
      send(guestWs, "joinRoom", { roomCode })

      // ゲストは roomJoined を受信
      const joinResponse = await waitForMessage(guestWs)
      expect(joinResponse.type).toBe("roomJoined")
      expect(
        (joinResponse.payload as { opponent: { name: string } }).opponent.name,
      ).toBe("Host")

      // ホストは playerJoined を受信
      const joinNotify = await waitForMessage(hostWs)
      expect(joinNotify.type).toBe("playerJoined")
      expect(
        (joinNotify.payload as { opponent: { name: string } }).opponent.name,
      ).toBe("Guest")

      hostWs.close()
      guestWs.close()
    })

    test("存在しないルームには参加できない", async () => {
      const { token } = await createTestUser("Guest")
      const ws = await connectWs(token)

      send(ws, "joinRoom", { roomCode: "NOTFND" })
      const response = await waitForMessage(ws)

      expect(response.type).toBe("error")
      expect(response.error?.code).toBe("ROOM_NOT_FOUND")

      ws.close()
    })
  })

  describe("スタンプ", () => {
    test("スタンプを送信できる", async () => {
      const { token: hostToken } = await createTestUser("Host")
      const { token: guestToken } = await createTestUser("Guest")

      const hostWs = await connectWs(hostToken)
      const guestWs = await connectWs(guestToken)

      // ルーム作成・参加
      send(hostWs, "createRoom")
      const createResponse = await waitForMessage(hostWs)
      const roomCode = (createResponse.payload as { roomCode: string }).roomCode

      send(guestWs, "joinRoom", { roomCode })
      await waitForMessage(guestWs) // roomJoined
      await waitForMessage(hostWs) // playerJoined

      // ホストがスタンプ送信
      send(hostWs, "stamp", { stampId: 1 })

      // ゲストがスタンプを受信
      const stampMsg = await waitForMessage(guestWs)
      expect(stampMsg.type).toBe("stamp")
      expect((stampMsg.payload as { stampId: number }).stampId).toBe(1)

      hostWs.close()
      guestWs.close()
    })

    test("無効なスタンプIDはエラー", async () => {
      const { token: hostToken } = await createTestUser("Host")
      const { token: guestToken } = await createTestUser("Guest")

      const hostWs = await connectWs(hostToken)
      const guestWs = await connectWs(guestToken)

      // ルーム作成・参加
      send(hostWs, "createRoom")
      const createResponse = await waitForMessage(hostWs)
      const roomCode = (createResponse.payload as { roomCode: string }).roomCode

      send(guestWs, "joinRoom", { roomCode })
      await waitForMessage(guestWs)
      await waitForMessage(hostWs)

      // 無効なスタンプ
      send(hostWs, "stamp", { stampId: 99 })
      const response = await waitForMessage(hostWs)

      expect(response.type).toBe("error")
      expect(response.error?.code).toBe("INVALID_STAMP_ID")

      hostWs.close()
      guestWs.close()
    })
  })

  describe("準備完了", () => {
    test("準備完了を通知できる", async () => {
      const { token: hostToken } = await createTestUser("Host")
      const { token: guestToken } = await createTestUser("Guest")

      const hostWs = await connectWs(hostToken)
      const guestWs = await connectWs(guestToken)

      // ルーム作成・参加
      send(hostWs, "createRoom")
      const createResponse = await waitForMessage(hostWs)
      const roomCode = (createResponse.payload as { roomCode: string }).roomCode

      send(guestWs, "joinRoom", { roomCode })
      await waitForMessage(guestWs)
      await waitForMessage(hostWs)

      // ホストが準備完了
      send(hostWs, "ready")

      // ゲストが通知を受信
      const readyMsg = await waitForMessage(guestWs)
      expect(readyMsg.type).toBe("playerReady")

      hostWs.close()
      guestWs.close()
    })

    test("双方準備完了でマッチスタート", async () => {
      const { token: hostToken } = await createTestUser("Host")
      const { token: guestToken } = await createTestUser("Guest")

      const hostWs = await connectWs(hostToken)
      const guestWs = await connectWs(guestToken)

      // ルーム作成・参加
      send(hostWs, "createRoom")
      const createResponse = await waitForMessage(hostWs)
      const roomCode = (createResponse.payload as { roomCode: string }).roomCode

      send(guestWs, "joinRoom", { roomCode })
      await waitForMessage(guestWs)
      await waitForMessage(hostWs)

      // ホストが準備完了
      send(hostWs, "ready")
      await waitForMessage(guestWs) // playerReady

      // ゲストが準備完了
      send(guestWs, "ready")
      await waitForMessage(hostWs) // playerReady

      // 両方がマッチスタートを受信
      const hostStart = await waitForMessage(hostWs)
      const guestStart = await waitForMessage(guestWs)

      expect(hostStart.type).toBe("matchStart")
      expect(guestStart.type).toBe("matchStart")
      expect(
        (hostStart.payload as { players: unknown[] }).players,
      ).toHaveLength(2)

      hostWs.close()
      guestWs.close()
    })

    test("準備完了を取り消せる", async () => {
      const { token: hostToken } = await createTestUser("Host")
      const { token: guestToken } = await createTestUser("Guest")

      const hostWs = await connectWs(hostToken)
      const guestWs = await connectWs(guestToken)

      // ルーム作成・参加
      send(hostWs, "createRoom")
      const createResponse = await waitForMessage(hostWs)
      const roomCode = (createResponse.payload as { roomCode: string }).roomCode

      send(guestWs, "joinRoom", { roomCode })
      await waitForMessage(guestWs)
      await waitForMessage(hostWs)

      // ホストが準備完了
      send(hostWs, "ready")
      await waitForMessage(guestWs)

      // ホストが準備取り消し
      send(hostWs, "cancelReady")

      const cancelMsg = await waitForMessage(guestWs)
      expect(cancelMsg.type).toBe("playerCancelReady")

      hostWs.close()
      guestWs.close()
    })
  })

  describe("ルーム退出", () => {
    test("ルームから退出できる", async () => {
      const { token: hostToken } = await createTestUser("Host")
      const { token: guestToken } = await createTestUser("Guest")

      const hostWs = await connectWs(hostToken)
      const guestWs = await connectWs(guestToken)

      // ルーム作成・参加
      send(hostWs, "createRoom")
      const createResponse = await waitForMessage(hostWs)
      const roomCode = (createResponse.payload as { roomCode: string }).roomCode

      send(guestWs, "joinRoom", { roomCode })
      await waitForMessage(guestWs)
      await waitForMessage(hostWs)

      // ゲストが退出
      send(guestWs, "leaveRoom")

      // ホストが通知を受信
      const leaveMsg = await waitForMessage(hostWs)
      expect(leaveMsg.type).toBe("playerLeft")

      hostWs.close()
      guestWs.close()
    })

    test("接続切断時にルームから退出", async () => {
      const { token: hostToken } = await createTestUser("Host")
      const { token: guestToken } = await createTestUser("Guest")

      const hostWs = await connectWs(hostToken)
      const guestWs = await connectWs(guestToken)

      // ルーム作成・参加
      send(hostWs, "createRoom")
      const createResponse = await waitForMessage(hostWs)
      const roomCode = (createResponse.payload as { roomCode: string }).roomCode

      send(guestWs, "joinRoom", { roomCode })
      await waitForMessage(guestWs)
      await waitForMessage(hostWs)

      // ゲストが切断
      guestWs.close()

      // ホストが通知を受信
      const leaveMsg = await waitForMessage(hostWs, 2000)
      expect(leaveMsg.type).toBe("playerLeft")

      hostWs.close()
    })
  })
})
