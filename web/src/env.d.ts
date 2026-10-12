interface ImportMetaEnv {
  /** NextShore API origin, without `/api/v1`. Defaults to http://localhost:5111. */
  readonly VITE_API_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
