import { Outlet } from "react-router"

import { Footer } from "./Footer"
import { Header } from "./Header"

export function AppLayout() {
  return (
    <div className="flex min-h-dvh flex-col">
      <Header />
      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-10">
        <Outlet />
      </main>
      <Footer />
    </div>
  )
}
