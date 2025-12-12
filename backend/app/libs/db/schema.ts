import { index, integer, sqliteTable, text } from "drizzle-orm/sqlite-core"

export const users = sqliteTable(
  "users",
  {
    id: integer("id").primaryKey({ autoIncrement: true }),
    name: text("name").notNull().default("NoName"),
    wins: integer("wins").notNull().default(0),
    losses: integer("losses").notNull().default(0),
    createdAt: text("created_at").notNull(),
    updatedAt: text("updated_at").notNull(),
  },
  (table) => [index("idx_users_wins_losses").on(table.wins, table.losses)],
)

export const authTokens = sqliteTable(
  "auth_tokens",
  {
    id: integer("id").primaryKey({ autoIncrement: true }),
    userId: integer("user_id")
      .notNull()
      .references(() => users.id, { onDelete: "cascade" }),
    accessToken: text("access_token").notNull().unique(),
    refreshToken: text("refresh_token").notNull().unique(),
    accessTokenExpiresAt: text("access_token_expires_at").notNull(),
    refreshTokenExpiresAt: text("refresh_token_expires_at").notNull(),
    createdAt: text("created_at").notNull(),
  },
  (table) => [
    index("idx_auth_tokens_access_token").on(table.accessToken),
    index("idx_auth_tokens_refresh_token").on(table.refreshToken),
    index("idx_auth_tokens_user_id").on(table.userId),
  ],
)

export const matches = sqliteTable(
  "matches",
  {
    id: integer("id").primaryKey({ autoIncrement: true }),
    winnerId: integer("winner_id")
      .notNull()
      .references(() => users.id),
    loserId: integer("loser_id")
      .notNull()
      .references(() => users.id),
    playedAt: text("played_at").notNull(),
  },
  (table) => [
    index("idx_matches_winner_id").on(table.winnerId),
    index("idx_matches_loser_id").on(table.loserId),
    index("idx_matches_played_at").on(table.playedAt),
  ],
)
