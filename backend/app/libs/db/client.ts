import { Database } from "bun:sqlite"
import { drizzle } from "drizzle-orm/bun-sqlite"
import { config } from "../../config.ts"
import * as schema from "./schema.ts"

const sqlite = new Database(config.db.path, { create: true })

// Enable WAL mode for better concurrency
sqlite.exec("PRAGMA journal_mode = WAL;")

export const db = drizzle(sqlite, { schema })
export type DrizzleDB = typeof db

// Initialize database tables
export const initializeDatabase = () => {
  sqlite.exec(`
    CREATE TABLE IF NOT EXISTS users (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      name TEXT NOT NULL DEFAULT 'NoName',
      wins INTEGER NOT NULL DEFAULT 0,
      losses INTEGER NOT NULL DEFAULT 0,
      created_at TEXT NOT NULL,
      updated_at TEXT NOT NULL
    );

    CREATE INDEX IF NOT EXISTS idx_users_wins_losses ON users(wins, losses);

    CREATE TABLE IF NOT EXISTS auth_tokens (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
      access_token TEXT NOT NULL UNIQUE,
      refresh_token TEXT NOT NULL UNIQUE,
      access_token_expires_at TEXT NOT NULL,
      refresh_token_expires_at TEXT NOT NULL,
      created_at TEXT NOT NULL
    );

    CREATE INDEX IF NOT EXISTS idx_auth_tokens_access_token ON auth_tokens(access_token);
    CREATE INDEX IF NOT EXISTS idx_auth_tokens_refresh_token ON auth_tokens(refresh_token);
    CREATE INDEX IF NOT EXISTS idx_auth_tokens_user_id ON auth_tokens(user_id);

    CREATE TABLE IF NOT EXISTS matches (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      winner_id INTEGER NOT NULL REFERENCES users(id),
      loser_id INTEGER NOT NULL REFERENCES users(id),
      played_at TEXT NOT NULL
    );

    CREATE INDEX IF NOT EXISTS idx_matches_winner_id ON matches(winner_id);
    CREATE INDEX IF NOT EXISTS idx_matches_loser_id ON matches(loser_id);
    CREATE INDEX IF NOT EXISTS idx_matches_played_at ON matches(played_at);
  `)
}
