/**
 * WebSocketメッセージハンドラー
 */
import type { ServerWebSocket } from "bun"
import {
  getConnectionRepository,
  getRoomRepository,
} from "../../domain/lobby/adapters.ts"
import type {
  ClientMessage,
  JoinRoomPayload,
  LobbyErrorCode,
  OpponentInfo,
  ServerMessage,
  StampPayload,
} from "../../domain/lobby/entity.ts"
import { createRoom } from "../../usecases/lobby/create-room.ts"
import { joinRoom } from "../../usecases/lobby/join-room.ts"
import { leaveRoom } from "../../usecases/lobby/leave-room.ts"
import { sendStamp } from "../../usecases/lobby/send-stamp.ts"
import { setReady } from "../../usecases/lobby/set-ready.ts"

export interface WsData {
  userId: number
  userName: string
}

/** メッセージを送信 */
function send(ws: ServerWebSocket<WsData>, message: ServerMessage): void {
  ws.send(JSON.stringify(message))
}

/** エラーメッセージを送信 */
function sendError(
  ws: ServerWebSocket<WsData>,
  code: LobbyErrorCode,
  message: string,
): void {
  send(ws, { type: "error", error: { code, message } })
}

/** 相手にメッセージを送信 */
function sendToOpponent(
  opponentUserId: number,
  message: ServerMessage,
): boolean {
  const connectionRepo = getConnectionRepository()
  const opponent = connectionRepo.getByUserId(opponentUserId)
  if (!opponent) return false

  const opponentWs = opponent.ws as ServerWebSocket<WsData>
  send(opponentWs, message)
  return true
}

/** ルーム作成ハンドラー */
function handleCreateRoom(ws: ServerWebSocket<WsData>): void {
  const { userId, userName } = ws.data
  const roomRepo = getRoomRepository()
  const connectionRepo = getConnectionRepository()

  const result = createRoom(roomRepo, connectionRepo, ws, { userId, userName })

  if (!result.success) {
    sendError(ws, result.error, "Already in a room")
    return
  }

  send(ws, {
    type: "roomCreated",
    payload: { roomCode: result.room.code },
  })
}

/** ルーム参加ハンドラー */
function handleJoinRoom(
  ws: ServerWebSocket<WsData>,
  payload: JoinRoomPayload,
): void {
  const { userId, userName } = ws.data
  const roomRepo = getRoomRepository()
  const connectionRepo = getConnectionRepository()

  if (!payload?.roomCode || typeof payload.roomCode !== "string") {
    sendError(ws, "ROOM_NOT_FOUND", "Room code is required")
    return
  }

  const roomCode = payload.roomCode.toUpperCase()
  const result = joinRoom(roomRepo, connectionRepo, ws, {
    userId,
    userName,
    roomCode,
  })

  if (!result.success) {
    const messages: Record<LobbyErrorCode, string> = {
      ROOM_NOT_FOUND: "Room not found",
      ROOM_FULL: "Room is full",
      ROOM_EXPIRED: "Room has expired",
      ALREADY_IN_ROOM: "Already in a room",
      NOT_IN_ROOM: "Not in a room",
      INVALID_STAMP_ID: "Invalid stamp ID",
      UNAUTHORIZED: "Unauthorized",
    }
    sendError(ws, result.error, messages[result.error])
    return
  }

  // 参加者にルーム情報を送信
  send(ws, {
    type: "roomJoined",
    payload: {
      roomCode,
      opponent: result.opponent,
    },
  })

  // ホストに参加通知を送信
  const guestInfo: OpponentInfo = { id: userId, name: userName }
  sendToOpponent(result.opponent.id, {
    type: "playerJoined",
    payload: { opponent: guestInfo },
  })
}

/** ルーム退出ハンドラー */
function handleLeaveRoom(ws: ServerWebSocket<WsData>): void {
  const { userId } = ws.data
  const roomRepo = getRoomRepository()
  const connectionRepo = getConnectionRepository()

  const result = leaveRoom(roomRepo, connectionRepo, ws, { userId })

  if (!result.success) {
    sendError(ws, result.error, "Not in a room")
    return
  }

  // 相手に退出通知を送信
  if (result.opponentUserId !== null) {
    sendToOpponent(result.opponentUserId, { type: "playerLeft" })

    // ホストが退出した場合、相手の接続からもルームコードを削除
    if (result.roomDeleted) {
      const opponent = connectionRepo.getByUserId(result.opponentUserId)
      if (opponent) {
        connectionRepo.setRoomCode(opponent.ws, null)
      }
    }
  }
}

/** スタンプ送信ハンドラー */
function handleStamp(ws: ServerWebSocket<WsData>, payload: StampPayload): void {
  const { userId } = ws.data
  const roomRepo = getRoomRepository()

  if (!payload?.stampId || typeof payload.stampId !== "number") {
    sendError(ws, "INVALID_STAMP_ID", "Stamp ID is required")
    return
  }

  const result = sendStamp(roomRepo, { userId, stampId: payload.stampId })

  if (!result.success) {
    const messages: Record<LobbyErrorCode, string> = {
      ROOM_NOT_FOUND: "Room not found",
      ROOM_FULL: "Room is full",
      ROOM_EXPIRED: "Room has expired",
      ALREADY_IN_ROOM: "Already in a room",
      NOT_IN_ROOM: "Not in a room or no opponent",
      INVALID_STAMP_ID: "Invalid stamp ID (must be 1-6)",
      UNAUTHORIZED: "Unauthorized",
    }
    sendError(ws, result.error, messages[result.error])
    return
  }

  // 相手にスタンプを送信
  sendToOpponent(result.opponentUserId, {
    type: "stamp",
    payload: { playerId: userId, stampId: result.stampId },
  })
}

/** 準備完了ハンドラー */
function handleReady(ws: ServerWebSocket<WsData>): void {
  const { userId } = ws.data
  const roomRepo = getRoomRepository()

  const result = setReady(roomRepo, { userId, ready: true })

  if (!result.success) {
    sendError(ws, result.error, "Not in a room")
    return
  }

  // 相手に準備完了を通知
  if (result.opponentUserId !== null) {
    sendToOpponent(result.opponentUserId, {
      type: "playerReady",
      payload: { playerId: userId },
    })
  }

  // 双方準備完了ならマッチスタート
  if (result.matchStart && result.players) {
    const payload = {
      roomCode: result.room.code,
      players: result.players,
    }

    send(ws, { type: "matchStart", payload })
    if (result.opponentUserId !== null) {
      sendToOpponent(result.opponentUserId, { type: "matchStart", payload })
    }
  }
}

/** 準備取り消しハンドラー */
function handleCancelReady(ws: ServerWebSocket<WsData>): void {
  const { userId } = ws.data
  const roomRepo = getRoomRepository()

  const result = setReady(roomRepo, { userId, ready: false })

  if (!result.success) {
    sendError(ws, result.error, "Not in a room")
    return
  }

  // 相手に準備取り消しを通知
  if (result.opponentUserId !== null) {
    sendToOpponent(result.opponentUserId, {
      type: "playerCancelReady",
      payload: { playerId: userId },
    })
  }
}

/** メッセージハンドラー */
export function handleMessage(
  ws: ServerWebSocket<WsData>,
  message: string,
): void {
  let parsed: ClientMessage
  try {
    parsed = JSON.parse(message)
  } catch {
    sendError(ws, "UNAUTHORIZED", "Invalid JSON")
    return
  }

  switch (parsed.type) {
    case "createRoom":
      handleCreateRoom(ws)
      break
    case "joinRoom":
      handleJoinRoom(ws, parsed.payload as JoinRoomPayload)
      break
    case "leaveRoom":
      handleLeaveRoom(ws)
      break
    case "stamp":
      handleStamp(ws, parsed.payload as StampPayload)
      break
    case "ready":
      handleReady(ws)
      break
    case "cancelReady":
      handleCancelReady(ws)
      break
    default:
      sendError(ws, "UNAUTHORIZED", `Unknown message type: ${parsed.type}`)
  }
}

/** 接続終了ハンドラー */
export function handleClose(ws: ServerWebSocket<WsData>): void {
  const { userId } = ws.data
  const roomRepo = getRoomRepository()
  const connectionRepo = getConnectionRepository()

  // ルームから退出
  const room = roomRepo.findByUserId(userId)
  if (room) {
    const opponentUserId =
      room.host.id === userId ? (room.guest?.id ?? null) : room.host.id

    roomRepo.removePlayer(room.code, userId)

    // 相手に退出通知を送信
    if (opponentUserId !== null) {
      sendToOpponent(opponentUserId, { type: "playerLeft" })

      // ホストが退出した場合、相手の接続からもルームコードを削除
      if (room.host.id === userId) {
        const opponent = connectionRepo.getByUserId(opponentUserId)
        if (opponent) {
          connectionRepo.setRoomCode(opponent.ws, null)
        }
      }
    }
  }

  // 接続を削除
  connectionRepo.remove(ws)
}
