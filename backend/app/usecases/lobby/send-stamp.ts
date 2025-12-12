/**
 * スタンプ送信ユースケース
 */
import type { LobbyErrorCode, StampId } from "../../domain/lobby/entity.ts"
import type { RoomRepository } from "../../domain/lobby/repository.ts"
import { isValidStampId } from "../../domain/lobby/repository.ts"

export interface SendStampInput {
  userId: number
  stampId: number
}

export interface SendStampResult {
  success: true
  roomCode: string
  stampId: StampId
  opponentUserId: number
}

export interface SendStampError {
  success: false
  error: LobbyErrorCode
}

export function sendStamp(
  roomRepo: RoomRepository,
  input: SendStampInput,
): SendStampResult | SendStampError {
  // スタンプIDの検証
  if (!isValidStampId(input.stampId)) {
    return { success: false, error: "INVALID_STAMP_ID" }
  }

  // ユーザーが所属しているルームを取得
  const room = roomRepo.findByUserId(input.userId)
  if (!room) {
    return { success: false, error: "NOT_IN_ROOM" }
  }

  // 相手のユーザーIDを取得
  const opponentUserId =
    room.host.id === input.userId ? room.guest?.id : room.host.id

  if (opponentUserId === undefined) {
    // 相手がいない（ルームにゲストがいない）
    return { success: false, error: "NOT_IN_ROOM" }
  }

  return {
    success: true,
    roomCode: room.code,
    stampId: input.stampId,
    opponentUserId,
  }
}
