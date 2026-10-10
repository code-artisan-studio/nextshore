import { cn } from "@/lib/utils"

export function Disclaimer({ className }: { className?: string }) {
  return (
    <p className={cn("text-sm text-slate", className)}>
      NextShore provides general information, not immigration advice. Rules
      change — always confirm with the official source or a licensed
      immigration adviser.
    </p>
  )
}
