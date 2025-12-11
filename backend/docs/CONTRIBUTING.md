# 開発ガイド

このドキュメントでは、APIの開発に参加するための情報を提供します。

## 目次

- [開発環境セットアップ](#開発環境セットアップ)
- [プロジェクト構成](#プロジェクト構成)
- [テスト](#テスト)
- [コード品質](#コード品質)
- [アーキテクチャ](#アーキテクチャ)
- [コミット規約](#コミット規約)

---

## 開発環境セットアップ

### 必要なツール

- [Bun](https://bun.sh) v1.0以上
- [k6](https://k6.io)（負荷テスト用、オプション）

### 初回セットアップ

```bash
# リポジトリをクローン
git clone <repository-url>
cd backend

# 依存パッケージをインストール
bun install

# 開発サーバーを起動
bun run dev
```

### IDE設定

VSCodeを使用する場合、以下の拡張機能を推奨します：

- [Biome](https://marketplace.visualstudio.com/items?itemName=biomejs.biome) - Lint & Format
- [SQLite Viewer](https://marketplace.visualstudio.com/items?itemName=qwtel.sqlite-viewer) - DBブラウザ

---

## プロジェクト構成

- `index.ts` - エントリーポイント
- `app/` - アプリケーション本体
  - `index.ts` - Honoアプリ初期化
  - `config.ts` - 設定値
  - `domain/` - ドメイン層
    - `user/` - ユーザードメイン
      - `entity.ts` - 型定義
      - `repository.ts` - リポジトリインターフェース
      - `adapters.ts` - Drizzle実装
      - `validator.ts` - バリデーション
    - `match/` - 対戦ドメイン
    - `ranking/` - ランキングドメイン
  - `usecases/` - ユースケース層
    - `user/`
    - `match/`
    - `ranking/`
    - `repositories-provider.ts` - DI設定
  - `routes/api/` - APIルート（インターフェース層）
  - `schemas/` - Zodスキーマ（OpenAPI用）
  - `helpers/` - ヘルパー（認証など）
  - `libs/` - ライブラリ（DB、キャッシュ）
- `tests/` - 統合テスト
  - `api/` - APIテスト
  - `load/` - 負荷テスト
- `docs/` - 設計ドキュメント
- `data/` - データベースファイル

---

## テスト

### テストの実行

```bash
# 全テスト実行
bun run test

# ウォッチモード（ファイル変更時に自動実行）
bun run test:watch

# ユニットテストのみ（app/配下）
bun run test:unit

# 統合テストのみ（tests/配下）
bun run test:integration

# カバレッジ付き
bun run test:coverage

# 詳細出力
bun run test:verbose
```

### テストファイルの配置

テストファイルは実装ファイルと同じディレクトリに配置します（コロケーション）。

```
app/domain/user/
├── validator.ts          # 実装
└── validator.test.ts     # テスト

app/usecases/ranking/
├── get-ranking.ts        # 実装
└── get-ranking.test.ts   # テスト
```

統合テスト（API全体のテスト）は `tests/` ディレクトリに配置します。

```
tests/api/
├── users.test.ts         # ユーザーAPI
├── matches.test.ts       # 対戦API
├── rankings.test.ts      # ランキングAPI
└── workflow.test.ts      # ワークフローテスト
```

### テストの書き方

```typescript
import { describe, expect, test } from "bun:test"

describe("機能名", () => {
  test("正常系: 期待する動作", () => {
    const result = someFunction()
    expect(result).toBe(expectedValue)
  })

  test("異常系: エラーケース", () => {
    const result = someFunction(invalidInput)
    expect(result.success).toBe(false)
  })
})
```

### テストデータベース

テスト実行時は `./data/test.db` を使用します。テスト間でデータは共有されますが、テストは独立して実行できるように設計してください。

---

## コード品質

### Lint & Format

[Biome](https://biomejs.dev) を使用しています。

```bash
# チェックのみ
bun run lint
bun run format

# 自動修正
bun run lint:fix
bun run format:fix

# 両方
bun run check
bun run check:fix
```

### 型チェック

```bash
bun run typecheck
```

### 依存関係チェック

[dependency-cruiser](https://github.com/sverweij/dependency-cruiser) を使用して、アーキテクチャルールに違反する依存がないか確認できます。

```bash
# テキスト出力
bun run arch

# グラフ生成（SVG）
bun run arch:graph
```

---

## アーキテクチャ

Clean Architectureに基づいた4層構造を採用しています。

```
[Interface Layer]     →  [Usecase Layer]  →  [Domain Layer]
(routes/api/)            (usecases/)          (domain/)
                              ↑
                     [Infrastructure Layer]
                     (domain/*/adapters.ts, libs/)
```

### レイヤーの責務

| レイヤー       | 責務                                                     | 配置                                    |
| -------------- | -------------------------------------------------------- | --------------------------------------- |
| Interface      | リクエストのパース・バリデーション、ユースケース呼び出し | `app/routes/api/`                       |
| Usecase        | ビジネスロジックの実装                                   | `app/usecases/`                         |
| Domain         | エンティティ、抽象リポジトリ定義                         | `app/domain/`                           |
| Infrastructure | Drizzleによるリポジトリ実装                              | `app/domain/*/adapters.ts`, `app/libs/` |

### 依存関係ルール

各レイヤーがimportできる対象：

| ファイル                 | import可能な対象                                                                           |
| ------------------------ | ------------------------------------------------------------------------------------------ |
| `domain/*/entity.ts`     | なし（純粋な型定義のみ）                                                                   |
| `domain/*/repository.ts` | `entity.ts` のみ                                                                           |
| `domain/*/adapters.ts`   | `entity.ts`, `repository.ts`, `libs/db/*`                                                  |
| `domain/*/validator.ts`  | `entity.ts` のみ                                                                           |
| `usecases/*/*.ts`        | `domain/*/entity.ts`, `domain/*/repository.ts`, `repositories-provider.ts`, `libs/cache/*` |
| `routes/api/*.ts`        | `usecases/*/*.ts`, `schemas/*.ts`, `helpers/*`                                             |

詳細は `docs/api-designdocs.md` の「実装上の注意点（Clean Architecture 準拠）」を参照してください。

---

## コミット規約

### コミットメッセージ

[Conventional Commits](https://www.conventionalcommits.org/) に従います。

```
<type>: <description>

[optional body]
```

### Type一覧

| Type     | 説明                                       |
| -------- | ------------------------------------------ |
| feat     | 新機能                                     |
| fix      | バグ修正                                   |
| docs     | ドキュメントのみ                           |
| style    | フォーマット変更（コードの動作に影響なし） |
| refactor | リファクタリング                           |
| test     | テストの追加・修正                         |
| chore    | ビルドプロセスやツールの変更               |

### 例

```
feat: ユーザー名バリデーションを追加

- 3〜15文字の制限を追加
- 記号の使用を禁止
```

---

## 負荷テスト

[k6](https://k6.io) を使用した負荷テストを実行できます。

### インストール

```bash
# macOS
brew install k6

# その他は公式サイト参照
```

### 実行

```bash
# スモークテスト（軽量）
bun run load:smoke

# ストレステスト（高負荷）
bun run load:stress
```

テストシナリオは `tests/load/` ディレクトリにあります。
