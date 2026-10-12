// Readable names for the generated OpenAPI types (`npm run gen:api` → schema.d.ts). The spec types enum-like
// values as plain strings; the unions below narrow them to the values in docs/API.md and docs/DATA-MODEL.md.
import type { components } from "./schema"

type Schemas = components["schemas"]

export type CountryCode = "CA" | "DE" | "AU"
export type Goal = "study" | "work" | "pr"
export type Grade = "eligible" | "near_eligible" | "not_eligible"
export type EducationLevel = "secondary" | "diploma" | "bachelors" | "masters" | "phd"
export type EnglishTest = "ielts" | "pte" | "toefl" | "none"
export type EnglishTestVariant = "academic" | "general"
export type GermanLevel = "A1" | "A2" | "B1" | "B2" | "C1" | "C2"
export type MaritalStatus = "single" | "married"
export type OccupationSystem = "NOC" | "ISCO" | "ANZSCO"

/**
 * `POST /assessments` and `/evaluations` body. `jobOffer` carries one yes/no per country next to `salaryEur`;
 * the API reads those as JSON extension data, so they are missing from the spec.
 */
export type CandidateProfile = Omit<Schemas["CandidateProfileRequest"], "jobOffer"> & {
  jobOffer?: (Schemas["JobOfferRequest"] & Partial<Record<CountryCode, boolean>>) | null
}

export type ExtractedProfile = Schemas["ExtractedProfile"]
export type ExtractedEducation = Schemas["ExtractedEducation"]
export type ExtractedWork = Schemas["ExtractedWork"]
export type OccupationSuggestion = Schemas["OccupationSuggestion"]

export type Country = Schemas["CountryResponse"]
export type PathwaySummary = Schemas["PathwaySummaryResponse"]
export type Occupation = Schemas["OccupationResponse"]

export type Assessment = Schemas["AssessmentResponse"]
export type Evaluation = Schemas["EvaluationResponse"]
export type PathwayResult = Schemas["PathwayResultResponse"]
export type PathwayChain = Schemas["PathwayChainResponse"]
export type ChainStep = Schemas["ChainStepResponse"]
export type Requirement = Schemas["RequirementResponse"]
export type Gap = Schemas["GapResponse"]
export type Points = Schemas["PointsResponse"]
export type Source = Schemas["SourceResponse"]
export type Summary = Schemas["SummaryResponse"]
