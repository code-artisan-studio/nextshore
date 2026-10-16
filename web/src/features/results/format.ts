import type { CountryCode, Gap, Grade, PathwayResult } from "@/api/types"

// Display text for engine output. The API returns profile field keys (docs/DATA-MODEL.md) and raw enum values;
// everything the user reads goes through here.

export const countries: Record<CountryCode, { name: string; flag: string }> = {
  CA: { name: "Canada", flag: "🇨🇦" },
  DE: { name: "Germany", flag: "🇩🇪" },
  AU: { name: "Australia", flag: "🇦🇺" },
}

export const countryOf = (code: string) => countries[code as CountryCode] ?? { name: code, flag: "" }

/** docs/UI.md → GradeBadge. */
export const gradeLabels: Record<Grade, string> = {
  eligible: "Eligible",
  near_eligible: "Near-eligible",
  not_eligible: "Not eligible",
}

export const gradeOrder: Grade[] = ["eligible", "near_eligible", "not_eligible"]

const skills: Record<string, string> = {
  listening: "listening",
  reading: "reading",
  writing: "writing",
  speaking: "speaking",
}

const fieldLabels: Record<string, string> = {
  age: "Age",
  "education.level": "Education",
  "education.years": "Years of study",
  "work.years_skilled": "Skilled work experience",
  "english.level.CLB": "English (CLB)",
  "english.level.CEFR": "English (CEFR)",
  "english.level.IELTS": "English (IELTS)",
  "french.level.CLB": "French (CLB)",
  "german.level.CEFR": "German (CEFR)",
  "funds.amount_usd": "Savings",
  marital_status: "Marital status",
  "spouse.education.level": "Partner's education",
  "spouse.english.level.CLB": "Partner's English (CLB)",
  "job_offer.salary_eur": "Salary in Germany",
  "student.gpa_percent": "Grade average",
  "student.tuition_budget_usd": "Tuition budget",
  "degree.recognized_DE": "Degree recognised in Germany",
}

/** "english.level.CLB.listening" → "English listening (CLB)", "work.years_in_country.CA" → "Work experience in Canada". */
export function factorLabel(key: string): string {
  if (fieldLabels[key]) return fieldLabels[key]
  const perSkill = key.match(/^english\.level\.(CLB|CEFR|IELTS)\.(\w+)$/)
  if (perSkill && skills[perSkill[2]]) return `English ${skills[perSkill[2]]} (${perSkill[1]})`
  const perCountry = key.match(/^(work\.years_in_country|job_offer|relative_in)\.([A-Z]{2})$/)
  if (perCountry) {
    const country = countryOf(perCountry[2]).name
    return { "work.years_in_country": `Work experience in ${country}`, job_offer: `Job offer in ${country}`, relative_in: `Relative in ${country}` }[
      perCountry[1]
    ]!
  }
  const occupation = key.match(/^occupation\.code\.(\w+)$/)
  if (occupation) return `Occupation (${occupation[1]})`
  const words = key.replace(/[._]/g, " ")
  return words[0].toUpperCase() + words.slice(1)
}

const valueLabels: Record<string, string> = {
  secondary: "Secondary school",
  diploma: "Diploma",
  bachelors: "Bachelor's",
  masters: "Master's",
  phd: "PhD",
  single: "Single",
  married: "Married",
}

/** Engine values are already readable ("CLB 8", "USD 20,000") apart from a few enums. */
export const formatValue = (value: string) => valueLabels[value] ?? value

/** Requirement labels can be a full rule ("Minimum gross annual salary (general threshold; …)"); chips use the field. */
export const shortGapLabel = (gap: Gap) => (gap.label.length <= 32 ? gap.label : factorLabel(gap.field))

export const pointsText = (points: number) => `+${points} pt${points === 1 ? "" : "s"}`

/** docs/UI.md → GapChip: "IELTS 6.5 → 7.0 · +24 pts", with a short label in front. */
export function gapText(gap: Gap) {
  const points = gap.impactPoints > 0 ? ` · ${pointsText(gap.impactPoints)}` : ""
  return `${shortGapLabel(gap)}: ${formatValue(gap.current)} → ${formatValue(gap.target)}${points}`
}

export function formatMonths(months: number) {
  if (months < 12) return `${months} month${months === 1 ? "" : "s"}`
  const years = Math.round((months / 12) * 10) / 10
  return `${years} year${years === 1 ? "" : "s"}`
}

export const formatDate = (iso: string) =>
  new Date(iso.length === 10 ? `${iso}T00:00:00` : iso).toLocaleDateString("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
  })

/** Same window as the API's Engine:StaleAfterDays (spec §9). */
export const staleAfterDays = 90

export function isStale(verifiedAt: string, today = new Date()) {
  const age = today.getTime() - new Date(`${verifiedAt}T00:00:00`).getTime()
  return age > staleAfterDays * 24 * 60 * 60 * 1000
}

export interface Filters {
  country: string
  grade: string
}

export const matchesFilters = (result: PathwayResult, { country, grade }: Filters) =>
  (!country || result.countryCode === country) && (!grade || result.grade === grade)
