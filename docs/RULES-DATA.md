# NextShore — Rules Data (T09 source of truth)

Researched **2026-10-10** from official sources. Use these values to replace `TODO_VERIFY` in `api/seed/*.json`.
Set `verified_at = 2026-10-10` for every row marked ✅. Rows marked ⚠️ were **not** confirmed on an official page — keep `TODO_VERIFY` until a human checks the linked source.

Legend: ✅ confirmed on official page · ⚠️ secondary source / needs human check

---

## 🇨🇦 Canada

### ca-study-permit — Study Permit

| Rule                                                                      | Value                                                        | Status        | Source (page modified)                                                                                                                                                   |
| ------------------------------------------------------------------------- | ------------------------------------------------------------ | ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Proof of funds, single applicant (outside Quebec), excl. tuition + travel | **CAD 23,448 / year**                                        | ✅            | [IRCC financial support](https://www.canada.ca/en/immigration-refugees-citizenship/services/study-canada/study-permit/get-documents/financial-support.html) (2026-08-28) |
| Funds by family size (2/3/4/5/6/7, +each extra)                           | 29,192 / 35,888 / 43,572 / 49,419 / 55,736 / 62,054 / +6,318 | ✅            | same                                                                                                                                                                     |
| Effective                                                                 | Applications on/after 2026-09-01 (previously 22,895)         | ✅            | same                                                                                                                                                                     |
| Admission                                                                 | Letter of acceptance from a DLI                              | ✅ (standard) | same section                                                                                                                                                             |

### ca-pgwp — Post-Graduation Work Permit

| Rule                                                | Value                                                                                                            | Status | Source                                                                                                                                                  |
| --------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Institution                                         | PGWP-eligible DLI                                                                                                | ✅     | [PGWP eligibility](https://www.canada.ca/en/immigration-refugees-citizenship/services/study-canada/work/after-graduation/eligibility.html) (2026-06-24) |
| Min program length                                  | **8 months**                                                                                                     | ✅     | same                                                                                                                                                    |
| Full-time study                                     | Every semester (final may be part-time)                                                                          | ✅     | same                                                                                                                                                    |
| Apply within                                        | **180 days** of completion confirmation                                                                          | ✅     | same                                                                                                                                                    |
| Language — bachelor's/master's/PhD/other university | **CLB 7 all four skills**                                                                                        | ✅     | same                                                                                                                                                    |
| Language — college/polytechnic/non-university       | **CLB 5 all four skills**                                                                                        | ✅     | same                                                                                                                                                    |
| Field of study                                      | Not required for bachelor's/master's/PhD; required for other programs (study permit applied on/after 2024-11-01) | ✅     | same                                                                                                                                                    |
| Duration                                            | Program < 2 yrs → up to program length; ≥ 2 yrs → **3 yrs**; master's ≥ 8 months → **3 yrs**                     | ✅     | [About PGWP](https://www.canada.ca/en/immigration-refugees-citizenship/services/study-canada/work/after-graduation/about.html) (2026-03-09)             |

### ca-express-entry — Express Entry (FSW + CRS)

**FSW minimums** ([FSW page](https://www.canada.ca/en/immigration-refugees-citizenship/services/immigrate-canada/express-entry/who-can-apply/federal-skilled-workers.html), 2026-09-24) ✅
| Rule | Value |
|---|---|
| Skilled work | ≥ 1 yr continuous (1,560 hrs) in last **10 yrs**, TEER 0–3, same NOC |
| Language | **CLB 7** all four abilities (first official language) |
| Education | Canadian credential or foreign credential + ECA |
| FSW selection grid | Pass mark **67 / 100** (factor max: language 28, education 25, experience 15, age 12, arranged employment 10, adaptability 10) — ⚠️ full grid page blocked; see note N2 |
| Proof of funds | Required unless authorised to work in Canada + valid job offer |

**Proof of funds** ([proof of funds](https://www.canada.ca/en/immigration-refugees-citizenship/services/immigrate-canada/express-entry/documents/proof-funds.html), 2026-06-22; table updated 2025-07-07) ✅
| Family size | 1 | 2 | 3 | 4 | 5 | 6 | 7 | +each |
|---|---|---|---|---|---|---|---|---|
| CAD | 15,263 | 19,001 | 23,360 | 28,362 | 32,168 | 36,280 | 40,392 | 4,112 |

**CRS points** ([CRS criteria](https://www.canada.ca/en/immigration-refugees-citizenship/services/immigrate-canada/express-entry/check-score/crs-criteria.html), 2026-06-22) ✅ — variants `with_spouse` / `single`

Age:
| Age | 18 | 19 | 20–29 | 30 | 31 | 32 | 33 | 34 | 35 | 36 | 37 | 38 | 39 | 40 | 41 | 42 | 43 | 44 | ≤17 or ≥45 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| with spouse | 90 | 95 | 100 | 95 | 90 | 85 | 80 | 75 | 70 | 65 | 60 | 55 | 50 | 45 | 35 | 25 | 15 | 5 | 0 |
| single | 99 | 105 | 110 | 105 | 99 | 94 | 88 | 83 | 77 | 72 | 66 | 61 | 55 | 50 | 39 | 28 | 17 | 6 | 0 |

Education (with spouse / single): less than secondary 0/0 · secondary 28/30 · 1-yr 84/90 · 2-yr 91/98 · bachelor's or 3+ yr 112/120 · two+ credentials (one 3+ yr) 119/128 · master's/professional 126/135 · PhD 140/150

First official language, **per ability** (with spouse / single): <CLB4 0/0 · CLB 4–5 6/6 · CLB 6 8/9 · CLB 7 16/17 · CLB 8 22/23 · CLB 9 29/31 · CLB 10+ 32/34

Second official language, per ability (both variants): ≤CLB4 0 · CLB 5–6 1 · CLB 7–8 3 · CLB 9+ 6 (cap 22 with spouse / 24 single)

Canadian work experience (with spouse / single): <1 yr 0/0 · 1 yr 35/40 · 2 yr 46/53 · 3 yr 56/64 · 4 yr 63/72 · 5+ yr 70/80

Spouse factors (max 40): education 0/2/6/7/8/9/10/10 (same order as above) · first language per ability ≤CLB4 0, CLB5–6 1, CLB7–8 3, CLB9+ 5 · Canadian work <1 0, 1 yr 5, 2 yr 7, 3 yr 8, 4 yr 9, 5+ yr 10

Skill transferability (max 100 total, each combo max 50):
| Combo | Level A | Level B |
|---|---|---|
| Education + language | CLB 7+ all, one < CLB 9: 1-yr+ post-sec **13**, two+ creds / master's / PhD **25** | CLB 9+ all: **25** / **50** |
| Education + Canadian work | 1 yr: 13 / 25 | 2+ yrs: 25 / 50 |
| Foreign work + language | CLB 7+ (one < 9): 1–2 yrs 13, 3+ yrs 25 | CLB 9+: 25 / 50 |
| Foreign work + Canadian work | 1 yr Cdn: 1–2 yrs 13, 3+ 25 | 2+ yrs Cdn: 25 / 50 |

Additional (max 600): sibling in Canada 15 · French NCLC 7+ & English ≤ CLB 4: 25 · French NCLC 7+ & English CLB 5+: 50 · Canadian post-secondary 1–2 yr 15, 3+ yr 30 · PNP nomination 600 · **Job offer points removed since 2025-03-25** ✅

**Recent draws** ([official rounds JSON](https://www.canada.ca/content/dam/ircc/documents/json/ee_rounds_123_en.json), read 2026-10-10) ✅
| Date | Round type | Invitations | CRS cutoff |
|---|---|---|---|
| 2026-10-01 | Trades occupations | 3,500 | 476 |
| 2026-09-29 | Canadian Experience Class | 2,000 | 518 |
| 2026-09-28 | Provincial Nominee Program | 733 | 725 |
| 2026-09-15 | Canadian Experience Class | 2,000 | 519 |
| 2026-09-04 | Healthcare & social services | 3,500 | 475 |
| 2026-08-19 | French-language proficiency | 5,000 | 382 |

➡️ **Seed:** `cutoff = 518`, `cutoff_as_of = 2026-09-29` (latest CEC draw). See note N1.

---

## 🇩🇪 Germany

### de-student-visa — Student Visa

| Rule                      | Value                                                       | Status | Source                                                                                                     |
| ------------------------- | ----------------------------------------------------------- | ------ | ---------------------------------------------------------------------------------------------------------- |
| Blocked account           | **€11,904 / year** (€992 / month)                           | ✅     | [German Missions in India — study visa](https://india.diplo.de/in-en/service/2756350-2756350) (2026-02-16) |
| Admission                 | Admission letter (degree / Studienkolleg / language course) | ✅     | same                                                                                                       |
| Language for degree study | Generally **C1** (German or English, per programme)         | ✅     | same                                                                                                       |
| APS certificate           | Required for Indian credentials (exemptions exist)          | ✅     | same                                                                                                       |
| Health insurance          | Required from arrival                                       | ✅     | same                                                                                                       |

### de-post-study — Job-seeker permit after German degree

| Rule                       | Value               | Status | Source                                                                                                       |
| -------------------------- | ------------------- | ------ | ------------------------------------------------------------------------------------------------------------ |
| Duration                   | **Up to 18 months** | ✅     | [§ 20 AufenthG Abs. 2](https://gesetze.legal/bund/aufenthg/20)                                               |
| Livelihood                 | Must be secured     | ✅     | same                                                                                                         |
| Work allowed during search | Any employment      | ⚠️     | § 20 Abs. 4 — verify on [gesetze-im-internet.de](https://www.gesetze-im-internet.de/aufenthg_2004/__20.html) |

### de-blue-card — EU Blue Card (2026 thresholds)

| Rule                                                        | Value                                                         | Status | Source                                                                                               |
| ----------------------------------------------------------- | ------------------------------------------------------------- | ------ | ---------------------------------------------------------------------------------------------------- |
| General min gross salary                                    | **€50,700 / yr** (€4,225 / month)                             | ✅     | [service.berlin.de — Blaue Karte EU](https://service.berlin.de/dienstleistung/324659/)               |
| Shortage occupation **or** degree < 3 yrs old (new entrant) | **€45,934.20 / yr** (€3,827.85 / month), needs BA approval    | ✅     | same                                                                                                 |
| IT specialist without degree                                | 3+ yrs IT experience in last 7 yrs + IT job offer; €45,934.20 | ✅     | same + [make-it-in-germany](https://www.make-it-in-germany.com/en/visa-residence/types/eu-blue-card) |
| Degree                                                      | German or comparable foreign degree (check anabin)            | ✅     | make-it-in-germany                                                                                   |
| Job offer                                                   | ≥ 6 months, matching qualification                            | ✅     | make-it-in-germany                                                                                   |
| German language                                             | Not required                                                  | ✅     | make-it-in-germany                                                                                   |

### de-opportunity-card — Chancenkarte

| Rule            | Value                                                                             | Status | Source                                                                                                                                                         |
| --------------- | --------------------------------------------------------------------------------- | ------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Route 1         | Fully recognised qualification → no points needed                                 | ✅     | [Embassy Cairo factsheet](https://kairo.diplo.de/resource/blob/2664804/5e952d2720a91e83a475a3e02452f1cf/250122-deu-merkblatt-chancenkarte-data.pdf) (Apr 2026) |
| Route 2 base    | Degree or ≥ 2-yr vocational qualification **and** German **A1** or English **B2** | ✅     | same + make-it-in-germany                                                                                                                                      |
| Funds           | **€1,091 / month** (€13,092 for 12 months)                                        | ✅     | Embassy Cairo factsheet                                                                                                                                        |
| Pass mark       | **6 points**                                                                      | ✅     | same                                                                                                                                                           |
| Validity / work | Up to 1 yr; part-time up to 20 h/week + trial work                                | ✅     | same                                                                                                                                                           |

Points ([make-it-in-germany — Opportunity Card](https://www.make-it-in-germany.com/en/visa-residence/types/job-search-opportunity-card)) ✅
| Factor | Points |
|---|---|
| Partial recognition of qualification | 4 |
| Qualification in shortage occupation | 1 |
| Work experience: ≥ 2 yrs in last 5 / ≥ 5 yrs in last 7 | 2 / 3 |
| German A2 / B1 / B2+ | 1 / 2 / 3 |
| English C1+ (or native) | +1 |
| Age ≤ 35 / 35–40 | 2 / 1 |
| ≥ 6 months legal stay in Germany in last 5 yrs | 1 |
| Partner also eligible and applying | 1 |

---

## 🇦🇺 Australia

### au-student-500 — Student visa

| Rule                            | Value                                  | Status        | Source                                                                                                                                                                                           |
| ------------------------------- | -------------------------------------- | ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Living costs, primary applicant | **AUD 29,710 / yr** (since 2024-05-10) | ⚠️            | secondary ([One Planet Migration](https://oneplanetmigrationlaw.com.au/immigration-blog/student-visa-500-financial-capacity-requirement-2026/)); confirm on Home Affairs financial-capacity page |
| Partner / child                 | AUD 10,394 / 4,449                     | ⚠️            | same                                                                                                                                                                                             |
| English                         | IELTS **6.0** overall                  | ⚠️            | from memory — confirm on Home Affairs                                                                                                                                                            |
| Enrolment                       | CoE in a CRICOS course                 | ✅ (standard) | —                                                                                                                                                                                                |

### au-485 — Temporary Graduate (Post-Higher Education Work stream)

| Rule                         | Value                                                                   | Status                                                                                                                              |
| ---------------------------- | ----------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| Age                          | Under **36** at application (exceptions: research master's/PhD, HK/BNO) | ⚠️ secondary ([myvisa](https://myvisa.com.au/immigration-guides/485-visa-requirements-2026-eligibility-streams-english-age-limits)) |
| Australian study requirement | ≥ 2 academic years (92 weeks) CRICOS, in English                        | ⚠️ secondary                                                                                                                        |
| Apply within                 | 6 months of course completion                                           | ⚠️ secondary                                                                                                                        |
| English                      | IELTS 6.5 overall, min 5.5 each                                         | ⚠️ from memory                                                                                                                      |
| Duration                     | Bachelor's / coursework master's 2 yrs; research master's / PhD 3 yrs   | ⚠️ from memory                                                                                                                      |

All ⚠️ → check [Home Affairs 485 page](https://immi.homeaffairs.gov.au/visas/getting-a-visa/visa-listing/temporary-graduate-485) in a browser (it's JS-rendered; automated fetch failed).

### au-189 — Skilled Independent (points-tested)

| Rule                    | Value                                                              | Status                                                                                                                                   |
| ----------------------- | ------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------- |
| Age                     | Under **45** at invitation                                         | ✅ (points table)                                                                                                                        |
| English minimum         | **Competent: IELTS 6 each** (PTE from 2025-08-07: L47 R48 W51 S54) | ✅ [Home Affairs](https://immi.homeaffairs.gov.au/help-support/meeting-our-requirements/english-language/competent-english) (2026-09-21) |
| Skills assessment       | Positive assessment for nominated occupation (ACS for ICT)         | ✅ (standard)                                                                                                                            |
| Occupation              | On MLTSSL                                                          | ✅ (standard)                                                                                                                            |
| Pass mark               | **65**                                                             | ✅                                                                                                                                       |
| Invitation cutoff (ICT) | Occupation-specific; round of 2026-06-04 ranged 65–100 overall     | ⚠️ `TODO_VERIFY` from [SkillSelect results](https://immi.homeaffairs.gov.au/visas/working-in-australia/skillselect/invitation-rounds)    |

Points (per [Settle AU calculator](https://www.settleau.com.au/visas/pr-points-calculator), reviewed 2026-09-30, mirrors Schedule 6D) ✅\*
| Factor | Brackets → points |
|---|---|
| Age | 18–24: 25 · 25–32: 30 · 33–39: 25 · 40–44: 15 |
| English | Competent 0 · Proficient (IELTS 7 each) 10 · Superior (IELTS 8 each) 20 |
| Overseas skilled work (last 10 yrs) | <3: 0 · 3–4: 5 · 5–7: 10 · 8+: 15 |
| Australian skilled work (last 10 yrs) | <1: 0 · 1–2: 5 · 3–4: 10 · 5–7: 15 · 8+: 20 |
| **Employment cap** | Overseas + Australian combined max **20** |
| Education | PhD 20 · bachelor's/master's 15 · diploma/trade 10 |
| Specialist education (Australian research master's/PhD, STEM) | 10 |
| Australian study requirement | 5 |
| Regional study | 5 |
| Professional Year | 5 |
| Credentialled community language (NAATI) | 5 |
| Partner: single, or partner is AU citizen/PR | 10 |
| Partner: competent English only | 5 |
| Partner: skilled (assessment + competent English + under 45) | 10 |

\* Proficient/Superior IELTS levels confirmed on Home Affairs (2026-09-21). Points values are from a reputable migration-agent calculator; spot-check against the points table on the [Home Affairs 189 page](https://immi.homeaffairs.gov.au/visas/getting-a-visa/visa-listing/skilled-independent-189/points-tested) (JS-rendered — open in a browser) before the demo.

---

## 🔤 Language mappings (`language-mappings.json`)

**IELTS General Training → CLB** ([IRCC](https://www.canada.ca/en/immigration-refugees-citizenship/services/immigrate-canada/express-entry/documents/language-test.html), 2026-06-22) ✅
| CLB | Listening | Reading | Writing | Speaking |
|---|---|---|---|---|
| 4 | 4.5 | 3.5 | 4.0 | 4.0 |
| 5 | 5.0 | 4.0 | 5.0 | 5.0 |
| 6 | 5.5 | 5.0 | 5.5 | 5.5 |
| 7 | 6.0 | 6.0 | 6.0 | 6.0 |
| 8 | 7.5 | 6.5 | 6.5 | 6.5 |
| 9 | 8.0 | 7.0 | 7.0 | 7.0 |
| 10 | 8.5 | 8.0 | 7.5 | 7.5 |

**PTE Core → CLB** ✅ (same page): CLB 7 = L60 R60 W69 S68 · CLB 9 = L82 R78 W88 S84 · CLB 10 = L89 R88 W90 S89 (full table on page)

**IELTS → Australian levels** ✅: Competent 6 each · Proficient 7 each · Superior 8 each

**IELTS → CEFR** ⚠️ not an official German-government mapping; use only for the Opportunity Card "English B2 / C1" check and label as approximate.

---

## 📌 Notes that affect the engine/product

**N1 — Canada: no general or STEM draws in 2026.** Every 2026 round in the official JSON is category-based (CEC, PNP, French, trades, healthcare, transport, senior managers). An overseas developer with no Canadian experience, French, or nomination currently has **no active invitation route** even with a high CRS. Options:

- (a) Seed `cutoff = 518` (CEC) and add an advisory requirement `work.years_in_country.CA >= 1` with label "Recent draws favour Canadian experience" — **recommended for POC**.
- (b) Split into `ca-ee-cec` and `ca-ee-fsw` pathways (more accurate, +1 pathway).

**N2 — FSW 67-point grid not modelled.** The official grid page blocks automated fetch. For POC, rely on the hard minimums (CLB 7, 1 yr skilled work, funds) + CRS. Add the grid in Phase 2.

**N3 — Canada requires IELTS _General Training_; Australia/Germany accept Academic.** Add `english.test_variant` (`academic` | `general`) to the profile; Canada CLB mapping applies only to General Training.

**N4 — Rules in flux.** IRCC opened a consultation (2026-04-23) on merging FSW/CEC/FST and trimming CRS points ([page](https://www.canada.ca/en/immigration-refugees-citizenship/corporate/transparency/consultations/2026-consultation-express-entry.html)) — nothing in force yet. Germany Blue Card thresholds change every January → add a reminder to update in **Jan 2027**.

**N5 — Australia per-component English.** Requirements are per skill, not overall; PTE thresholds changed for tests on/after 2025-08-07. Store per-skill scores (already in the profile model).
