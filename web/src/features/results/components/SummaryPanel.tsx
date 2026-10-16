import { Sparkles } from "lucide-react"

import { useAssessmentSummary } from "@/api/hooks"

/** docs/UI.md → AI summary on a mist panel, loading after the cards with a skeleton. */
export function SummaryPanel({ assessmentId }: { assessmentId: string }) {
  const summary = useAssessmentSummary(assessmentId)

  return (
    <section aria-labelledby="summary-heading" className="flex flex-col gap-3 rounded-card bg-mist p-5 sm:p-6">
      <div className="flex items-center gap-2">
        <Sparkles className="size-4 text-teal" aria-hidden="true" />
        <h2 id="summary-heading" className="text-lg">
          Your summary
        </h2>
      </div>

      {summary.isPending && (
        <div className="flex flex-col gap-2" aria-busy="true" aria-label="Writing your summary">
          <div className="h-3.5 w-full animate-pulse rounded bg-line" />
          <div className="h-3.5 w-11/12 animate-pulse rounded bg-line" />
          <div className="h-3.5 w-4/5 animate-pulse rounded bg-line" />
          <div className="h-3.5 w-2/3 animate-pulse rounded bg-line" />
        </div>
      )}

      {summary.isError && (
        <p>The written summary isn't available right now. Your results below are complete and don't depend on it.</p>
      )}

      {summary.isSuccess && (
        <>
          <p className="leading-relaxed whitespace-pre-line">{summary.data.summary}</p>
          <p className="text-xs">Written by AI from the results below. It doesn't change any grade or score.</p>
        </>
      )}
    </section>
  )
}
