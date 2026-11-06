import type { ReactNode } from "react"
import { Controller, type Control, type FieldValues, type Path } from "react-hook-form"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { cn } from "@/lib/utils"

import { today } from "./formValues"

export function AdminField({
  id,
  label,
  error,
  hint,
  className,
  children,
}: {
  id: string
  label: ReactNode
  error?: string
  hint?: ReactNode
  className?: string
  children: ReactNode
}) {
  return (
    <div className={cn("flex flex-col gap-1.5", className)}>
      <Label htmlFor={id} className="text-navy">
        {label}
      </Label>
      {children}
      {hint && <p className="text-xs">{hint}</p>}
      {error && <p className="text-sm text-not-ink">{error}</p>}
    </div>
  )
}

/** Source URL, verified date (with "Today") and the TODO_VERIFY marks of one rule row. */
export function SourceFields<T extends FieldValues>({
  control,
  verifiable,
}: {
  control: Control<T>
  verifiable: readonly string[]
}) {
  return (
    <fieldset className="flex flex-col gap-4 rounded-card border border-line p-4">
      <legend className="px-1 text-sm font-medium text-navy">Source</legend>
      <Controller
        control={control}
        name={"sourceUrl" as Path<T>}
        render={({ field, fieldState }) => (
          <AdminField id="sourceUrl" label="Official source URL" error={fieldState.error?.message}>
            <Input id="sourceUrl" type="url" {...field} aria-invalid={fieldState.invalid} />
          </AdminField>
        )}
      />
      <Controller
        control={control}
        name={"verifiedAt" as Path<T>}
        render={({ field, fieldState }) => (
          <AdminField
            id="verifiedAt"
            label="Verified on"
            hint="The date you checked these values against the source."
            error={fieldState.error?.message}
          >
            <div className="flex gap-2">
              <Input id="verifiedAt" type="date" max={today()} className="w-44" {...field} aria-invalid={fieldState.invalid} />
              <Button type="button" variant="outline" onClick={() => field.onChange(today())}>
                Today
              </Button>
            </div>
          </AdminField>
        )}
      />
      {verifiable.length > 0 && (
        <Controller
          control={control}
          name={"unverifiedFields" as Path<T>}
          render={({ field, fieldState }) => {
            const marked = field.value as string[]
            return (
              <div className="flex flex-col gap-2">
                <span className="text-sm font-medium text-navy">Still TODO_VERIFY</span>
                <div className="flex flex-wrap gap-x-4 gap-y-2">
                  {verifiable.map((name) => (
                    <label key={name} className="flex items-center gap-2 text-sm">
                      <Checkbox
                        checked={marked.includes(name)}
                        onCheckedChange={(on) =>
                          field.onChange(on ? [...marked, name] : marked.filter((f) => f !== name))
                        }
                      />
                      <code>{name}</code>
                    </label>
                  ))}
                </div>
                <p className="text-xs">Entering a value clears its mark when you save.</p>
                {fieldState.error && <p className="text-sm text-not-ink">{fieldState.error.message}</p>}
              </div>
            )
          }}
        />
      )}
    </fieldset>
  )
}
