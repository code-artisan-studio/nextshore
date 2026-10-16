import { ExternalLink } from "lucide-react"

import type { Source } from "@/api/types"

import { formatDate, isStale, staleAfterDays } from "../format"

/** docs/UI.md → label + "Verified on" date + ⚠️ when not re-verified within the stale window. */
export function SourceLink({ source }: { source: Source }) {
  const stale = isStale(source.verifiedAt)
  return (
    <div className="flex flex-col gap-0.5">
      <a
        href={source.url}
        target="_blank"
        rel="noopener noreferrer"
        className="inline-flex items-center gap-1.5 font-medium text-teal underline-offset-4 hover:underline"
      >
        {source.label}
        <ExternalLink className="size-3.5 shrink-0" aria-hidden="true" />
        <span className="sr-only">(opens in a new tab)</span>
      </a>
      <p className="flex items-center gap-1.5 text-sm">
        Verified on {formatDate(source.verifiedAt)}
        {stale && (
          <span className="text-coral-ink">
            <span aria-hidden="true">⚠️ </span>
            Not re-checked in over {staleAfterDays} days
          </span>
        )}
      </p>
    </div>
  )
}
