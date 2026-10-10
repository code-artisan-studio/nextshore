# NextShore — Product Requirements (Phase 1 POC)

**Tagline:** Find your next country.

## Problem
Students and software developers who want to move abroad face dozens of visa routes across countries, each with different points systems, thresholds, and occupation lists. Most tools show one visa at a time and give no sense of *how close* you are or *what to fix*.

## Target users (POC)
| Persona | Example | Goal |
|---|---|---|
| Student | Bachelor's graduate, wants a master's abroad | Study → work → PR route |
| Software developer | 3–10 yrs experience | Skilled work visa or direct PR |

Doctors and other professions are **out of scope** for Phase 1.

## Scope
**Countries & pathways (10):**
| Country | Study | Post-study work | Work / PR |
|---|---|---|---|
| Canada | Study Permit | Post-Graduation Work Permit (PGWP) | Express Entry (CRS) |
| Germany | Student Visa | Post-study job-seeker permit | EU Blue Card, Opportunity Card (Chancenkarte) |
| Australia | Student visa (500) | Temporary Graduate (485) | Skilled Independent (189) |

**In scope**
- Resume upload (PDF/DOCX) → AI extraction → user-editable profile.
- Short goal-driven questionnaire (6–8 questions).
- Deterministic eligibility engine → grade, Readiness Score, gaps per pathway.
- Results page with ranked pathway cards, detail view, pathway chain, what-if sliders.
- AI-written plain-language summary grounded in engine output.
- Admin area to edit rules (draft → publish), stale-rule flags, audit log.

**Out of scope (Phase 2+)**
User accounts, saved history, more countries, doctors/licensing, cost of living, consultant marketplace, automated rule-change detection, dark mode.

## User stories
1. As a user, I upload my resume and see my education/experience pre-filled so I don't retype it.
2. As a user, I can correct anything the AI extracted, with low-confidence fields highlighted.
3. As a user, I choose my goal (study / work / PR) and only answer relevant questions.
4. As a user, I see pathways ranked by Readiness Score with a clear grade (Eligible / Near-eligible / Not eligible).
5. As a user, I see the gaps blocking me, how hard each is to fix, and its impact.
6. As a student, I see the full chain (study → post-study work → PR) with approximate years.
7. As a user, I move what-if sliders (English score, experience, job offer) and scores update live.
8. As a user, every rule shown links to an official source with a "verified on" date.
9. As an admin, I edit rules, publish changes, and see which rules are stale (>90 days unverified).

## Readiness Score (not "success %")
0–100 measure of how closely the profile meets a pathway's published requirements. It is **not** a prediction of approval. Formula and grading in `VISA-RULES-SPEC.md`.

## Legal & privacy
- Persistent disclaimer: *"NextShore provides general information, not immigration advice. Rules change — always confirm with the official source or a licensed immigration adviser."*
- Consent checkbox on upload. Resume files are never stored. Assessments are anonymous and auto-deleted after 30 days.

## POC success criteria (demo to stakeholder)
- [ ] Student resume → pathway chain shown for all 3 countries in < 15 s end to end.
- [ ] Developer resume → correct grades for Express Entry, Blue Card, 189.
- [ ] What-if slider flips at least one card from Near-eligible → Eligible live.
- [ ] Admin edits a threshold, publishes, and the result changes.
- [ ] Every displayed rule shows a source link + verified date.
