import { ArrowRight, FileUp, ListChecks, Route, ShieldCheck } from "lucide-react"
import { Link } from "react-router"

import { Button } from "@/components/ui/button"

const steps = [
  {
    icon: FileUp,
    title: "Upload",
    text: "Add your resume (PDF or Word). We read it once and never store the file.",
  },
  {
    icon: ListChecks,
    title: "Confirm",
    text: "Check what we found and answer a few questions about your goal, languages and funds.",
  },
  {
    icon: Route,
    title: "See your paths",
    text: "Get a Readiness Score for each visa pathway, what's missing, and how study leads to work and residence.",
  },
]

// Flags are emoji so no colours live outside the theme tokens.
const countries = [
  { flag: "🇨🇦", name: "Canada", pathways: ["Study Permit", "Post-Graduation Work Permit", "Express Entry"] },
  { flag: "🇩🇪", name: "Germany", pathways: ["Student Visa", "Job-seeker permit", "EU Blue Card", "Opportunity Card"] },
  { flag: "🇦🇺", name: "Australia", pathways: ["Student visa (500)", "Temporary Graduate (485)", "Skilled Independent (189)"] },
]

export function LandingPage() {
  return (
    <div className="flex flex-col gap-16">
      <section className="flex flex-col items-start gap-6 rounded-card bg-mist px-6 py-12 sm:px-10 sm:py-16">
        <p className="text-sm font-medium tracking-wide text-teal uppercase">For students and software developers</p>
        <h1 className="max-w-2xl text-4xl leading-tight sm:text-5xl">Find your next country.</h1>
        <p className="max-w-xl text-lg">
          Upload your resume and see which visa pathways in Canada, Germany and Australia fit you, how ready you are
          for each, and what to work on next.
        </p>
        <div className="flex flex-wrap items-center gap-3">
          <Button asChild size="lg" className="h-11 px-5 text-base">
            <Link to="/start">
              Check my options
              <ArrowRight data-icon="inline-end" />
            </Link>
          </Button>
          <Button asChild variant="link" size="lg" className="text-base">
            <Link to="/profile">Or fill in your details manually</Link>
          </Button>
        </div>
        <p className="flex items-center gap-2 text-sm">
          <ShieldCheck className="size-4 text-teal" aria-hidden="true" />
          Free, no account. Your resume file is never stored.
        </p>
      </section>

      <section aria-labelledby="how-it-works" className="flex flex-col gap-6">
        <h2 id="how-it-works" className="text-2xl">
          How it works
        </h2>
        <ol className="grid gap-4 sm:grid-cols-3">
          {steps.map((step, index) => (
            <li key={step.title} className="flex flex-col gap-3 rounded-card border bg-card p-6">
              <div className="flex items-center gap-3">
                <span className="flex size-10 items-center justify-center rounded-full bg-mist text-teal">
                  <step.icon className="size-5" aria-hidden="true" />
                </span>
                <span className="text-sm font-medium text-teal">Step {index + 1}</span>
              </div>
              <h3 className="text-lg">{step.title}</h3>
              <p>{step.text}</p>
            </li>
          ))}
        </ol>
      </section>

      <section aria-labelledby="countries" className="flex flex-col gap-6">
        <div className="flex flex-col gap-2">
          <h2 id="countries" className="text-2xl">
            3 countries, 10 pathways
          </h2>
          <p>From your first study visa to permanent residence. Every rule links to its official source.</p>
        </div>
        <ul className="grid gap-4 sm:grid-cols-3">
          {countries.map((country) => (
            <li key={country.name} className="flex flex-col gap-3 rounded-card border bg-card p-6">
              <h3 className="flex items-center gap-3 text-lg">
                <span className="text-3xl leading-none" aria-hidden="true">
                  {country.flag}
                </span>
                {country.name}
              </h3>
              <ul className="flex flex-wrap gap-2">
                {country.pathways.map((pathway) => (
                  <li key={pathway} className="rounded-full bg-mist px-3 py-1 text-sm text-navy">
                    {pathway}
                  </li>
                ))}
              </ul>
            </li>
          ))}
        </ul>
      </section>

      <section className="flex flex-col items-start gap-4 rounded-card border bg-card p-6 sm:flex-row sm:items-center sm:justify-between sm:p-8">
        <div className="flex flex-col gap-1">
          <h2 className="text-xl">Ready to see where you stand?</h2>
          <p>It takes about two minutes. Results are saved for 30 days so you can share the link.</p>
        </div>
        <Button asChild size="lg" className="h-11 px-5 text-base">
          <Link to="/start">Check my options</Link>
        </Button>
      </section>
    </div>
  )
}
