interface ImportMetaEnv {
  /** NextShore API origin, without `/api/v1`. Defaults to http://localhost:5111. */
  readonly VITE_API_URL?: string
  /** Clerk publishable key. Only the admin area uses it; without it /admin explains that sign-in isn't set up. */
  readonly VITE_CLERK_PUBLISHABLE_KEY?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}

/** Custom Clerk session-token claims (Clerk dashboard → Sessions → customise session token). */
interface CustomJwtSessionClaims {
  /** `{{user.public_metadata.role}}`; `admin` unlocks /admin. */
  role?: string
}
