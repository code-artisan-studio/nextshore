# NextShore — Architecture

## System overview
```
[React SPA · Vercel] ──HTTPS──► [.NET 10 API · Railway] ──► [Neon Postgres]
        │                              │
   Clerk (admin login)                 └──► Claude API (Haiku: parse · Sonnet: summary)
```
- The API is the only component that talks to Claude or the database.
- Clerk issues JWTs for admins only; public endpoints are anonymous.

## Core flows

### 1. Parse resume — `POST /api/v1/resumes/parse`
1. Validate: ≤ 5 MB, PDF or DOCX by **magic bytes** (`%PDF` / `PK\x03\x04` + `word/document.xml`), consent flag = true.
2. PDF → sent to Claude Haiku as a `document` content block. DOCX → text extracted with `DocumentFormat.OpenXml`, sent as text.
3. Claude is forced to call tool `record_profile` whose input schema = `ExtractedProfile` (see `DATA-MODEL.md`). Each field has `confidence` (0–1).
4. Suggested occupation codes are validated against `occupation_lists`; unknown codes dropped.
5. Return `ExtractedProfile`. **The file and raw text are discarded** — never logged or stored.

### 2. Assess — `POST /api/v1/assessments`
1. Validate `CandidateProfile` (FluentValidation).
2. Normalise: age from DOB, English score → each country's scale via `language_score_mappings`, total skilled work years.
3. Load published `RuleSnapshot` (in-memory cache).
4. Engine evaluates every pathway → `PathwayResult[]`, plus pathway chains.
5. Persist `Assessment` (profile JSON, results JSON, rule version, `expires_at = now + 30d`).
6. Return `{ id, results }`.

### 3. Summary — `GET /api/v1/assessments/{id}/summary`
- Lazily generated with Claude Sonnet from the **engine output only** (results JSON + profile), cached on the assessment row.
- Prompt instructs: explain grades and top gaps; never add requirements not in the input; never estimate approval odds.

### 4. What-if — `POST /api/v1/evaluations`
- Same engine as assess, no persistence, no AI. Target p95 < 150 ms. Frontend debounces 300 ms.

### 5. Admin publish
- Admin edits create/modify rows with `status = draft`.
- `POST /admin/pathways/{id}/publish` → drafts become `published`, `rule_version` increments, audit rows written, `RuleSnapshot` cache invalidated.

## Eligibility engine (`NextShore.Domain`)
```
EligibilityEngine
 ├─ ProfileNormalizer      → NormalizedProfile (flat dictionary of field keys → values)
 ├─ RequirementChecker     → RequirementOutcome[] (met / unmet + gap)
 ├─ PointsCalculator       → PointsBreakdown (per-factor points, total, variant)
 ├─ Grader                 → Grade + ReadinessScore
 ├─ GapBuilder             → Gap[] sorted by impact ÷ effort
 └─ ChainBuilder           → PathwayChain[] via pathways.next_pathway_id
```
- Pure functions, no I/O. Inputs: `NormalizedProfile` + `RuleSnapshot`. Output: `PathwayResult[]`.
- No country-specific code — everything is data. Rules spec: `VISA-RULES-SPEC.md`.

## Folder structure
```
nextshore/
├─ CLAUDE.md
├─ docs/
├─ .github/workflows/        api.yml, web.yml
├─ api/
│  ├─ NextShore.sln
│  ├─ src/
│  │  ├─ NextShore.Domain/          Entities, ValueObjects, Engine/, Result<T>
│  │  ├─ NextShore.Application/     Assessments/, Resumes/, Admin/, Abstractions/ (IResumeParser, IRuleRepository, ISummaryWriter, IClock)
│  │  ├─ NextShore.Infrastructure/  Persistence/ (DbContext, configs, migrations), Ai/ (ClaudeResumeParser, ClaudeSummaryWriter), Seed/, Auth/
│  │  └─ NextShore.Api/             Endpoints/, Middleware/, Program.cs
│  ├─ tests/
│  │  ├─ NextShore.Domain.Tests/
│  │  └─ NextShore.Api.IntegrationTests/   (Testcontainers Postgres)
│  └─ seed/                         canada.json, germany.json, australia.json, language-mappings.json
└─ web/
   ├─ src/
   │  ├─ api/          generated client + query hooks
   │  ├─ features/     upload/, profile/, results/, admin/
   │  ├─ components/   ui/ (shadcn), layout/
   │  ├─ lib/          theme.css, utils.ts
   │  └─ routes.tsx
   └─ vite.config.ts
```

## Cross-cutting
| Concern | Decision |
|---|---|
| Rate limiting | ASP.NET rate limiter: `/resumes/parse` 10/IP/day; `/evaluations` 60/IP/min |
| Errors | RFC 7807 ProblemDetails everywhere |
| Logging | Serilog → console (Railway). Never log resume content or profile PII |
| CORS | Allow web origin(s) from config only |
| Config | Env vars: `ConnectionStrings__Db`, `Anthropic__ApiKey`, `Anthropic__ParseModel`, `Anthropic__SummaryModel`, `Clerk__Authority`, `Cors__Origins` |
| Health | `GET /health` (DB check) |
| Cleanup | Hosted service deletes assessments past `expires_at` daily |
| Types | OpenAPI → `openapi-typescript` → `web/src/api/schema.ts` |
| CI | GitHub Actions: build + test on PR; deploy on main (Railway, Vercel) |
