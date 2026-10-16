import { useMemo, useState } from "react"
import { SlidersHorizontal } from "lucide-react"
import { Link, useParams, useSearchParams } from "react-router"

import { ApiError } from "@/api/client"
import { useAssessment, useEvaluation } from "@/api/hooks"
import type { PathwayResult } from "@/api/types"
import { Button } from "@/components/ui/button"
import { useAssessmentProfile } from "@/features/profile/draft"
import { toCandidateProfile } from "@/features/profile/toCandidateProfile"
import { useDebounced } from "@/lib/useDebounced"

import { ChainsSection } from "./components/ChainTimeline"
import { CopyLinkButton } from "./components/CopyLinkButton"
import { CountryFlag } from "./components/CountryFlag"
import { GradeBadge } from "./components/GradeBadge"
import { PathwayCard } from "./components/PathwayCard"
import { ProfileChips } from "./components/ProfileChips"
import { ResultsFilters } from "./components/ResultsFilters"
import { SummaryPanel } from "./components/SummaryPanel"
import { WhatIfPanel } from "./components/WhatIfPanel"
import { countryOf, matchesFilters, type Filters } from "./format"
import { applyWhatIf, hasChanges, type WhatIf } from "./whatIf"

export function ResultsPage() {
  const { id = "" } = useParams()
  const assessment = useAssessment(id)
  const profile = useAssessmentProfile(id)
  const [params, setParams] = useSearchParams()
  const filters: Filters = { country: params.get("country") ?? "", grade: params.get("grade") ?? "" }

  // What-if: the submitted answers (this tab only) with the panel's changes, evaluated 400 ms after the last change.
  const [whatIfOpen, setWhatIfOpen] = useState(false)
  const [whatIf, setWhatIf] = useState<WhatIf>({})
  const base = useMemo(() => (profile ? toCandidateProfile(profile) : null), [profile])
  const whatIfActive = base !== null && hasChanges(whatIf)
  const whatIfProfile = useMemo(() => (base && whatIfActive ? applyWhatIf(base, whatIf) : null), [base, whatIf, whatIfActive])
  const evaluation = useEvaluation(useDebounced(whatIfProfile, 400))

  if (assessment.isPending) return <ResultsSkeleton />
  if (assessment.isError) return <ResultsError notFound={assessment.error instanceof ApiError && assessment.error.status === 404} />

  const { expiresAt } = assessment.data
  const saved = assessment.data.results
  const live = whatIfActive && evaluation.data ? evaluation.data : assessment.data
  const { results, chains } = live
  const best = saved[0] as PathwayResult | undefined
  const shown = results.filter((r) => matchesFilters(r, filters))
  const savedGrade = (slug: string) => saved.find((r) => r.pathwaySlug === slug)?.grade

  const setFilters = (next: Filters) =>
    setParams(
      (current) => {
        for (const [key, value] of Object.entries(next)) {
          if (value) current.set(key, value)
          else current.delete(key)
        }
        return current
      },
      { replace: true },
    )

  return (
    <div className="flex flex-col gap-8">
      <header className="flex flex-col gap-5">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div className="flex flex-col gap-3">
            <h1 className="text-3xl">Your results</h1>
            {profile && <ProfileChips profile={profile} />}
          </div>
          <CopyLinkButton path={`/results/${id}`} expiresAt={expiresAt} />
        </div>

        {best && (
          <div className="flex flex-col gap-2 rounded-card border bg-card p-5 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex items-center gap-3">
              <CountryFlag code={best.countryCode} className="text-3xl" />
              <div>
                <p className="text-sm">Best match</p>
                <p className="font-heading text-lg font-semibold text-navy">
                  {countryOf(best.countryCode).name} · {best.pathwayName} · Readiness {best.readinessScore}
                </p>
              </div>
            </div>
            <GradeBadge grade={best.grade} className="self-start sm:self-center" />
          </div>
        )}
      </header>

      <SummaryPanel assessmentId={id} />

      <ChainsSection chains={chains} results={results} hrefFor={(slug) => `/results/${id}/${slug}`} />

      <section aria-labelledby="pathways-heading" className="flex flex-col gap-5">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
          <div className="flex flex-col gap-1">
            <h2 id="pathways-heading" className="text-2xl">
              Pathways ranked by readiness
            </h2>
            <p className="text-sm">
              Readiness shows how closely you meet each pathway's published requirements today. It isn't a prediction
              of any decision.
            </p>
          </div>
          {base ? (
            <Button size="lg" className="h-10 shrink-0 px-4" onClick={() => setWhatIfOpen(true)}>
              <SlidersHorizontal data-icon="inline-start" />
              Try what-if
            </Button>
          ) : (
            <p className="max-w-56 shrink-0 text-xs">What-if works in the browser tab where these results were created.</p>
          )}
        </div>

        {whatIfActive && (
          <div className="flex flex-col gap-2 rounded-card bg-coral-soft px-4 py-3 text-coral-ink sm:flex-row sm:items-center sm:justify-between">
            <p className="text-sm">
              <span className="font-medium">What-if view.</span> These results use your changes; your saved results
              and link haven't changed.
            </p>
            <div className="flex gap-2">
              <Button variant="outline" size="sm" onClick={() => setWhatIfOpen(true)}>
                Edit
              </Button>
              <Button variant="outline" size="sm" onClick={() => setWhatIf({})}>
                Reset
              </Button>
            </div>
          </div>
        )}

        <ResultsFilters results={results} filters={filters} onChange={setFilters} />

        {shown.length > 0 ? (
          <div className="grid gap-4 md:grid-cols-2">
            {shown.map((result) => (
              <PathwayCard
                key={result.pathwaySlug}
                result={result}
                href={`/results/${id}/${result.pathwaySlug}`}
                savedGrade={whatIfActive ? savedGrade(result.pathwaySlug) : undefined}
              />
            ))}
          </div>
        ) : (
          <div className="flex flex-col items-start gap-3 rounded-card border bg-card p-6">
            <p>No pathways match these filters.</p>
            <Button variant="outline" onClick={() => setFilters({ country: "", grade: "" })}>
              Show all pathways
            </Button>
          </div>
        )}
      </section>

      {base && (
        <WhatIfPanel
          open={whatIfOpen}
          onOpenChange={setWhatIfOpen}
          base={base}
          whatIf={whatIf}
          onChange={setWhatIf}
          saved={saved}
          live={results}
          updating={whatIfActive && evaluation.isFetching}
        />
      )}
    </div>
  )
}

function ResultsSkeleton() {
  return (
    <div className="flex flex-col gap-8" aria-busy="true" aria-label="Loading your results">
      <div className="h-9 w-56 animate-pulse rounded bg-line" />
      <div className="h-20 animate-pulse rounded-card bg-mist" />
      <div className="grid gap-4 md:grid-cols-2">
        {[0, 1, 2, 3].map((i) => (
          <div key={i} className="h-56 animate-pulse rounded-card border bg-card" />
        ))}
      </div>
    </div>
  )
}

function ResultsError({ notFound }: { notFound: boolean }) {
  return (
    <div className="mx-auto flex max-w-xl flex-col items-start gap-4 py-10">
      <h1 className="text-3xl">{notFound ? "These results aren't available" : "We couldn't load your results"}</h1>
      <p>
        {notFound
          ? "The link may be mistyped, or the results have expired. Results are kept for 30 days."
          : "Something went wrong on our side. Try again in a moment."}
      </p>
      <Button asChild size="lg" className="h-10">
        <Link to="/start">Check my options again</Link>
      </Button>
    </div>
  )
}
