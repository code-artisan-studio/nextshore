import { Link } from "react-router"

import { useStaleRules } from "./api"
import { QueryState } from "./QueryState"

const entityLabels: Record<string, string> = {
  pathways: "Pathway",
  requirements: "Requirement",
  points_factors: "Points factor",
  occupation_lists: "Occupation",
  language_score_mappings: "Language mapping",
}

export function StaleRulesPage() {
  const stale = useStaleRules()

  return (
    <div className="flex flex-col gap-4">
      <p className="text-sm">Published rules last verified more than 90 days ago, oldest first. Re-check each against its source.</p>
      <QueryState query={stale}>
        {(rows) =>
          rows.length === 0 ? (
            <p className="rounded-card bg-mist p-4 text-sm text-navy">Every published rule was verified in the last 90 days.</p>
          ) : (
            <div className="overflow-x-auto rounded-card border border-line bg-white">
              <table className="w-full text-left text-sm">
                <thead className="border-b border-line bg-sand text-navy">
                  <tr>
                    <th className="px-4 py-3 font-medium">Rule</th>
                    <th className="px-4 py-3 font-medium">Type</th>
                    <th className="px-4 py-3 font-medium">Pathway</th>
                    <th className="px-4 py-3 font-medium">Last verified</th>
                  </tr>
                </thead>
                <tbody>
                  {rows.map((r) => (
                    <tr key={`${r.entity}-${r.id}`} className="border-b border-line last:border-0">
                      <td className="px-4 py-3">{r.label}</td>
                      <td className="px-4 py-3">{entityLabels[r.entity] ?? r.entity}</td>
                      <td className="px-4 py-3">
                        {r.pathwayId ? (
                          <Link to={`/admin/pathways/${r.pathwayId}`} className="text-teal hover:underline">
                            {r.pathwaySlug}
                          </Link>
                        ) : (
                          "—"
                        )}
                      </td>
                      <td className="px-4 py-3 whitespace-nowrap">
                        ⚠️ {r.verifiedAt} · {r.daysSinceVerified} days ago
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )
        }
      </QueryState>
    </div>
  )
}
