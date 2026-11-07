import { ArrowLeft } from "lucide-react"
import { useState } from "react"
import { Link, useParams } from "react-router"

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Button } from "@/components/ui/button"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"

import { useAdminPathway, useDiscardDraft, usePublishPathway } from "./api"
import { StaleFlag, StatusBadge } from "./badges"
import { DetailsTab } from "./editor/DetailsTab"
import { HistoryTab } from "./editor/HistoryTab"
import { OccupationsTab } from "./editor/OccupationsTab"
import { PointsTab } from "./editor/PointsTab"
import { RequirementsTab } from "./editor/RequirementsTab"
import { QueryState } from "./QueryState"
import type { AdminPathway } from "./types"

export function PathwayEditorPage() {
  const { id = "" } = useParams()
  const pathway = useAdminPathway(id)

  return (
    <div className="flex flex-col gap-6">
      <Link to="/admin" className="inline-flex items-center gap-1 text-sm text-teal hover:underline">
        <ArrowLeft className="size-4" aria-hidden="true" />
        All pathways
      </Link>
      <QueryState query={pathway}>{(p) => <Editor pathway={p} />}</QueryState>
    </div>
  )
}

function Editor({ pathway: p }: { pathway: AdminPathway }) {
  const staleRows =
    [p, ...p.requirements, ...p.pointsFactors, ...p.occupations].filter((row) => row.stale).length

  return (
    <>
      <div className="flex flex-col gap-2">
        <div className="flex flex-wrap items-center gap-2">
          <h2 className="font-heading text-xl font-semibold text-navy">{p.name}</h2>
          <StatusBadge status={p.status} hasDraft={p.hasDraft} />
          <StaleFlag count={staleRows} />
        </div>
        <p className="text-sm">
          {p.slug} · {p.countryCode} · {p.ruleVersion > 0 ? `live version v${p.ruleVersion}` : "never published"}
        </p>
      </div>

      <PublishBar pathway={p} />

      <Tabs defaultValue="details">
        <TabsList>
          <TabsTrigger value="details">Details</TabsTrigger>
          <TabsTrigger value="requirements">Requirements ({p.requirements.length})</TabsTrigger>
          <TabsTrigger value="points">Points ({p.pointsFactors.length})</TabsTrigger>
          <TabsTrigger value="occupations">Occupations ({p.occupations.length})</TabsTrigger>
          <TabsTrigger value="history">History</TabsTrigger>
        </TabsList>
        <TabsContent value="details">
          <DetailsTab pathway={p} />
        </TabsContent>
        <TabsContent value="requirements">
          <RequirementsTab pathway={p} />
        </TabsContent>
        <TabsContent value="points">
          <PointsTab pathway={p} />
        </TabsContent>
        <TabsContent value="occupations">
          <OccupationsTab pathway={p} />
        </TabsContent>
        <TabsContent value="history">
          <HistoryTab pathwayId={p.id} />
        </TabsContent>
      </Tabs>
    </>
  )
}

/** "Publish" is disabled with the reason on screen while values are unverified or there is nothing new. */
function PublishBar({ pathway: p }: { pathway: AdminPathway }) {
  const publish = usePublishPathway(p.id)
  const discard = useDiscardDraft(p.id)
  const [published, setPublished] = useState<number | null>(null)

  const blockers = p.publishBlockers
  const nothingNew = p.status === "published" && !p.hasDraft
  const reason =
    blockers.length > 0
      ? `${blockers.length} value${blockers.length === 1 ? "" : "s"} still TODO_VERIFY or without a verified date`
      : nothingNew
        ? "No unpublished changes"
        : null

  return (
    <section className="flex flex-col gap-3 rounded-card border border-line bg-white p-4">
      <div className="flex flex-wrap items-center gap-3">
        <Button
          disabled={reason !== null || publish.isPending}
          aria-describedby="publish-reason"
          onClick={() =>
            publish.mutate(undefined, {
              onSuccess: (result) => setPublished(result.ruleVersion),
            })
          }
        >
          {publish.isPending ? "Publishing…" : "Publish"}
        </Button>
        {p.hasDraft && (
          <Button
            variant="outline"
            disabled={discard.isPending}
            onClick={() => {
              if (window.confirm("Discard every unpublished change to this pathway?")) discard.mutate()
            }}
          >
            Discard changes
          </Button>
        )}
        <p id="publish-reason" className="text-sm">
          {reason ??
            (p.status === "published"
              ? "Your changes are saved as a draft. Results use the live version until you publish."
              : "Publishing makes this pathway visible in results.")}
        </p>
      </div>
      {published !== null && !p.hasDraft && (
        <p className="text-sm text-eligible-ink">Published as rule version v{published}. Results now use it.</p>
      )}
      {(publish.isError || discard.isError) && (
        <p className="text-sm text-not-ink">{(publish.error ?? discard.error)?.message}</p>
      )}
      {blockers.length > 0 && (
        <Alert>
          <AlertTitle>Verify before publishing</AlertTitle>
          <AlertDescription>
            <ul className="list-disc pl-5">
              {blockers.map((b, i) => (
                <li key={i}>
                  <span className="font-medium">{b.entity.replace("_", " ")}</span> {b.label}: {b.reason}
                </li>
              ))}
            </ul>
          </AlertDescription>
        </Alert>
      )}
    </section>
  )
}
