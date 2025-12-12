/**
 * ルーム参加ユースケース
 */
import type {
  LobbyErrorCode,
  OpponentInfo,
  Player,
  Room,
} from "../../domain/lobby/entity.ts"
import type {
  ConnectionRepository,
  RoomRepository,
} from "../../domain/lobby/repository.ts"

export interface JoinRoomInput {
  userId: number
  userName: string
  roomCode: string
}

export interface JoinRoomResult {
  success: true
  room: Room
  opponent: OpponentInfo
}

export interface JoinRoomError {
  success: false
  error: LobbyErrorCode
}

export function joinRoom(
  roomRepo: RoomRepository,
  connectionRepo: ConnectionRepository,
  ws: unknown,
  input: JoinRoomInput,
): JoinRoomResult | JoinRoomError {
  // 既存のルームに参加していないか確認
  const existingRoom = roomRepo.findByUserId(input.userId)
  if (existingRoom) {
    return { success: false, error: "ALREADY_IN_ROOM" }
  }

  const guest: Player = {
    id: input.userId,
    name: input.userName,
    role: "guest",
    ready: false,
  }

  // ルームコードを大文字に正規化
  const normalizedRoomCode = input.roomCode.toUpperCase()

  const result = roomRepo.addGuest(normalizedRoomCode, guest)
  if (!result.success) {
    return result
  }

  // 接続情報にルームコードを設定
  connectionRepo.setRoomCode(ws, normalizedRoomCode)

  return {
    success: true,
    room: result.data,
    opponent: {
      id: result.data.host.id,
      name: result.data.host.name,
    },
  }
}
