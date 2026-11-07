import { useState } from "react"

import { Button } from "@/components/ui/button"

import { usePathwayAudit } from "../api"
import { QueryState } from "../QueryState"
import type { AuditEntry, Json } from "../types"

const entityLabels: Record<string, string> = {
  pathways: "Pathway",
  requirements: "Requirement",
  points_factors: "Points factor",
  occupation_lists: "Occupation",
}

const actionLabels: Record<string, string> = {
  create: "added",
  update: "changed",
  delete: "deleted",
  publish: "published",
}

export function HistoryTab({ pathwayId }: { pathwayId: string }) {
  const [page, setPage] = useState(1)
  const audit = usePathwayAudit(pathwayId, page)

  return (
    <QueryState query={audit}>
      {(data) =>
        data.total === 0 ? (
          <p className="text-sm">No admin changes yet. Seed loads aren't recorded here.</p>
        ) : (
          <div className="flex flex-col gap-3">
            <ol className="flex flex-col gap-2">
              {data.items.map((entry) => (
                <HistoryEntry key={entry.id} entry={entry} />
              ))}
            </ol>
            <div className="flex items-center gap-3 text-sm">
              <Button size="sm" variant="outline" disabled={page === 1} onClick={() => setPage(page - 1)}>
                Newer
              </Button>
              <span>
                Page {data.page} of {Math.max(1, Math.ceil(data.total / data.pageSize))}
              </span>
              <Button
                size="sm"
                variant="outline"
                disabled={data.page * data.pageSize >= data.total}
                onClick={() => setPage(page + 1)}
              >
                Older
              </Button>
            </div>
          </div>
        )
      }
    </QueryState>
  )
}

function HistoryEntry({ entry }: { entry: AuditEntry }) {
  const changes = entry.action === "update" ? changedKeys(entry.oldValue, entry.newValue) : []
  const label = rowLabel(entry.newValue) ?? rowLabel(entry.oldValue)

  return (
    <li className="rounded-card border border-line bg-white p-3 text-sm">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <span>
          <span className="font-medium text-navy">
            {entityLabels[entry.entity] ?? entry.entity} {actionLabels[entry.action] ?? entry.action}
          </span>
          {label && <> · {label}</>}
          {entry.action === "publish" && isRecord(entry.newValue) && <> · v{String(entry.newValue.ruleVersion)}</>}
          {entry.entity === "pathways" && entry.action === "delete" && <> · draft discarded</>}
        </span>
        <span className="text-xs">
          {new Date(entry.at).toLocaleString()} · {entry.actor}
        </span>
      </div>
      {changes.length > 0 && (
        <ul className="mt-2 flex flex-col gap-0.5 text-xs">
          {changes.map(([key, before, after]) => (
            <li key={key}>
              <code>{key}</code>: {show(before)} → {show(after)}
            </li>
          ))}
        </ul>
      )}
      {(entry.action !== "update" || changes.length === 0) && (
        <details className="mt-2 text-xs">
          <summary className="cursor-pointer text-teal">Details</summary>
          <pre className="mt-1 max-h-64 overflow-auto rounded bg-sand p-2">
            {JSON.stringify(entry.newValue ?? entry.oldValue, null, 2)}
          </pre>
        </details>
      )}
    </li>
  )
}

function isRecord(v: Json): v is { [key: string]: Json } {
  return typeof v === "object" && v !== null && !Array.isArray(v)
}

function rowLabel(v: Json): string | null {
  if (!isRecord(v)) return null
  const label = v.label ?? v.name ?? v.code ?? v.factor ?? (isRecord(v.pathway) ? v.pathway.name : null)
  return typeof label === "string" ? label : null
}

/** Top-level fields that differ, ignoring bookkeeping. */
function changedKeys(before: Json, after: Json): [string, Json, Json][] {
  if (!isRecord(before) || !isRecord(after)) return []
  return Object.keys({ ...before, ...after })
    .filter((k) => k !== "id" && k !== "status" && k !== "stale")
    .filter((k) => JSON.stringify(before[k]) !== JSON.stringify(after[k]))
    .map((k) => [k, before[k] ?? null, after[k] ?? null])
}

function show(v: Json): string {
  if (v === null) return "—"
  return typeof v === "string" ? v : JSON.stringify(v)
}
