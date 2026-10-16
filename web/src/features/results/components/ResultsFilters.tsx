import type { Grade, PathwayResult } from "@/api/types"
import { cn } from "@/lib/utils"

import { countryOf, gradeLabels, gradeOrder, type Filters } from "../format"

/** docs/UI.md → filters by country and grade. Only values present in the results are offered. */
export function ResultsFilters({
  results,
  filters,
  onChange,
}: {
  results: PathwayResult[]
  filters: Filters
  onChange: (filters: Filters) => void
}) {
  const countries = [...new Set(results.map((r) => r.countryCode))]
  const grades = gradeOrder.filter((g) => results.some((r) => r.grade === g))

  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-center sm:gap-6">
      <FilterGroup
        label="Country"
        value={filters.country}
        options={countries.map((c) => ({ value: c, label: `${countryOf(c).flag} ${countryOf(c).name}` }))}
        onChange={(country) => onChange({ ...filters, country })}
      />
      <FilterGroup
        label="Grade"
        value={filters.grade}
        options={grades.map((g: Grade) => ({ value: g, label: gradeLabels[g] }))}
        onChange={(grade) => onChange({ ...filters, grade })}
      />
    </div>
  )
}

function FilterGroup({
  label,
  value,
  options,
  onChange,
}: {
  label: string
  value: string
  options: { value: string; label: string }[]
  onChange: (value: string) => void
}) {
  return (
    <div role="group" aria-label={label} className="flex flex-wrap items-center gap-2">
      <span className="mr-1 text-sm font-medium text-navy">{label}</span>
      {[{ value: "", label: "All" }, ...options].map((option) => (
        <button
          key={option.value}
          type="button"
          aria-pressed={value === option.value}
          onClick={() => onChange(option.value)}
          className={cn(
            "h-8 rounded-full border px-3 text-sm transition-colors",
            value === option.value ? "border-teal bg-mist font-medium text-navy" : "bg-card hover:border-teal",
          )}
        >
          {option.label}
        </button>
      ))}
    </div>
  )
}
