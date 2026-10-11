# NextShore — Visa Rules Spec

How rules are expressed, evaluated, scored and graded. The engine is generic; all country specifics are data.

> ⚠️ **No visa value in this repo is trusted until verified.** Every numeric threshold, points value and cutoff in seed files starts as `"TODO_VERIFY"` and must be filled from the linked official source, with `verified_at` set to the date checked. The seed loader **refuses to publish** any row still containing `TODO_VERIFY`.

## 1. Evaluation order (per pathway)
1. **Audience filter** — skip pathways whose `audience` doesn't include the profile's audience. Students also see every pathway in their study chains; chains start only at study pathways the audience sees, so developers get no chains. Results are ranked by Readiness Score, then slug.
2. **Requirements** — evaluate each `requirements` row: `profile[field] <operator> value`. Missing profile field → `unmet`, `current = "Not provided"`, fixability `easy`, fix hint "Add this to your profile" (user can answer it).
   - Value types: numbers, text, yes/no, money (amount + currency) and ordered levels (CEFR `A1`…`C2`, education `secondary`…`phd`, compared by rank). Ordering (`gte`, `lt`, …) a text or yes/no field, or naming an unknown level, is a rule error and fails loudly.
   - A language score below a scale's lowest mapped level is "Below CLB 4": lower than every level, so `gte` is unmet and `lt` is met.
   - Money: a funds requirement's `unit` is the currency of the official threshold (`CAD`, `EUR`, `AUD`); the profile amount is converted into that currency before comparing, using exchange rates (units per 1 USD) supplied from configuration. A missing rate is a configuration error. The UI shows both, e.g. `USD 20,000 (≈ CAD 27,400)`.
   - Language: `english.level.{scale}` is the lowest level reached across the four skills, using the mappings for the profile's test **and** `english.test_variant`.
3. **Points** (if `is_points_based`):
   - Pick `variant`: `with_spouse` if `marital_status = married` and the pathway has with-spouse rows, else `single`, else `default`.
   - For each factor, find the matching bracket (`min ≤ v ≤ max` or `match_value`), sum points, apply `max_points_for_factor` caps.
   - Year-count factors (`age`, `work.*`) are floored to whole years before matching, so brackets are whole numbers (`1–1`, `5–null`).
   - Per factor, the best matching row counts (brackets should not overlap; if they do, the higher points win). No match or a missing value → 0, shown as "Not provided" in the breakdown. A value below a language scale matches no bracket.
   - `match_value` compares case-insensitively with text, level labels (`B2`, `masters`) and yes/no (`true`). A row with neither bracket nor `match_value` applies to any provided value. A numeric bracket on a text, level or yes/no factor is a rule error.
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
- Only mandatory requirements count; advisory ones never affect score or grade. No mandatory requirements → `reqRatio = 1`.
- Rounding is half away from zero (47.5 → 48).
- A points-based pathway with no usable target (no cutoff or pass mark yet, or 0) gets `pointsRatio = 1` and points never block its grade.

Score is 0–100, displayed as "Readiness 74 / 100". Never displayed as a percentage of success.

## 3. Grade
| Grade | Condition |
|---|---|
| `eligible` | all mandatory met AND (not points-based OR total ≥ target) |
| `near_eligible` | not eligible, no `impossible` gap, unmet mandatory ≤ 2, and (not points-based OR total ≥ 85% of target) |
| `not_eligible` | everything else |

Thresholds (2 gaps, 85%) live in config `Engine:NearEligibleMaxGaps` and `Engine:NearEligiblePointsRatio`.

## 4. Gaps
- **Requirement gaps:** one per unmet mandatory requirement, with the outcome's current/target/fix. The fix is simulated by setting the field to a value that meets the rule (the target for `eq`/`gte`/`lte`, ±1 step for `gt`/`lt`, the first listed value for `in`, `true`, money in the rule's currency) and re-running the engine: `impactPoints` = points gained, `unlocksGrade` = grade after only that fix. `impossible` gaps and rules with no derivable value (`neq`, `not_in`, `exists`) are not simulated (`impactPoints` 0, `unlocksGrade` null).
- **Points gaps** (only when points < target): for each factor except `age`, take the next bracket (lowest `min` above the current whole-year value) or next level (ordered `match_value`) that scores more; a missing factor starts at its lowest scoring bracket. Text and yes/no factors are not suggested. Simulate each and report the best 3 by `impactPoints` (`requirementId` null). If none adds points, report one `points` gap ("50 points" → "100 points").
- Language fixes keep levels consistent: raising an overall level raises the skills below it; raising a skill recomputes the overall as the lowest skill.
- Fix effort for points gaps: year-count factors need `(next − current) × 12` months. Fixability, hint and months come from a "reach this level" requirement (`gte`/`gt`) on the same field (or on the overall language level for a per-skill factor); other requirements on the field (`exists`, `in`, …) are not used. Otherwise a generic hint per field family applies.
- The points breakdown shows each value with its unit ("29 years", "CLB 9", "IELTS 7").
- `unlocksGrade`: grade after applying that single fix (computed by re-running the engine).
- Sort: `impactPoints / max(fix_months, 1)` desc; `impossible` last; ties → easier fixability, fewer months, label.

## 5. Pathway chains
- Built by following `next_pathway_id` from each study pathway.
- Chain shows each step's grade and `typical_duration_months`; total years = sum.
- Steps after the current one are evaluated on a **projected profile**, labelled "projected" in the UI. Projections depend only on pathway type and country code:
  - after a `study` step: `education.level` moves up one level (bachelors → masters; phd stays), and `degree.recognized_{country}` = true;
  - after a `post_study_work` step: its duration in years is added to `work.years_in_country.{country}` and `work.years_skilled`;
  - `age` = starting age + whole years elapsed before the step.
- A step with unknown `typical_duration_months` makes the chain total unknown and stops the time-based projections (age, work) for later steps; education and degree projections still apply.
- A chain ends at a pathway that is missing from the snapshot (unpublished) and never repeats a pathway.

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
