# Game Backend API

Tanks向けのバックエンドAPIサーバーです。ユーザー管理、認証、対戦記録、ランキング機能を提供します。

## 技術スタック

| カテゴリ       | 技術                                    |
| -------------- | --------------------------------------- |
| ランタイム     | [Bun](https://bun.sh)                   |
| フレームワーク | [Hono](https://hono.dev)                |
| ORM            | [Drizzle](https://orm.drizzle.team)     |
| データベース   | [SQLite](https://sqlite.org/index.html) |
| バリデーション | [Zod](https://zod.dev)                  |

## セットアップ

### 必要環境

miseがインストールされている場合、以下のコマンドで必要なツールをインストールできます。

```bash
mise i
```

### インストール

```bash
bun install
```

### 環境変数（オプション）

`.env` ファイルで設定を上書きできます：

```bash
# データベースファイルのパス（デフォルト: ./data/app.db）
DATABASE_PATH=./data/app.db

# サーバーポート（デフォルト: 3000）
PORT=3000
```

## サーバー起動

```bash
# 開発モード（ホットリロード有効）
bun run dev

# 本番モード
bun run start
```

サーバーが起動すると、以下のURLでアクセスできます：

| URL                                 | 説明                          |
| ----------------------------------- | ----------------------------- |
| http://localhost:3000/api           | API エンドポイント            |
| http://localhost:3000/health        | ヘルスチェック                |
| http://localhost:3000/api/doc       | OpenAPI JSON                  |
| http://localhost:3000/api/reference | API リファレンス（Scalar UI） |

## 利用可能なスクリプト

### 開発

| コマンド        | 説明                     |
| --------------- | ------------------------ |
| `bun run dev`   | ホットリロード付きで起動 |
| `bun run start` | 本番モードで起動         |

### テスト

| コマンド                   | 説明                           |
| -------------------------- | ------------------------------ |
| `bun run test`             | 全テスト実行                   |
| `bun run test:watch`       | ウォッチモードでテスト         |
| `bun run test:unit`        | ユニットテストのみ（app/配下） |
| `bun run test:integration` | 統合テストのみ（tests/配下）   |
| `bun run test:coverage`    | カバレッジ付きテスト           |

### コード品質

| コマンド             | 説明                   |
| -------------------- | ---------------------- |
| `bun run typecheck`  | 型チェック             |
| `bun run lint`       | Lintチェック           |
| `bun run lint:fix`   | Lint自動修正           |
| `bun run format`     | フォーマットチェック   |
| `bun run format:fix` | フォーマット自動修正   |
| `bun run check`      | Lint + Format チェック |
| `bun run check:fix`  | Lint + Format 自動修正 |

### アーキテクチャ

| コマンド             | 説明                      |
| -------------------- | ------------------------- |
| `bun run arch`       | 依存関係チェック          |
| `bun run arch:graph` | 依存関係グラフ生成（SVG） |

### OpenAPI

| コマンド                | 説明                |
| ----------------------- | ------------------- |
| `bun run openapi:gen`   | OpenAPI JSON生成    |
| `bun run openapi:check` | OpenAPIスキーマ検証 |

### 負荷テスト

| コマンド              | 説明           |
| --------------------- | -------------- |
| `bun run load:smoke`  | スモークテスト |
| `bun run load:stress` | ストレステスト |

## ドキュメント

- [API利用ガイド](./docs/api.md) - APIの使い方、エンドポイント一覧、curl例
- [設計ドキュメント](./docs/design.md) - 技術設計、アーキテクチャ
