import type { CandidateProfile, OccupationSystem } from "@/api/types"

import type { ProfileFormValues } from "./schema"

const num = (text: string) => (text.trim() === "" ? null : Number(text))

/** Converts validated form values to the `POST /assessments` body (docs/API.md → CandidateProfile). */
export function toCandidateProfile(v: ProfileFormValues): CandidateProfile {
  const yearsInCountry = Object.fromEntries(
    Object.entries(v.work.yearsInCountry).flatMap(([country, years]) => (num(years) === null ? [] : [[country, num(years)!]])),
  )
  const occupationCodes = Object.fromEntries(
    (Object.entries(v.occupation) as [OccupationSystem, { code: string }][]).flatMap(([system, o]) =>
      o.code.trim() === "" ? [] : [[system, o.code.trim()]],
    ),
  )
  const tookTest = v.english.test !== "none"

  return {
    goal: v.goal,
    dateOfBirth: v.dateOfBirth,
    citizenship: v.citizenship,
    education: { level: v.education.level, field: v.education.field.trim() || null, years: num(v.education.years) },
    work: { yearsSkilled: num(v.work.yearsSkilled), yearsInCountry },
    occupationCodes: Object.keys(occupationCodes).length > 0 ? occupationCodes : null,
    english: {
      test: v.english.test,
      testVariant: v.english.test === "ielts" || v.english.test === "pte" ? v.english.testVariant : null,
      scores: tookTest
        ? {
            listening: num(v.english.scores.listening),
            reading: num(v.english.scores.reading),
            writing: num(v.english.scores.writing),
            speaking: num(v.english.scores.speaking),
          }
        : null,
    },
    french: v.frenchClb ? { clb: num(v.frenchClb) } : null,
    german: v.german ? { level: v.german } : null,
    fundsUsd: num(v.fundsUsd),
    maritalStatus: v.maritalStatus,
    spouse:
      v.maritalStatus === "married"
        ? { educationLevel: v.spouse.educationLevel || null, englishClb: num(v.spouse.englishClb) }
        : null,
    jobOffer: {
      CA: v.jobOffer.CA,
      DE: v.jobOffer.DE,
      AU: v.jobOffer.AU,
      salaryEur: v.jobOffer.DE ? num(v.jobOffer.salaryEur) : null,
    },
    relativeIn: { CA: v.relativeIn.CA, AU: v.relativeIn.AU },
    student:
      v.goal === "study"
        ? { gpaPercent: num(v.student.gpaPercent), tuitionBudgetUsd: num(v.student.tuitionBudgetUsd) }
        : null,
    degreeRecognizedDE: v.degreeRecognizedDE === "yes" ? true : v.degreeRecognizedDE === "no" ? false : null,
  }
}
