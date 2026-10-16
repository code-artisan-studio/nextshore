import { countryName } from "@/features/profile/countries"
import { educationLevels, englishTests, goals, type ProfileFormValues } from "@/features/profile/schema"

/** docs/UI.md → profile snapshot chips in the results header. */
export function ProfileChips({ profile }: { profile: ProfileFormValues }) {
  const chips = [
    goals.find((g) => g.value === profile.goal)?.label,
    age(profile.dateOfBirth),
    countryName(profile.citizenship) && `${countryName(profile.citizenship)} citizen`,
    educationLevels.find((l) => l.value === profile.education.level)?.label,
    experience(profile.work.yearsSkilled),
    english(profile),
    profile.german && `German ${profile.german}`,
    profile.frenchClb && `French CLB ${profile.frenchClb}`,
  ].filter((chip): chip is string => !!chip)

  return (
    <ul className="flex flex-wrap gap-2" aria-label="Your profile">
      {chips.map((chip) => (
        <li key={chip} className="rounded-full border bg-card px-3 py-1 text-sm text-navy">
          {chip}
        </li>
      ))}
    </ul>
  )
}

function age(dateOfBirth: string) {
  if (!dateOfBirth) return null
  const dob = new Date(`${dateOfBirth}T00:00:00`)
  const today = new Date()
  let years = today.getFullYear() - dob.getFullYear()
  if (today < new Date(today.getFullYear(), dob.getMonth(), dob.getDate())) years--
  return `Age ${years}`
}

function experience(years: string) {
  if (years === "") return null
  const value = Number(years)
  return value === 0 ? "No work experience yet" : `${value} yr${value === 1 ? "" : "s"} experience`
}

function english({ english }: ProfileFormValues) {
  if (english.test === "none") return "No English test yet"
  const test = englishTests.find((t) => t.value === english.test)?.label
  const scores = Object.values(english.scores).filter((s) => s !== "").map(Number)
  if (!test || scores.length === 0) return null
  const [min, max] = [Math.min(...scores), Math.max(...scores)]
  return `${test} ${min === max ? min : `${min}–${max}`}`
}
