import type { CountryCode, ExtractedProfile, GermanLevel } from "@/api/types"

import { cefrLevels, educationLevels, emptyProfile, type ProfileFormValues } from "./schema"

/** Extracted values below this confidence get a "Check this" tag (DATA-MODEL.md). */
export const lowConfidence = 0.7

const countries: CountryCode[] = ["CA", "DE", "AU"]

export interface Prefill {
  values: ProfileFormValues
  /** Form field paths filled from the resume with confidence below 0.7. */
  checkThis: string[]
  /** Form field paths filled from the resume at all. */
  fromResume: string[]
}

/** Starts the form from what the resume parser found. Nothing here is a guess the user can't see and change. */
export function prefillFromResume(extracted: ExtractedProfile, today = new Date()): Prefill {
  const values = structuredClone(emptyProfile)
  const fromResume: string[] = []
  const checkThis: string[] = []
  const mark = (path: string, confidence: number) => {
    fromResume.push(path)
    if (confidence < lowConfidence) checkThis.push(path)
  }

  // Highest education level; the field of study comes from the same entry.
  const rank = (level: string) => educationLevels.findIndex((l) => l.value === level)
  const highest = extracted.education
    .filter((e) => rank(e.level) >= 0)
    .sort((a, b) => rank(b.level) - rank(a.level))[0]
  if (highest) {
    values.education.level = highest.level
    mark("education.level", highest.confidence)
    if (highest.field) {
      values.education.field = highest.field
      mark("education.field", highest.confidence)
    }
  }

  const years = extracted.totalSkilledYears
  if (years.value !== null) {
    values.work.yearsSkilled = String(Math.round(years.value * 10) / 10)
    mark("work.yearsSkilled", years.confidence)
  }

  // Skilled years in each target country, summed from the dated jobs there.
  for (const country of countries) {
    const jobs = extracted.work.filter((w) => w.isSkilled && w.country === country && w.startDate)
    const months = jobs.reduce((sum, w) => sum + monthsBetween(w.startDate!, w.endDate, today), 0)
    if (months > 0) {
      values.work.yearsInCountry[country] = String(Math.round((months / 12) * 10) / 10)
      mark(`work.yearsInCountry.${country}`, Math.min(...jobs.map((w) => w.confidence)))
    }
  }

  // Best suggestion per system (the API only returns published codes).
  for (const suggestion of [...extracted.occupationSuggestions].sort((a, b) => b.confidence - a.confidence)) {
    const system = suggestion.system as keyof ProfileFormValues["occupation"]
    if (system in values.occupation && values.occupation[system].code === "") {
      values.occupation[system] = { code: suggestion.code, title: suggestion.title }
      mark(`occupation.${system}`, suggestion.confidence)
    }
  }

  const german = extracted.languagesMentioned.find((l) => /^(german|deutsch)$/i.test(l.language.trim()))
  const germanLevel = german?.level?.trim().toUpperCase().slice(0, 2) as GermanLevel | undefined
  if (german && germanLevel && cefrLevels.includes(germanLevel)) {
    values.german = germanLevel
    mark("german", german.confidence)
  }

  return { values, checkThis, fromResume }
}

/** Whole months from `start` (YYYY-MM or YYYY) to `end` or today. */
function monthsBetween(start: string, end: string | null, today: Date) {
  const parse = (value: string) => {
    const [year, month = "1"] = value.split("-")
    return Number(year) * 12 + Number(month) - 1
  }
  const to = end ? parse(end) : today.getFullYear() * 12 + today.getMonth()
  const months = to - parse(start)
  return Number.isFinite(months) && months > 0 ? months : 0
}
