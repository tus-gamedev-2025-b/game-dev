import { check, sleep } from "k6"
import http from "k6/http"
import { Counter, Rate, Trend } from "k6/metrics"
import type { Options } from "k6/options"

// Types
type User = {
  user: { id: number }
  accessToken: string
  refreshToken: string
}

type SetupData = {
  users: User[]
}

// Custom metrics
const matchRecordDuration = new Trend("match_record_duration")
const rankingFetchDuration = new Trend("ranking_fetch_duration")
const userCreateDuration = new Trend("user_create_duration")
const errorRate = new Rate("errors")
const matchesRecorded = new Counter("matches_recorded")

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
  } else {
    // 20%: Get user info
    const user = users[Math.floor(Math.random() * users.length)]
    getUser(user)
  }

  sleep(0.1 + Math.random() * 0.2)
}

export function teardown(data: SetupData): void {
  console.log(`Test completed with ${data.users.length} users`)
}
