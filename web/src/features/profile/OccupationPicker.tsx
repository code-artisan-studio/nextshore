import { useId, useState, type KeyboardEvent } from "react"
import { LoaderCircle, X } from "lucide-react"
import { useController } from "react-hook-form"

import { useOccupations } from "@/api/hooks"
import type { OccupationSuggestion, OccupationSystem } from "@/api/types"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useDebounced } from "@/lib/useDebounced"
import { cn } from "@/lib/utils"

import { inputClass, ResumeTag } from "./fields"
import type { ProfileFormValues } from "./schema"

interface Option {
  code: string
  title: string
}

/** Type-ahead over published occupation codes for one system, with the resume's other suggestions as shortcuts. */
export function OccupationPicker({
  system,
  label,
  suggestions,
}: {
  system: OccupationSystem
  label: string
  suggestions: OccupationSuggestion[]
}) {
  const { field } = useController<ProfileFormValues, `occupation.${OccupationSystem}`>({ name: `occupation.${system}` })
  const [query, setQuery] = useState("")
  const [open, setOpen] = useState(false)
  const [active, setActive] = useState(0)
  const debounced = useDebounced(query, 250)
  const results = useOccupations(open ? debounced : "", system)
  const id = useId()
  const listId = `${id}-list`

  const typed = query.trim()
  const options: (Option & { asTyped?: boolean })[] = [
    ...(results.data ?? []),
    // Codes not in the published list can still be entered; the engine treats them as "not on the list".
    ...(/^\d{3,8}$/.test(typed) && !results.data?.some((o) => o.code === typed)
      ? [{ code: typed, title: "Use this code as entered", asTyped: true }]
      : []),
  ]

  const select = (option: Option & { asTyped?: boolean }) => {
    field.onChange({ code: option.code, title: option.asTyped ? "" : option.title })
    setQuery("")
    setOpen(false)
  }

  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === "ArrowDown") {
      event.preventDefault()
      setOpen(true)
      setActive((i) => Math.min(i + 1, options.length - 1))
    } else if (event.key === "ArrowUp") {
      event.preventDefault()
      setActive((i) => Math.max(i - 1, 0))
    } else if (event.key === "Enter" && open && options[active]) {
      event.preventDefault()
      select(options[active])
    } else if (event.key === "Escape") {
      setOpen(false)
    }
  }

  const alternatives = suggestions.filter((s) => s.system === system && s.code !== field.value.code)

  return (
    <div className="flex flex-col gap-1.5">
      <div className="flex flex-wrap items-center gap-2">
        <Label htmlFor={id} className="text-navy">
          {label}
        </Label>
        <ResumeTag name={field.name} />
      </div>

      {field.value.code ? (
        <div className="flex min-h-10 items-center gap-2 rounded-lg border bg-card px-3 py-1.5">
          <span className="font-medium text-navy tabular-nums">{field.value.code}</span>
          <span className="min-w-0 flex-1 truncate text-sm">{field.value.title}</span>
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            aria-label={`Clear ${label}`}
            onClick={() => field.onChange({ code: "", title: "" })}
          >
            <X />
          </Button>
        </div>
      ) : (
        <div className="relative">
          <Input
            id={id}
            role="combobox"
            aria-expanded={open && typed.length > 0}
            aria-controls={listId}
            aria-autocomplete="list"
            aria-activedescendant={open && options[active] ? `${listId}-${active}` : undefined}
            autoComplete="off"
            placeholder="Search by job title or code"
            className={inputClass}
            value={query}
            onChange={(event) => {
              setQuery(event.target.value)
              setActive(0)
              setOpen(true)
            }}
            onFocus={() => setOpen(true)}
            onBlur={() => setTimeout(() => setOpen(false), 150)}
            onKeyDown={onKeyDown}
          />
          {results.isFetching && (
            <LoaderCircle className="absolute top-3 right-3 size-4 animate-spin text-teal" aria-hidden="true" />
          )}
          {open && typed.length > 0 && (
            <ul
              id={listId}
              role="listbox"
              className="absolute z-10 mt-1 max-h-64 w-full overflow-auto rounded-lg border bg-card py-1 shadow-md"
            >
              {options.map((option, index) => (
                <li
                  key={`${option.code}-${option.asTyped ? "typed" : "listed"}`}
                  id={`${listId}-${index}`}
                  role="option"
                  aria-selected={index === active}
                  className={cn("flex cursor-pointer gap-3 px-3 py-2 text-sm", index === active && "bg-mist")}
                  onMouseEnter={() => setActive(index)}
                  onMouseDown={(event) => {
                    event.preventDefault()
                    select(option)
                  }}
                >
                  <span className="font-medium text-navy tabular-nums">{option.code}</span>
                  <span className={cn(option.asTyped && "italic")}>{option.title}</span>
                </li>
              ))}
              {options.length === 0 && !results.isFetching && debounced === query && (
                <li className="px-3 py-2 text-sm">No matching occupations. You can type the code instead.</li>
              )}
            </ul>
          )}
        </div>
      )}

      {alternatives.length > 0 && (
        <div className="flex flex-wrap items-center gap-2 text-sm">
          <span>Also suggested:</span>
          {alternatives.map((s) => (
            <button
              key={s.code}
              type="button"
              className="rounded-full border bg-card px-2.5 py-0.5 hover:border-teal"
              onClick={() => select(s)}
            >
              {s.code} · {s.title}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
