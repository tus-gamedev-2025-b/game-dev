import { config } from "../../config.ts"
import type { RankedUser } from "../../domain/ranking/entity.ts"

type CacheEntry<T> = {
  data: T
  expiresAt: number
}

export class RankingCache {
  private cache: CacheEntry<RankedUser[]> | null = null
  private readonly ttlMs: number

  constructor(ttlMs: number = config.ranking.cacheTtlMs) {
    this.ttlMs = ttlMs
  }

  get(): RankedUser[] | null {
    if (!this.cache) return null
    if (Date.now() > this.cache.expiresAt) {
      this.cache = null
      return null
    }
    return this.cache.data
  }

  set(data: RankedUser[]): void {
    this.cache = {
      data,
      expiresAt: Date.now() + this.ttlMs,
    }
  }

  invalidate(): void {
    this.cache = null
  }
}

export const rankingCache = new RankingCache()
