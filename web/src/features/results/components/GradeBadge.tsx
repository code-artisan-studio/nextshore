import type { Grade } from "@/api/types"
import { cn } from "@/lib/utils"

import { gradeLabels } from "../format"

// Grade colours mean only grades: tinted background + dark ink of the same family (docs/UI.md).
const styles: Record<Grade, string> = {
  eligible: "bg-eligible-bg text-eligible-ink",
  near_eligible: "bg-near-bg text-near-ink",
  not_eligible: "bg-not-bg text-not-ink",
}

export function GradeBadge({ grade, className }: { grade: string; className?: string }) {
  const known = grade in styles ? (grade as Grade) : "not_eligible"
  return (
    <span
      className={cn(
        "inline-flex h-6 shrink-0 items-center rounded-full px-2.5 text-xs font-semibold whitespace-nowrap",
        styles[known],
        className,
      )}
    >
      {gradeLabels[known]}
    </span>
  )
}
