import type { ReactNode } from "react"
import { LoaderCircle, RotateCcw } from "lucide-react"

import type { CandidateProfile, CountryCode, GermanLevel, PathwayResult } from "@/api/types"
import { Button } from "@/components/ui/button"
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle } from "@/components/ui/sheet"
import { Slider } from "@/components/ui/slider"
import { Switch } from "@/components/ui/switch"

import { countries, countryOf, gradeLabels } from "../format"
import { englishScale, germanSteps, hasChanges, type WhatIf } from "../whatIf"

/**
 * docs/UI.md → what-if drawer: English overall, years of experience, job offers, German level. Changes are sent to
 * POST /evaluations (debounced, nothing stored); the page's cards and chains show the outcome.
 */
export function WhatIfPanel({
  open,
  onOpenChange,
  base,
  whatIf,
  onChange,
  saved,
  live,
  updating,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  base: CandidateProfile
  whatIf: WhatIf
  onChange: (whatIf: WhatIf) => void
  saved: PathwayResult[]
  live: PathwayResult[]
  updating: boolean
}) {
  const english = englishScale(base)
  const englishValue = whatIf.englishOverall ?? english.current
  const years = whatIf.yearsSkilled ?? base.work?.yearsSkilled ?? 0
  const german = whatIf.german ?? ((base.german?.level ?? "") as GermanLevel | "")
  const offer = (code: CountryCode) => whatIf.jobOffer?.[code] ?? base.jobOffer?.[code] ?? false

  return (
    <Sheet open={open} onOpenChange={onOpenChange} modal={false}>
      <SheetContent
        showOverlay={false}
        onInteractOutside={(event) => event.preventDefault()}
        className="w-full gap-0 overflow-y-auto sm:max-w-sm"
      >
        <SheetHeader className="border-b p-5">
          <SheetTitle className="font-heading text-lg font-semibold text-navy">Try what-if</SheetTitle>
          <SheetDescription className="text-slate">
            See how your results would change. Your saved results stay as they are.
          </SheetDescription>
        </SheetHeader>

        <div className="flex flex-col gap-7 p-5">
          <Control
            label="English overall"
            value={englishValue === null ? "Not taken" : `${english.label} ${formatScore(englishValue, english.step)} in each skill`}
            hint={english.taken ? undefined : "No test yet: try IELTS General Training scores."}
          >
            <Slider
              thumbLabel="English score in each skill"
              min={english.min}
              max={english.max}
              step={english.step}
              value={[englishValue ?? english.min]}
              onValueChange={([value]) => onChange({ ...whatIf, englishOverall: value })}
            />
          </Control>

          <Control label="Years of skilled work" value={`${years} year${years === 1 ? "" : "s"}`}>
            <Slider
              thumbLabel="Years of skilled work"
              min={0}
              max={20}
              step={0.5}
              value={[years]}
              onValueChange={([value]) => onChange({ ...whatIf, yearsSkilled: value })}
            />
          </Control>

          <Control label="German level" value={german || "None"}>
            <Slider
              thumbLabel="German level"
              min={0}
              max={germanSteps.length - 1}
              step={1}
              value={[germanSteps.indexOf(german)]}
              onValueChange={([index]) => onChange({ ...whatIf, german: germanSteps[index] })}
            />
            <div className="flex justify-between text-xs" aria-hidden="true">
              {germanSteps.map((level) => (
                <span key={level || "none"}>{level || "None"}</span>
              ))}
            </div>
          </Control>

          <fieldset className="flex flex-col gap-2">
            <legend className="mb-1 text-sm font-medium text-navy">Job offer in…</legend>
            {(Object.keys(countries) as CountryCode[]).map((code) => (
              <label key={code} className="flex items-center justify-between gap-3 rounded-lg border bg-card px-3 py-2">
                <span>
                  {countryOf(code).flag} {countryOf(code).name}
                </span>
                <Switch
                  checked={offer(code)}
                  onCheckedChange={(checked) => onChange({ ...whatIf, jobOffer: { ...whatIf.jobOffer, [code]: checked } })}
                />
              </label>
            ))}
          </fieldset>

          <Changes saved={saved} live={live} active={hasChanges(whatIf)} updating={updating} />

          <Button variant="outline" disabled={!hasChanges(whatIf)} onClick={() => onChange({})}>
            <RotateCcw data-icon="inline-start" />
            Reset to my answers
          </Button>
        </div>
      </SheetContent>
    </Sheet>
  )
}

function Control({
  label,
  value,
  hint,
  children,
}: {
  label: string
  value: string
  hint?: string
  children: ReactNode
}) {
  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-baseline justify-between gap-3">
        <span className="text-sm font-medium text-navy">{label}</span>
        <span className="text-sm font-medium text-coral-ink tabular-nums">{value}</span>
      </div>
      {children}
      {hint && <p className="text-xs">{hint}</p>}
    </div>
  )
}

/** Pathways whose grade or score differs from the saved results, so the effect is visible even on a phone. */
function Changes({
  saved,
  live,
  active,
  updating,
}: {
  saved: PathwayResult[]
  live: PathwayResult[]
  active: boolean
  updating: boolean
}) {
  const changes = live.flatMap((now) => {
    const before = saved.find((s) => s.pathwaySlug === now.pathwaySlug)
    return before && (before.grade !== now.grade || before.readinessScore !== now.readinessScore) ? [{ before, now }] : []
  })

  return (
    <section aria-live="polite" className="flex flex-col gap-2 rounded-lg bg-mist p-3">
      <h3 className="flex items-center gap-2 text-sm">
        What changes
        {updating && <LoaderCircle className="size-3.5 animate-spin text-teal" aria-label="Updating" />}
      </h3>
      {!active || changes.length === 0 ? (
        <p className="text-sm">{active ? "No grade or score changes yet." : "Move a slider to see the effect."}</p>
      ) : (
        <ul className="flex flex-col gap-1.5 text-sm">
          {changes.map(({ before, now }) => (
            <li key={now.pathwaySlug}>
              <span className="font-medium text-navy">
                {countryOf(now.countryCode).flag} {now.pathwayName}
              </span>
              <br />
              {before.grade !== now.grade && (
                <>
                  {gradeLabels[before.grade as keyof typeof gradeLabels]} →{" "}
                  <span className="font-medium text-navy">{gradeLabels[now.grade as keyof typeof gradeLabels]}</span> ·{" "}
                </>
              )}
              Readiness {before.readinessScore} → {now.readinessScore}
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

const formatScore = (value: number, step: number) => (step < 1 ? value.toFixed(1) : String(value))
