import { check, sleep } from "k6"
import http from "k6/http"
import { Rate, Trend } from "k6/metrics"
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

// Metrics
const errorRate = new Rate("errors")
const matchDuration = new Trend("match_duration")

// Options
export const options: Options = {
  vus: 10,
  duration: "30s",
  thresholds: {
    http_req_duration: ["p(95)<500"],
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

  if (action < 0.6) {
    // 60%: Record match
    let oppIdx = Math.floor(Math.random() * users.length)
    while (oppIdx === idx) oppIdx = Math.floor(Math.random() * users.length)
    recordMatch(
      user.accessToken,
      users[oppIdx].user.id,
      Math.floor(Math.random() * 5) + 1,
      Math.floor(Math.random() * 5),
    )
  } else {
    // 40%: Get rankings
    getRankings(user.accessToken)
  }

  sleep(0.05)
}
