import { useSyncExternalStore } from "react"

import type { ExtractedProfile } from "@/api/types"

import type { ProfileFormValues } from "./schema"

/**
 * What the user brings into and types on `/profile`: the profile extracted from their resume (or nothing, for
 * manual entry), then the form as they fill it in. Kept in sessionStorage so a refresh doesn't lose it; it never
 * leaves this browser tab and is cleared when the tab closes. The resume file and its text are never kept, here or
 * on the server.
 */
export interface ProfileDraft {
  source: "resume" | "manual"
  extracted: ExtractedProfile | null
  values?: ProfileFormValues
  step?: number
  /** Resume-filled fields the user has changed since. */
  edited?: string[]
  /** The assessment these answers produced, so its results page can show them and run what-ifs. */
  assessmentId?: string
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

/** Merges `patch` into the current draft (starting a manual one if there is none). */
export function updateProfileDraft(patch: Partial<ProfileDraft>) {
  setProfileDraft({ source: "manual", extracted: null, ...current, ...patch })
}

export function getProfileDraft() {
  return current
}

function subscribe(listener: () => void) {
  listeners.add(listener)
  return () => listeners.delete(listener)
}

export function useProfileDraft(): ProfileDraft | null {
  return useSyncExternalStore(subscribe, () => current)
}

/**
 * The answers behind assessment `id`, if this tab submitted them. The API doesn't return the profile with a stored
 * assessment (a shared link shouldn't reveal date of birth or citizenship), so a shared link has none.
 */
export function useAssessmentProfile(id: string): ProfileFormValues | null {
  const draft = useProfileDraft()
  return draft?.assessmentId === id && draft.values ? draft.values : null
}
