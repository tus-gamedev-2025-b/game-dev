/**
 * ルーム作成ユースケース
 */
import type { Player, Room } from "../../domain/lobby/entity.ts"
import type {
  ConnectionRepository,
  RoomRepository,
} from "../../domain/lobby/repository.ts"

export interface CreateRoomInput {
  userId: number
  userName: string
}

export interface CreateRoomResult {
  success: true
  room: Room
}

export interface CreateRoomError {
  success: false
  error: "ALREADY_IN_ROOM"
}

export function createRoom(
  roomRepo: RoomRepository,
  connectionRepo: ConnectionRepository,
  ws: unknown,
  input: CreateRoomInput,
): CreateRoomResult | CreateRoomError {
  // 既存のルームに参加していないか確認
  const existingRoom = roomRepo.findByUserId(input.userId)
  if (existingRoom) {
    return { success: false, error: "ALREADY_IN_ROOM" }
  }

  const host: Player = {
    id: input.userId,
    name: input.userName,
    role: "host",
    ready: false,
  }

  const room = roomRepo.create(host)

  // 接続情報にルームコードを設定
  connectionRepo.setRoomCode(ws, room.code)

  return { success: true, room }
}
