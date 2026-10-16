import { cn } from "@/lib/utils"

/** docs/UI.md → teal fill on mist track, label "Readiness 74 / 100". Not a likelihood of approval. */
export function ReadinessBar({ score, className }: { score: number; className?: string }) {
  const value = Math.max(0, Math.min(100, Math.round(score)))
  return (
    <div className={cn("flex flex-col gap-1.5", className)}>
      <span className="text-sm font-medium text-navy">Readiness {value} / 100</span>
      <div
        role="meter"
        aria-label="Readiness score"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={value}
        className="h-2 w-full overflow-hidden rounded-full bg-mist"
      >
        <div className="h-full rounded-full bg-teal transition-[width] duration-500" style={{ width: `${value}%` }} />
      </div>
    </div>
  )
}
