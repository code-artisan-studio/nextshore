import { useSyncExternalStore } from "react"

import type { ExtractedProfile } from "@/api/types"

/**
 * What the user brings into `/profile`: the profile extracted from their resume (or nothing, for manual entry).
 * Kept in sessionStorage so a refresh doesn't lose it; it never leaves this browser tab and is cleared when the
 * tab closes. The resume file and its text are never kept, here or on the server.
 */
export interface ProfileDraft {
  source: "resume" | "manual"
  extracted: ExtractedProfile | null
}

const key = "nextshore.profileDraft"
const listeners = new Set<() => void>()
let current: ProfileDraft | null = read()

function read(): ProfileDraft | null {
  try {
    const raw = sessionStorage.getItem(key)
    return raw ? (JSON.parse(raw) as ProfileDraft) : null
  } catch {
    return null
  }
}

export function setProfileDraft(draft: ProfileDraft | null) {
  current = draft
  try {
    if (draft) sessionStorage.setItem(key, JSON.stringify(draft))
    else sessionStorage.removeItem(key)
  } catch {
    // Storage blocked or full: the draft still lives in memory for this page load.
  }
  listeners.forEach((listener) => listener())
}

function subscribe(listener: () => void) {
  listeners.add(listener)
  return () => listeners.delete(listener)
}

export function useProfileDraft(): ProfileDraft | null {
  return useSyncExternalStore(subscribe, () => current)
}
