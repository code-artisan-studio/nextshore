import { createBrowserRouter } from "react-router"

import { AppLayout } from "@/components/layout/AppLayout"
import { LandingPage } from "@/features/landing/LandingPage"
import { ProfilePage } from "@/features/profile/ProfilePage"
import { UploadPage } from "@/features/upload/UploadPage"

export const router = createBrowserRouter([
  {
    element: <AppLayout />,
    children: [
      { index: true, element: <LandingPage /> },
      { path: "start", element: <UploadPage /> },
      { path: "profile", element: <ProfilePage /> },
    ],
  },
])
