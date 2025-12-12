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
  pvp: {
    room: {
      codeLength: 6, // ルームコード長
      expiresIn: 30 * 60 * 1000, // ルーム有効期限（30分）
      maxPlayers: 2, // 最大プレイヤー数
    },
    stamp: {
      count: 6, // スタンプ種類数
    },
    websocket: {
      path: "/ws", // WebSocketパス
      pingInterval: 30_000, // Ping間隔（30秒）
    },
  },
} as const
