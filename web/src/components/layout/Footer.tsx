import { Disclaimer } from "./Disclaimer"

const year = new Date().getFullYear()

export function Footer() {
  return (
    <footer className="border-t bg-mist">
      <div className="mx-auto flex max-w-6xl flex-col gap-2 px-4 py-6">
        <Disclaimer />
        <p className="text-sm text-slate">
          © {year} NextShore · Find your next country.
        </p>
      </div>
    </footer>
  )
}
