# NextShore — Build Tasks

Work top to bottom. One task per change. Tick when tests/lint pass.

## M0 · Setup
- [x] T01 Create monorepo: `api/` (.NET 10 sln with Domain, Application, Infrastructure, Api, Domain.Tests, Api.IntegrationTests), `web/` (Vite React TS), root `.gitignore`, `.editorconfig`.
- [x] T02 Web: Tailwind v4 + shadcn/ui init, `theme.css` tokens from `UI.md`, fonts, base layout (header, footer with disclaimer).
- [x] T03 API: Serilog, ProblemDetails, CORS from config, `/health`, OpenAPI document, rate limiter policies.
- [x] T04 GitHub Actions: api build+test, web build+lint on PR.

## M1 · Data & rules
- [x] T05 Domain entities + enums per `DATA-MODEL.md`.
- [x] T06 EF Core DbContext, configurations (snake_case, jsonb), initial migration against Neon; add DB check to `/health`.
- [x] T07 Seed loader: reads `api/seed/*.json`, upserts by slug, loads drafts; publish blocked if `TODO_VERIFY` present.
- [x] T08 Seed files skeleton for 10 pathways (`VISA-RULES-SPEC.md` §8) with `TODO_VERIFY` placeholders + source URLs.
- [ ] T09 **Manual:** verify and fill all seed values from official sources; set `verified_at`. ✅ values from `RULES-DATA.md` filled 2026-10-10; ⚠️ and missing values still `TODO_VERIFY`.
- [x] T10 `RuleSnapshot` repository with in-memory cache + invalidation.

## M2 · Engine (Domain, TDD)
- [x] T11 `ProfileNormalizer` (age, audience, language mappings by test + variant incl. per-skill levels) + tests.
- [x] T12 `RequirementChecker` (all operators, missing fields, money in the rule's currency) + tests.
- [x] T13 `PointsCalculator` (brackets on whole years, categorical, variants, caps) + tests.
- [x] T14 `Grader` + Readiness Score formula + tests (boundaries: 85%, 2 gaps, impossible cap).
- [x] T15 `GapBuilder` (impact via re-run, unlocksGrade, sorting) + tests.
- [x] T16 `ChainBuilder` with projected profiles + tests.
- [x] T17 `EligibilityEngine` façade + 3 persona golden tests (student, mid dev, senior dev).

## M3 · API
- [x] T18 `GET /countries`, `GET /occupations`.
- [x] T19 `POST /evaluations` + validator.
- [x] T20 `POST /assessments`, `GET /assessments/{id}`; expiry cleanup hosted service.
- [ ] T21 Claude client (Infrastructure): typed options, retry, timeouts.
- [ ] T22 `POST /resumes/parse`: magic-byte validation, DOCX text extraction, forced tool call, occupation code validation. No persistence/logging of content.
- [ ] T23 `GET /assessments/{id}/summary` with caching + guardrail prompt.
- [ ] T24 Integration tests (Testcontainers Postgres; Claude mocked).

## M4 · Web
- [ ] T25 `npm run gen:api` (openapi-typescript) + TanStack Query hooks.
- [ ] T26 Landing page.
- [ ] T27 Upload page (dropzone, consent, loading states, manual skip).
- [ ] T28 Profile stepper (5 steps, Zod schemas, prefill, low-confidence tags, occupation type-ahead).
- [ ] T29 Results page: header, PathwayCard list, filters, AI summary panel.
- [ ] T30 ChainTimeline section.
- [ ] T31 WhatIfPanel with debounced `/evaluations`.
- [ ] T32 Pathway detail page (checklist, points table, gaps, sources).

## M5 · Admin
- [ ] T33 Clerk setup (web + API JWT validation, `role=admin` policy).
- [ ] T34 Admin API endpoints + audit logging.
- [ ] T35 Admin UI: pathway list, editor tabs, publish, stale view, history.

## M6 · Ship & demo
- [ ] T36 Deploy API to Railway, web to Vercel, env vars, Neon prod branch. Set the Railway health check path to `/api/v1/health` (consider moving it to root `/health`).
- [ ] T37 Prepare 3 demo resumes (student, developer, senior developer) — fictional data.
- [ ] T38 Run PRD success criteria checklist; fix gaps.
