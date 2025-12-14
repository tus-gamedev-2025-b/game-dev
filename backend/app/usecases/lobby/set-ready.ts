/**
 * 準備完了設定ユースケース
 */
import type {
  LobbyErrorCode,
  MatchStartPlayer,
  Room,
} from "../../domain/lobby/entity.ts"
import type { RoomRepository } from "../../domain/lobby/repository.ts"

export interface SetReadyInput {
  userId: number
  ready: boolean
}

export interface SetReadyResult {
  success: true
  room: Room
  opponentUserId: number | null
  matchStart: boolean
  players?: MatchStartPlayer[]
}

export interface SetReadyError {
  success: false
  error: LobbyErrorCode
}

export function setReady(
  roomRepo: RoomRepository,
  input: SetReadyInput,
): SetReadyResult | SetReadyError {
  // ユーザーが所属しているルームを取得
  const room = roomRepo.findByUserId(input.userId)
  if (!room) {
    return { success: false, error: "NOT_IN_ROOM" }
  }

  const result = roomRepo.setReady(room.code, input.userId, input.ready)
  if (!result.success) {
    return result
  }

  const updatedRoom = result.data

  // 相手のユーザーIDを取得
  const opponentUserId =
    updatedRoom.host.id === input.userId
      ? (updatedRoom.guest?.id ?? null)
      : updatedRoom.host.id

  // 双方が準備完了かチェック
  const guest = updatedRoom.guest
  const matchStart = updatedRoom.host.ready && guest !== null && guest.ready

  const players: MatchStartPlayer[] | undefined =
    matchStart && guest
      ? [
          {
            id: updatedRoom.host.id,
            name: updatedRoom.host.name,
            role: "host",
          },
          {
            id: guest.id,
            name: guest.name,
            role: "guest",
          },
        ]
      : undefined

  return {
    success: true,
    room: updatedRoom,
    opponentUserId,
    matchStart,
    players,
  }
}
