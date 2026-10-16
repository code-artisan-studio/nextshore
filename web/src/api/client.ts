/** Base URL of the NextShore API. Set `VITE_API_URL` per environment; the default is the local dev API. */
export const API_URL = (import.meta.env.VITE_API_URL ?? "http://localhost:5111").replace(/\/$/, "")

/** An RFC 7807 ProblemDetails response (or a network failure, with status 0). */
export class ApiError extends Error {
  readonly status: number
  readonly title: string
  readonly detail: string | undefined
  /** Validation errors keyed by camelCase JSON path, e.g. `english.scores.listening`. */
  readonly errors: Record<string, string[]>

  constructor(status: number, title: string, detail?: string, errors: Record<string, string[]> = {}) {
    super(detail ?? title)
    this.name = "ApiError"
    this.status = status
    this.title = title
    this.detail = detail
    this.errors = errors
  }
}

interface Problem {
  title?: string | null
  detail?: string | null
  errors?: Record<string, string[]>
}

/** Fetches `/api/v1{path}` and returns the parsed JSON body, or throws `ApiError`. */
export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${API_URL}/api/v1${path}`, {
      ...init,
      headers: { Accept: "application/json", ...init?.headers },
    })
  } catch (cause) {
    if (cause instanceof DOMException && cause.name === "AbortError") throw cause
    throw new ApiError(0, "Can't reach NextShore", "Check your connection and try again.")
  }

  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as Problem
    throw new ApiError(
      response.status,
      problem.title ?? response.statusText,
      problem.detail ?? undefined,
      problem.errors ?? {},
    )
  }

  return (await response.json()) as T
}

/** Sends `body` as JSON. */
export function jsonBody(body: unknown, method = "POST"): RequestInit {
  return { method, body: JSON.stringify(body), headers: { "Content-Type": "application/json" } }
}
