# NextShore — Data Model

Postgres (Neon). EF Core code-first. snake_case table/column names. All ids `uuid` unless noted. All rule tables carry `status` (`draft` | `published` | `archived`), `created_at`, `updated_at`.

## Rule tables

### countries
| column | type | notes |
|---|---|---|
| id | uuid | |
| code | char(2) | ISO: `CA`, `DE`, `AU` |
| name | text | |
| occupation_system | text | `NOC` · `ISCO` · `ANZSCO` |
| language_scale | text | `CLB` · `CEFR` · `IELTS` |

### pathways
| column | type | notes |
|---|---|---|
| id | uuid | |
| country_id | uuid FK | |
| slug | text unique | `ca-express-entry` |
| name | text | |
| type | enum | `study` · `post_study_work` · `work` · `pr` |
| audience | enum[] | `student`, `developer` |
| is_points_based | bool | |
| pass_mark | int null | fixed pass mark (e.g. AU 189 minimum) |
| cutoff | int null | latest observed invitation cutoff |
| cutoff_as_of | date null | |
| next_pathway_id | uuid null FK | builds the chain |
| typical_duration_months | int null | for chain timeline |
| summary | text | one-line description |
| source_url | text | |
| verified_at | date | |
| rule_version | int | bumped on publish |

### requirements
| column | type | notes |
|---|---|---|
| id | uuid | |
| pathway_id | uuid FK | |
| field | text | profile field key (see below) |
| operator | enum | `eq` `neq` `gte` `lte` `gt` `lt` `in` `not_in` `exists` `true` |
| value | jsonb | scalar or array |
| unit | text null | `years`, `EUR`, `CAD`, `CLB`… |
| is_mandatory | bool | false = advisory |
| fixability | enum | `easy` · `medium` · `hard` · `impossible` |
| fix_hint | text | "Retake IELTS" |
| fix_months | int null | rough effort |
| label | text | human-readable requirement |
| source_url / verified_at | | |

### points_factors
| column | type | notes |
|---|---|---|
| id | uuid | |
| pathway_id | uuid FK | |
| factor | text | profile field key |
| variant | text | `default` · `single` · `with_spouse` |
| min / max | numeric null | inclusive bracket; null = open |
| match_value | text null | for categorical factors (`education.level = masters`) |
| points | int | |
| max_points_for_factor | int null | cap |
| source_url / verified_at | | |

### occupation_lists
`id, pathway_id, code, title, is_eligible bool, source_url, verified_at`

### language_score_mappings
`id, test (ielts|pte|toefl), skill (listening|reading|writing|speaking), min_score numeric, target_scale (CLB|CEFR|IELTS), target_level text`

### rule_audit_log
`id, entity text, entity_id uuid, action (create|update|delete|publish), old_value jsonb, new_value jsonb, actor text, at timestamptz`

## Assessment table
### assessments
`id uuid, profile jsonb, results jsonb, rule_version int, summary text null, created_at, expires_at`
No name, email or contact info is stored.

## Profile field keys (shared vocabulary)
| key | type | source |
|---|---|---|
| `goal` | `study` · `work` · `pr` | form |
| `audience` | `student` · `developer` | derived from goal + experience |
| `age` | int | form (DOB → age) |
| `citizenship` | ISO2 | form |
| `education.level` | `secondary` · `diploma` · `bachelors` · `masters` · `phd` | resume |
| `education.field` | text | resume |
| `education.years` | int | resume |
| `work.years_skilled` | number | resume |
| `work.years_in_country.{CA,DE,AU}` | number | form |
| `occupation.code.{NOC,ISCO,ANZSCO}` | text | AI suggest + user confirm |
| `english.test` | `ielts` · `pte` · `toefl` · `none` | form |
| `english.scores` | `{listening,reading,writing,speaking}` | form |
| `english.level.{CLB,CEFR,IELTS}` | normalised | computed |
| `french.level.CLB` | int | form (optional) |
| `german.level.CEFR` | `A1`…`C2` | form (optional) |
| `funds.amount_usd` | number | form |
| `marital_status` | `single` · `married` | form |
| `spouse.education.level` / `spouse.english.level.CLB` | | form (if married) |
| `job_offer.{CA,DE,AU}` | bool | form |
| `job_offer.salary_eur` | number | form (DE) |
| `relative_in.{CA,AU}` | bool | form |
| `student.gpa_percent` | number | form |
| `student.tuition_budget_usd` | number | form |
| `degree.recognized_DE` | bool | form ("Is your degree recognised in Germany?" — link to anabin) |

## JSON contracts

### ExtractedProfile (Claude `record_profile` tool output)
```json
{
  "education": [{ "level": "bachelors", "field": "Computer Science", "institutionCountry": "IN", "graduationYear": 2019, "confidence": 0.95 }],
  "work": [{ "title": "Software Engineer", "employer": "Acme", "startDate": "2019-07", "endDate": null, "country": "IN", "isSkilled": true, "confidence": 0.9 }],
  "totalSkilledYears": { "value": 5.2, "confidence": 0.85 },
  "skills": ["C#", ".NET", "React", "Azure"],
  "languagesMentioned": [{ "language": "German", "level": "A2", "confidence": 0.6 }],
  "certifications": ["AZ-204"],
  "occupationSuggestions": [{ "system": "NOC", "code": "21231", "title": "Software engineers and designers", "confidence": 0.8 }]
}
```
Fields with confidence < 0.7 are highlighted in the UI.

### PathwayResult (engine output)
```json
{
  "pathwaySlug": "ca-express-entry",
  "countryCode": "CA",
  "grade": "near_eligible",
  "readinessScore": 74,
  "points": { "variant": "single", "total": 455, "target": 480, "targetKind": "cutoff", "breakdown": [{ "factor": "age", "points": 110 }] },
  "requirements": [{ "id": "…", "label": "English CLB 7 in all skills", "met": false, "current": "CLB 6", "target": "CLB 7" }],
  "gaps": [{ "requirementId": "…", "label": "English", "current": "IELTS 6.5", "target": "IELTS 7.0", "fixability": "easy", "fixHint": "Retake IELTS", "fixMonths": 2, "impactPoints": 24, "unlocksGrade": "eligible" }],
  "chain": ["ca-study-permit", "ca-pgwp", "ca-express-entry"],
  "sources": [{ "label": "IRCC — Express Entry", "url": "https://www.canada.ca/…", "verifiedAt": "2026-10-01" }],
  "staleRules": 0
}
```
