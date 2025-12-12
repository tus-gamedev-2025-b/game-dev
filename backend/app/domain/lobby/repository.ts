/**
 * ロビー機能のリポジトリインターフェース
 */
import type {
  LobbyErrorCode,
  Player,
  PlayerConnection,
  Room,
  StampId,
} from "./entity.ts"

/** 操作結果 */
export type Result<T> =
  | { success: true; data: T }
  | { success: false; error: LobbyErrorCode }

/** ルームリポジトリインターフェース */
export interface RoomRepository {
  /** ルームを作成 */
  create(host: Player): Room

  /** ルームコードでルームを取得 */
  findByCode(code: string): Room | null

  /** ルームにゲストを追加 */
  addGuest(code: string, guest: Player): Result<Room>

  /** ルームからプレイヤーを削除 */
  removePlayer(code: string, userId: number): Result<Room | null>

  /** プレイヤーの準備状態を更新 */
  setReady(code: string, userId: number, ready: boolean): Result<Room>

  /** ルームを削除 */
  delete(code: string): boolean

  /** 期限切れルームを削除 */
  cleanupExpired(): number

  /** ユーザーが所属しているルームを取得 */
  findByUserId(userId: number): Room | null
}

/** 接続管理リポジトリインターフェース */
export interface ConnectionRepository {
  /** 接続を追加 */
  add(ws: unknown, connection: PlayerConnection): void

  /** 接続を削除 */
  remove(ws: unknown): PlayerConnection | null

  /** WebSocketから接続情報を取得 */
  getByWs(ws: unknown): PlayerConnection | null

  /** ユーザーIDから接続を取得 */
  getByUserId(
    userId: number,
  ): { ws: unknown; connection: PlayerConnection } | null

  /** ルームの全接続を取得 */
  getByRoomCode(
    roomCode: string,
  ): Array<{ ws: unknown; connection: PlayerConnection }>

  /** 接続のルームコードを更新 */
  setRoomCode(ws: unknown, roomCode: string | null): boolean
}

/** スタンプ検証 */
export function isValidStampId(id: number): id is StampId {
  return Number.isInteger(id) && id >= 1 && id <= 6
}
