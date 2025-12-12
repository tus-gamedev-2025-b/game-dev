/**
 * ルーム退出ユースケース
 */
import type { LobbyErrorCode } from "../../domain/lobby/entity.ts"
import type {
  ConnectionRepository,
  RoomRepository,
} from "../../domain/lobby/repository.ts"

export interface LeaveRoomInput {
  userId: number
}

export interface LeaveRoomResult {
  success: true
  roomCode: string
  roomDeleted: boolean
  opponentUserId: number | null
}

export interface LeaveRoomError {
  success: false
  error: LobbyErrorCode
}

export function leaveRoom(
  roomRepo: RoomRepository,
  connectionRepo: ConnectionRepository,
  ws: unknown,
  input: LeaveRoomInput,
): LeaveRoomResult | LeaveRoomError {
  // ユーザーが所属しているルームを取得
  const room = roomRepo.findByUserId(input.userId)
  if (!room) {
    return { success: false, error: "NOT_IN_ROOM" }
  }

  // 相手のユーザーIDを取得
  const opponentUserId =
    room.host.id === input.userId ? (room.guest?.id ?? null) : room.host.id

  const result = roomRepo.removePlayer(room.code, input.userId)
  if (!result.success) {
    return result
  }

  // 接続情報からルームコードを削除
  connectionRepo.setRoomCode(ws, null)

  return {
    success: true,
    roomCode: room.code,
    roomDeleted: result.data === null,
    opponentUserId,
  }
}
