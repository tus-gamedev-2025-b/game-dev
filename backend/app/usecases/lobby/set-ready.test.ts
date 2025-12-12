import { beforeEach, describe, expect, test } from "bun:test"
import { InMemoryRoomRepository } from "../../domain/lobby/adapters.ts"
import type { Player } from "../../domain/lobby/entity.ts"
import { setReady } from "./set-ready.ts"

describe("setReady", () => {
  let roomRepo: InMemoryRoomRepository

  beforeEach(() => {
    roomRepo = new InMemoryRoomRepository()
  })

  const createHost = (): Player => ({
    id: 1,
    name: "Host",
    role: "host",
    ready: false,
  })

  const createGuest = (): Player => ({
    id: 2,
    name: "Guest",
    role: "guest",
    ready: false,
  })

  test("sets host ready", () => {
    roomRepo.create(createHost())

    const result = setReady(roomRepo, { userId: 1, ready: true })

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.room.host.ready).toBe(true)
      expect(result.matchStart).toBe(false)
    }
  })

  test("sets guest ready", () => {
    const room = roomRepo.create(createHost())
    roomRepo.addGuest(room.code, createGuest())

    const result = setReady(roomRepo, { userId: 2, ready: true })

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.room.guest?.ready).toBe(true)
      expect(result.opponentUserId).toBe(1)
    }
  })

  test("cancels ready", () => {
    const room = roomRepo.create(createHost())
    roomRepo.setReady(room.code, 1, true)

    const result = setReady(roomRepo, { userId: 1, ready: false })

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.room.host.ready).toBe(false)
    }
  })

  test("triggers matchStart when both ready", () => {
    const room = roomRepo.create(createHost())
    roomRepo.addGuest(room.code, createGuest())
    roomRepo.setReady(room.code, 1, true)

    const result = setReady(roomRepo, { userId: 2, ready: true })

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.matchStart).toBe(true)
      expect(result.players).toHaveLength(2)
      expect(result.players?.[0]).toEqual({ id: 1, name: "Host", role: "host" })
      expect(result.players?.[1]).toEqual({
        id: 2,
        name: "Guest",
        role: "guest",
      })
    }
  })

  test("does not trigger matchStart without guest", () => {
    roomRepo.create(createHost())

    const result = setReady(roomRepo, { userId: 1, ready: true })

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.matchStart).toBe(false)
      expect(result.opponentUserId).toBeNull()
    }
  })

  test("does not trigger matchStart when only one ready", () => {
    const room = roomRepo.create(createHost())
    roomRepo.addGuest(room.code, createGuest())

    const result = setReady(roomRepo, { userId: 1, ready: true })

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.matchStart).toBe(false)
    }
  })

  test("fails when not in room", () => {
    const result = setReady(roomRepo, { userId: 999, ready: true })

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error).toBe("NOT_IN_ROOM")
    }
  })
})
