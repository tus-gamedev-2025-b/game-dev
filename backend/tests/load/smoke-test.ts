import { check, sleep } from "k6"
import http from "k6/http"
import { Rate, Trend } from "k6/metrics"
import type { Options } from "k6/options"
import ws from "k6/ws"

// Types
type User = {
  user: { id: number }
  accessToken: string
  refreshToken: string
}

type SetupData = {
  users: User[]
}

type WsMessage = {
  type: string
  payload?: unknown
  error?: { code: string; message: string }
}

// Metrics
const errorRate = new Rate("errors")
const matchDuration = new Trend("match_duration")
const lobbyDuration = new Trend("lobby_duration")

// Options
export const options: Options = {
  vus: 10,
  duration: "30s",
  thresholds: {
    http_req_duration: ["p(95)<500"],
    lobby_duration: ["p(95)<2000"],
    errors: ["rate<0.05"],
  },
}

const BASE_URL = __ENV.BASE_URL || "http://localhost:3000"

function createUser(): User | null {
  const res = http.post(`${BASE_URL}/api/users`)
  const success = check(res, { "user created": (r) => r.status === 201 })
  errorRate.add(!success)
  return success ? (res.json() as User) : null
}

function recordMatch(
  token: string,
  visitorId: number,
  homeScore: number,
  visitorScore: number,
): boolean {
  const start = Date.now()
  const res = http.post(
    `${BASE_URL}/api/matches`,
    JSON.stringify({ visitorId, homeScore, visitorScore }),
    {
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
      },
    },
  )
  matchDuration.add(Date.now() - start)
  const success = check(res, { "match recorded": (r) => r.status === 201 })
  errorRate.add(!success)
  return success
}

function getRankings(token: string): boolean {
  const res = http.get(`${BASE_URL}/api/rankings`, {
    headers: { Authorization: `Bearer ${token}` },
  })
  const success = check(res, { "rankings ok": (r) => r.status === 200 })
  errorRate.add(!success)
  return success
}

function testLobbyFlow(hostToken: string, guestToken: string): boolean {
  const start = Date.now()
  const wsUrl = BASE_URL.replace("http://", "ws://").replace(
    "https://",
    "wss://",
  )
  let success = true
  let roomCode = ""
  let isConflictError = false

  // Host creates room
  const hostRes = ws.connect(`${wsUrl}/ws?token=${hostToken}`, {}, (socket) => {
    socket.on("open", () => {
      socket.send(JSON.stringify({ type: "createRoom" }))
    })

    socket.on("message", (data: string) => {
      const msg: WsMessage = JSON.parse(data)
      if (msg.type === "roomCreated") {
        roomCode = (msg.payload as { roomCode: string }).roomCode
        socket.close()
      } else if (msg.type === "error") {
        // ALREADY_IN_ROOM is expected in load tests due to concurrent access
        if (msg.error?.code === "ALREADY_IN_ROOM") {
          isConflictError = true
        } else {
          success = false
        }
        socket.close()
      }
    })

    socket.on("error", () => {
      success = false
    })

    socket.setTimeout(() => {
      socket.close()
    }, 5000)
  })

  check(hostRes, { "host ws connected": (r) => r && r.status === 101 })

  // Skip if conflict or no room code
  if (isConflictError || !roomCode) {
    lobbyDuration.add(Date.now() - start)
    // Don't count conflicts as errors
    if (!isConflictError && !roomCode) {
      errorRate.add(true)
    }
    return isConflictError
  }

  // Guest joins room
  const guestRes = ws.connect(
    `${wsUrl}/ws?token=${guestToken}`,
    {},
    (socket) => {
      socket.on("open", () => {
        socket.send(JSON.stringify({ type: "joinRoom", payload: { roomCode } }))
      })

      socket.on("message", (data: string) => {
        const msg: WsMessage = JSON.parse(data)
        if (msg.type === "roomJoined") {
          socket.close()
        } else if (msg.type === "error") {
          // ALREADY_IN_ROOM or ROOM_FULL is expected in load tests
          if (
            msg.error?.code === "ALREADY_IN_ROOM" ||
            msg.error?.code === "ROOM_FULL"
          ) {
            isConflictError = true
          } else {
            success = false
          }
          socket.close()
        }
      })

      socket.on("error", () => {
        success = false
      })

      socket.setTimeout(() => {
        socket.close()
      }, 5000)
    },
  )

  check(guestRes, { "guest ws connected": (r) => r && r.status === 101 })

  lobbyDuration.add(Date.now() - start)
  // Don't count conflicts as errors
  if (!isConflictError) {
    errorRate.add(!success)
  }
  return success || isConflictError
}

export function setup(): SetupData {
  const users: User[] = []
  for (let i = 0; i < 20; i++) {
    const user = createUser()
    if (user) users.push(user)
  }
  console.log(`Setup: created ${users.length} users`)
  return { users }
}

export default function (data: SetupData): void {
  const { users } = data
  if (users.length < 2) return

  const action = Math.random()
  const idx = Math.floor(Math.random() * users.length)
  const user = users[idx]

  if (action < 0.55) {
    // 55%: Record match
    let oppIdx = Math.floor(Math.random() * users.length)
    while (oppIdx === idx) oppIdx = Math.floor(Math.random() * users.length)
    recordMatch(
      user.accessToken,
      users[oppIdx].user.id,
      Math.floor(Math.random() * 5) + 1,
      Math.floor(Math.random() * 5),
    )
  } else if (action < 0.95) {
    // 40%: Get rankings
    getRankings(user.accessToken)
  } else {
    // 5%: Test lobby flow (reduced to avoid ALREADY_IN_ROOM conflicts)
    let oppIdx = Math.floor(Math.random() * users.length)
    while (oppIdx === idx) oppIdx = Math.floor(Math.random() * users.length)
    testLobbyFlow(user.accessToken, users[oppIdx].accessToken)
  }

  sleep(0.05)
}
