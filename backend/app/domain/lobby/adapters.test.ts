import { beforeEach, describe, expect, test } from "bun:test"
import {
  InMemoryConnectionRepository,
  InMemoryRoomRepository,
} from "./adapters.ts"
import type { Player } from "./entity.ts"

describe("InMemoryRoomRepository", () => {
  let repo: InMemoryRoomRepository

  beforeEach(() => {
    repo = new InMemoryRoomRepository()
  })

  const createHost = (id = 1, name = "Host"): Player => ({
    id,
    name,
    role: "host",
    ready: false,
  })

  const createGuest = (id = 2, name = "Guest"): Player => ({
    id,
    name,
    role: "guest",
    ready: false,
  })

  describe("create", () => {
    test("creates a room with 6-character code", () => {
      const host = createHost()
      const room = repo.create(host)

      expect(room.code).toHaveLength(6)
      expect(room.host).toEqual(host)
      expect(room.guest).toBeNull()
      expect(room.createdAt).toBeInstanceOf(Date)
      expect(room.expiresAt).toBeInstanceOf(Date)
    })

    test("generates unique room codes", () => {
      const codes = new Set<string>()
      for (let i = 0; i < 100; i++) {
        const room = repo.create(createHost(i))
        codes.add(room.code)
      }
      expect(codes.size).toBe(100)
    })
  })

  describe("findByCode", () => {
    test("finds existing room", () => {
      const host = createHost()
      const created = repo.create(host)

      const found = repo.findByCode(created.code)
      expect(found).toEqual(created)
    })

    test("returns null for non-existent code", () => {
      const found = repo.findByCode("ABCDEF")
      expect(found).toBeNull()
    })
  })

  describe("addGuest", () => {
    test("adds guest to room", () => {
      const host = createHost()
      const guest = createGuest()
      const room = repo.create(host)

      const result = repo.addGuest(room.code, guest)

      expect(result.success).toBe(true)
      if (result.success) {
        expect(result.data.guest).toEqual(guest)
      }
    })

    test("fails when room is full", () => {
      const host = createHost()
      const guest1 = createGuest(2, "Guest1")
      const guest2 = createGuest(3, "Guest2")
      const room = repo.create(host)

      repo.addGuest(room.code, guest1)
      const result = repo.addGuest(room.code, guest2)

      expect(result.success).toBe(false)
      if (!result.success) {
        expect(result.error).toBe("ROOM_FULL")
      }
    })

    test("fails when room not found", () => {
      const guest = createGuest()
      const result = repo.addGuest("NOTFOUND", guest)

      expect(result.success).toBe(false)
      if (!result.success) {
        expect(result.error).toBe("ROOM_NOT_FOUND")
      }
    })
  })

  describe("removePlayer", () => {
    test("removes guest from room", () => {
      const host = createHost()
      const guest = createGuest()
      const room = repo.create(host)
      repo.addGuest(room.code, guest)

      const result = repo.removePlayer(room.code, guest.id)

      expect(result.success).toBe(true)
      if (result.success) {
        expect(result.data).not.toBeNull()
        expect(result.data?.guest).toBeNull()
      }
    })

    test("deletes room when host leaves", () => {
      const host = createHost()
      const room = repo.create(host)

      const result = repo.removePlayer(room.code, host.id)

      expect(result.success).toBe(true)
      if (result.success) {
        expect(result.data).toBeNull()
      }
      expect(repo.findByCode(room.code)).toBeNull()
    })

    test("fails when player not in room", () => {
      const host = createHost()
      const room = repo.create(host)

      const result = repo.removePlayer(room.code, 999)

      expect(result.success).toBe(false)
      if (!result.success) {
        expect(result.error).toBe("NOT_IN_ROOM")
      }
    })
  })

  describe("setReady", () => {
    test("sets host ready", () => {
      const host = createHost()
      const room = repo.create(host)

      const result = repo.setReady(room.code, host.id, true)

      expect(result.success).toBe(true)
      if (result.success) {
        expect(result.data.host.ready).toBe(true)
      }
    })

    test("sets guest ready", () => {
      const host = createHost()
      const guest = createGuest()
      const room = repo.create(host)
      repo.addGuest(room.code, guest)

      const result = repo.setReady(room.code, guest.id, true)

      expect(result.success).toBe(true)
      if (result.success) {
        expect(result.data.guest?.ready).toBe(true)
      }
    })

    test("cancels ready", () => {
      const host = createHost()
      const room = repo.create(host)
      repo.setReady(room.code, host.id, true)

      const result = repo.setReady(room.code, host.id, false)

      expect(result.success).toBe(true)
      if (result.success) {
        expect(result.data.host.ready).toBe(false)
      }
    })
  })

  describe("findByUserId", () => {
    test("finds room by host id", () => {
      const host = createHost()
      const room = repo.create(host)

      const found = repo.findByUserId(host.id)
      expect(found).toEqual(room)
    })

    test("finds room by guest id", () => {
      const host = createHost()
      const guest = createGuest()
      const room = repo.create(host)
      repo.addGuest(room.code, guest)

      const found = repo.findByUserId(guest.id)
      expect(found?.code).toBe(room.code)
    })

    test("returns null when not in any room", () => {
      const found = repo.findByUserId(999)
      expect(found).toBeNull()
    })
  })
})

describe("InMemoryConnectionRepository", () => {
  let repo: InMemoryConnectionRepository

  beforeEach(() => {
    repo = new InMemoryConnectionRepository()
  })

  const createMockWs = (id: number) => ({ id })
  const createConnection = (userId: number, userName: string) => ({
    userId,
    userName,
    roomCode: null,
  })

  describe("add and getByWs", () => {
    test("adds and retrieves connection", () => {
      const ws = createMockWs(1)
      const conn = createConnection(1, "User1")

      repo.add(ws, conn)
      const found = repo.getByWs(ws)

      expect(found).toEqual(conn)
    })

    test("replaces existing connection for same user", () => {
      const ws1 = createMockWs(1)
      const ws2 = createMockWs(2)
      const conn = createConnection(1, "User1")

      repo.add(ws1, conn)
      repo.add(ws2, conn)

      expect(repo.getByWs(ws1)).toBeNull()
      expect(repo.getByWs(ws2)).toEqual(conn)
    })
  })

  describe("remove", () => {
    test("removes and returns connection", () => {
      const ws = createMockWs(1)
      const conn = createConnection(1, "User1")

      repo.add(ws, conn)
      const removed = repo.remove(ws)

      expect(removed).toEqual(conn)
      expect(repo.getByWs(ws)).toBeNull()
    })

    test("returns null for non-existent ws", () => {
      const ws = createMockWs(1)
      const removed = repo.remove(ws)
      expect(removed).toBeNull()
    })
  })

  describe("getByUserId", () => {
    test("retrieves connection by user id", () => {
      const ws = createMockWs(1)
      const conn = createConnection(1, "User1")

      repo.add(ws, conn)
      const found = repo.getByUserId(1)

      expect(found).toEqual({ ws, connection: conn })
    })

    test("returns null for non-existent user", () => {
      const found = repo.getByUserId(999)
      expect(found).toBeNull()
    })
  })

  describe("getByRoomCode", () => {
    test("retrieves all connections in room", () => {
      const ws1 = createMockWs(1)
      const ws2 = createMockWs(2)
      const conn1 = { userId: 1, userName: "User1", roomCode: "ROOM01" }
      const conn2 = { userId: 2, userName: "User2", roomCode: "ROOM01" }
      const conn3 = { userId: 3, userName: "User3", roomCode: "ROOM02" }

      repo.add(ws1, conn1)
      repo.add(ws2, conn2)
      repo.add(createMockWs(3), conn3)

      const found = repo.getByRoomCode("ROOM01")

      expect(found).toHaveLength(2)
      expect(found.map((f) => f.connection.userId).sort()).toEqual([1, 2])
    })
  })

  describe("setRoomCode", () => {
    test("updates room code", () => {
      const ws = createMockWs(1)
      const conn = createConnection(1, "User1")

      repo.add(ws, conn)
      const success = repo.setRoomCode(ws, "ROOM01")

      expect(success).toBe(true)
      expect(repo.getByWs(ws)?.roomCode).toBe("ROOM01")
    })

    test("clears room code", () => {
      const ws = createMockWs(1)
      const conn = { userId: 1, userName: "User1", roomCode: "ROOM01" }

      repo.add(ws, conn)
      repo.setRoomCode(ws, null)

      expect(repo.getByWs(ws)?.roomCode).toBeNull()
    })

    test("returns false for non-existent ws", () => {
      const ws = createMockWs(1)
      const success = repo.setRoomCode(ws, "ROOM01")
      expect(success).toBe(false)
    })
  })
})
