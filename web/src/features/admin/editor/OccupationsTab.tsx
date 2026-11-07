import { useState } from "react"

import { ApiError } from "@/api/client"
import { Button } from "@/components/ui/button"
import { Label } from "@/components/ui/label"

import { useDeleteOccupation, useImportOccupations } from "../api"
import { StaleFlag, StatusBadge, UnverifiedFlag } from "../badges"
import type { AdminPathway } from "../types"
import { RowFlags } from "./RowSheet"

const example = "code,title,is_eligible,source_url,verified_at\n21231,Software engineers and designers,true,,2026-10-10"

export function OccupationsTab({ pathway }: { pathway: AdminPathway }) {
  const remove = useDeleteOccupation(pathway.id)

  return (
    <div className="flex flex-col gap-6">
      <CsvImport pathway={pathway} />
      {pathway.occupations.length === 0 ? (
        <p className="text-sm">No occupations listed. Pathways without a list accept any occupation.</p>
      ) : (
        <div className="overflow-x-auto rounded-card border border-line bg-white">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-line bg-sand text-navy">
              <tr>
                <th className="px-4 py-3 font-medium">Code</th>
                <th className="px-4 py-3 font-medium">Title</th>
                <th className="px-4 py-3 font-medium">Eligible</th>
                <th className="px-4 py-3 font-medium">Flags</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody>
              {pathway.occupations.map((o) => (
                <tr key={o.id} className="border-b border-line last:border-0">
                  <td className="px-4 py-3">
                    <code className="text-xs">{o.code}</code>
                  </td>
                  <td className="px-4 py-3">{o.title}</td>
                  <td className="px-4 py-3">{o.isEligible ? "Yes" : "No"}</td>
                  <td className="px-4 py-3">
                    <RowFlags>
                      {o.status === "draft" && pathway.status === "published" && <StatusBadge status="draft" />}
                      <UnverifiedFlag fields={o.unverifiedFields} verifiedAt={o.verifiedAt} />
                      {o.stale && <StaleFlag />}
                    </RowFlags>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <Button
                      size="sm"
                      variant="ghost"
                      disabled={remove.isPending}
                      onClick={() => {
                        if (window.confirm(`Delete ${o.code} from the draft?`)) remove.mutate(o.id)
                      }}
                    >
                      Delete
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {remove.isError && <p className="text-sm text-not-ink">{remove.error.message}</p>}
    </div>
  )
}

/** Paste or pick a CSV; rows upsert by code into the draft. Bad lines are listed and nothing is saved. */
function CsvImport({ pathway }: { pathway: AdminPathway }) {
  const [csv, setCsv] = useState("")
  const importCsv = useImportOccupations(pathway.id)
  const lineErrors = importCsv.error instanceof ApiError ? Object.entries(importCsv.error.errors) : []

  return (
    <section className="flex flex-col gap-3 rounded-card border border-line bg-white p-4">
      <h3 className="font-heading font-semibold text-navy">Import occupations (CSV)</h3>
      <p className="text-sm">
        Header <code>code,title,is_eligible</code>, optional <code>source_url,verified_at</code> (yyyy-mm-dd). Existing codes are
        updated; an empty source URL uses the pathway's.
      </p>
      <Label htmlFor="csv-file" className="text-navy">
        File
      </Label>
      <input
        id="csv-file"
        type="file"
        accept=".csv,text/csv"
        className="text-sm"
        onChange={async (e) => {
          const file = e.target.files?.[0]
          if (file) setCsv(await file.text())
        }}
      />
      <Label htmlFor="csv-text" className="text-navy">
        Or paste
      </Label>
      <textarea
        id="csv-text"
        rows={5}
        value={csv}
        placeholder={example}
        onChange={(e) => setCsv(e.target.value)}
        className="w-full rounded-lg border border-input bg-transparent px-2.5 py-2 font-mono text-xs outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
      />
      <div className="flex flex-wrap items-center gap-3">
        <Button
          disabled={csv.trim() === "" || importCsv.isPending}
          onClick={() => importCsv.mutate(csv, { onSuccess: () => setCsv("") })}
        >
          {importCsv.isPending ? "Importing…" : "Import"}
        </Button>
        {importCsv.isSuccess && (
          <span className="text-sm">
            {importCsv.data.created} added, {importCsv.data.updated} updated in the draft.
          </span>
        )}
        {importCsv.isError && lineErrors.length === 0 && <span className="text-sm text-not-ink">{importCsv.error.message}</span>}
      </div>
      {lineErrors.length > 0 && (
        <ul className="list-disc pl-5 text-sm text-not-ink">
          {lineErrors.map(([line, messages]) => (
            <li key={line}>
              {line}: {messages.join("; ")}
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
