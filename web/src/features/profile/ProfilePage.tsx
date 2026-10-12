import { useEffect, useMemo, useRef, useState } from "react"
import { zodResolver } from "@hookform/resolvers/zod"
import { AlertCircle, ArrowLeft, ArrowRight, FileText, LoaderCircle } from "lucide-react"
import { FormProvider, useForm, type FieldPath } from "react-hook-form"
import { useNavigate } from "react-router"

import { ApiError } from "@/api/client"
import { useCreateAssessment } from "@/api/hooks"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Button } from "@/components/ui/button"
import { Progress } from "@/components/ui/progress"

import { getProfileDraft, updateProfileDraft } from "./draft"
import { ResumeMarksContext } from "./resumeMarks"
import { prefillFromResume } from "./prefill"
import { emptyProfile, profileSchema, scoreSkills, type ProfileFormValues } from "./schema"
import { AboutStep, EducationWorkStep, ExtrasStep, GoalStep, LanguagesStep, ReviewStep } from "./steps"
import { toCandidateProfile } from "./toCandidateProfile"

type Path = FieldPath<ProfileFormValues>

const steps: { title: string; fields: Path[] }[] = [
  { title: "Goal", fields: ["goal"] },
  { title: "About you", fields: ["dateOfBirth", "citizenship", "maritalStatus", "spouse.educationLevel", "spouse.englishClb"] },
  {
    title: "Education & work",
    fields: [
      "education.level",
      "education.field",
      "education.years",
      "degreeRecognizedDE",
      "work.yearsSkilled",
      "work.yearsInCountry.CA",
      "work.yearsInCountry.DE",
      "work.yearsInCountry.AU",
    ],
  },
  {
    title: "Languages",
    fields: [
      "english.test",
      "english.testVariant",
      ...scoreSkills.map((skill) => `english.scores.${skill}` as const),
      "german",
      "frenchClb",
    ],
  },
  { title: "Extras", fields: ["fundsUsd", "jobOffer.salaryEur", "student.gpaPercent", "student.tuitionBudgetUsd"] },
]
const reviewStep = steps.length

/** API validation keys that name a different form field. */
const apiFieldAliases: Record<string, Path> = {
  "occupationCodes.NOC": "occupation.NOC",
  "occupationCodes.ISCO": "occupation.ISCO",
  "occupationCodes.ANZSCO": "occupation.ANZSCO",
  "french.clb": "frenchClb",
  "german.level": "german",
}

export function ProfilePage() {
  const navigate = useNavigate()
  const create = useCreateAssessment()
  const topRef = useRef<HTMLDivElement>(null)

  // Read once: the draft seeds the form, then the form writes back to it.
  const [draft] = useState(getProfileDraft)
  const prefill = useMemo(() => (draft?.extracted ? prefillFromResume(draft.extracted) : null), [draft])
  const [edited, setEdited] = useState(() => draft?.edited ?? [])
  const [step, setStep] = useState(() => Math.min(draft?.step ?? 0, reviewStep))

  const form = useForm<ProfileFormValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: draft?.values ?? prefill?.values ?? emptyProfile,
    mode: "onTouched",
  })

  // Keep the draft current so a refresh resumes here; editing a resume-filled field clears its tag.
  useEffect(() => {
    const fromResume = prefill?.fromResume ?? []
    return form.subscribe({
      formState: { values: true },
      callback: ({ values, name }) => {
        const touched = name ? fromResume.find((path) => name === path || name.startsWith(`${path}.`)) : undefined
        const next = touched && !edited.includes(touched) ? [...edited, touched] : edited
        if (next !== edited) setEdited(next)
        updateProfileDraft({ values, edited: next })
      },
    })
  }, [form, prefill, edited])

  useEffect(() => {
    updateProfileDraft({ step })
    topRef.current?.scrollIntoView({ behavior: "smooth", block: "start" })
  }, [step])

  const marks = useMemo(
    () => ({ fromResume: prefill?.fromResume ?? [], checkThis: prefill?.checkThis ?? [], edited }),
    [prefill, edited],
  )
  const toCheck = marks.checkThis.filter((path) => !edited.includes(path))

  async function next() {
    if (await form.trigger(steps[step].fields, { shouldFocus: true })) setStep(step + 1)
  }

  const submit = form.handleSubmit(
    (values) =>
      create.mutate(toCandidateProfile(values), {
        onSuccess: (assessment) => navigate(`/results/${assessment.id}`),
        onError: (error) => {
          if (!(error instanceof ApiError)) return
          for (const [key, messages] of Object.entries(error.errors)) {
            form.setError((apiFieldAliases[key] ?? key) as Path, { message: messages[0] })
          }
        },
      }),
    // Shouldn't happen (each step was checked), but send the user to the first step with a problem.
    (errors) => {
      const first = steps.findIndex((s) => s.fields.some((f) => form.getFieldState(f).error))
      if (first >= 0) setStep(first)
      else if (Object.keys(errors).length > 0) setStep(0)
    },
  )

  const progress = Math.round((Math.min(step + 1, steps.length) / steps.length) * 100)

  return (
    <div ref={topRef} className="mx-auto flex max-w-3xl scroll-mt-6 flex-col gap-8">
      <div className="flex flex-col gap-3">
        <div className="flex items-baseline justify-between gap-4">
          <h1 className="text-3xl">Your profile</h1>
          <p className="text-sm whitespace-nowrap">
            {step < reviewStep ? `Step ${step + 1} of ${steps.length} · ${steps[step].title}` : "Review"}
          </p>
        </div>
        <Progress value={progress} aria-label="Profile progress" className="h-2 bg-mist" />
      </div>

      {draft?.source === "resume" && step < reviewStep && (
        <div className="flex items-start gap-3 rounded-card bg-mist p-4 text-sm">
          <FileText className="mt-0.5 size-4 shrink-0 text-teal" aria-hidden="true" />
          <p>
            We filled in what we found in your resume.{" "}
            {toCheck.length > 0 && (
              <>
                Fields marked <span className="rounded-full bg-coral-soft px-1.5 font-medium text-coral-ink">Check this</span>{" "}
                were harder to read, so please confirm them.
              </>
            )}
          </p>
        </div>
      )}

      <FormProvider {...form}>
        <ResumeMarksContext value={marks}>
          <form
            noValidate
            onSubmit={(event) => {
              event.preventDefault()
              if (step < reviewStep) void next()
              else void submit(event)
            }}
            className="flex flex-col gap-8"
          >
            {step === 0 && <GoalStep />}
            {step === 1 && <AboutStep />}
            {step === 2 && <EducationWorkStep suggestions={draft?.extracted?.occupationSuggestions ?? []} />}
            {step === 3 && <LanguagesStep />}
            {step === 4 && <ExtrasStep />}
            {step === reviewStep && <ReviewStep onEdit={setStep} />}

            {create.isError && (
              <Alert variant="destructive">
                <AlertCircle />
                <AlertTitle>We couldn't work out your results</AlertTitle>
                <AlertDescription>
                  {create.error instanceof ApiError && create.error.status === 400
                    ? "Some answers need fixing. Use Edit to go back to them."
                    : "Something went wrong on our side. Try again in a moment."}
                </AlertDescription>
              </Alert>
            )}

            <div className="flex items-center justify-between gap-3 border-t pt-6">
              <Button
                type="button"
                variant="outline"
                size="lg"
                className="h-10"
                disabled={step === 0 || create.isPending}
                onClick={() => setStep(step - 1)}
              >
                <ArrowLeft data-icon="inline-start" />
                Back
              </Button>
              {step < reviewStep ? (
                <Button type="submit" size="lg" className="h-10 px-4">
                  {step === steps.length - 1 ? "Review answers" : "Next"}
                  <ArrowRight data-icon="inline-end" />
                </Button>
              ) : (
                <Button type="submit" size="lg" className="h-11 px-5 text-base" disabled={create.isPending}>
                  {create.isPending && <LoaderCircle className="animate-spin" data-icon="inline-start" />}
                  See my results
                </Button>
              )}
            </div>
          </form>
        </ResumeMarksContext>
      </FormProvider>
    </div>
  )
}
