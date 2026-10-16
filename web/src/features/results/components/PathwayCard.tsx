import { ArrowRight } from "lucide-react"
import { Link } from "react-router"

import type { PathwayResult } from "@/api/types"
import { cn } from "@/lib/utils"

import { countryOf } from "../format"
import { CountryFlag } from "./CountryFlag"
import { GapChip } from "./GapChip"
import { GradeBadge } from "./GradeBadge"
import { ReadinessBar } from "./ReadinessBar"

/** docs/UI.md → country flag, name, GradeBadge, ReadinessBar, top 2 GapChips, "See full breakdown". */
export function PathwayCard({ result, href, className }: { result: PathwayResult; href: string; className?: string }) {
  const topGaps = result.gaps.slice(0, 2)
  return (
    <article className={cn("flex flex-col gap-4 rounded-card border bg-card p-5", className)}>
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
