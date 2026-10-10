import { createBrowserRouter } from "react-router"

import { AppLayout } from "@/components/layout/AppLayout"
import { LandingPage } from "@/features/landing/LandingPage"

export const router = createBrowserRouter([
  {
    element: <AppLayout />,
    children: [{ index: true, element: <LandingPage /> }],
  },
])
