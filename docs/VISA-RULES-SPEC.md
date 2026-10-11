# NextShore — Visa Rules Spec

How rules are expressed, evaluated, scored and graded. The engine is generic; all country specifics are data.

> ⚠️ **No visa value in this repo is trusted until verified.** Every numeric threshold, points value and cutoff in seed files starts as `"TODO_VERIFY"` and must be filled from the linked official source, with `verified_at` set to the date checked. The seed loader **refuses to publish** any row still containing `TODO_VERIFY`.

## 1. Evaluation order (per pathway)
1. **Audience filter** — skip pathways whose `audience` doesn't include the profile's audience (students also see work/PR pathways at the end of their chain).
2. **Requirements** — evaluate each `requirements` row: `profile[field] <operator> value`. Missing profile field → `unmet`, `current = "Not provided"`, fixability `easy` (user can answer it).
   - Money: a funds requirement's `unit` is the currency of the official threshold (`CAD`, `EUR`, `AUD`); the normaliser converts `funds.amount_usd` into that currency before comparing.
   - Language: `english.level.{scale}` is the lowest level reached across the four skills, using the mappings for the profile's test **and** `english.test_variant`.
3. **Points** (if `is_points_based`):
   - Pick `variant`: `with_spouse` if `marital_status = married` and the pathway has with-spouse rows, else `single`, else `default`.
   - For each factor, find the matching bracket (`min ≤ v ≤ max` or `match_value`), sum points, apply `max_points_for_factor` caps.
   - Year-count factors (`age`, `work.*`) are floored to whole years before matching, so brackets are whole numbers (`1–1`, `5–null`).
   - Per-skill factors use per-skill keys, e.g. CRS first language scores `english.level.CLB.listening` etc. separately. Where the profile holds a single level (French, spouse English), the seed stores 4 × the per-ability points.
   - Target = `cutoff` if present, else `pass_mark`.
4. **Grade + score + gaps** (below).

## 2. Readiness Score
```
reqRatio    = mandatoryMet / mandatoryTotal
pointsRatio = isPointsBased ? min(total / target, 1) : 1
score       = round(60 * reqRatio + 40 * pointsRatio)
if any unmet mandatory requirement has fixability = impossible → score = min(score, 20)
```
Score is 0–100, displayed as "Readiness 74 / 100". Never displayed as a percentage of success.

## 3. Grade
| Grade | Condition |
|---|---|
| `eligible` | all mandatory met AND (not points-based OR total ≥ target) |
| `near_eligible` | not eligible, no `impossible` gap, unmet mandatory ≤ 2, and (not points-based OR total ≥ 85% of target) |
| `not_eligible` | everything else |

Thresholds (2 gaps, 85%) live in config `Engine:NearEligibleMaxGaps` and `Engine:NearEligiblePointsRatio`.

## 4. Gaps
- One gap per unmet mandatory requirement, plus a synthetic "points shortfall" gap when points < target.
- For points gaps, the engine computes **impact**: re-run the points calc with the next achievable bracket for each factor (e.g. English +1 level, +1 yr experience) and report the best 3 improvements as gaps with `impactPoints`.
- `unlocksGrade`: grade after applying that single fix (computed by re-running the engine).
- Sort: `impactPoints / max(fix_months, 1)` desc; `impossible` last.

## 5. Pathway chains
- Built by following `next_pathway_id` from each study pathway.
- Chain shows each step's grade and `typical_duration_months`; total years = sum.
- Steps after the current one are evaluated on a **projected profile** (e.g. after study: `education.level` = degree studied, `work.years_in_country` += post-study work duration). Projections are labelled "projected" in the UI.

## 6. What-if
Same engine with an overridden profile. Allowed slider fields: `english.scores` (overall), `work.years_skilled`, `job_offer.*`, `german.level.CEFR`, `french.level.CLB`, `funds.amount_usd`.

## 7. Seed file format (`api/seed/{country}.json`)
```json
{
  "country": { "code": "CA", "name": "Canada", "occupationSystem": "NOC", "languageScale": "CLB" },
  "pathways": [
    {
      "slug": "ca-express-entry",
      "name": "Express Entry (Federal Skilled Worker)",
      "type": "pr",
      "audience": ["developer", "student"],
      "isPointsBased": true,
      "passMark": null,
      "cutoff": "TODO_VERIFY",
      "cutoffAsOf": "TODO_VERIFY",
      "nextPathwaySlug": null,
      "typicalDurationMonths": "TODO_VERIFY",
      "summary": "Points-ranked route to permanent residence for skilled workers.",
      "sourceUrl": "https://www.canada.ca/en/immigration-refugees-citizenship/services/immigrate-canada/express-entry.html",
      "verifiedAt": null,
      "requirements": [
        { "field": "english.level.CLB", "operator": "gte", "value": "TODO_VERIFY", "unit": "CLB", "isMandatory": true, "fixability": "easy", "fixHint": "Retake IELTS/CELPIP", "fixMonths": 2, "label": "Minimum English level" },
        { "field": "work.years_skilled", "operator": "gte", "value": "TODO_VERIFY", "unit": "years", "isMandatory": true, "fixability": "medium", "fixHint": "Gain more skilled experience", "fixMonths": 12, "label": "Skilled work experience" }
      ],
      "pointsFactors": [
        { "factor": "age", "variant": "single", "min": 20, "max": 29, "points": "TODO_VERIFY" }
      ],
      "occupations": [
        { "code": "21231", "title": "Software engineers and designers", "isEligible": "TODO_VERIFY" }
      ]
    }
  ]
}
```

**Field rules**
- Visa values that may be `"TODO_VERIFY"`: pathway `passMark`, `cutoff`, `cutoffAsOf`, `typicalDurationMonths`; requirement `value` (also inside arrays); points-factor `min`, `max`, `matchValue`, `points`, `maxPointsForFactor`; occupation `isEligible`; language-mapping `minScores.*`. They load as null and are listed in the row's `unverified_fields`.
- `sourceUrl` on a requirement, points factor or occupation is optional and defaults to the pathway's. `verifiedAt` (`yyyy-MM-dd`) is per row and **never inherited** — each row is verified on its own.
- Unknown properties, unknown enum values, wrong types and missing required properties fail the whole load with the file and JSON path.

**`language-mappings.json`**: one group per test → scale, with minimum score per skill for each level:
```json
{
  "groups": [
    {
      "test": "ielts", "testVariant": "general", "targetScale": "CLB", "sourceUrl": "https://…", "verifiedAt": null,
      "levels": [
        { "targetLevel": "7", "minScores": { "listening": "TODO_VERIFY", "reading": "TODO_VERIFY", "writing": "TODO_VERIFY", "speaking": "TODO_VERIFY" } }
      ]
    }
  ]
}
```

**Loading**: `dotnet run --project src/NextShore.Api -- seed [dir]` (defaults to `api/seed/`). Runs in one transaction; everything lands as `draft`. Countries upsert by `code`, pathways by `slug`, mappings by (test, test variant, skill, scale, level). `testVariant` is optional (null = any variant). A draft pathway's requirements, points factors and occupations are replaced from the file; published rows are never touched. Publishing (admin, T34) is refused while any row of the pathway has `unverified_fields` or no `verified_at` (`PublishGuard` in Domain).

## 8. Pathways to seed (POC)
| Slug | Type | Points? | Chain next | Official source (start here) |
|---|---|---|---|---|
| `ca-study-permit` | study | no | `ca-pgwp` | canada.ca → Study permit |
| `ca-pgwp` | post_study_work | no | `ca-express-entry` | canada.ca → Post-graduation work permit |
| `ca-express-entry` | pr | yes (CRS) | — | canada.ca → Express Entry / CRS criteria |
| `de-student-visa` | study | no | `de-post-study` | make-it-in-germany.com → Study |
| `de-post-study` | post_study_work | no | `de-blue-card` | make-it-in-germany.com → Job search after graduation |
| `de-blue-card` | work | no | — | make-it-in-germany.com → EU Blue Card |
| `de-opportunity-card` | work | yes | `de-blue-card` | make-it-in-germany.com → Opportunity Card |
| `au-student-500` | study | no | `au-485` | immi.homeaffairs.gov.au → Student visa 500 |
| `au-485` | post_study_work | no | `au-189` | immi.homeaffairs.gov.au → Temporary Graduate 485 |
| `au-189` | pr | yes | — | immi.homeaffairs.gov.au → Skilled Independent 189 + points table |

Also seed `language-mappings.json` (IELTS → CLB, IELTS → CEFR) from official conversion tables.

## 9. Data verification process
1. Admin opens the source URL, updates the value in the admin screen, sets `verified_at` to today.
2. Publish → `rule_version` bumps; audit row written.
3. Any rule with `verified_at` older than 90 days shows ⚠️ in admin and "Last verified X days ago" on results.
