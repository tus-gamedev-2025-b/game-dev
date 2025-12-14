import { beforeEach, describe, expect, test } from "bun:test"
import {
  InMemoryConnectionRepository,
  InMemoryRoomRepository,
} from "../../domain/lobby/adapters.ts"
import { createRoom } from "./create-room.ts"

describe("createRoom", () => {
  let roomRepo: InMemoryRoomRepository
  let connRepo: InMemoryConnectionRepository

  beforeEach(() => {
    roomRepo = new InMemoryRoomRepository()
    connRepo = new InMemoryConnectionRepository()
  })

  const mockWs = { id: 1 }

  test("creates room successfully", () => {
    connRepo.add(mockWs, { userId: 1, userName: "User1", roomCode: null })

    const result = createRoom(roomRepo, connRepo, mockWs, {
      userId: 1,
      userName: "User1",
    })

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.room.code).toHaveLength(6)
      expect(result.room.host.id).toBe(1)
      expect(result.room.host.name).toBe("User1")
      expect(result.room.host.role).toBe("host")
      expect(result.room.guest).toBeNull()
    }
  })

  test("sets room code on connection", () => {
    connRepo.add(mockWs, { userId: 1, userName: "User1", roomCode: null })

    const result = createRoom(roomRepo, connRepo, mockWs, {
      userId: 1,
      userName: "User1",
    })

    expect(result.success).toBe(true)
    if (result.success) {
      const conn = connRepo.getByWs(mockWs)
      expect(conn?.roomCode).toBe(result.room.code)
    }
  })

  test("fails when already in a room", () => {
    connRepo.add(mockWs, { userId: 1, userName: "User1", roomCode: null })

    // Create first room
    createRoom(roomRepo, connRepo, mockWs, {
      userId: 1,
      userName: "User1",
    })

    // Try to create second room
    const mockWs2 = { id: 2 }
    connRepo.add(mockWs2, { userId: 1, userName: "User1", roomCode: null })
    const result = createRoom(roomRepo, connRepo, mockWs2, {
      userId: 1,
      userName: "User1",
    })

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error).toBe("ALREADY_IN_ROOM")
    }
  })
})
