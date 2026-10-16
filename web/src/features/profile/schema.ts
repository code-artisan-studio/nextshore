import { z } from "zod"

import type { EducationLevel, EnglishTest, GermanLevel, Goal, OccupationSystem } from "@/api/types"

// Form model for the profile stepper. Inputs hold strings (an empty string = not answered) and are converted to
// the API's CandidateProfile in toCandidateProfile.ts. The rules mirror the API validator (docs/API.md), so most
// mistakes are caught before submit; the API stays the authority.

export const goals: { value: Goal; label: string; text: string }[] = [
  { value: "study", label: "Study", text: "A degree abroad, then work and maybe stay." },
  { value: "work", label: "Work", text: "A job or work visa in a new country." },
  { value: "pr", label: "Permanent residence", text: "Settle for good, directly or step by step." },
]

export const educationLevels: { value: EducationLevel; label: string }[] = [
  { value: "secondary", label: "Secondary school" },
  { value: "diploma", label: "Diploma or certificate" },
  { value: "bachelors", label: "Bachelor's degree" },
  { value: "masters", label: "Master's degree" },
  { value: "phd", label: "PhD" },
]

export const englishTests: { value: EnglishTest; label: string }[] = [
  { value: "ielts", label: "IELTS" },
  { value: "pte", label: "PTE" },
  { value: "toefl", label: "TOEFL iBT" },
  { value: "none", label: "Not taken yet" },
]

export const cefrLevels: GermanLevel[] = ["A1", "A2", "B1", "B2", "C1", "C2"]

export const scoreSkills = ["listening", "reading", "writing", "speaking"] as const

/** Per-skill score range for each test (same as the API). */
export const scoreRanges: Record<Exclude<EnglishTest, "none">, { min: number; max: number; step: number }> = {
  ielts: { min: 0, max: 9, step: 0.5 },
  pte: { min: 10, max: 90, step: 1 },
  toefl: { min: 0, max: 30, step: 1 },
}

export const occupationSystems: { system: OccupationSystem; country: string; label: string }[] = [
  { system: "NOC", country: "Canada", label: "NOC 2021" },
  { system: "ISCO", country: "Germany", label: "ISCO-08" },
  { system: "ANZSCO", country: "Australia", label: "ANZSCO" },
]

const oneOf = (values: readonly string[], message: string) =>
  z.string().refine((value) => values.includes(value), message)

/** A number typed into a text field: optional unless `required`, within [min, max], a multiple of `step`. */
function numberText({ min, max, step, required = false }: { min: number; max?: number; step?: number; required?: boolean }) {
  return z.string().superRefine((raw, ctx) => {
    const text = raw.trim()
    if (text === "") {
      if (required) ctx.addIssue({ code: "custom", message: "Required." })
      return
    }
    const value = Number(text)
    if (!Number.isFinite(value)) {
      ctx.addIssue({ code: "custom", message: "Enter a number." })
    } else if (value < min || (max !== undefined && value > max)) {
      ctx.addIssue({ code: "custom", message: max === undefined ? `Must be ${min} or more.` : `Must be from ${min} to ${max}.` })
    } else if (step !== undefined && !Number.isInteger(Math.round(((value - min) / step) * 1e6) / 1e6)) {
      ctx.addIssue({ code: "custom", message: step === 1 ? "Use a whole number." : `Use steps of ${step}.` })
    }
  })
}

/** Same bounds as the API: born more than 15 and less than 100 years before today. */
export function isAllowedDateOfBirth(dateOfBirth: string, today = new Date()) {
  const shift = (years: number) =>
    new Date(Date.UTC(today.getFullYear() - years, today.getMonth(), today.getDate())).toISOString().slice(0, 10)
  return dateOfBirth < shift(15) && dateOfBirth > shift(100)
}

const occupationCode = z.object({ code: z.string(), title: z.string() })

const english = z
  .object({
    test: z.string().refine((value) => value !== "", "Choose a test, or “Not taken yet”."),
    testVariant: z.string(),
    scores: z.object({
      listening: z.string(),
      reading: z.string(),
      writing: z.string(),
      speaking: z.string(),
    }),
  })
  .superRefine((value, ctx) => {
    if ((value.test === "ielts" || value.test === "pte") && value.testVariant === "") {
      ctx.addIssue({ code: "custom", path: ["testVariant"], message: "Choose which version you took." })
    }
    if (value.test === "ielts" || value.test === "pte" || value.test === "toefl") {
      const range = scoreRanges[value.test]
      for (const skill of scoreSkills) {
        const result = numberText({ ...range, required: true }).safeParse(value.scores[skill])
        if (!result.success) {
          ctx.addIssue({ code: "custom", path: ["scores", skill], message: result.error.issues[0].message })
        }
      }
    }
  })

export const profileSchema = z.object({
  goal: oneOf(
    goals.map((g) => g.value),
    "Choose a goal.",
  ),

  dateOfBirth: z.string().superRefine((value, ctx) => {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) {
      ctx.addIssue({ code: "custom", message: "Enter your date of birth." })
    } else if (!isAllowedDateOfBirth(value)) {
      ctx.addIssue({ code: "custom", message: "You need to be between 15 and 100 years old." })
    }
  }),
  citizenship: z.string().regex(/^[A-Z]{2}$/, "Choose your citizenship."),
  maritalStatus: oneOf(["single", "married"], "Choose one."),
  spouse: z.object({
    educationLevel: z.string(),
    englishClb: numberText({ min: 1, max: 12, step: 1 }),
  }),

  education: z.object({
    level: oneOf(
      educationLevels.map((l) => l.value),
      "Choose your highest level.",
    ),
    field: z.string().max(200, "Keep it under 200 characters."),
    years: numberText({ min: 0, max: 15, step: 1 }),
  }),
  degreeRecognizedDE: z.enum(["", "yes", "no", "unsure"]),
  work: z.object({
    yearsSkilled: numberText({ min: 0, max: 60, required: true }),
    yearsInCountry: z.object({
      CA: numberText({ min: 0, max: 60 }),
      DE: numberText({ min: 0, max: 60 }),
      AU: numberText({ min: 0, max: 60 }),
    }),
  }),
  occupation: z.object({ NOC: occupationCode, ISCO: occupationCode, ANZSCO: occupationCode }),

  english,
  german: z.enum(["", ...cefrLevels]),
  frenchClb: numberText({ min: 1, max: 12, step: 1 }),

  fundsUsd: numberText({ min: 0 }),
  jobOffer: z.object({ CA: z.boolean(), DE: z.boolean(), AU: z.boolean(), salaryEur: numberText({ min: 0 }) }),
  relativeIn: z.object({ CA: z.boolean(), AU: z.boolean() }),
  student: z.object({
    gpaPercent: numberText({ min: 0, max: 100 }),
    tuitionBudgetUsd: numberText({ min: 0 }),
  }),
})

export type ProfileFormValues = z.infer<typeof profileSchema>

export const emptyProfile: ProfileFormValues = {
  goal: "",
  dateOfBirth: "",
  citizenship: "",
  maritalStatus: "",
  spouse: { educationLevel: "", englishClb: "" },
  education: { level: "", field: "", years: "" },
  degreeRecognizedDE: "",
  work: { yearsSkilled: "", yearsInCountry: { CA: "", DE: "", AU: "" } },
  occupation: {
    NOC: { code: "", title: "" },
    ISCO: { code: "", title: "" },
    ANZSCO: { code: "", title: "" },
  },
  english: { test: "", testVariant: "", scores: { listening: "", reading: "", writing: "", speaking: "" } },
  german: "",
  frenchClb: "",
  fundsUsd: "",
  jobOffer: { CA: false, DE: false, AU: false, salaryEur: "" },
  relativeIn: { CA: false, AU: false },
  student: { gpaPercent: "", tuitionBudgetUsd: "" },
}
