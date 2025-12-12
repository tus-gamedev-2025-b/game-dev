export type MatchValidationResult =
  | { success: true }
  | { success: false; code: "SELF_MATCH_NOT_ALLOWED" }

export const validateMatchRequest = (
  homeUserId: number,
  visitorId: number,
): MatchValidationResult => {
  if (homeUserId === visitorId) {
    return { success: false, code: "SELF_MATCH_NOT_ALLOWED" }
  }

  return { success: true }
}
