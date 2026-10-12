// Placeholder until T29 (Results page): lists the stored results so the upload → profile → submit flow can be checked.
import { useParams } from "react-router"

import { useAssessment } from "@/api/hooks"

export function ResultsPage() {
  const { id = "" } = useParams()
  const assessment = useAssessment(id)

  if (assessment.isPending) return <p>Loading your results…</p>
  if (assessment.isError) return <p>{assessment.error.message}</p>

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-3xl">Your results</h1>
      <ul className="flex flex-col gap-2">
        {assessment.data.results.map((r) => (
          <li key={r.pathwaySlug} className="flex justify-between rounded-card border bg-card px-4 py-3">
            <span className="font-medium text-navy">
              {r.countryCode} · {r.pathwayName}
            </span>
            <span>
              {r.grade} · Readiness {r.readinessScore}
            </span>
          </li>
        ))}
      </ul>
    </div>
  )
}
