import { zodResolver } from "@hookform/resolvers/zod"
import { useState } from "react"
import { Controller, useForm } from "react-hook-form"
import { z } from "zod"

import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import { Input } from "@/components/ui/input"
import { NativeSelect } from "@/components/ui/native-select"

import { useSaveRow } from "../api"
import { StaleFlag, StatusBadge, UnverifiedFlag } from "../badges"
import { AdminField, SourceFields } from "../form"
import { applyServerErrors, fromNumber, optionalInt, sourceSchema, stillUnverified, toNumber } from "../formValues"
import { fixabilities, operators, verifiableFields, type AdminPathway, type AdminRequirement, type Json } from "../types"
import { RowFlags, RowSheet } from "./RowSheet"

export function RequirementsTab({ pathway }: { pathway: AdminPathway }) {
  const [editing, setEditing] = useState<AdminRequirement | "new" | null>(null)
  const save = useSaveRow(pathway.id, "requirements")

  return (
    <div className="flex flex-col gap-4">
      <div>
        <Button onClick={() => setEditing("new")}>Add requirement</Button>
      </div>
      <div className="overflow-x-auto rounded-card border border-line bg-white">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-line bg-sand text-navy">
            <tr>
              <th className="px-4 py-3 font-medium">Requirement</th>
              <th className="px-4 py-3 font-medium">Rule</th>
              <th className="px-4 py-3 font-medium">Kind</th>
              <th className="px-4 py-3 font-medium">Flags</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody>
            {pathway.requirements.map((r) => (
              <tr key={r.id} className="border-b border-line align-top last:border-0">
                <td className="px-4 py-3 font-medium text-navy">{r.label}</td>
                <td className="px-4 py-3">
                  <code className="text-xs">
                    {r.field} {r.operator} {valueText(r.value)} {r.unit ?? ""}
                  </code>
                </td>
                <td className="px-4 py-3">
                  {r.isMandatory ? "Mandatory" : "Advisory"} · {r.fixability}
                </td>
                <td className="px-4 py-3">
                  <RowFlags>
                    {r.status === "draft" && pathway.status === "published" && <StatusBadge status="draft" />}
                    <UnverifiedFlag fields={r.unverifiedFields} verifiedAt={r.verifiedAt} />
                    {r.stale && <StaleFlag />}
                  </RowFlags>
                </td>
                <td className="px-4 py-3">
                  <div className="flex justify-end gap-1">
                    <Button size="sm" variant="outline" onClick={() => setEditing(r)}>
                      Edit
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      disabled={save.isPending}
                      onClick={() => {
                        if (window.confirm(`Delete "${r.label}" from the draft?`)) save.mutate({ rowId: r.id, body: null })
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
        title={editing === "new" ? "Add requirement" : "Edit requirement"}
      >
        {editing !== null && (
          <RequirementForm
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

/** JSON values shown and typed as text: numbers and lists as JSON, plain text as itself. */
function valueText(value: Json): string {
  if (value === null) return ""
  return typeof value === "string" ? value : JSON.stringify(value)
}

function parseValue(text: string): Json {
  const t = text.trim()
  if (t === "") return null
  try {
    return JSON.parse(t) as Json
  } catch {
    return t
  }
}

const schema = z.object({
  label: z.string().trim().min(1, "Required"),
  field: z.string().trim().min(1, "Required"),
  operator: z.enum(operators),
  value: z.string(),
  unit: z.string(),
  isMandatory: z.boolean(),
  fixability: z.enum(fixabilities),
  fixHint: z.string(),
  fixMonths: optionalInt,
  ...sourceSchema,
})
type Values = z.infer<typeof schema>
const fields = Object.keys(schema.shape)

function RequirementForm({ pathway, row, onDone }: { pathway: AdminPathway; row: AdminRequirement | null; onDone: () => void }) {
  const save = useSaveRow(pathway.id, "requirements")
  const [formError, setFormError] = useState<string | null>(null)
  const { control, register, handleSubmit, setError, formState } = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      label: row?.label ?? "",
      field: row?.field ?? "",
      operator: (row?.operator ?? "gte") as Values["operator"],
      value: valueText(row?.value ?? null),
      unit: row?.unit ?? "",
      isMandatory: row?.isMandatory ?? true,
      fixability: (row?.fixability ?? "medium") as Values["fixability"],
      fixHint: row?.fixHint ?? "",
      fixMonths: fromNumber(row?.fixMonths),
      sourceUrl: row?.sourceUrl ?? pathway.sourceUrl,
      verifiedAt: row?.verifiedAt ?? "",
      unverifiedFields: row?.unverifiedFields ?? [],
    },
  })

  const onSubmit = handleSubmit((v) => {
    setFormError(null)
    const value = parseValue(v.value)
    save.mutate(
      {
        rowId: row?.id,
        body: {
          label: v.label,
          field: v.field,
          operator: v.operator,
          value,
          unit: v.unit.trim() || null,
          isMandatory: v.isMandatory,
          fixability: v.fixability,
          fixHint: v.fixHint,
          fixMonths: toNumber(v.fixMonths),
          sourceUrl: v.sourceUrl,
          verifiedAt: v.verifiedAt || null,
          unverifiedFields: stillUnverified(v.unverifiedFields, { value }),
        },
      },
      { onSuccess: onDone, onError: (e) => setFormError(applyServerErrors(e, setError, fields)) },
    )
  })

  const err = (name: keyof Values) => formState.errors[name]?.message

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-4" noValidate>
      <AdminField id="label" label="Label" hint="Shown to users, e.g. “English CLB 7 in all skills”." error={err("label")}>
        <Input id="label" {...register("label")} />
      </AdminField>
      <div className="grid gap-4 sm:grid-cols-3">
        <AdminField id="field" label="Profile field" error={err("field")} className="sm:col-span-3">
          <Input id="field" placeholder="english.level.CLB" {...register("field")} />
        </AdminField>
        <AdminField id="operator" label="Operator" error={err("operator")}>
          <NativeSelect id="operator" className="w-full" {...register("operator")}>
            {operators.map((o) => (
              <option key={o} value={o}>
                {o}
              </option>
            ))}
          </NativeSelect>
        </AdminField>
        <AdminField id="value" label="Value" error={err("value")}>
          <Input id="value" placeholder='7 or ["bachelors","masters"]' {...register("value")} />
        </AdminField>
        <AdminField id="unit" label="Unit" error={err("unit")}>
          <Input id="unit" placeholder="CLB, years, EUR" {...register("unit")} />
        </AdminField>
      </div>
      <div className="grid gap-4 sm:grid-cols-3">
        <Controller
          control={control}
          name="isMandatory"
          render={({ field }) => (
            <AdminField id="isMandatory" label="Kind">
              <label className="flex items-center gap-2 text-sm">
                <Checkbox id="isMandatory" checked={field.value} onCheckedChange={(on) => field.onChange(on === true)} />
                Mandatory
              </label>
            </AdminField>
          )}
        />
        <AdminField id="fixability" label="Fixability" error={err("fixability")}>
          <NativeSelect id="fixability" className="w-full" {...register("fixability")}>
            {fixabilities.map((f) => (
              <option key={f} value={f}>
                {f}
              </option>
            ))}
          </NativeSelect>
        </AdminField>
        <AdminField id="fixMonths" label="Fix effort (months)" error={err("fixMonths")}>
          <Input id="fixMonths" inputMode="numeric" {...register("fixMonths")} />
        </AdminField>
      </div>
      <AdminField id="fixHint" label="Fix hint" error={err("fixHint")}>
        <Input id="fixHint" placeholder="Retake IELTS" {...register("fixHint")} />
      </AdminField>
      <SourceFields control={control} verifiable={verifiableFields.requirement} />
      <div className="flex items-center gap-3">
        <Button type="submit" disabled={save.isPending}>
          {save.isPending ? "Saving…" : row ? "Save requirement" : "Add requirement"}
        </Button>
        {formError && <span className="text-sm text-not-ink">{formError}</span>}
      </div>
    </form>
  )
}
