import { useState } from "react"
import { Check, Link2 } from "lucide-react"

import { Button } from "@/components/ui/button"

import { formatDate } from "../format"

/** docs/UI.md → "Copy link" to share results (they expire after 30 days). */
export function CopyLinkButton({ path, expiresAt }: { path: string; expiresAt: string }) {
  const [copied, setCopied] = useState(false)

  async function copy() {
    try {
      await navigator.clipboard.writeText(`${window.location.origin}${path}`)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch {
      window.prompt("Copy this link:", `${window.location.origin}${path}`)
    }
  }

  return (
    <div className="flex flex-col items-start gap-1 sm:items-end">
      <Button variant="outline" size="lg" className="h-9" onClick={copy}>
        {copied ? <Check data-icon="inline-start" /> : <Link2 data-icon="inline-start" />}
        {copied ? "Link copied" : "Copy link"}
      </Button>
      <p className="text-xs" aria-live="polite">
        Link works until {formatDate(expiresAt)}
      </p>
    </div>
  )
}
