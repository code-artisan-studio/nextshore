import { SignOutButton } from "@clerk/react"
import { useQuery } from "@tanstack/react-query"

import { Button } from "@/components/ui/button"

import { useAdminFetch } from "./useAdminApi"

interface AdminMe {
  userId: string
  role: string
}

export function AdminHome() {
  const adminFetch = useAdminFetch()
  const me = useQuery({ queryKey: ["admin", "me"], queryFn: () => adminFetch<AdminMe>("/me") })

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h1 className="font-heading text-2xl font-semibold text-navy">Admin</h1>
        <SignOutButton>
          <Button variant="outline">Sign out</Button>
        </SignOutButton>
      </div>
      {me.isPending && <p className="text-muted-foreground">Checking access…</p>}
      {me.isError && <p className="text-destructive">The API refused this session: {me.error.message}</p>}
      {me.data && <p>Signed in as {me.data.userId} ({me.data.role}).</p>}
    </div>
  )
}
