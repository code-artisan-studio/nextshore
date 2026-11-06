import { ClerkProvider, SignIn, SignOutButton, useAuth } from "@clerk/react"

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Button } from "@/components/ui/button"

import { AdminHome } from "./AdminHome"

const publishableKey = import.meta.env.VITE_CLERK_PUBLISHABLE_KEY

/** `/admin/*`. Loaded lazily, so Clerk never ships with the public pages. */
export function AdminRoot() {
  if (!publishableKey) {
    return (
      <Alert>
        <AlertTitle>Admin sign-in isn't set up</AlertTitle>
        <AlertDescription>Set VITE_CLERK_PUBLISHABLE_KEY and rebuild the web app.</AlertDescription>
      </Alert>
    )
  }

  return (
    <ClerkProvider publishableKey={publishableKey} afterSignOutUrl="/admin">
      <AdminGate />
    </ClerkProvider>
  )
}

function AdminGate() {
  const { isLoaded, isSignedIn, sessionClaims } = useAuth()

  if (!isLoaded) return <p className="text-muted-foreground">Loading…</p>

  if (!isSignedIn) {
    return (
      <div className="flex justify-center">
        <SignIn routing="hash" />
      </div>
    )
  }

  // The API enforces the same claim; this only avoids showing screens that would all answer 403.
  if (sessionClaims?.role !== "admin") {
    return (
      <Alert>
        <AlertTitle>No admin access</AlertTitle>
        <AlertDescription className="flex flex-col items-start gap-3">
          This account isn't an admin. Ask an existing admin to set your role.
          <SignOutButton>
            <Button variant="outline">Sign out</Button>
          </SignOutButton>
        </AlertDescription>
      </Alert>
    )
  }

  return <AdminHome />
}
