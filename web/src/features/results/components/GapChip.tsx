import type { Gap } from "@/api/types"
import { cn } from "@/lib/utils"

import { gapText } from "../format"

/** docs/UI.md → coral-soft background, coral-ink text. */
export function GapChip({ gap, className }: { gap: Gap; className?: string }) {
  return (
    <span
      title={gap.label}
      className={cn("inline-flex rounded-full bg-coral-soft px-3 py-1 text-sm text-coral-ink", className)}
    >
      {gapText(gap)}
    </span>
  )
}
