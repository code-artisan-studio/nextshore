import { useContext, type ReactNode } from "react"
import { CircleAlert, FileText } from "lucide-react"
import { useFormContext, useFormState, type FieldPath } from "react-hook-form"

import { Label } from "@/components/ui/label"
import { cn } from "@/lib/utils"

import { ResumeMarksContext } from "./resumeMarks"
import type { ProfileFormValues } from "./schema"

export type ProfileField = FieldPath<ProfileFormValues>

export interface ControlProps {
  id: string
  "aria-invalid": boolean
  "aria-describedby"?: string
}

/** Label, resume tag, control, hint and error for one form field. */
export function Field({
  name,
  label,
  hint,
  className,
  children,
}: {
  name: ProfileField
  label: ReactNode
  hint?: ReactNode
  className?: string
  children: (control: ControlProps) => ReactNode
}) {
  const { getFieldState } = useFormContext<ProfileFormValues>()
  const { error } = getFieldState(name, useFormState<ProfileFormValues>({ name }))
  const id = `field-${name}`
  const describedBy = [hint && `${id}-hint`, error && `${id}-error`].filter(Boolean).join(" ") || undefined

  return (
    <div className={cn("flex flex-col gap-1.5", className)}>
      <div className="flex flex-wrap items-center gap-2">
        <Label htmlFor={id} className="text-navy">
          {label}
        </Label>
        <ResumeTag name={name} />
      </div>
      {children({ id, "aria-invalid": !!error, "aria-describedby": describedBy })}
      {hint && (
        <p id={`${id}-hint`} className="text-sm">
          {hint}
        </p>
      )}
      {error?.message && (
        <p id={`${id}-error`} className="text-sm text-not-ink">
          {error.message}
        </p>
      )}
    </div>
  )
}

/** "Check this" (coral) for low-confidence resume values, "From your resume" otherwise. */
export function ResumeTag({ name }: { name: string }) {
  const { fromResume, checkThis, edited } = useContext(ResumeMarksContext)
  if (edited.includes(name)) return null
  if (checkThis.includes(name)) {
    return (
      <span className="inline-flex items-center gap-1 rounded-full bg-coral-soft px-2 py-0.5 text-xs font-medium text-coral-ink">
        <CircleAlert className="size-3" aria-hidden="true" />
        Check this
      </span>
    )
  }
  if (fromResume.includes(name)) {
    return (
      <span className="inline-flex items-center gap-1 rounded-full bg-mist px-2 py-0.5 text-xs text-navy">
        <FileText className="size-3" aria-hidden="true" />
        From your resume
      </span>
    )
  }
  return null
}

/** A step's heading and intro. */
export function StepHeader({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="flex flex-col gap-1">
      <h2 className="text-2xl">{title}</h2>
      {children && <p>{children}</p>}
    </div>
  )
}

/** Card-style radio options bound to a string field. */
export function ChoiceCards({
  name,
  options,
  columns = 3,
}: {
  name: ProfileField
  options: { value: string; label: string; text?: string }[]
  columns?: 2 | 3 | 4
}) {
  const { register } = useFormContext<ProfileFormValues>()
  return (
    <div
      className={cn(
        "grid gap-3",
        columns === 2 && "sm:grid-cols-2",
        columns === 3 && "sm:grid-cols-3",
        columns === 4 && "grid-cols-2 sm:grid-cols-4",
      )}
    >
      {options.map((option) => (
        <label
          key={option.value}
          className="flex cursor-pointer flex-col gap-1 rounded-card border bg-card p-4 transition-colors hover:border-teal has-checked:border-teal has-checked:bg-mist has-focus-visible:ring-3 has-focus-visible:ring-ring/50"
        >
          <input type="radio" value={option.value} className="sr-only" {...register(name)} />
          <span className="font-medium text-navy">{option.label}</span>
          {option.text && <span className="text-sm">{option.text}</span>}
        </label>
      ))}
    </div>
  )
}

export const inputClass = "h-10 bg-card text-base sm:text-sm"
export const selectClass = "w-full [&_select]:h-10 [&_select]:bg-card [&_select]:text-base sm:[&_select]:text-sm"
