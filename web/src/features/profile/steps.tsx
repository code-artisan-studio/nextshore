import type { ComponentProps, ReactNode } from "react"
import { Info, Pencil } from "lucide-react"
import { Controller, useFormContext, useWatch } from "react-hook-form"

import type { OccupationSuggestion } from "@/api/types"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { NativeSelect, NativeSelectOption } from "@/components/ui/native-select"
import { Switch } from "@/components/ui/switch"

import { citizenshipOptions, countryName } from "./countries"
import { ChoiceCards, Field, inputClass, selectClass, StepHeader, type ControlProps, type ProfileField } from "./fields"
import { OccupationPicker } from "./OccupationPicker"
import {
  cefrLevels,
  educationLevels,
  englishTests,
  goals,
  occupationSystems,
  scoreRanges,
  scoreSkills,
  type ProfileFormValues,
} from "./schema"

function useProfileForm() {
  return useFormContext<ProfileFormValues>()
}

function GroupError({ name }: { name: ProfileField }) {
  const { getFieldState, formState } = useProfileForm()
  const { error } = getFieldState(name, formState)
  return error?.message ? <p className="text-sm text-not-ink">{error.message}</p> : null
}

function NumberInput({
  name,
  control,
  ...props
}: { name: ProfileField; control: ControlProps } & Omit<ComponentProps<"input">, "name">) {
  const { register } = useProfileForm()
  return <Input inputMode="decimal" autoComplete="off" className={inputClass} {...control} {...props} {...register(name)} />
}

function YesNo({ name, label }: { name: ProfileField; label: ReactNode }) {
  const { control } = useProfileForm()
  return (
    <Controller
      control={control}
      name={name}
      render={({ field }) => (
        <label className="flex items-center justify-between gap-4 rounded-lg border bg-card px-4 py-3">
          <span className="text-navy">{label}</span>
          <Switch checked={field.value === true} onCheckedChange={field.onChange} onBlur={field.onBlur} />
        </label>
      )}
    />
  )
}

export function GoalStep() {
  return (
    <div className="flex flex-col gap-6">
      <StepHeader title="What's your goal?">We'll show every pathway, but rank and explain them for this.</StepHeader>
      <fieldset className="flex flex-col gap-2">
        <legend className="sr-only">Goal</legend>
        <ChoiceCards name="goal" options={goals} />
        <GroupError name="goal" />
      </fieldset>
    </div>
  )
}

export function AboutStep() {
  const { register } = useProfileForm()
  const married = useWatch<ProfileFormValues, "maritalStatus">({ name: "maritalStatus" }) === "married"

  return (
    <div className="flex flex-col gap-6">
      <StepHeader title="About you">Age, citizenship and family affect points and which visas apply.</StepHeader>
      <div className="grid gap-5 sm:grid-cols-2">
        <Field name="dateOfBirth" label="Date of birth">
          {(control) => <Input type="date" className={inputClass} {...control} {...register("dateOfBirth")} />}
        </Field>
        <Field name="citizenship" label="Citizenship">
          {(control) => (
            <NativeSelect className={selectClass} {...control} {...register("citizenship")}>
              <NativeSelectOption value="">Choose a country</NativeSelectOption>
              {citizenshipOptions.map((c) => (
                <NativeSelectOption key={c.code} value={c.code}>
                  {c.name}
                </NativeSelectOption>
              ))}
            </NativeSelect>
          )}
        </Field>
      </div>
      <fieldset className="flex flex-col gap-2">
        <legend className="mb-1.5 text-sm font-medium text-navy">Marital status</legend>
        <ChoiceCards
          name="maritalStatus"
          columns={2}
          options={[
            { value: "single", label: "Single" },
            { value: "married", label: "Married or common-law" },
          ]}
        />
        <GroupError name="maritalStatus" />
      </fieldset>
      {married && (
        <div className="grid gap-5 rounded-card bg-mist p-5 sm:grid-cols-2">
          <p className="sm:col-span-2">Your partner's details can add points in Canada and Australia. Both are optional.</p>
          <Field name="spouse.educationLevel" label="Partner's highest education">
            {(control) => (
              <NativeSelect className={selectClass} {...control} {...register("spouse.educationLevel")}>
                <NativeSelectOption value="">Not sure</NativeSelectOption>
                {educationLevels.map((l) => (
                  <NativeSelectOption key={l.value} value={l.value}>
                    {l.label}
                  </NativeSelectOption>
                ))}
              </NativeSelect>
            )}
          </Field>
          <Field name="spouse.englishClb" label="Partner's English (CLB 1–12)" hint="Leave empty if they haven't taken a test.">
            {(control) => <NumberInput name="spouse.englishClb" control={control} />}
          </Field>
        </div>
      )}
    </div>
  )
}

export function EducationWorkStep({ suggestions }: { suggestions: OccupationSuggestion[] }) {
  const { register } = useProfileForm()

  return (
    <div className="flex flex-col gap-8">
      <StepHeader title="Education and work">
        Check what we found in your resume and fill in anything missing.
      </StepHeader>

      <section className="flex flex-col gap-5">
        <h3 className="text-lg">Education</h3>
        <div className="grid gap-5 sm:grid-cols-2">
          <Field name="education.level" label="Highest level completed">
            {(control) => (
              <NativeSelect className={selectClass} {...control} {...register("education.level")}>
                <NativeSelectOption value="">Choose a level</NativeSelectOption>
                {educationLevels.map((l) => (
                  <NativeSelectOption key={l.value} value={l.value}>
                    {l.label}
                  </NativeSelectOption>
                ))}
              </NativeSelect>
            )}
          </Field>
          <Field name="education.field" label="Field of study">
            {(control) => (
              <Input className={inputClass} placeholder="e.g. Computer Science" {...control} {...register("education.field")} />
            )}
          </Field>
          <Field name="education.years" label="Years of full-time study after school" hint="Optional. A 4-year degree = 4.">
            {(control) => <NumberInput name="education.years" control={control} />}
          </Field>
          <Field
            name="degreeRecognizedDE"
            label="Is your degree recognised in Germany?"
            hint="Check your university on anabin (rated H+). Needed for the EU Blue Card."
          >
            {(control) => (
              <NativeSelect className={selectClass} {...control} {...register("degreeRecognizedDE")}>
                <NativeSelectOption value="">Not sure</NativeSelectOption>
                <NativeSelectOption value="yes">Yes</NativeSelectOption>
                <NativeSelectOption value="no">No</NativeSelectOption>
              </NativeSelect>
            )}
          </Field>
        </div>
      </section>

      <section className="flex flex-col gap-5">
        <h3 className="text-lg">Work</h3>
        <Field
          name="work.yearsSkilled"
          label="Years of skilled work experience"
          hint="Paid, full-time (or equivalent) work in a professional role, e.g. 5 or 2.5."
          className="sm:max-w-xs"
        >
          {(control) => <NumberInput name="work.yearsSkilled" control={control} />}
        </Field>
        <div className="flex flex-col gap-2">
          <p className="text-sm font-medium text-navy">
            Of those, years worked in… <span className="font-normal">(leave empty if none)</span>
          </p>
          <div className="grid gap-5 sm:grid-cols-3">
            {(["CA", "DE", "AU"] as const).map((code) => (
              <Field key={code} name={`work.yearsInCountry.${code}`} label={countryName(code)}>
                {(control) => <NumberInput name={`work.yearsInCountry.${code}`} control={control} />}
              </Field>
            ))}
          </div>
        </div>
      </section>

      <section className="flex flex-col gap-5">
        <div className="flex flex-col gap-1">
          <h3 className="text-lg">Occupation</h3>
          <p className="text-sm">
            Each country classifies jobs its own way. Pick the closest match; it decides which occupation lists apply.
          </p>
        </div>
        <div className="grid gap-5">
          {occupationSystems.map((o) => (
            <OccupationPicker
              key={o.system}
              system={o.system}
              label={`${o.country} — ${o.label}`}
              suggestions={suggestions}
            />
          ))}
        </div>
      </section>
    </div>
  )
}

export function LanguagesStep() {
  const { register } = useProfileForm()
  const [test, variant] = useWatch<ProfileFormValues, ["english.test", "english.testVariant"]>({
    name: ["english.test", "english.testVariant"],
  })
  const range = test === "ielts" || test === "pte" || test === "toefl" ? scoreRanges[test] : null

  return (
    <div className="flex flex-col gap-8">
      <StepHeader title="Languages">Test scores are often the quickest way to raise your readiness.</StepHeader>

      <section className="flex flex-col gap-5">
        <fieldset className="flex flex-col gap-2">
          <legend className="mb-1.5 text-lg font-semibold text-navy font-heading">English test</legend>
          <ChoiceCards name="english.test" options={englishTests} columns={4} />
          <GroupError name="english.test" />
        </fieldset>

        {(test === "ielts" || test === "pte") && (
          <fieldset className="flex flex-col gap-2">
            <legend className="mb-1.5 text-sm font-medium text-navy">Which version?</legend>
            <ChoiceCards
              name="english.testVariant"
              columns={2}
              options={
                test === "ielts"
                  ? [
                      { value: "academic", label: "IELTS Academic" },
                      { value: "general", label: "IELTS General Training" },
                    ]
                  : [
                      { value: "academic", label: "PTE Academic" },
                      { value: "general", label: "PTE Core" },
                    ]
              }
            />
            <GroupError name="english.testVariant" />
            <p className="flex items-start gap-2 text-sm">
              <Info className="mt-0.5 size-4 shrink-0 text-teal" aria-hidden="true" />
              {test === "ielts"
                ? "Canada accepts IELTS General Training only for work and residence."
                : "Canada accepts PTE Core only for work and residence."}
              {variant === "academic" && " Your Academic score won't count there."}
            </p>
          </fieldset>
        )}

        {range && (
          <div className="flex flex-col gap-2">
            <p className="text-sm font-medium text-navy">
              Scores ({range.min}–{range.max}
              {range.step !== 1 ? `, steps of ${range.step}` : ""})
            </p>
            <div className="grid grid-cols-2 gap-5 sm:grid-cols-4">
              {scoreSkills.map((skill) => (
                <Field key={skill} name={`english.scores.${skill}`} label={skill[0].toUpperCase() + skill.slice(1)}>
                  {(control) => <NumberInput name={`english.scores.${skill}`} control={control} />}
                </Field>
              ))}
            </div>
          </div>
        )}
      </section>

      <section className="grid gap-5 sm:grid-cols-2">
        <Field name="german" label="German level" hint="Your level now (CEFR). Optional.">
          {(control) => (
            <NativeSelect className={selectClass} {...control} {...register("german")}>
              <NativeSelectOption value="">None</NativeSelectOption>
              {cefrLevels.map((level) => (
                <NativeSelectOption key={level} value={level}>
                  {level}
                </NativeSelectOption>
              ))}
            </NativeSelect>
          )}
        </Field>
        <Field name="frenchClb" label="French level (CLB 1–12)" hint="From TEF or TCF Canada. Optional.">
          {(control) => <NumberInput name="frenchClb" control={control} placeholder="None" />}
        </Field>
      </section>
    </div>
  )
}

export function ExtrasStep() {
  const [goal, offerDE] = useWatch<ProfileFormValues, ["goal", "jobOffer.DE"]>({ name: ["goal", "jobOffer.DE"] })

  return (
    <div className="flex flex-col gap-8">
      <StepHeader title="A few extras">These can change which pathways are open to you. Skip anything you're unsure of.</StepHeader>

      <Field
        name="fundsUsd"
        label="Savings you could show (USD)"
        hint="Money available to support yourself, e.g. 20000."
        className="sm:max-w-xs"
      >
        {(control) => <NumberInput name="fundsUsd" control={control} />}
      </Field>

      <section className="flex flex-col gap-3">
        <h3 className="text-lg">Do you have a job offer in…</h3>
        <YesNo name="jobOffer.CA" label="Canada" />
        <YesNo name="jobOffer.DE" label="Germany" />
        {offerDE && (
          <Field
            name="jobOffer.salaryEur"
            label="Gross annual salary in Germany (EUR)"
            hint="Decides whether the EU Blue Card salary threshold is met."
            className="sm:max-w-xs"
          >
            {(control) => <NumberInput name="jobOffer.salaryEur" control={control} />}
          </Field>
        )}
        <YesNo name="jobOffer.AU" label="Australia" />
      </section>

      <section className="flex flex-col gap-3">
        <h3 className="text-lg">Do you have a close relative living in…</h3>
        <YesNo name="relativeIn.CA" label="Canada (citizen or permanent resident)" />
        <YesNo name="relativeIn.AU" label="Australia (citizen or permanent resident)" />
      </section>

      {goal === "study" && (
        <section className="grid gap-5 rounded-card bg-mist p-5 sm:grid-cols-2">
          <h3 className="text-lg sm:col-span-2">For your studies</h3>
          <Field name="student.gpaPercent" label="Grade average (%)" hint="Your last degree, as a percentage.">
            {(control) => <NumberInput name="student.gpaPercent" control={control} />}
          </Field>
          <Field name="student.tuitionBudgetUsd" label="Yearly tuition budget (USD)">
            {(control) => <NumberInput name="student.tuitionBudgetUsd" control={control} />}
          </Field>
        </section>
      )}
    </div>
  )
}

export function ReviewStep({ onEdit }: { onEdit: (step: number) => void }) {
  const v = useWatch<ProfileFormValues>()
  const label = <T extends { value: string; label: string }>(list: T[], value?: string) =>
    list.find((o) => o.value === value)?.label ?? "—"
  const or = (value?: string, suffix = "") => (value && value.trim() !== "" ? `${value}${suffix}` : "—")
  const yesNo = (value?: boolean) => (value ? "Yes" : "No")

  const english =
    v.english?.test === "none"
      ? "Not taken yet"
      : `${label(englishTests, v.english?.test)}${v.english?.testVariant ? ` (${v.english.testVariant === "general" ? (v.english.test === "pte" ? "Core" : "General Training") : "Academic"})` : ""} · L ${or(v.english?.scores?.listening)} · R ${or(v.english?.scores?.reading)} · W ${or(v.english?.scores?.writing)} · S ${or(v.english?.scores?.speaking)}`
  const occupations = occupationSystems
    .map((o) => v.occupation?.[o.system]?.code && `${o.system} ${v.occupation[o.system]!.code}`)
    .filter(Boolean)
    .join(" · ")
  const offers = (["CA", "DE", "AU"] as const).filter((c) => v.jobOffer?.[c]).map(countryName)

  const sections: { title: string; step: number; rows: [string, string][] }[] = [
    { title: "Goal", step: 0, rows: [["Goal", label(goals, v.goal)]] },
    {
      title: "About you",
      step: 1,
      rows: [
        ["Date of birth", or(v.dateOfBirth)],
        ["Citizenship", countryName(v.citizenship ?? "") || "—"],
        ["Marital status", v.maritalStatus === "married" ? "Married or common-law" : v.maritalStatus === "single" ? "Single" : "—"],
      ],
    },
    {
      title: "Education and work",
      step: 2,
      rows: [
        ["Education", `${label(educationLevels, v.education?.level)}${v.education?.field ? `, ${v.education.field}` : ""}`],
        ["Skilled work", or(v.work?.yearsSkilled, " years")],
        ["Occupation codes", occupations || "—"],
      ],
    },
    {
      title: "Languages",
      step: 3,
      rows: [
        ["English", english],
        ["German", or(v.german)],
        ["French", or(v.frenchClb && `CLB ${v.frenchClb}`)],
      ],
    },
    {
      title: "Extras",
      step: 4,
      rows: [
        ["Savings", or(v.fundsUsd && `USD ${Number(v.fundsUsd).toLocaleString("en")}`)],
        ["Job offers", offers.join(", ") || "None"],
        ["Relatives", `Canada: ${yesNo(v.relativeIn?.CA)} · Australia: ${yesNo(v.relativeIn?.AU)}`],
      ],
    },
  ]

  return (
    <div className="flex flex-col gap-6">
      <StepHeader title="Check your answers">Your results are worked out from exactly this.</StepHeader>
      <div className="flex flex-col divide-y rounded-card border bg-card">
        {sections.map((section) => (
          <section key={section.title} className="flex flex-col gap-2 p-5">
            <div className="flex items-center justify-between">
              <h3 className="text-base">{section.title}</h3>
              <Button type="button" variant="ghost" size="sm" onClick={() => onEdit(section.step)}>
                <Pencil data-icon="inline-start" />
                Edit
              </Button>
            </div>
            <dl className="grid gap-x-6 gap-y-1 text-sm sm:grid-cols-[12rem_1fr]">
              {section.rows.map(([term, value]) => (
                <div key={term} className="contents">
                  <dt className="font-medium text-navy">{term}</dt>
                  <dd>{value}</dd>
                </div>
              ))}
            </dl>
          </section>
        ))}
      </div>
    </div>
  )
}
