import { createBrowserRouter } from "react-router"

import { AppLayout } from "@/components/layout/AppLayout"
import { LandingPage } from "@/features/landing/LandingPage"
import { ProfilePage } from "@/features/profile/ProfilePage"
import { PathwayDetailPage } from "@/features/results/PathwayDetailPage"
import { ResultsPage } from "@/features/results/ResultsPage"
import { UploadPage } from "@/features/upload/UploadPage"

export const router = createBrowserRouter([
  {
    element: <AppLayout />,
    children: [
      { index: true, element: <LandingPage /> },
      { path: "start", element: <UploadPage /> },
      { path: "profile", element: <ProfilePage /> },
      { path: "results/:id", element: <ResultsPage /> },
      { path: "results/:id/:pathwaySlug", element: <PathwayDetailPage /> },
    ],
  },
])
