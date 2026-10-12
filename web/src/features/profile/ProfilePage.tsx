// Placeholder until T28 (Profile stepper).
import { useProfileDraft } from "./draft"

export function ProfilePage() {
  const draft = useProfileDraft()
  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-3xl">Your profile</h1>
      <pre className="overflow-auto rounded-card border bg-card p-4 text-sm">{JSON.stringify(draft, null, 2)}</pre>
    </div>
  )
}
