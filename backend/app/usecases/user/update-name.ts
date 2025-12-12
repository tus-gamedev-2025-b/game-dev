import type { User } from "../../domain/user/entity.ts"
import { validateUserName } from "../../domain/user/validator.ts"
import { userRepository } from "../../infra/repositories/index.ts"

export type UpdateNameResult =
  | {
      success: true
      user: User
    }
  | {
      success: false
      code:
        | "USER_NOT_FOUND"
        | "FORBIDDEN"
        | "INVALID_NAME_LENGTH"
        | "INVALID_NAME_CHARACTERS"
    }

export const updateName = async (
  targetUserId: number,
  requesterId: number,
  newName: string,
): Promise<UpdateNameResult> => {
  // Check if requester is updating their own profile
  if (targetUserId !== requesterId) {
    return { success: false, code: "FORBIDDEN" }
  }

  // Validate name
  const validationResult = validateUserName(newName)
  if (!validationResult.success) {
    return { success: false, code: validationResult.code }
  }

  // Update name
  const user = await userRepository.updateName(targetUserId, newName)

  if (!user) {
    return { success: false, code: "USER_NOT_FOUND" }
  }

  return { success: true, user }
}
