import { QueryClient } from "@tanstack/react-query"

import { ApiError } from "./client"

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // 4xx answers won't change on retry; network errors, 5xx and 429 get one more try.
      retry: (failures, error) =>
        failures < 1 && !(error instanceof ApiError && error.status >= 400 && error.status < 500 && error.status !== 429),
      refetchOnWindowFocus: false,
    },
  },
})
