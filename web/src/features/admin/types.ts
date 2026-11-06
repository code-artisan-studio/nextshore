// Admin API types (generated from OpenAPI → api/schema.d.ts). JSON values (`value`, audit rows) are typed loosely
// by the spec; they are any JSON here.
import type { components } from "@/api/schema"

type Schemas = components["schemas"]
export type Json = string | number | boolean | null | Json[] | { [key: string]: Json }

export type AdminPathwayListItem = Schemas["AdminPathwayListItem"]
export type AdminRequirement = Omit<Schemas["AdminRequirementResponse"], "value"> & { value: Json }
export type AdminPointsFactor = Schemas["AdminPointsFactorResponse"]
export type AdminOccupation = Schemas["AdminOccupationResponse"]
export type PublishBlocker = Schemas["PublishBlockerResponse"]
export type AdminPathway = Omit<Schemas["AdminPathwayResponse"], "requirements"> & { requirements: AdminRequirement[] }
export type PathwayUpdateRequest = Schemas["PathwayUpdateRequest"]
export type RequirementRequest = Omit<Schemas["RequirementRequest"], "value"> & { value: Json }
export type PointsFactorRequest = Schemas["PointsFactorRequest"]
export type OccupationImport = Omit<Schemas["OccupationImportResponse"], "pathway"> & { pathway: AdminPathway }
export type PublishResult = Schemas["PublishResponse"]
export type StaleRule = Schemas["StaleRuleResponse"]
export type AuditEntry = Omit<Schemas["AuditEntryResponse"], "oldValue" | "newValue"> & { oldValue: Json; newValue: Json }
export type AuditPage = Omit<Schemas["AuditPageResponse"], "items"> & { items: AuditEntry[] }
export type LanguageMappingsStatus = Schemas["LanguageMappingsStatusResponse"]

/** Columns that may be marked unverified (TODO_VERIFY), per table — same lists as the API validator. */
export const verifiableFields = {
  pathway: ["pass_mark", "cutoff", "cutoff_as_of", "typical_duration_months"],
  requirement: ["value"],
  pointsFactor: ["min", "max", "match_value", "points", "max_points_for_factor"],
} as const

export const pathwayTypes = ["study", "post_study_work", "work", "pr"] as const
export const audiences = ["student", "developer"] as const
export const operators = ["eq", "neq", "gte", "lte", "gt", "lt", "in", "not_in", "exists", "true"] as const
export const fixabilities = ["easy", "medium", "hard", "impossible"] as const
export const variants = ["default", "single", "with_spouse"] as const
