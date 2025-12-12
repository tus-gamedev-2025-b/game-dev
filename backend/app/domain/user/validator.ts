// 許可する文字パターン
// - ひらがな、カタカナ、漢字
// - 英数字（半角・全角）
// - スペース（半角・全角）
// - 長音記号（ー）、中点（・）
const ALLOWED_PATTERN =
  /^[\p{Script=Hiragana}\p{Script=Katakana}\p{Script=Han}a-zA-Z0-9\uFF21-\uFF3A\uFF41-\uFF5A\uFF10-\uFF19\s\u3000\u30FC\u30FB]+$/u

export type ValidationResult =
  | { success: true }
  | { success: false; code: "INVALID_NAME_LENGTH" | "INVALID_NAME_CHARACTERS" }

export const validateUserName = (name: string): ValidationResult => {
  // 文字数チェック（Unicode文字数）
  const length = [...name].length
  if (length < 3 || length > 15) {
    return { success: false, code: "INVALID_NAME_LENGTH" }
  }

  // 許可文字チェック
  if (!ALLOWED_PATTERN.test(name)) {
    return { success: false, code: "INVALID_NAME_CHARACTERS" }
  }

  return { success: true }
}
