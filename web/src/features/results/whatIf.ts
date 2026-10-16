import type { CandidateProfile, CountryCode, EnglishTest, GermanLevel } from "@/api/types"
import { cefrLevels, scoreRanges } from "@/features/profile/schema"

/** What the user changed in the what-if panel. Anything left undefined keeps the submitted answer. */
export interface WhatIf {
  englishOverall?: number
  yearsSkilled?: number
  german?: GermanLevel | ""
  jobOffer?: Partial<Record<CountryCode, boolean>>
}

export const hasChanges = (w: WhatIf) => Object.values(w).some((v) => v !== undefined)

type TakenTest = Exclude<EnglishTest, "none">

/** Slider bounds for "English overall". No test yet → try IELTS General Training (accepted by all three countries). */
export function englishScale(base: CandidateProfile) {
  const taken = base.english?.test && base.english.test !== "none" ? (base.english.test as TakenTest) : null
  const test: TakenTest = taken ?? "ielts"
  const { max, step } = scoreRanges[test]
  // The lower ends below these aren't useful to explore.
  const min = { ielts: 4, pte: 30, toefl: 10 }[test]
  const scores = Object.values(base.english?.scores ?? {}).filter((s): s is number => typeof s === "number")
  const average = scores.length ? scores.reduce((a, b) => a + b, 0) / scores.length : null
  const current = average === null ? null : Math.round(average / step) * step
  const label = { ielts: "IELTS", pte: "PTE", toefl: "TOEFL iBT" }[test]
  return { test, taken: taken !== null, min, max, step, current, label }
}

export const germanSteps: (GermanLevel | "")[] = ["", ...cefrLevels]

export function applyWhatIf(base: CandidateProfile, w: WhatIf): CandidateProfile {
  const next: CandidateProfile = structuredClone(base)

  if (w.englishOverall !== undefined) {
    const scale = englishScale(base)
    const score = w.englishOverall
    next.english = {
      test: scale.test,
      testVariant: scale.test === "toefl" ? null : scale.taken ? (base.english?.testVariant ?? "general") : "general",
      scores: { listening: score, reading: score, writing: score, speaking: score },
    }
  }
  if (w.yearsSkilled !== undefined) {
    next.work = { ...next.work, yearsSkilled: w.yearsSkilled }
  }
  if (w.german !== undefined) {
    next.german = w.german ? { level: w.german } : null
  }
  if (w.jobOffer) {
    next.jobOffer = { CA: false, DE: false, AU: false, salaryEur: null, ...next.jobOffer, ...w.jobOffer }
  }
  return next
}
