import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query"

import { apiFetch, jsonBody } from "./client"
import type {
  Assessment,
  CandidateProfile,
  Country,
  Evaluation,
  ExtractedProfile,
  Occupation,
  OccupationSystem,
  Summary,
} from "./types"

export const queryKeys = {
  countries: ["countries"] as const,
  occupations: (q: string, system?: OccupationSystem) => ["occupations", q, system ?? null] as const,
  assessment: (id: string) => ["assessments", id] as const,
  summary: (id: string) => ["assessments", id, "summary"] as const,
  evaluation: (profile: CandidateProfile) => ["evaluations", profile] as const,
}

/** Countries with their published pathways. Rules change rarely, so this stays fresh for the session. */
export function useCountries() {
  return useQuery({
    queryKey: queryKeys.countries,
    queryFn: ({ signal }) => apiFetch<Country[]>("/countries", { signal }),
    staleTime: Infinity,
  })
}

/** Occupation type-ahead (max 20). Pass the debounced input; nothing is fetched for an empty query. */
export function useOccupations(q: string, system?: OccupationSystem) {
  const query = q.trim()
  return useQuery({
    queryKey: queryKeys.occupations(query, system),
    queryFn: ({ signal }) => {
      const params = new URLSearchParams({ q: query })
      if (system) params.set("system", system)
      return apiFetch<Occupation[]>(`/occupations?${params}`, { signal })
    },
    enabled: query.length > 0,
    staleTime: 5 * 60_000,
    placeholderData: keepPreviousData,
  })
}

/** Uploads a resume for extraction. The API keeps neither the file nor its text. */
export function useParseResume() {
  return useMutation({
    mutationFn: ({ file, signal }: { file: File; signal?: AbortSignal }) => {
      const form = new FormData()
      form.append("file", file)
      form.append("consent", "true")
      return apiFetch<ExtractedProfile>("/resumes/parse", { method: "POST", body: form, signal })
    },
  })
}

/** Evaluates and stores a profile; the response id is the shareable results link. */
export function useCreateAssessment() {
  return useMutation({
    mutationFn: (profile: CandidateProfile) => apiFetch<Assessment>("/assessments", jsonBody(profile)),
  })
}

/** A stored assessment. It never changes once created, so it is never refetched. */
export function useAssessment(id: string) {
  return useQuery({
    queryKey: queryKeys.assessment(id),
    queryFn: ({ signal }) => apiFetch<Assessment>(`/assessments/${encodeURIComponent(id)}`, { signal }),
    staleTime: Infinity,
  })
}

/** The AI summary of an assessment, generated on first request and cached by the API. */
export function useAssessmentSummary(id: string, enabled = true) {
  return useQuery({
    queryKey: queryKeys.summary(id),
    queryFn: ({ signal }) => apiFetch<Summary>(`/assessments/${encodeURIComponent(id)}/summary`, { signal }),
    enabled,
    staleTime: Infinity,
  })
}

/** What-if evaluation (not stored, no AI). Pass a debounced profile; `null` disables the query. */
export function useEvaluation(profile: CandidateProfile | null) {
  return useQuery({
    queryKey: queryKeys.evaluation(profile ?? {}),
    queryFn: ({ signal }) => apiFetch<Evaluation>("/evaluations", { ...jsonBody(profile), signal }),
    enabled: profile !== null,
    staleTime: Infinity,
    placeholderData: keepPreviousData,
  })
}
