import type { FieldValues, Path, UseFormSetError } from "react-hook-form"
import { z } from "zod"

import { ApiError } from "@/api/client"

/** Today as yyyy-MM-dd in the admin's time zone. */
export function today() {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`
}

export const optionalNumber = z
  .string()
  .trim()
  .refine((v) => v === "" || Number.isFinite(Number(v)), "Must be a number")

export const optionalInt = z
  .string()
  .trim()
  .refine((v) => v === "" || Number.isInteger(Number(v)), "Must be a whole number")

export const sourceSchema = {
  sourceUrl: z.url({ protocol: /^https?$/, message: "Absolute http(s) URL" }),
  verifiedAt: z.string(),
  unverifiedFields: z.array(z.string()),
}

/** "" → null, else the number. */
export function toNumber(v: string): number | null {
  return v.trim() === "" ? null : Number(v)
}

export function fromNumber(v: number | null | undefined): string {
  return v === null || v === undefined ? "" : String(v)
}

/** Entering a value is verifying it: drop TODO_VERIFY marks for fields that now have one. */
export function stillUnverified(marked: string[], filled: Record<string, unknown>): string[] {
  return marked.filter((f) => filled[f] === null || filled[f] === undefined || filled[f] === "")
}

/** Puts API validation errors (camelCase keys, `field[0]` for list items) on the form; returns the rest. */
export function applyServerErrors<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
  fields: readonly string[],
  aliases: Record<string, string> = {},
): string | null {
  if (!(error instanceof ApiError)) return "Something went wrong. Try again."
  const unmatched: string[] = []
  for (const [key, messages] of Object.entries(error.errors)) {
    const base = key.replace(/\[\d+\]$/, "")
    const name = aliases[base] ?? base
    if (fields.includes(name)) setError(name as Path<T>, { message: messages.join(" ") })
    else unmatched.push(...messages)
  }
  return unmatched.length > 0 || Object.keys(error.errors).length === 0
    ? [error.detail ?? error.title, ...unmatched].join(" ")
    : null
}
