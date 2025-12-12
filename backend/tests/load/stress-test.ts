import { check, sleep } from "k6"
import http from "k6/http"
import { Counter, Rate, Trend } from "k6/metrics"
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

// Custom metrics
const matchRecordDuration = new Trend("match_record_duration")
const rankingFetchDuration = new Trend("ranking_fetch_duration")
const userCreateDuration = new Trend("user_create_duration")
const lobbyDuration = new Trend("lobby_duration")
const errorRate = new Rate("errors")
const matchesRecorded = new Counter("matches_recorded")
const lobbyFlowsCompleted = new Counter("lobby_flows_completed")

// Test configuration
export const options: Options = {
  scenarios: {
    // Smoke test: quick sanity check
    smoke: {
      executor: "constant-vus",
      vus: 5,
      duration: "30s",
      startTime: "0s",
      tags: { scenario: "smoke" },
    },
    // Load test: normal expected load
    load: {
      executor: "ramping-vus",
      startVUs: 0,
      stages: [
        { duration: "30s", target: 20 },
        { duration: "1m", target: 20 },
        { duration: "30s", target: 0 },
      ],
      startTime: "35s",
      tags: { scenario: "load" },
    },
    // Stress test: find breaking point
    stress: {
      executor: "ramping-vus",
      startVUs: 0,
      stages: [
        { duration: "30s", target: 50 },
        { duration: "1m", target: 50 },
        { duration: "30s", target: 100 },
        { duration: "1m", target: 100 },
        { duration: "30s", target: 0 },
      ],
      startTime: "3m",
      tags: { scenario: "stress" },
    },
  },
  thresholds: {
    http_req_duration: ["p(95)<500", "p(99)<1000"],
    errors: ["rate<0.1"],
    match_record_duration: ["p(95)<300"],
    ranking_fetch_duration: ["p(95)<200"],
    lobby_duration: ["p(95)<3000"],
  },
}

const BASE_URL = __ENV.BASE_URL || "http://localhost:3000"

function createUser(): User | null {
  const start = Date.now()
  const res = http.post(`${BASE_URL}/api/users`)
  userCreateDuration.add(Date.now() - start)

  const success = check(res, {
    "user created": (r) => r.status === 201,
  })

  if (!success) {
    errorRate.add(1)
    return null
  }

  errorRate.add(0)
  return res.json() as User
}

function recordMatch(homeUser: User, visitorId: number): boolean {
  const start = Date.now()
  const res = http.post(
    `${BASE_URL}/api/matches`,
    JSON.stringify({
      visitorId: visitorId,
      homeScore: Math.floor(Math.random() * 5) + 1,
      visitorScore: Math.floor(Math.random() * 5),
    }),
    {
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${homeUser.accessToken}`,
      },
    },
  )
  matchRecordDuration.add(Date.now() - start)

  const success = check(res, {
    "match recorded": (r) => r.status === 201,
  })

  if (success) {
    matchesRecorded.add(1)
    errorRate.add(0)
  } else {
    errorRate.add(1)
  }

  return success
}

function getRankings(user: User): boolean {
  const start = Date.now()
  const res = http.get(`${BASE_URL}/api/rankings`, {
    headers: {
      Authorization: `Bearer ${user.accessToken}`,
    },
  })
  rankingFetchDuration.add(Date.now() - start)

  const success = check(res, {
    "rankings fetched": (r) => r.status === 200,
    "has rankings array": (r) => {
      const body = r.json() as { rankings?: unknown[] }
      return body && Array.isArray(body.rankings)
    },
  })

  errorRate.add(!success)
  return success
}

function getUser(user: User): boolean {
  const res = http.get(`${BASE_URL}/api/users/${user.user.id}`, {
    headers: {
      Authorization: `Bearer ${user.accessToken}`,
    },
  })

  const success = check(res, {
    "user fetched": (r) => r.status === 200,
  })

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
      errorRate.add(1)
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
    if (success) {
      lobbyFlowsCompleted.add(1)
      errorRate.add(0)
    } else {
      errorRate.add(1)
    }
  }

  return success || isConflictError
}

export function setup(): SetupData {
  console.log("Creating initial users for load test...")
  const initialUsers: User[] = []

  for (let i = 0; i < 50; i++) {
    const user = createUser()
    if (user) {
      initialUsers.push(user)
    }
  }

  console.log(`Created ${initialUsers.length} users`)
  return { users: initialUsers }
}

export default function (data: SetupData): void {
  const users = data.users

  if (users.length < 2) {
    console.log("Not enough users to run test")
    return
  }

  // Randomly select an action
  const action = Math.random()

  if (action < 0.1) {
    // 10%: Create new user
    const newUser = createUser()
    if (newUser) {
      users.push(newUser)
    }
  } else if (action < 0.5) {
    // 40%: Record a match
    const homeIdx = Math.floor(Math.random() * users.length)
    let visitorIdx = Math.floor(Math.random() * users.length)
    while (visitorIdx === homeIdx) {
      visitorIdx = Math.floor(Math.random() * users.length)
    }

    const home = users[homeIdx]
    const visitor = users[visitorIdx]
    recordMatch(home, visitor.user.id)
  } else if (action < 0.8) {
    // 30%: Get rankings
    const user = users[Math.floor(Math.random() * users.length)]
    getRankings(user)
  } else if (action < 0.95) {
    // 15%: Get user info
    const user = users[Math.floor(Math.random() * users.length)]
    getUser(user)
  } else {
    // 5%: Test lobby flow (reduced to avoid ALREADY_IN_ROOM conflicts)
    const hostIdx = Math.floor(Math.random() * users.length)
    let guestIdx = Math.floor(Math.random() * users.length)
    while (guestIdx === hostIdx) {
      guestIdx = Math.floor(Math.random() * users.length)
    }

    const host = users[hostIdx]
    const guest = users[guestIdx]
    testLobbyFlow(host.accessToken, guest.accessToken)
  }

  sleep(0.1 + Math.random() * 0.2)
}

export function teardown(data: SetupData): void {
  console.log(`Test completed with ${data.users.length} users`)
}
