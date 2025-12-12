/**
 * ロビー機能のエンティティ定義
 */

/** スタンプID */
export type StampId = 1 | 2 | 3 | 4 | 5 | 6

/** プレイヤーのロール */
export type PlayerRole = "host" | "guest"

/** プレイヤー情報 */
export interface Player {
  id: number
  name: string
  role: PlayerRole
  ready: boolean
}

/** ルーム */
export interface Room {
  code: string
  host: Player
  guest: Player | null
  createdAt: Date
  expiresAt: Date
}

/** プレイヤー接続情報 */
export interface PlayerConnection {
  userId: number
  userName: string
  roomCode: string | null
}

/** WebSocketメッセージ（クライアント → サーバー） */
export type ClientMessageType =
  | "createRoom"
  | "joinRoom"
  | "leaveRoom"
  | "stamp"
  | "ready"
  | "cancelReady"

export interface ClientMessage {
  type: ClientMessageType
  payload?: unknown
}

export interface JoinRoomPayload {
  roomCode: string
}

export interface StampPayload {
  stampId: StampId
}

/** WebSocketメッセージ（サーバー → クライアント） */
export type ServerMessageType =
  | "roomCreated"
  | "roomJoined"
  | "playerJoined"
  | "playerLeft"
  | "stamp"
  | "playerReady"
  | "playerCancelReady"
  | "matchStart"
  | "error"

export interface ServerMessage {
  type: ServerMessageType
  payload?: unknown
  error?: {
    code: string
    message: string
  }
}

export interface OpponentInfo {
  id: number
  name: string
}

export interface MatchStartPlayer {
  id: number
  name: string
  role: PlayerRole
}

/** エラーコード */
export type LobbyErrorCode =
  | "ROOM_NOT_FOUND"
  | "ROOM_FULL"
  | "ROOM_EXPIRED"
  | "ALREADY_IN_ROOM"
  | "NOT_IN_ROOM"
  | "INVALID_STAMP_ID"
  | "UNAUTHORIZED"
