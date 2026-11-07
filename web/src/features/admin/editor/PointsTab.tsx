import { zodResolver } from "@hookform/resolvers/zod"
import { useState } from "react"
import { useForm } from "react-hook-form"
import { z } from "zod"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { NativeSelect } from "@/components/ui/native-select"

import { useSaveRow } from "../api"
import { StaleFlag, StatusBadge, UnverifiedFlag } from "../badges"
import { AdminField, SourceFields } from "../form"
import {
  applyServerErrors,
  fromNumber,
  optionalInt,
  optionalNumber,
  sourceSchema,
  stillUnverified,
  toNumber,
} from "../formValues"
import { variants, verifiableFields, type AdminPathway, type AdminPointsFactor } from "../types"
import { RowFlags, RowSheet } from "./RowSheet"

function bracket(f: AdminPointsFactor) {
  if (f.matchValue) return `= ${f.matchValue}`
  if (f.min === null && f.max === null) return "any"
  return `${f.min ?? "…"} – ${f.max ?? "…"}`
}

export function PointsTab({ pathway }: { pathway: AdminPathway }) {
  const [editing, setEditing] = useState<AdminPointsFactor | "new" | null>(null)
  const save = useSaveRow(pathway.id, "points-factors")

  return (
    <div className="flex flex-col gap-4">
      {!pathway.isPointsBased && pathway.pointsFactors.length === 0 && (
        <p className="text-sm">This pathway isn't points-based (see Details), so points factors are ignored.</p>
      )}
      <div>
        <Button onClick={() => setEditing("new")}>Add points factor</Button>
      </div>
      <div className="overflow-x-auto rounded-card border border-line bg-white">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-line bg-sand text-navy">
            <tr>
              <th className="px-4 py-3 font-medium">Factor</th>
              <th className="px-4 py-3 font-medium">Variant</th>
              <th className="px-4 py-3 font-medium">Bracket / match</th>
              <th className="px-4 py-3 font-medium">Points</th>
              <th className="px-4 py-3 font-medium">Cap</th>
              <th className="px-4 py-3 font-medium">Flags</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody>
            {pathway.pointsFactors.map((f) => (
              <tr key={f.id} className="border-b border-line align-top last:border-0">
                <td className="px-4 py-3">
                  <code className="text-xs">{f.factor}</code>
                </td>
                <td className="px-4 py-3">{f.variant}</td>
                <td className="px-4 py-3">{bracket(f)}</td>
                <td className="px-4 py-3">{f.points ?? "—"}</td>
                <td className="px-4 py-3">{f.maxPointsForFactor ?? "—"}</td>
                <td className="px-4 py-3">
                  <RowFlags>
                    {f.status === "draft" && pathway.status === "published" && <StatusBadge status="draft" />}
                    <UnverifiedFlag fields={f.unverifiedFields} verifiedAt={f.verifiedAt} />
                    {f.stale && <StaleFlag />}
                  </RowFlags>
                </td>
                <td className="px-4 py-3">
                  <div className="flex justify-end gap-1">
                    <Button size="sm" variant="outline" onClick={() => setEditing(f)}>
                      Edit
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      disabled={save.isPending}
                      onClick={() => {
                        if (window.confirm(`Delete this ${f.factor} row from the draft?`)) save.mutate({ rowId: f.id, body: null })
                      }}
                    >
                      Delete
                    </Button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {save.isError && <p className="text-sm text-not-ink">{save.error.message}</p>}
      <RowSheet
        open={editing !== null}
        onOpenChange={(open) => !open && setEditing(null)}
        title={editing === "new" ? "Add points factor" : "Edit points factor"}
      >
        {editing !== null && (
          <PointsFactorForm
            key={editing === "new" ? "new" : editing.id}
            pathway={pathway}
            row={editing === "new" ? null : editing}
            onDone={() => setEditing(null)}
          />
        )}
      </RowSheet>
    </div>
  )
}

const schema = z.object({
  factor: z.string().trim().min(1, "Required"),
  variant: z.enum(variants),
  min: optionalNumber,
  max: optionalNumber,
  matchValue: z.string(),
  points: optionalInt,
  maxPointsForFactor: optionalInt,
  ...sourceSchema,
})
type Values = z.infer<typeof schema>
const fields = Object.keys(schema.shape)

function PointsFactorForm({ pathway, row, onDone }: { pathway: AdminPathway; row: AdminPointsFactor | null; onDone: () => void }) {
  const save = useSaveRow(pathway.id, "points-factors")
  const [formError, setFormError] = useState<string | null>(null)
  const { control, register, handleSubmit, setError, formState } = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      factor: row?.factor ?? "",
      variant: (row?.variant ?? "default") as Values["variant"],
      min: fromNumber(row?.min),
      max: fromNumber(row?.max),
      matchValue: row?.matchValue ?? "",
      points: fromNumber(row?.points),
      maxPointsForFactor: fromNumber(row?.maxPointsForFactor),
      sourceUrl: row?.sourceUrl ?? pathway.sourceUrl,
      verifiedAt: row?.verifiedAt ?? "",
      unverifiedFields: row?.unverifiedFields ?? [],
    },
  })

  const onSubmit = handleSubmit((v) => {
    setFormError(null)
    const filled = {
      min: toNumber(v.min),
      max: toNumber(v.max),
      match_value: v.matchValue.trim() || null,
      points: toNumber(v.points),
      max_points_for_factor: toNumber(v.maxPointsForFactor),
    }
    save.mutate(
      {
        rowId: row?.id,
        body: {
          factor: v.factor,
          variant: v.variant,
          min: filled.min,
          max: filled.max,
          matchValue: filled.match_value,
          points: filled.points,
          maxPointsForFactor: filled.max_points_for_factor,
          sourceUrl: v.sourceUrl,
          verifiedAt: v.verifiedAt || null,
          unverifiedFields: stillUnverified(v.unverifiedFields, filled),
        },
      },
      { onSuccess: onDone, onError: (e) => setFormError(applyServerErrors(e, setError, fields)) },
    )
  })

  const err = (name: keyof Values) => formState.errors[name]?.message

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-4" noValidate>
      <div className="grid gap-4 sm:grid-cols-2">
        <AdminField id="factor" label="Profile field" error={err("factor")}>
          <Input id="factor" placeholder="age" {...register("factor")} />
        </AdminField>
        <AdminField id="variant" label="Variant" error={err("variant")}>
          <NativeSelect id="variant" className="w-full" {...register("variant")}>
            {variants.map((v) => (
              <option key={v} value={v}>
                {v}
              </option>
            ))}
          </NativeSelect>
        </AdminField>
        <AdminField id="min" label="Min (inclusive)" error={err("min")}>
          <Input id="min" inputMode="decimal" {...register("min")} />
        </AdminField>
        <AdminField id="max" label="Max (inclusive)" error={err("max")}>
          <Input id="max" inputMode="decimal" {...register("max")} />
        </AdminField>
        <AdminField id="matchValue" label="Match value" hint="For categorical factors, e.g. masters." error={err("matchValue")}>
          <Input id="matchValue" {...register("matchValue")} />
        </AdminField>
        <AdminField id="points" label="Points" error={err("points")}>
          <Input id="points" inputMode="numeric" {...register("points")} />
        </AdminField>
        <AdminField id="maxPointsForFactor" label="Cap for this factor" error={err("maxPointsForFactor")}>
          <Input id="maxPointsForFactor" inputMode="numeric" {...register("maxPointsForFactor")} />
        </AdminField>
      </div>
      <SourceFields control={control} verifiable={verifiableFields.pointsFactor} />
      <div className="flex items-center gap-3">
        <Button type="submit" disabled={save.isPending}>
          {save.isPending ? "Saving…" : row ? "Save points factor" : "Add points factor"}
        </Button>
        {formError && <span className="text-sm text-not-ink">{formError}</span>}
      </div>
    </form>
  )
}
