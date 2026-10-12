# NextShore — UI

## Brand
- **Name:** NextShore · **Tagline:** Find your next country.
- **Logo:** horizon line with a half-sun rising (simple SVG, navy + coral).
- **Theme:** "Ocean & sunrise" — calm and official, with a warm accent.
- **Fonts:** Plus Jakarta Sans (headings, 500/600), Inter (body, 400/500). Google Fonts.

## Tokens (`web/src/lib/theme.css`, Tailwind v4 `@theme`)
```css
@theme {
  --color-navy: #0B2545;      /* headings, nav, outline buttons */
  --color-teal: #0E7C86;      /* primary buttons, links, score bars */
  --color-teal-dark: #0A5F67; /* primary hover */
  --color-coral: #FF7A59;     /* accents only: highlights, what-if markers */
  --color-coral-soft: #FFE8E0;
  --color-coral-ink: #9A3412;
  --color-mist: #E6F4F5;      /* tinted panels, score bar track */
  --color-sand: #FAF7F2;      /* page background */
  --color-slate: #334155;     /* body text */
  --color-line: #D6E4E6;      /* borders */

  --color-eligible-bg: #DCFCE7;  --color-eligible-ink: #14532D;
  --color-near-bg: #FEF3C7;      --color-near-ink: #78350F;
  --color-not-bg: #FEE2E2;       --color-not-ink: #7F1D1D;

  --font-heading: "Plus Jakarta Sans", sans-serif;
  --font-body: "Inter", sans-serif;
  --radius-card: 12px;
}
```
Rules:
- **Never white text on coral** (fails contrast). Primary CTA = teal with white text.
- Grade colours mean only grades. Always tint background + dark ink of the same family.
- No raw hex in components — tokens only.
- Dark mode: not in POC.

## Routes
| Route | Page |
|---|---|
| `/` | Landing |
| `/start` | Upload + consent |
| `/profile` | Review extracted profile + questionnaire (stepper) |
| `/results/:id` | Results |
| `/results/:id/:pathwaySlug` | Pathway detail |
| `/admin` | Admin (Clerk-protected) |

## Pages

### Landing
Hero (tagline, "Check my options" CTA), 3-step "how it works" (Upload → Confirm → See your paths), country flags strip, disclaimer footer.

### Upload (`/start`)
Drag-and-drop zone (PDF/DOCX, 5 MB), consent checkbox, "Analyse resume" button. Option: "Skip — fill in manually". Loading state with steps: "Reading your resume…", "Finding your experience…".

### Profile (`/profile`)
- Step 1 **Goal**: Study · Work · Permanent residence (cards).
- Step 2 **About you**: DOB, citizenship, marital status (+ spouse fields if married).
- Step 3 **Education & work**: pre-filled from resume; low-confidence fields have a coral "Check this" tag; occupation type-ahead.
- Step 4 **Languages**: English test + variant (IELTS Academic / General Training, PTE Academic / Core) + 4 scores, or "Not taken yet"; optional German/French level. Hint: Canada accepts General Training only.
- Step 5 **Extras**: funds, job offer, relatives; student-only GPA + tuition budget.
- Progress bar, back/next, summary before submit. React Hook Form + Zod; state kept across steps.
- Resume-filled fields carry a mist "From your resume" tag (coral "Check this" below confidence 0.7); the tag goes once the user edits the field.
- Occupation type-ahead per system (NOC · ISCO · ANZSCO) over published codes, with the resume's other suggestions as shortcuts; a code that isn't listed can still be entered as typed.
- The draft (extracted profile, answers, step) lives in `sessionStorage` for the tab only, so a refresh resumes where the user was. It is never sent anywhere until submit, and the resume file and text are never kept.

### Results (`/results/:id`)
- **Header**: profile snapshot chips; best match ("Germany · EU Blue Card · Readiness 92").
- **AI summary** panel (mist background), loads after cards with a skeleton.
- **Filters**: country, grade.
- **PathwayCard** list ranked by score.
- **Pathway chains** section (students): horizontal timeline per country — step → step → step with grade badges and total years; projected steps dashed.
- **What-if drawer**: sliders (English overall, years experience, job offer toggle, German level); changed cards pulse coral briefly when grade changes.
- "Copy link" to share results (expires in 30 days).
- Disclaimer.

### Pathway detail
Requirements checklist (✓/✗ with current vs target), points breakdown table (factor · your value · points), gaps list, sources with "Verified on" dates and ⚠️ if stale.

### Admin
Pathway list with status + stale flags → pathway editor with tabs: Details · Requirements · Points · Occupations · History. "Publish" button disabled-with-reason while `TODO_VERIFY` remains.

## Components
| Component | Notes |
|---|---|
| `GradeBadge` | `eligible` / `near_eligible` / `not_eligible` → label "Eligible" / "Near-eligible" / "Not eligible" |
| `ReadinessBar` | teal fill on mist track, label "Readiness 74 / 100" |
| `PathwayCard` | country flag, name, GradeBadge, ReadinessBar, top 2 GapChips, "See full breakdown" |
| `GapChip` | coral-soft bg, coral-ink text: "IELTS 6.5 → 7.0 · +24 pts" |
| `ChainTimeline` | steps with badges, durations, total |
| `WhatIfPanel` | sliders, debounced `/evaluations` call |
| `SourceLink` | label + verified date + stale warning |
| `Disclaimer` | standard text below |

## Copy
- Sentence case everywhere. Verbs on buttons: "Analyse resume", "See my results", "Try what-if".
- Never say "chance", "probability", "guaranteed" or "you will get".
- Disclaimer: *"NextShore provides general information, not immigration advice. Rules change — always confirm with the official source or a licensed immigration adviser."*

## Demo script (5 min)
1. Student resume → pathway chains for 3 countries.
2. Developer resume → Germany vs Canada vs Australia cards.
3. Drag English slider → a card flips Near-eligible → Eligible.
4. (Optional) Admin edits a threshold, publishes, result changes.
