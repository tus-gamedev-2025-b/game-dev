# API利用ガイド

このドキュメントでは、APIの使い方を説明します。

## 目次

- [認証](#認証)
- [エンドポイント一覧](#エンドポイント一覧)
- [API詳細](#api詳細)
  - [ユーザー作成](#ユーザー作成)
  - [ログイン](#ログイン)
  - [ユーザー情報取得](#ユーザー情報取得)
  - [ユーザー名変更](#ユーザー名変更)
  - [トークンリフレッシュ](#トークンリフレッシュ)
  - [対戦結果記録](#対戦結果記録)
  - [ランキング取得](#ランキング取得)
- [バリデーションルール](#バリデーションルール)
- [エラーレスポンス](#エラーレスポンス)

---

## 認証

このAPIはトークンベース認証を使用します。

### 認証フロー

```
1. ユーザー作成（POST /api/users）
   → accessToken と refreshToken を取得
   → クライアントに保存

2. APIリクエスト時
   → Authorization: Bearer {accessToken} ヘッダーを付与

3. アクセストークン期限切れ時（401エラー）
   → POST /api/auth/refresh で新しいトークンを取得

4. 再ログイン時（アプリ再起動など）
   → POST /api/users/login で新しいトークンを取得
```

### トークン有効期限

| トークン種別         | 有効期限 |
| -------------------- | -------- |
| アクセストークン     | 1時間    |
| リフレッシュトークン | 30日     |

---

## エンドポイント一覧

| メソッド | パス                | 認証 | 説明                 |
| -------- | ------------------- | :--: | -------------------- |
| POST     | /api/users          |  -   | ユーザー新規作成     |
| POST     | /api/users/login    |  -   | ログイン             |
| GET      | /api/users/:id      | 必要 | ユーザー情報取得     |
| PATCH    | /api/users/:id/name | 必要 | ユーザー名変更       |
| POST     | /api/auth/refresh   |  -   | トークンリフレッシュ |
| POST     | /api/matches        | 必要 | 対戦結果を記録       |
| GET      | /api/rankings       | 必要 | ランキング取得       |

---

## API詳細

### ユーザー作成

新規ユーザーを作成し、認証トークンを取得します。

```
POST /api/users
```

#### リクエスト

```bash
# 名前を指定せずに作成（デフォルト名: NoName）
curl -X POST http://localhost:3000/api/users

# 名前を指定して作成
curl -X POST http://localhost:3000/api/users \
  -H "Content-Type: application/json" \
  -d '{"name": "プレイヤー1"}'
```

#### レスポンス（201 Created）

```json
{
  "user": {
    "id": 1,
    "name": "プレイヤー1",
    "wins": 0,
    "losses": 0
  },
  "accessToken": "abc123-...",
  "refreshToken": "xyz789-...",
  "accessTokenExpiresAt": "2025-01-15T11:00:00.000Z",
  "refreshTokenExpiresAt": "2025-02-14T10:00:00.000Z"
}
```

---

### ログイン

保存済みのリフレッシュトークンを使って再認証します。

```
POST /api/users/login
```

#### リクエスト

```bash
curl -X POST http://localhost:3000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"userId": 1, "refreshToken": "xyz789-..."}'
```

| パラメータ   | 型     | 必須 | 説明                 |
| ------------ | ------ | :--: | -------------------- |
| userId       | number |  ○   | ユーザーID           |
| refreshToken | string |  ○   | リフレッシュトークン |

#### レスポンス（200 OK）

```json
{
  "user": {
    "id": 1,
    "name": "プレイヤー1",
    "wins": 5,
    "losses": 3
  },
  "accessToken": "new-access-token-...",
  "refreshToken": "new-refresh-token-...",
  "accessTokenExpiresAt": "2025-01-15T12:00:00.000Z",
  "refreshTokenExpiresAt": "2025-02-14T11:00:00.000Z"
}
```

#### エラー

| ステータス | コード                | 説明                         |
| ---------- | --------------------- | ---------------------------- |
| 401        | INVALID_REFRESH_TOKEN | トークンが無効または期限切れ |

---

### ユーザー情報取得

指定したユーザーの情報を取得します。

```
GET /api/users/:id
```

#### リクエスト

```bash
curl http://localhost:3000/api/users/1 \
  -H "Authorization: Bearer {accessToken}"
```

#### レスポンス（200 OK）

```json
{
  "user": {
    "id": 1,
    "name": "プレイヤー1",
    "wins": 5,
    "losses": 3
  }
}
```

#### エラー

| ステータス | コード         | 説明                 |
| ---------- | -------------- | -------------------- |
| 401        | UNAUTHORIZED   | トークンが無効       |
| 404        | USER_NOT_FOUND | ユーザーが存在しない |

---

### ユーザー名変更

自分のユーザー名を変更します。

```
PATCH /api/users/:id/name
```

#### リクエスト

```bash
curl -X PATCH http://localhost:3000/api/users/1/name \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {accessToken}" \
  -d '{"name": "新しい名前"}'
```

| パラメータ | 型     | 必須 | 説明                          |
| ---------- | ------ | :--: | ----------------------------- |
| name       | string |  ○   | 新しいユーザー名（3〜15文字） |

#### レスポンス（200 OK）

```json
{
  "user": {
    "id": 1,
    "name": "新しい名前",
    "wins": 5,
    "losses": 3
  }
}
```

#### エラー

| ステータス | コード                  | 説明                                 |
| ---------- | ----------------------- | ------------------------------------ |
| 400        | INVALID_NAME_LENGTH     | 名前が3〜15文字でない                |
| 400        | INVALID_NAME_CHARACTERS | 許可されていない文字が含まれる       |
| 401        | UNAUTHORIZED            | トークンが無効                       |
| 403        | FORBIDDEN               | 他人のプロフィールを変更しようとした |

---

### トークンリフレッシュ

アクセストークンが期限切れの場合、リフレッシュトークンで新しいトークンを取得します。

```
POST /api/auth/refresh
```

#### リクエスト

```bash
curl -X POST http://localhost:3000/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken": "xyz789-..."}'
```

| パラメータ   | 型     | 必須 | 説明                 |
| ------------ | ------ | :--: | -------------------- |
| refreshToken | string |  ○   | リフレッシュトークン |

#### レスポンス（200 OK）

```json
{
  "accessToken": "new-access-token-...",
  "refreshToken": "new-refresh-token-...",
  "accessTokenExpiresAt": "2025-01-15T12:00:00.000Z",
  "refreshTokenExpiresAt": "2025-02-14T11:00:00.000Z"
}
```

#### エラー

| ステータス | コード                | 説明                         |
| ---------- | --------------------- | ---------------------------- |
| 401        | INVALID_REFRESH_TOKEN | トークンが無効または期限切れ |

---

### 対戦結果記録

対戦結果を記録し、勝敗をカウントします。

```
POST /api/matches
```

#### リクエスト

```bash
curl -X POST http://localhost:3000/api/matches \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {accessToken}" \
  -d '{"visitorId": 2, "homeScore": 3, "visitorScore": 1}'
```

| パラメータ   | 型     | 必須 | 説明                         |
| ------------ | ------ | :--: | ---------------------------- |
| visitorId    | number |  ○   | 対戦相手のユーザーID         |
| homeScore    | number |  ○   | 自分（リクエスト者）のスコア |
| visitorScore | number |  ○   | 対戦相手のスコア             |

#### 勝敗判定

- `homeScore > visitorScore` → リクエスト者の勝利
- `homeScore < visitorScore` → 対戦相手の勝利
- 引き分けは考慮しません（同点の場合は対戦相手の勝利扱い）

#### レスポンス（201 Created）

```json
{
  "match": {
    "id": 1,
    "winnerId": 1,
    "loserId": 2,
    "playedAt": "2025-01-15T10:30:00.000Z"
  },
  "updatedStats": {
    "wins": 1,
    "losses": 0,
    "totalMatches": 1
  }
}
```

#### エラー

| ステータス | コード                 | 説明                 |
| ---------- | ---------------------- | -------------------- |
| 400        | SELF_MATCH_NOT_ALLOWED | 自分自身との対戦     |
| 401        | UNAUTHORIZED           | トークンが無効       |
| 404        | VISITOR_NOT_FOUND      | 対戦相手が存在しない |

---

### ランキング取得

TOP10ランキングと自分の順位を取得します。

```
GET /api/rankings
```

#### リクエスト

```bash
curl http://localhost:3000/api/rankings \
  -H "Authorization: Bearer {accessToken}"
```

#### レスポンス（200 OK）

```json
{
  "rankings": [
    {
      "rank": 1,
      "userId": 10,
      "userName": "Champion",
      "winRate": 85.0,
      "wins": 17,
      "losses": 3,
      "totalMatches": 20
    },
    {
      "rank": 2,
      "userId": 5,
      "userName": "ProGamer",
      "winRate": 75.0,
      "wins": 15,
      "losses": 5,
      "totalMatches": 20
    }
  ],
  "currentUser": {
    "rank": 25,
    "userId": 1,
    "userName": "プレイヤー1",
    "winRate": 40.0,
    "wins": 4,
    "losses": 6,
    "totalMatches": 10
  }
}
```

#### 10戦未満の場合

10戦未満のユーザーはランキング対象外となり、`rank` が `null` になります。

```json
{
  "rankings": [...],
  "currentUser": {
    "rank": null,
    "userId": 1,
    "userName": "NewPlayer",
    "winRate": 66.67,
    "wins": 2,
    "losses": 1,
    "totalMatches": 3
  }
}
```

#### ランキング仕様

| 項目         | 仕様                                        |
| ------------ | ------------------------------------------- |
| 対象ユーザー | 10戦以上プレイしたユーザー                  |
| 表示内容     | TOP10 + 自分自身                            |
| ソート基準   | 勝率降順 → 勝ち数降順 → ID昇順              |
| 圏外表示     | 10戦未満または10位以下の場合は `rank: null` |

#### エラー

| ステータス | コード       | 説明           |
| ---------- | ------------ | -------------- |
| 401        | UNAUTHORIZED | トークンが無効 |

---

## バリデーションルール

### ユーザー名

| ルール   | 内容                                                                                             |
| -------- | ------------------------------------------------------------------------------------------------ |
| 文字数   | 3〜15文字（Unicode文字単位）                                                                     |
| 許可文字 | ひらがな、カタカナ、漢字、英数字（半角・全角）、スペース（半角・全角）、長音符（ー）、中点（・） |
| 禁止文字 | 記号（@, !, #, etc.）、絵文字                                                                    |

有効な例：

- `プレイヤー1`
- `田中太郎`
- `John Doe`
- `ユーザー・テスト`

無効な例：

- `AB`（2文字 - 短すぎる）
- `VeryLongUserName123`（16文字以上 - 長すぎる）
- `user@name`（記号を含む）
- `player😀`（絵文字を含む）

---

## エラーレスポンス

すべてのエラーは以下の形式で返されます：

```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "エラーの説明"
  }
}
```

### エラーコード一覧

| コード                  | HTTPステータス | 説明                                 |
| ----------------------- | -------------- | ------------------------------------ |
| UNAUTHORIZED            | 401            | アクセストークンが無効または期限切れ |
| INVALID_REFRESH_TOKEN   | 401            | リフレッシュトークンが無効           |
| FORBIDDEN               | 403            | 権限がない                           |
| USER_NOT_FOUND          | 404            | ユーザーが存在しない                 |
| VISITOR_NOT_FOUND       | 404            | 対戦相手が存在しない                 |
| INVALID_NAME_LENGTH     | 400            | 名前の長さが不正（3〜15文字）        |
| INVALID_NAME_CHARACTERS | 400            | 名前に不正な文字が含まれる           |
| SELF_MATCH_NOT_ALLOWED  | 400            | 自分自身との対戦は不可               |
| VALIDATION_ERROR        | 400            | リクエストボディが不正               |
| INTERNAL_ERROR          | 500            | サーバー内部エラー                   |
