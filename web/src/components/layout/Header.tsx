import { Link, useLocation } from "react-router"

import { Button } from "@/components/ui/button"

import { Logo } from "./Logo"

export function Header() {
  // The CTA starts the flow; once in it (upload, profile, results) it would only restart it.
  const onLanding = useLocation().pathname === "/"

  return (
    <header className="border-b bg-white">
      <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-4">
        <Link
          to="/"
          className="flex items-center gap-2 font-heading text-lg font-semibold text-navy"
        >
          <Logo />
          NextShore
        </Link>
        {onLanding && (
          <Button asChild size="lg">
            <Link to="/start">Check my options</Link>
          </Button>
        )}
      </div>
    </header>
  )
}
