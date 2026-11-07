import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"

import { jsonBody } from "@/api/client"

import type {
  AdminPathway,
  AdminPathwayListItem,
  AuditPage,
  LanguageMappingsStatus,
  OccupationImport,
  PathwayUpdateRequest,
  PointsFactorRequest,
  PublishResult,
  RequirementRequest,
  StaleRule,
} from "./types"
import { useAdminFetch } from "./useAdminApi"

export const adminKeys = {
  all: ["admin"] as const,
  pathways: ["admin", "pathways"] as const,
  pathway: (id: string) => ["admin", "pathways", id] as const,
  audit: (pathwayId: string, page: number) => ["admin", "audit", pathwayId, page] as const,
  stale: ["admin", "stale"] as const,
  languageMappings: ["admin", "language-mappings"] as const,
}

export function useAdminPathways() {
  const adminFetch = useAdminFetch()
  return useQuery({
    queryKey: adminKeys.pathways,
    queryFn: ({ signal }) => adminFetch<AdminPathwayListItem[]>("/pathways", { signal }),
  })
}

export function useAdminPathway(id: string) {
  const adminFetch = useAdminFetch()
  return useQuery({
    queryKey: adminKeys.pathway(id),
    queryFn: ({ signal }) => adminFetch<AdminPathway>(`/pathways/${id}`, { signal }),
  })
}

export function usePathwayAudit(pathwayId: string, page: number) {
  const adminFetch = useAdminFetch()
  return useQuery({
    queryKey: adminKeys.audit(pathwayId, page),
    queryFn: ({ signal }) => adminFetch<AuditPage>(`/audit?pathwayId=${pathwayId}&page=${page}&pageSize=20`, { signal }),
  })
}

export function useStaleRules() {
  const adminFetch = useAdminFetch()
  return useQuery({ queryKey: adminKeys.stale, queryFn: ({ signal }) => adminFetch<StaleRule[]>("/rules/stale", { signal }) })
}

export function useLanguageMappings() {
  const adminFetch = useAdminFetch()
  return useQuery({
    queryKey: adminKeys.languageMappings,
    queryFn: ({ signal }) => adminFetch<LanguageMappingsStatus>("/language-mappings", { signal }),
  })
}

/** Every pathway write returns the working view; store it and refresh the list and history around it. */
function usePathwayWrite<TArgs>(pathwayId: string, request: (args: TArgs) => Promise<AdminPathway>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: request,
    onSuccess: (pathway) => {
      queryClient.setQueryData(adminKeys.pathway(pathwayId), pathway)
      void queryClient.invalidateQueries({ queryKey: adminKeys.pathways })
      void queryClient.invalidateQueries({ queryKey: ["admin", "audit", pathwayId] })
    },
  })
}

export function useUpdatePathway(id: string) {
  const adminFetch = useAdminFetch()
  return usePathwayWrite(id, (body: PathwayUpdateRequest) => adminFetch<AdminPathway>(`/pathways/${id}`, jsonBody(body, "PUT")))
}

/** Add (no `rowId`), update or delete (`body` null) a requirement or points factor. */
export function useSaveRow(pathwayId: string, kind: "requirements" | "points-factors") {
  const adminFetch = useAdminFetch()
  return usePathwayWrite(
    pathwayId,
    ({ rowId, body }: { rowId?: string; body: RequirementRequest | PointsFactorRequest | null }) => {
      if (body === null) return adminFetch<AdminPathway>(`/${kind}/${rowId}`, { method: "DELETE" })
      return rowId
        ? adminFetch<AdminPathway>(`/${kind}/${rowId}`, jsonBody(body, "PUT"))
        : adminFetch<AdminPathway>(`/pathways/${pathwayId}/${kind}`, jsonBody(body))
    },
  )
}

export function useDeleteOccupation(pathwayId: string) {
  const adminFetch = useAdminFetch()
  return usePathwayWrite(pathwayId, (id: string) => adminFetch<AdminPathway>(`/occupations/${id}`, { method: "DELETE" }))
}

export function useImportOccupations(pathwayId: string) {
  const adminFetch = useAdminFetch()
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (csv: string) =>
      adminFetch<OccupationImport>(`/pathways/${pathwayId}/occupations`, {
        method: "POST",
        body: csv,
        headers: { "Content-Type": "text/csv" },
      }),
    onSuccess: (result) => {
      queryClient.setQueryData(adminKeys.pathway(pathwayId), result.pathway)
      void queryClient.invalidateQueries({ queryKey: adminKeys.pathways })
      void queryClient.invalidateQueries({ queryKey: ["admin", "audit", pathwayId] })
    },
  })
}

export function useDiscardDraft(id: string) {
  const adminFetch = useAdminFetch()
  return usePathwayWrite(id, () => adminFetch<AdminPathway>(`/pathways/${id}/draft`, { method: "DELETE" }))
}

/** Publishing changes live rules: refresh everything admin, and the public rule queries (countries, occupations). */
export function usePublishPathway(id: string) {
  const adminFetch = useAdminFetch()
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => adminFetch<PublishResult>(`/pathways/${id}/publish`, { method: "POST" }),
    onSuccess: () => queryClient.invalidateQueries(),
  })
}

export function usePublishLanguageMappings() {
  const adminFetch = useAdminFetch()
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => adminFetch<LanguageMappingsStatus>("/language-mappings/publish", { method: "POST" }),
    onSuccess: (status) => {
      queryClient.setQueryData(adminKeys.languageMappings, status)
      void queryClient.invalidateQueries({ queryKey: adminKeys.stale })
    },
  })
}
