/**
 * ロビー機能のインメモリ実装
 */
import { config } from "../../config.ts"
import type { Player, PlayerConnection, Room } from "./entity.ts"
import type {
  ConnectionRepository,
  Result,
  RoomRepository,
} from "./repository.ts"

/** ルームコードを生成 */
function generateRoomCode(): string {
  const chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
  let code = ""
  for (let i = 0; i < config.pvp.room.codeLength; i++) {
    code += chars.charAt(Math.floor(Math.random() * chars.length))
  }
  return code
}

/** インメモリルームリポジトリ */
export class InMemoryRoomRepository implements RoomRepository {
  private rooms = new Map<string, Room>()

  create(host: Player): Room {
    let code: string
    do {
      code = generateRoomCode()
    } while (this.rooms.has(code))

    const now = new Date()
    const room: Room = {
      code,
      host,
      guest: null,
      createdAt: now,
      expiresAt: new Date(now.getTime() + config.pvp.room.expiresIn),
    }

    this.rooms.set(code, room)
    return room
  }

  findByCode(code: string): Room | null {
    const room = this.rooms.get(code)
    if (!room) return null

    if (new Date() > room.expiresAt) {
      this.rooms.delete(code)
      return null
    }

    return room
  }

  addGuest(code: string, guest: Player): Result<Room> {
    const room = this.findByCode(code)
    if (!room) {
      return { success: false, error: "ROOM_NOT_FOUND" }
    }

    if (room.guest !== null) {
      return { success: false, error: "ROOM_FULL" }
    }

    room.guest = guest
    return { success: true, data: room }
  }

  removePlayer(code: string, userId: number): Result<Room | null> {
    const room = this.findByCode(code)
    if (!room) {
      return { success: false, error: "ROOM_NOT_FOUND" }
    }

    if (room.host.id === userId) {
      // ホストが退出したらルームを削除
      this.rooms.delete(code)
      return { success: true, data: null }
    }

    if (room.guest?.id === userId) {
      room.guest = null
      return { success: true, data: room }
    }

    return { success: false, error: "NOT_IN_ROOM" }
  }

  setReady(code: string, userId: number, ready: boolean): Result<Room> {
    const room = this.findByCode(code)
    if (!room) {
      return { success: false, error: "ROOM_NOT_FOUND" }
    }

    if (room.host.id === userId) {
      room.host.ready = ready
      return { success: true, data: room }
    }

    if (room.guest?.id === userId) {
      room.guest.ready = ready
      return { success: true, data: room }
    }

    return { success: false, error: "NOT_IN_ROOM" }
  }

  delete(code: string): boolean {
    return this.rooms.delete(code)
  }

  cleanupExpired(): number {
    const now = new Date()
    let count = 0

    for (const [code, room] of this.rooms) {
      if (now > room.expiresAt) {
        this.rooms.delete(code)
        count++
      }
    }

    return count
  }

  findByUserId(userId: number): Room | null {
    for (const room of this.rooms.values()) {
      if (room.host.id === userId || room.guest?.id === userId) {
        return room
      }
    }
    return null
  }
}

/** インメモリ接続リポジトリ */
export class InMemoryConnectionRepository implements ConnectionRepository {
  private connections = new Map<unknown, PlayerConnection>()
  private userIdToWs = new Map<number, unknown>()

  add(ws: unknown, connection: PlayerConnection): void {
    // 既存の接続があれば削除
    const existing = this.userIdToWs.get(connection.userId)
    if (existing) {
      this.connections.delete(existing)
    }

    this.connections.set(ws, connection)
    this.userIdToWs.set(connection.userId, ws)
  }

  remove(ws: unknown): PlayerConnection | null {
    const connection = this.connections.get(ws)
    if (!connection) return null

    this.connections.delete(ws)
    this.userIdToWs.delete(connection.userId)
    return connection
  }

  getByWs(ws: unknown): PlayerConnection | null {
    return this.connections.get(ws) ?? null
  }

  getByUserId(
    userId: number,
  ): { ws: unknown; connection: PlayerConnection } | null {
    const ws = this.userIdToWs.get(userId)
    if (!ws) return null

    const connection = this.connections.get(ws)
    if (!connection) return null

    return { ws, connection }
  }

  getByRoomCode(
    roomCode: string,
  ): Array<{ ws: unknown; connection: PlayerConnection }> {
    const result: Array<{ ws: unknown; connection: PlayerConnection }> = []

    for (const [ws, connection] of this.connections) {
      if (connection.roomCode === roomCode) {
        result.push({ ws, connection })
      }
    }

    return result
  }

  setRoomCode(ws: unknown, roomCode: string | null): boolean {
    const connection = this.connections.get(ws)
    if (!connection) return false

    connection.roomCode = roomCode
    return true
  }
}

/** シングルトンインスタンス */
let roomRepositoryInstance: InMemoryRoomRepository | null = null
let connectionRepositoryInstance: InMemoryConnectionRepository | null = null

export function getRoomRepository(): RoomRepository {
  if (!roomRepositoryInstance) {
    roomRepositoryInstance = new InMemoryRoomRepository()
  }
  return roomRepositoryInstance
}

export function getConnectionRepository(): ConnectionRepository {
  if (!connectionRepositoryInstance) {
    connectionRepositoryInstance = new InMemoryConnectionRepository()
  }
  return connectionRepositoryInstance
}

/** テスト用：リポジトリをリセット */
export function resetLobbyRepositories(): void {
  roomRepositoryInstance = new InMemoryRoomRepository()
  connectionRepositoryInstance = new InMemoryConnectionRepository()
}
