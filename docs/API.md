# NextShore — API

Base: `/api/v1`. JSON, camelCase. Errors: RFC 7807 ProblemDetails with `type`, `title`, `status`, `detail`, `errors` (validation).

## Public

### POST `/resumes/parse`
`multipart/form-data`: `file` (PDF/DOCX ≤ 5 MB), `consent=true`
- 200 → `ExtractedProfile` (see `DATA-MODEL.md`)
- 400 invalid type/size/consent · 422 unreadable resume · 429 rate limit (10/IP/day) · 502 AI provider error
- Never stores the file or text.

### GET `/countries`
200 → `[{ code, name, pathways: [{ slug, name, type, isPointsBased, summary }] }]` — published pathways only; enum values are snake_case (`post_study_work`).

### GET `/occupations?q=dev&system=NOC`
200 → `[{ system, code, title }]` (max 20, published only). `q` (optional) matches a code prefix or, case-insensitively, the title; code matches rank first. `system` (optional) filters `NOC` · `ISCO` · `ANZSCO`. Lists published entries of any eligibility — eligibility shows in results.

### POST `/assessments`
Body: `CandidateProfile`
```json
{
  "goal": "pr",
  "dateOfBirth": "1995-04-12",
  "citizenship": "IN",
  "education": { "level": "bachelors", "field": "Computer Science", "years": 4 },
  "work": { "yearsSkilled": 5, "yearsInCountry": { "CA": 0, "DE": 0, "AU": 0 } },
  "occupationCodes": { "NOC": "21231", "ISCO": "2512", "ANZSCO": "261313" },
  "english": { "test": "ielts", "testVariant": "general", "scores": { "listening": 7.5, "reading": 7, "writing": 6.5, "speaking": 7 } },
  "french": null,
  "german": { "level": "A2" },
  "fundsUsd": 20000,
  "maritalStatus": "single",
  "spouse": null,
  "jobOffer": { "CA": false, "DE": false, "AU": false, "salaryEur": null },
  "relativeIn": { "CA": false, "AU": false },
  "student": null,
  "degreeRecognizedDE": true
}
```
- 201 → `{ id, ruleVersion, expiresAt, results: PathwayResult[], chains: PathwayChain[] }`, `Location: /api/v1/assessments/{id}`. Stores only the submitted profile and the results; deleted after `Assessments:LifetimeDays` (30).
- 400 validation errors

**CandidateProfile fields** (enum-like values are lowercase strings; countries are ISO alpha-2 keys)
| Field | Rules |
|---|---|
| `goal` | required: `study` · `work` · `pr` |
| `dateOfBirth` | required, age 15–100 |
| `citizenship` | required, e.g. `IN` |
| `education` | required: `{ level: secondary·diploma·bachelors·masters·phd, field?, years? 0–15 }` |
| `work` | required: `{ yearsSkilled 0–60, yearsInCountry?: { CA: 0–60, … } }` |
| `occupationCodes` | optional `{ NOC?, ISCO?, ANZSCO? }` |
| `english` | optional `{ test: ielts·pte·toefl·none, testVariant: academic·general (required for ielts and pte; PTE general = PTE Core), scores: { listening, reading, writing, speaking } }` — IELTS 0–9 in 0.5 steps, PTE 10–90, TOEFL 0–30; no scores when `none` |
| `french` | optional `{ clb: 1–12 }` |
| `german` | optional `{ level: A1…C2 }` |
| `fundsUsd` | optional, ≥ 0 |
| `maritalStatus` | required: `single` · `married`; `spouse` only when married: `{ educationLevel?, englishClb? 1–12 }` |
| `jobOffer` | optional `{ CA: bool, DE: bool, …, salaryEur? ≥ 0 }` |
| `relativeIn` | optional `{ CA: bool, AU: bool }` |
| `student` | optional `{ gpaPercent? 0–100, tuitionBudgetUsd? ≥ 0 }` |
| `degreeRecognizedDE` | optional bool |

Validation errors are keyed by camelCase JSON path, e.g. `english.scores.listening`, `jobOffer.DE`.

**PathwayResult** (as in `DATA-MODEL.md`, plus `pathwayName`): `{ pathwaySlug, pathwayName, countryCode, grade, readinessScore, points: { variant, total, target, targetKind: cutoff·pass_mark, breakdown: [{ factor, current, points }] } | null, requirements: [{ id, label, met, current, target, isMandatory, fixability }], gaps: [{ requirementId, field, label, current, target, fixability, fixHint, fixMonths, impactPoints, unlocksGrade }], chain: [slug], sources: [{ label, url, verifiedAt }], staleRules }`
**PathwayChain**: `{ countryCode, totalMonths | null, steps: [{ pathwaySlug, type, grade, readinessScore, durationMonths, isProjected }] }`

### GET `/assessments/{id}`
200 → same as POST response, exactly as stored (not re-evaluated against newer rules) · 404 not found/expired (ProblemDetails)

### GET `/assessments/{id}/summary`
200 → `{ summary: string, generatedAt }` (generated once, cached) · 404 · 502

### POST `/evaluations`
Body: `CandidateProfile` → 200 `{ results, chains }`. Not persisted, no AI. Rate limit 60/IP/min.

### GET `/health`
200 `{ status: "ok" }`

## Admin (`/admin`, Clerk JWT with claim `role=admin`)
| Method | Route | Notes |
|---|---|---|
| GET | `/admin/pathways` | incl. drafts, stale flags |
| GET/PUT | `/admin/pathways/{id}` | edit pathway fields |
| GET/POST | `/admin/pathways/{id}/requirements` | |
| PUT/DELETE | `/admin/requirements/{id}` | |
| GET/POST | `/admin/pathways/{id}/points-factors` | |
| PUT/DELETE | `/admin/points-factors/{id}` | |
| GET/POST | `/admin/pathways/{id}/occupations` | bulk CSV import via POST |
| DELETE | `/admin/occupations/{id}` | |
| POST | `/admin/pathways/{id}/publish` | 409 if any `TODO_VERIFY` remains |
| GET | `/admin/rules/stale?days=90` | |
| GET | `/admin/audit?entity=&from=` | paged |

All admin writes create `draft` rows and audit entries. 401 unauthenticated · 403 not admin.

## Claude integration (Infrastructure)
All calls go through `ClaudeClient` (official `Anthropic` C# SDK). The API key is read only from `Anthropic__ApiKey`.
| Use | Model config | Notes |
|---|---|---|
| Resume parse | `Anthropic:ParseModel` = `claude-haiku-5-5` | structured outputs (`output_config.format` with the `ExtractedProfile` JSON schema) — not a forced tool call (newer models reject forced `tool_choice`); no `temperature` (current models reject non-default sampling); thinking counts toward `max_tokens`, so size it with headroom or lower effort (T22) |
| Summary | `Anthropic:SummaryModel` = `claude-sonnet-5-5` | input = profile + results JSON only; ≤ 200 words; must not mention probability or add rules |

Timeouts are the **total** per call, retries included: 30 s parse (`Anthropic:ParseTimeout`), 20 s summary (`Anthropic:SummaryTimeout`). The SDK retries once (`Anthropic:MaxRetries` = 1) on 429, 5xx, connection errors and timed-out attempts, with backoff that honours `retry-after`. Failures become `AiProviderException` (`NotConfigured` · `Timeout` · `RateLimited` · `Unavailable` · `Rejected`) → 502. Logs never contain prompt or response content.
