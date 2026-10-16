import { ArrowDown, ArrowRight } from "lucide-react"
import { Fragment } from "react"
import { Link } from "react-router"

import type { ChainStep, PathwayChain, PathwayResult } from "@/api/types"
import { cn } from "@/lib/utils"

import { countryOf, formatMonths } from "../format"
import { CountryFlag } from "./CountryFlag"
import { GradeBadge } from "./GradeBadge"

const stepTypes: Record<string, string> = {
  study: "Study",
  post_study_work: "Post-study work",
  work: "Work",
  pr: "Permanent residence",
}

/**
 * docs/UI.md → per-country timeline: step → step → step with grade badges, durations and the total. Projected steps
 * (graded as if the earlier steps were done) are dashed.
 */
export function ChainTimeline({
  chain,
  results,
  hrefFor,
}: {
  chain: PathwayChain
  results: PathwayResult[]
  hrefFor: (slug: string) => string
}) {
  const country = countryOf(chain.countryCode)
  const nameOf = (slug: string) => results.find((r) => r.pathwaySlug === slug)?.pathwayName ?? slug

  return (
    <article className="flex flex-col gap-4 rounded-card border bg-card p-5">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="flex items-center gap-2 text-lg">
          <CountryFlag code={chain.countryCode} />
          {country.name}
        </h3>
        <p className="text-sm font-medium text-navy">{totalText(chain)}</p>
      </div>

      <ol className="flex flex-col items-stretch gap-2 md:flex-row md:items-stretch">
        {chain.steps.map((step, index) => (
          <Fragment key={step.pathwaySlug}>
            {index > 0 && (
              <li aria-hidden="true" className="flex items-center justify-center text-teal md:px-1">
                <ArrowDown className="size-4 md:hidden" />
                <ArrowRight className="hidden size-4 md:block" />
              </li>
            )}
            <li className="flex-1">
              <StepCard step={step} name={nameOf(step.pathwaySlug)} href={hrefFor(step.pathwaySlug)} />
            </li>
          </Fragment>
        ))}
      </ol>
    </article>
  )
}

function StepCard({ step, name, href }: { step: ChainStep; name: string; href: string }) {
  return (
    <Link
      to={href}
      className={cn(
        "flex h-full flex-col gap-2 rounded-lg border-2 p-3 transition-colors hover:border-teal",
        step.isProjected ? "border-dashed border-line bg-sand" : "border-solid border-line bg-card",
      )}
    >
      <div className="flex items-center justify-between gap-2">
        <span className="text-xs font-medium tracking-wide text-teal uppercase">{stepTypes[step.type] ?? step.type}</span>
        {step.isProjected && <span className="text-xs">Projected</span>}
      </div>
      <span className="leading-snug font-medium text-navy">{name}</span>
      <div className="mt-auto flex flex-wrap items-center gap-2">
        <GradeBadge grade={step.grade} />
        <span className="text-sm">Readiness {step.readinessScore}</span>
      </div>
      <span className="text-sm">{durationText(step)}</span>
    </Link>
  )
}

function durationText(step: ChainStep) {
  if (step.durationMonths !== null) return formatMonths(step.durationMonths)
  if (step.type === "study") return "Length of your course"
  if (step.type === "pr") return "Permanent"
  return "Length varies"
}

/** "About 4.5 years in total", or what is known when a step (usually the course) has no set length. */
function totalText(chain: PathwayChain) {
  if (chain.totalMonths !== null) return `About ${formatMonths(chain.totalMonths)} in total`
  const known = chain.steps.reduce((sum, s) => sum + (s.durationMonths ?? 0), 0)
  const courseUnknown = chain.steps.some((s) => s.type === "study" && s.durationMonths === null)
  if (known === 0) return "Total time depends on your course"
  return courseUnknown ? `Your course + about ${formatMonths(known)}` : `At least ${formatMonths(known)}`
}

export function ChainsSection({
  chains,
  results,
  hrefFor,
}: {
  chains: PathwayChain[]
  results: PathwayResult[]
  hrefFor: (slug: string) => string
}) {
  if (chains.length === 0) return null
  return (
    <section aria-labelledby="chains-heading" className="flex flex-col gap-5">
      <div className="flex flex-col gap-1">
        <h2 id="chains-heading" className="text-2xl">
          From study to staying
        </h2>
        <p className="text-sm">
          Each chain shows how study can lead to work and residence. Dashed steps are projected: they're graded as if
          you had completed the steps before them, so their readiness can differ from today's.
        </p>
      </div>
      <div className="flex flex-col gap-4">
        {chains.map((chain) => (
          <ChainTimeline key={chain.countryCode} chain={chain} results={results} hrefFor={hrefFor} />
        ))}
      </div>
    </section>
  )
}
