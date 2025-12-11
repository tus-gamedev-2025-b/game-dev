import { db } from "../../libs/db/client.ts"
import { createMatchRepository } from "./match.ts"
import { createRankingRepository } from "./ranking.ts"
import { createAuthTokenRepository, createUserRepository } from "./user.ts"

export const userRepository = createUserRepository(db)
export const authTokenRepository = createAuthTokenRepository(db)
export const matchRepository = createMatchRepository(db)
export const rankingRepository = createRankingRepository(db)
