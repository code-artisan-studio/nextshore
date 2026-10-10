# NextShore — API

Base: `/api/v1`. JSON, camelCase. Errors: RFC 7807 ProblemDetails with `type`, `title`, `status`, `detail`, `errors` (validation).

## Public

### POST `/resumes/parse`
`multipart/form-data`: `file` (PDF/DOCX ≤ 5 MB), `consent=true`
- 200 → `ExtractedProfile` (see `DATA-MODEL.md`)
- 400 invalid type/size/consent · 422 unreadable resume · 429 rate limit (10/IP/day) · 502 AI provider error
- Never stores the file or text.

### GET `/countries`
200 → `[{ code, name, pathways: [{ slug, name, type, isPointsBased, summary }] }]`

### GET `/occupations?q=dev&system=NOC`
200 → `[{ system, code, title }]` (max 20, published only)

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
  "english": { "test": "ielts", "scores": { "listening": 7.5, "reading": 7, "writing": 6.5, "speaking": 7 } },
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
- 201 → `{ id, ruleVersion, results: PathwayResult[], chains: PathwayChain[] }`
- 400 validation errors

### GET `/assessments/{id}`
200 → same as POST response · 404 not found/expired

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
| Use | Model config | Notes |
|---|---|---|
| Resume parse | `Anthropic:ParseModel` (Haiku) | forced tool call `record_profile`; temperature 0; max 2k output tokens |
| Summary | `Anthropic:SummaryModel` (Sonnet) | input = profile + results JSON only; ≤ 200 words; must not mention probability or add rules |

Timeouts: 30 s parse, 20 s summary. One retry on 429/5xx with backoff.
