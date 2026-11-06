import { SignOutButton } from "@clerk/react"
import { NavLink, Outlet } from "react-router"

import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

const tab = ({ isActive }: { isActive: boolean }) =>
  cn("rounded-lg px-3 py-1.5 text-sm font-medium", isActive ? "bg-mist text-navy" : "text-slate hover:text-navy")

export function AdminLayout() {
  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-4">
          <h1 className="font-heading text-2xl font-semibold text-navy">Admin</h1>
          <nav className="flex gap-1" aria-label="Admin">
            <NavLink to="/admin" end className={tab}>
              Pathways
            </NavLink>
            <NavLink to="/admin/stale" className={tab}>
              Stale rules
            </NavLink>
          </nav>
        </div>
        <SignOutButton>
          <Button variant="outline">Sign out</Button>
        </SignOutButton>
      </div>
      <Outlet />
    </div>
  )
}
