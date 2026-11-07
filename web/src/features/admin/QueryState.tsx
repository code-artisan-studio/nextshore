import type { UseQueryResult } from "@tanstack/react-query"
import type { ReactNode } from "react"

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"

/** Loading / error / data for one admin query. */
export function QueryState<T>({ query, children }: { query: UseQueryResult<T>; children: (data: T) => ReactNode }) {
  if (query.isPending) return <p className="text-sm">Loading…</p>
  if (query.isError) {
    return (
      <Alert>
        <AlertTitle>Couldn't load this</AlertTitle>
        <AlertDescription>{query.error.message}</AlertDescription>
      </Alert>
    )
  }
  return <>{children(query.data)}</>
}
