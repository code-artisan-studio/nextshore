import { cn } from "@/lib/utils"

import { countryOf } from "../format"

/** Emoji flag, so no colours live outside the theme tokens. Decorative: the country name is always shown too. */
export function CountryFlag({ code, className }: { code: string; className?: string }) {
  return (
    <span aria-hidden="true" className={cn("text-xl leading-none", className)}>
      {countryOf(code).flag}
    </span>
  )
}
