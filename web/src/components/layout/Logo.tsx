import { cn } from "@/lib/utils"

/** Horizon line with a half-sun rising. */
export function Logo({ className }: { className?: string }) {
  return (
    <svg
      viewBox="0 0 32 32"
      aria-hidden="true"
      className={cn("size-8 shrink-0", className)}
    >
      <path d="M8 20a8 8 0 0 1 16 0Z" className="fill-coral" />
      <path
        d="M3 20h26M8 25h16"
        className="stroke-navy"
        strokeWidth={2.5}
        strokeLinecap="round"
      />
    </svg>
  )
}
