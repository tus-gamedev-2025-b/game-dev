import { beforeEach, describe, expect, test } from "bun:test"
import {
  InMemoryConnectionRepository,
  InMemoryRoomRepository,
} from "../../domain/lobby/adapters.ts"
import type { Player } from "../../domain/lobby/entity.ts"
import { joinRoom } from "./join-room.ts"

describe("joinRoom", () => {
  let roomRepo: InMemoryRoomRepository
  let connRepo: InMemoryConnectionRepository

  beforeEach(() => {
    roomRepo = new InMemoryRoomRepository()
    connRepo = new InMemoryConnectionRepository()
  })

  const guestWs = { id: 2 }

  const createTestRoom = () => {
    const host: Player = {
      id: 1,
      name: "Host",
      role: "host",
      ready: false,
    }
    return roomRepo.create(host)
  }

  test("joins room successfully", () => {
    const room = createTestRoom()
    connRepo.add(guestWs, { userId: 2, userName: "Guest", roomCode: null })

    const result = joinRoom(roomRepo, connRepo, guestWs, {
      userId: 2,
      userName: "Guest",
      roomCode: room.code,
    })

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.room.guest?.id).toBe(2)
      expect(result.room.guest?.name).toBe("Guest")
      expect(result.opponent.id).toBe(1)
      expect(result.opponent.name).toBe("Host")
    }
  })

  test("case insensitive room code", () => {
    const room = createTestRoom()
    connRepo.add(guestWs, { userId: 2, userName: "Guest", roomCode: null })

    const result = joinRoom(roomRepo, connRepo, guestWs, {
      userId: 2,
      userName: "Guest",
      roomCode: room.code.toLowerCase(),
    })

    expect(result.success).toBe(true)
  })

  test("sets room code on connection", () => {
    const room = createTestRoom()
    connRepo.add(guestWs, { userId: 2, userName: "Guest", roomCode: null })

    const result = joinRoom(roomRepo, connRepo, guestWs, {
      userId: 2,
      userName: "Guest",
      roomCode: room.code,
    })

    expect(result.success).toBe(true)
    const conn = connRepo.getByWs(guestWs)
    expect(conn?.roomCode).toBe(room.code)
  })

  test("fails when room not found", () => {
    connRepo.add(guestWs, { userId: 2, userName: "Guest", roomCode: null })

    const result = joinRoom(roomRepo, connRepo, guestWs, {
      userId: 2,
      userName: "Guest",
      roomCode: "NOTFND",
    })

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error).toBe("ROOM_NOT_FOUND")
    }
  })

  test("fails when room is full", () => {
    const room = createTestRoom()
    const guest1: Player = {
      id: 2,
      name: "Guest1",
      role: "guest",
      ready: false,
    }
    roomRepo.addGuest(room.code, guest1)

    const guest2Ws = { id: 3 }
    connRepo.add(guest2Ws, { userId: 3, userName: "Guest2", roomCode: null })

    const result = joinRoom(roomRepo, connRepo, guest2Ws, {
      userId: 3,
      userName: "Guest2",
      roomCode: room.code,
    })

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error).toBe("ROOM_FULL")
    }
  })

  test("fails when already in a room", () => {
    const room = createTestRoom()
    connRepo.add(guestWs, { userId: 2, userName: "Guest", roomCode: null })

    // Join first room
    joinRoom(roomRepo, connRepo, guestWs, {
      userId: 2,
      userName: "Guest",
      roomCode: room.code,
    })

    // Create another room and try to join
    const host2: Player = {
      id: 3,
      name: "Host2",
      role: "host",
      ready: false,
    }
    const room2 = roomRepo.create(host2)

    const result = joinRoom(roomRepo, connRepo, guestWs, {
      userId: 2,
      userName: "Guest",
      roomCode: room2.code,
    })

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error).toBe("ALREADY_IN_ROOM")
    }
  })
})
