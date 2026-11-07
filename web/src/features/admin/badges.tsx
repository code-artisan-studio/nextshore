import { CircleAlert, TriangleAlert } from "lucide-react"

import { cn } from "@/lib/utils"

const pill = "inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium whitespace-nowrap"

/** Live / draft / unpublished changes, for a pathway or a row. */
export function StatusBadge({ status, hasDraft = false }: { status: string; hasDraft?: boolean }) {
  if (status === "published" && hasDraft) return <span className={cn(pill, "bg-near-bg text-near-ink")}>Unpublished changes</span>
  if (status === "published") return <span className={cn(pill, "bg-eligible-bg text-eligible-ink")}>Published</span>
  return <span className={cn(pill, "bg-mist text-navy")}>Draft</span>
}

export function StaleFlag({ count, label }: { count?: number; label?: string }) {
  if (count === 0) return null
  return (
    <span className={cn(pill, "bg-coral-soft text-coral-ink")} title="Verified more than 90 days ago">
      <TriangleAlert className="size-3" aria-hidden="true" />
      {label ?? (count === undefined ? "Stale" : `${count} stale`)}
    </span>
  )
}

export function UnverifiedFlag({ fields, verifiedAt }: { fields: string[]; verifiedAt: string | null }) {
  if (fields.length === 0 && verifiedAt) return null
  const text = fields.length > 0 ? `TODO_VERIFY: ${fields.join(", ")}` : "Not verified"
  return (
    <span className={cn(pill, "bg-not-bg text-not-ink")}>
      <CircleAlert className="size-3" aria-hidden="true" />
      {text}
    </span>
  )
}
