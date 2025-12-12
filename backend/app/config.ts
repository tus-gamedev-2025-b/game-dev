export const config = {
  db: {
    path: process.env.DATABASE_PATH ?? "./data/app.db",
  },
  auth: {
    accessTokenExpiresIn: 60 * 60 * 1000, // 1時間
    refreshTokenExpiresIn: 30 * 24 * 60 * 60 * 1000, // 30日
  },
  user: {
    defaultName: "NoName",
    nameMinLength: 3,
    nameMaxLength: 15,
  },
  ranking: {
    minMatchesForRanking: 10,
    topRanksCount: 10,
    cacheTtlMs: 30_000,
  },
} as const
