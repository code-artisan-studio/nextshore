import { useState } from "react"
import { ArrowRight } from "lucide-react"
import { Link } from "react-router"

import type { PathwayResult } from "@/api/types"
import { cn } from "@/lib/utils"

import { countryOf, gradeLabels } from "../format"
import { CountryFlag } from "./CountryFlag"
import { GapChip } from "./GapChip"
import { GradeBadge } from "./GradeBadge"
import { ReadinessBar } from "./ReadinessBar"

/**
 * docs/UI.md → country flag, name, GradeBadge, ReadinessBar, top 2 GapChips, "See full breakdown". In what-if mode
 * `savedGrade` is the grade before the user's changes, and the card pulses coral whenever its grade changes.
 */
export function PathwayCard({
  result,
  href,
  savedGrade,
  className,
}: {
  result: PathwayResult
  href: string
  savedGrade?: string
  className?: string
}) {
  const topGaps = result.gaps.slice(0, 2)
  const flips = useGradeFlips(result.grade)
  const changed = savedGrade !== undefined && savedGrade !== result.grade

  return (
    <article className={cn("relative flex flex-col gap-4 rounded-card border bg-card p-5", className)}>
      {flips > 0 && (
        <span
          key={flips}
          aria-hidden="true"
          className="pointer-events-none absolute -inset-px rounded-card ring-4 ring-coral motion-safe:animate-coral-pulse motion-reduce:hidden"
        />
      )}
      <div className="flex items-start justify-between gap-3">
        <div className="flex min-w-0 items-start gap-3">
          <CountryFlag code={result.countryCode} className="mt-0.5 text-2xl" />
          <div className="min-w-0">
            <p className="text-sm">{countryOf(result.countryCode).name}</p>
            <h3 className="text-lg leading-snug">{result.pathwayName}</h3>
          </div>
        </div>
        <GradeBadge grade={result.grade} />
      </div>

      {changed && (
        <p className="-mt-2 text-sm">
          Was {gradeLabels[savedGrade as keyof typeof gradeLabels]} with your answers
        </p>
      )}

      <ReadinessBar score={result.readinessScore} />

      {topGaps.length > 0 ? (
        <div className="flex flex-col gap-2">
          <p className="text-sm font-medium text-navy">What would help most</p>
          <div className="flex flex-wrap gap-2">
            {topGaps.map((gap) => (
              <GapChip key={`${gap.field}-${gap.requirementId ?? ""}`} gap={gap} />
            ))}
          </div>
        </div>
      ) : (
        <p className="text-sm">You meet every requirement we check for this pathway.</p>
      )}

      <Link
        to={href}
        className="mt-auto inline-flex items-center gap-1 self-start text-sm font-medium text-teal underline-offset-4 hover:underline"
      >
        See full breakdown
        <ArrowRight className="size-4" aria-hidden="true" />
      </Link>
    </article>
  )
}

/** Counts grade changes across renders (React's "store information from previous renders" pattern). */
function useGradeFlips(grade: string) {
  const [previous, setPrevious] = useState(grade)
  const [flips, setFlips] = useState(0)
  if (grade !== previous) {
    setPrevious(grade)
    setFlips(flips + 1)
  }
  return flips
}
