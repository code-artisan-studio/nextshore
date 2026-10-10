# NextShore — Agent Instructions

NextShore helps **students and software developers** upload a resume and see which countries and visa pathways fit them, with a **Readiness Score (0–100)**, gaps, and a study → work → PR pathway chain.

Phase 1 = POC: 3 countries (Canada, Germany, Australia), 10 pathways, no user accounts, admin-editable rules.

## Read before coding
| Doc | What it holds |
|---|---|
| `docs/PRD.md` | Scope, users, user stories, demo criteria |
| `docs/ARCHITECTURE.md` | System design, flows, folder structure |
| `docs/DATA-MODEL.md` | Tables, enums, profile field keys, JSON contracts |
| `docs/VISA-RULES-SPEC.md` | Rule format, scoring, grading, seed data |
| `docs/API.md` | Endpoints, request/response shapes, errors |
| `docs/UI.md` | Brand tokens, pages, components, copy |
| `docs/TASKS.md` | Ordered build checklist — work top to bottom |

## Stack
- **web/**: React 19, Vite, TypeScript (strict), Tailwind v4, shadcn/ui, TanStack Query, React Hook Form + Zod, React Router. Deploy: Vercel.
- **api/**: .NET 10 Web API (minimal APIs), Clean Architecture, EF Core + Npgsql, FluentValidation, Serilog, xUnit. Deploy: Railway.
- **DB**: Neon Postgres.
- **AI**: Claude API — Haiku for resume extraction, Sonnet for result summaries.
- **Auth**: Clerk (admin role only).

## Non-negotiable rules
1. **The LLM never decides eligibility.** It only extracts profile data and writes explanations grounded in engine output. All grading comes from the deterministic engine in `NextShore.Domain`.
2. **No country-specific C# code.** Every country rule lives in the database (seeded from `api/seed/*.json`). Adding a country = adding data.
3. **Never persist resume files or raw resume text.** Parse in memory, discard. Only the user-confirmed profile is stored.
4. **Never show "chance of success" or approval probability.** Use "Readiness Score".
5. Every rule carries `source_url` and `verified_at`. Never invent visa values — seed values marked `TODO_VERIFY` must be filled from the official source.
6. Domain layer has zero external dependencies. Engine code requires unit tests.
7. Do not use MediatR (commercial license). Use plain application-service handlers.
8. Claude API key lives only in the API (env var). Never in web/.

## Conventions
- C#: file-scoped namespaces, records for DTOs, `Result<T>` for expected failures, ProblemDetails for errors, async all the way, nullable enabled.
- API routes: `/api/v1/...`, kebab-case, plural nouns.
- TS: feature folders (`web/src/features/*`), no `any`, API types generated from OpenAPI into `web/src/api/`.
- Colors only via theme tokens (see `docs/UI.md`) — no raw hex in components.
- Commits: conventional commits (`feat:`, `fix:`, `chore:`).

## Commands
```bash
# api
cd api && dotnet build && dotnet test
dotnet run --project src/NextShore.Api
dotnet ef migrations add <Name> -p src/NextShore.Infrastructure -s src/NextShore.Api

# web
cd web && npm install && npm run dev
npm run gen:api     # regenerate TS client from API OpenAPI spec
npm run build && npm run lint
```

## Workflow
- Pick the next unchecked task in `docs/TASKS.md`, implement it, run tests/lint, tick it off.
- Keep changes scoped to one task. If a doc is wrong or missing something, update the doc in the same change.
