import type { User } from "../../domain/user/entity.ts"
import { userRepository } from "../../infra/repositories/index.ts"

export type GetUserResult =
  | {
      success: true
      user: User
    }
  | {
      success: false
      code: "USER_NOT_FOUND"
    }

export const getUser = async (id: number): Promise<GetUserResult> => {
  const user = await userRepository.findById(id)

  if (!user) {
    return { success: false, code: "USER_NOT_FOUND" }
  }

  return { success: true, user }
}
