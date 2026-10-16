import type { ReactNode } from "react"
import { ArrowLeft, Check, Clock, X } from "lucide-react"
import { Link, useParams } from "react-router"

import { ApiError } from "@/api/client"
import { useAssessment, useCountries } from "@/api/hooks"
import type { Gap, PathwayResult, Points, Requirement } from "@/api/types"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

import { CountryFlag } from "./components/CountryFlag"
import { GradeBadge } from "./components/GradeBadge"
import { ReadinessBar } from "./components/ReadinessBar"
import { SourceLink } from "./components/SourceLink"
import { countryOf, factorLabel, formatMonths, formatValue, gradeLabels, pointsText, staleAfterDays } from "./format"

const fixabilityLabels: Record<string, string> = {
  easy: "Quick to fix",
  medium: "Takes some time",
  hard: "Hard to change",
  impossible: "Can't be changed",
}

/** docs/UI.md → requirements checklist, points breakdown, gaps, sources. Shows the saved results (not what-if). */
export function PathwayDetailPage() {
  const { id = "", pathwaySlug = "" } = useParams()
  const assessment = useAssessment(id)
  const countries = useCountries()
  const back = `/results/${id}`

  if (assessment.isPending) {
    return <div className="h-96 animate-pulse rounded-card bg-mist" aria-busy="true" aria-label="Loading" />
  }
  const result = assessment.data?.results.find((r) => r.pathwaySlug === pathwaySlug)
  if (!result) {
    const expired = assessment.error instanceof ApiError && assessment.error.status === 404
    return (
      <div className="mx-auto flex max-w-xl flex-col items-start gap-4 py-10">
        <h1 className="text-3xl">This pathway isn't available</h1>
        <p>
          {expired
            ? "The link may be mistyped, or the results have expired. Results are kept for 30 days."
            : assessment.isError
              ? "We couldn't load your results. Try again in a moment."
              : "These results don't include this pathway."}
        </p>
        <Button asChild variant="outline" size="lg" className="h-10">
          <Link to={assessment.isSuccess ? back : "/start"}>{assessment.isSuccess ? "Back to all results" : "Check my options"}</Link>
        </Button>
      </div>
    )
  }

  const summary = countries.data
    ?.find((c) => c.code === result.countryCode)
    ?.pathways.find((p) => p.slug === result.pathwaySlug)?.summary
  const mandatory = result.requirements.filter((r) => r.isMandatory)
  const advisory = result.requirements.filter((r) => !r.isMandatory)

  return (
    <div className="mx-auto flex max-w-4xl flex-col gap-10">
      <div className="flex flex-col gap-5">
        <Link to={back} className="inline-flex items-center gap-1 self-start text-sm font-medium text-teal underline-offset-4 hover:underline">
          <ArrowLeft className="size-4" aria-hidden="true" />
          All results
        </Link>
        <header className="flex flex-col gap-4 rounded-card border bg-card p-5 sm:p-6">
          <div className="flex items-start justify-between gap-4">
            <div className="flex items-start gap-3">
              <CountryFlag code={result.countryCode} className="mt-1 text-3xl" />
              <div className="flex flex-col gap-1">
                <p className="text-sm">{countryOf(result.countryCode).name}</p>
                <h1 className="text-2xl sm:text-3xl">{result.pathwayName}</h1>
                {summary && <p>{summary}</p>}
              </div>
            </div>
            <GradeBadge grade={result.grade} />
          </div>
          <ReadinessBar score={result.readinessScore} className="max-w-md" />
        </header>
      </div>

      <Section title="Requirements" intro="What this pathway asks for, compared with your answers.">
        <RequirementList requirements={mandatory} />
        {advisory.length > 0 && (
          <div className="flex flex-col gap-2">
            <h3 className="text-base">Good to know</h3>
            <p className="text-sm">These don't affect your grade or readiness, but they can matter in practice.</p>
            <RequirementList requirements={advisory} />
          </div>
        )}
      </Section>

      {result.points && <PointsTable points={result.points} />}

      <Section
        title="What's missing"
        intro={
          result.gaps.length > 0
            ? "Ordered by how much each change helps for the time it takes."
            : "Nothing: you meet every requirement we check for this pathway."
        }
      >
        {result.gaps.length > 0 && (
          <ol className="flex flex-col gap-3">
            {result.gaps.map((gap) => (
              <GapItem key={`${gap.field}-${gap.requirementId ?? ""}`} gap={gap} />
            ))}
          </ol>
        )}
      </Section>

      <Sources result={result} />
    </div>
  )
}

function Section({ title, intro, children }: { title: string; intro?: string; children?: ReactNode }) {
  return (
    <section className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <h2 className="text-xl">{title}</h2>
        {intro && <p className="text-sm">{intro}</p>}
      </div>
      {children}
    </section>
  )
}

/** docs/UI.md → checklist with ✓/✗ and current vs target. */
function RequirementList({ requirements }: { requirements: Requirement[] }) {
  if (requirements.length === 0) {
    return <p className="rounded-card border bg-card p-4 text-sm">No requirements are published for this pathway yet.</p>
  }
  return (
    <ul className="flex flex-col divide-y rounded-card border bg-card">
      {requirements.map((req) => (
        <li key={req.id} className="flex items-start gap-3 p-4">
          <span
            className={cn(
              "mt-0.5 flex size-5 shrink-0 items-center justify-center rounded-full",
              req.met ? "bg-mist text-teal" : "bg-coral-soft text-coral-ink",
            )}
          >
            {req.met ? <Check className="size-3.5" aria-hidden="true" /> : <X className="size-3.5" aria-hidden="true" />}
            <span className="sr-only">{req.met ? "Met:" : "Not met:"}</span>
          </span>
          <div className="flex min-w-0 flex-col gap-1">
            <p className="font-medium text-navy">{req.label}</p>
            <p className="text-sm">
              You: <span className="font-medium text-navy">{formatValue(req.current)}</span>
              <span aria-hidden="true"> · </span>
              <span className="sr-only">, </span>
              Needed: <span className="font-medium text-navy">{formatValue(req.target)}</span>
            </p>
          </div>
        </li>
      ))}
    </ul>
  )
}

/** docs/UI.md → factor · your value · points. */
function PointsTable({ points }: { points: Points }) {
  const targetLabel = points.targetKind === "cutoff" ? "Latest cutoff" : "Pass mark"
  return (
    <Section
      title="Points"
      intro={
        points.target !== null
          ? `${points.total} points against a ${targetLabel.toLowerCase()} of ${points.target}.`
          : `${points.total} points. No ${targetLabel.toLowerCase()} is published yet, so points don't limit your grade.`
      }
    >
      <div className="overflow-x-auto rounded-card border bg-card">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b bg-mist text-left text-navy">
              <th scope="col" className="px-4 py-2.5 font-medium">
                Factor
              </th>
              <th scope="col" className="px-4 py-2.5 font-medium">
                Your value
              </th>
              <th scope="col" className="px-4 py-2.5 text-right font-medium">
                Points
              </th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {points.breakdown.map((row) => (
              <tr key={row.factor}>
                <th scope="row" className="px-4 py-2.5 text-left font-normal">
                  {factorLabel(row.factor)}
                </th>
                <td className="px-4 py-2.5">{formatValue(row.current)}</td>
                <td className="px-4 py-2.5 text-right tabular-nums">{row.points}</td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr className="border-t font-medium text-navy">
              <th scope="row" colSpan={2} className="px-4 py-2.5 text-left">
                Total
              </th>
              <td className="px-4 py-2.5 text-right tabular-nums">{points.total}</td>
            </tr>
            {points.target !== null && (
              <tr className="border-t">
                <th scope="row" colSpan={2} className="px-4 py-2.5 text-left font-normal">
                  {targetLabel}
                </th>
                <td className="px-4 py-2.5 text-right tabular-nums">{points.target}</td>
              </tr>
            )}
          </tfoot>
        </table>
      </div>
    </Section>
  )
}

function GapItem({ gap }: { gap: Gap }) {
  return (
    <li className="flex flex-col gap-2 rounded-card border bg-card p-4">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <p className="font-medium text-navy">{gap.label}</p>
        {gap.impactPoints > 0 && (
          <span className="rounded-full bg-coral-soft px-2.5 py-0.5 text-sm text-coral-ink">{pointsText(gap.impactPoints)}</span>
        )}
      </div>
      <p className="text-sm">
        {formatValue(gap.current)} → <span className="font-medium text-navy">{formatValue(gap.target)}</span>
      </p>
      <p className="text-sm">{gap.fixHint}</p>
      <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm">
        <span>{fixabilityLabels[gap.fixability] ?? gap.fixability}</span>
        {gap.fixMonths !== null && gap.fixability !== "impossible" && (
          <span className="inline-flex items-center gap-1">
            <Clock className="size-3.5" aria-hidden="true" />
            About {formatMonths(gap.fixMonths)}
          </span>
        )}
        {gap.unlocksGrade && (
          <span>
            Fixing only this: <span className="font-medium text-navy">{gradeLabels[gap.unlocksGrade as keyof typeof gradeLabels]}</span>
          </span>
        )}
      </div>
    </li>
  )
}

function Sources({ result }: { result: PathwayResult }) {
  const unique = result.sources.filter(
    (source, index, all) => all.findIndex((s) => s.url === source.url && s.label === source.label) === index,
  )
  return (
    <Section title="Sources" intro="Every rule comes from an official source. Always check it before you apply.">
      {result.staleRules > 0 && (
        <p className="rounded-card bg-coral-soft px-4 py-3 text-sm text-coral-ink">
          <span aria-hidden="true">⚠️ </span>
          {result.staleRules} rule{result.staleRules === 1 ? " for this pathway hasn't" : "s for this pathway haven't"} been
          re-checked in over {staleAfterDays} days.
        </p>
      )}
      <ul className="flex flex-col gap-4 rounded-card border bg-card p-4">
        {unique.map((source) => (
          <li key={`${source.url}-${source.label}`}>
            <SourceLink source={source} />
          </li>
        ))}
      </ul>
    </Section>
  )
}
