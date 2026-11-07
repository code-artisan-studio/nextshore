import { zodResolver } from "@hookform/resolvers/zod"
import { Controller, useForm } from "react-hook-form"
import { useState } from "react"
import { z } from "zod"

import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import { Input } from "@/components/ui/input"
import { NativeSelect } from "@/components/ui/native-select"

import { useAdminPathways, useUpdatePathway } from "../api"
import { AdminField, SourceFields } from "../form"
import {
  applyServerErrors,
  fromNumber,
  optionalInt,
  sourceSchema,
  stillUnverified,
  toNumber,
} from "../formValues"
import { audiences, pathwayTypes, verifiableFields, type AdminPathway } from "../types"

const schema = z.object({
  name: z.string().trim().min(1, "Required"),
  type: z.enum(pathwayTypes),
  audience: z.array(z.enum(audiences)).min(1, "Pick at least one"),
  isPointsBased: z.boolean(),
  passMark: optionalInt,
  cutoff: optionalInt,
  cutoffAsOf: z.string(),
  nextPathwayId: z.string(),
  typicalDurationMonths: optionalInt,
  summary: z.string().trim().min(1, "Required"),
  ...sourceSchema,
})
type Values = z.infer<typeof schema>
const fields = Object.keys(schema.shape)

function toValues(p: AdminPathway): Values {
  return {
    name: p.name,
    type: p.type as Values["type"],
    audience: p.audience as Values["audience"],
    isPointsBased: p.isPointsBased,
    passMark: fromNumber(p.passMark),
    cutoff: fromNumber(p.cutoff),
    cutoffAsOf: p.cutoffAsOf ?? "",
    nextPathwayId: p.nextPathwayId ?? "",
    typicalDurationMonths: fromNumber(p.typicalDurationMonths),
    summary: p.summary,
    sourceUrl: p.sourceUrl,
    verifiedAt: p.verifiedAt ?? "",
    unverifiedFields: p.unverifiedFields,
  }
}

export function DetailsTab({ pathway }: { pathway: AdminPathway }) {
  const update = useUpdatePathway(pathway.id)
  const others = useAdminPathways().data?.filter((p) => p.id !== pathway.id) ?? []
  const [formError, setFormError] = useState<string | null>(null)
  const { control, register, handleSubmit, setError, formState } = useForm<Values>({
    resolver: zodResolver(schema),
    values: toValues(pathway),
  })

  const onSubmit = handleSubmit((v) => {
    setFormError(null)
    const filled = {
      pass_mark: toNumber(v.passMark),
      cutoff: toNumber(v.cutoff),
      cutoff_as_of: v.cutoffAsOf || null,
      typical_duration_months: toNumber(v.typicalDurationMonths),
    }
    update.mutate(
      {
        name: v.name,
        type: v.type,
        audience: v.audience,
        isPointsBased: v.isPointsBased,
        passMark: filled.pass_mark,
        cutoff: filled.cutoff,
        cutoffAsOf: filled.cutoff_as_of,
        nextPathwayId: v.nextPathwayId || null,
        typicalDurationMonths: filled.typical_duration_months,
        summary: v.summary,
        sourceUrl: v.sourceUrl,
        verifiedAt: v.verifiedAt || null,
        unverifiedFields: stillUnverified(v.unverifiedFields, filled),
      },
      { onError: (e) => setFormError(applyServerErrors(e, setError, fields)) },
    )
  })

  const err = (name: keyof Values) => formState.errors[name]?.message

  return (
    <form onSubmit={onSubmit} className="flex max-w-3xl flex-col gap-5" noValidate>
      <div className="grid gap-4 sm:grid-cols-2">
        <AdminField id="name" label="Name" error={err("name")}>
          <Input id="name" {...register("name")} />
        </AdminField>
        <AdminField id="type" label="Type" error={err("type")}>
          <NativeSelect id="type" className="w-full" {...register("type")}>
            {pathwayTypes.map((t) => (
              <option key={t} value={t}>
                {t}
              </option>
            ))}
          </NativeSelect>
        </AdminField>
        <Controller
          control={control}
          name="audience"
          render={({ field, fieldState }) => (
            <AdminField id="audience" label="Audience" error={fieldState.error?.message}>
              <div id="audience" className="flex gap-4">
                {audiences.map((a) => (
                  <label key={a} className="flex items-center gap-2 text-sm">
                    <Checkbox
                      checked={field.value.includes(a)}
                      onCheckedChange={(on) => field.onChange(on ? [...field.value, a] : field.value.filter((x) => x !== a))}
                    />
                    {a}
                  </label>
                ))}
              </div>
            </AdminField>
          )}
        />
        <Controller
          control={control}
          name="isPointsBased"
          render={({ field }) => (
            <AdminField id="isPointsBased" label="Scoring">
              <label className="flex items-center gap-2 text-sm">
                <Checkbox id="isPointsBased" checked={field.value} onCheckedChange={(on) => field.onChange(on === true)} />
                Points-based
              </label>
            </AdminField>
          )}
        />
        <AdminField id="passMark" label="Pass mark" hint="Fixed minimum points, if any." error={err("passMark")}>
          <Input id="passMark" inputMode="numeric" {...register("passMark")} />
        </AdminField>
        <AdminField id="cutoff" label="Latest cutoff" error={err("cutoff")}>
          <Input id="cutoff" inputMode="numeric" {...register("cutoff")} />
        </AdminField>
        <AdminField id="cutoffAsOf" label="Cutoff as of" error={err("cutoffAsOf")}>
          <Input id="cutoffAsOf" type="date" className="w-44" {...register("cutoffAsOf")} />
        </AdminField>
        <AdminField id="typicalDurationMonths" label="Typical duration (months)" error={err("typicalDurationMonths")}>
          <Input id="typicalDurationMonths" inputMode="numeric" {...register("typicalDurationMonths")} />
        </AdminField>
        <AdminField id="nextPathwayId" label="Next pathway in chain" error={err("nextPathwayId")} className="sm:col-span-2">
          <NativeSelect id="nextPathwayId" className="w-full" {...register("nextPathwayId")}>
            <option value="">None (end of chain)</option>
            {others.map((p) => (
              <option key={p.id} value={p.id}>
                {p.countryCode} · {p.name}
              </option>
            ))}
          </NativeSelect>
        </AdminField>
        <AdminField id="summary" label="Summary" error={err("summary")} className="sm:col-span-2">
          <textarea
            id="summary"
            rows={3}
            className="w-full rounded-lg border border-input bg-transparent px-2.5 py-2 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
            {...register("summary")}
          />
        </AdminField>
      </div>
      <SourceFields control={control} verifiable={verifiableFields.pathway} />
      <div className="flex items-center gap-3">
        <Button type="submit" disabled={update.isPending || !formState.isDirty}>
          {update.isPending ? "Saving…" : "Save details"}
        </Button>
        {update.isSuccess && !formState.isDirty && <span className="text-sm">Saved to draft.</span>}
        {formError && <span className="text-sm text-not-ink">{formError}</span>}
      </div>
    </form>
  )
}
