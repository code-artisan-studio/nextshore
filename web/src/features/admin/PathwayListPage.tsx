import { Link } from "react-router"

import { Button } from "@/components/ui/button"

import { useAdminPathways, useLanguageMappings, usePublishLanguageMappings } from "./api"
import { StaleFlag, StatusBadge } from "./badges"
import { QueryState } from "./QueryState"

export function PathwayListPage() {
  const pathways = useAdminPathways()

  return (
    <div className="flex flex-col gap-6">
      <QueryState query={pathways}>
        {(list) => (
          <div className="overflow-x-auto rounded-card border border-line bg-white">
            <table className="w-full text-left text-sm">
              <thead className="border-b border-line bg-sand text-navy">
                <tr>
                  <th className="px-4 py-3 font-medium">Pathway</th>
                  <th className="px-4 py-3 font-medium">Country</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                  <th className="px-4 py-3 font-medium">Version</th>
                  <th className="px-4 py-3 font-medium">Verified</th>
                  <th className="px-4 py-3 font-medium">Flags</th>
                </tr>
              </thead>
              <tbody>
                {list.map((p) => (
                  <tr key={p.id} className="border-b border-line last:border-0">
                    <td className="px-4 py-3">
                      <Link to={`/admin/pathways/${p.id}`} className="font-medium text-teal hover:underline">
                        {p.name}
                      </Link>
                      <div className="text-xs">{p.slug}</div>
                    </td>
                    <td className="px-4 py-3">{p.countryCode}</td>
                    <td className="px-4 py-3">
                      <StatusBadge status={p.status} hasDraft={p.hasDraft} />
                    </td>
                    <td className="px-4 py-3">{p.ruleVersion > 0 ? `v${p.ruleVersion}` : "—"}</td>
                    <td className="px-4 py-3">{p.verifiedAt ?? "—"}</td>
                    <td className="px-4 py-3">
                      <div className="flex flex-wrap gap-1.5">
                        <StaleFlag count={p.staleRules} />
                        {p.publishBlockers > 0 && (
                          <span className="rounded-full bg-not-bg px-2 py-0.5 text-xs font-medium text-not-ink">
                            {p.publishBlockers} to verify
                          </span>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </QueryState>
      <LanguageMappingsCard />
    </div>
  )
}

/** Mappings turn test scores into CLB/CEFR levels; results can't read English scores until they're published. */
function LanguageMappingsCard() {
  const status = useLanguageMappings()
  const publish = usePublishLanguageMappings()

  return (
    <section className="flex flex-col gap-3 rounded-card border border-line bg-white p-4">
      <h2 className="font-heading text-lg font-semibold text-navy">Language score mappings</h2>
      <QueryState query={status}>
        {(s) => (
          <div className="flex flex-wrap items-center justify-between gap-3 text-sm">
            <p>
              {s.published} published · {s.draftReady} ready to publish · {s.draftBlocked} still TODO_VERIFY (edit the seed
              file and reload)
            </p>
            <Button
              disabled={s.draftReady === 0 || publish.isPending}
              title={s.draftReady === 0 ? "No verified draft mappings" : undefined}
              onClick={() => publish.mutate()}
            >
              Publish {s.draftReady} mapping{s.draftReady === 1 ? "" : "s"}
            </Button>
          </div>
        )}
      </QueryState>
      {publish.isError && <p className="text-sm text-not-ink">{publish.error.message}</p>}
    </section>
  )
}
