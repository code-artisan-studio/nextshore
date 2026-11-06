import { useAuth } from "@clerk/react"
import { useCallback } from "react"

import { apiFetch } from "@/api/client"

/** `apiFetch` with the signed-in admin's Clerk session token. */
export function useAdminFetch() {
  const { getToken } = useAuth()
  return useCallback(
    async <T,>(path: string, init?: RequestInit): Promise<T> => {
      const token = await getToken()
      return apiFetch<T>(`/admin${path}`, {
        ...init,
        headers: { ...init?.headers, ...(token ? { Authorization: `Bearer ${token}` } : {}) },
      })
    },
    [getToken],
  )
}
