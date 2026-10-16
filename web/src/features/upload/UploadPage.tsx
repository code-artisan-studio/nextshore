import { useEffect, useId, useRef, useState, type DragEvent } from "react"
import { AlertCircle, Check, FileText, FileUp, LoaderCircle, X } from "lucide-react"
import { Link, useNavigate } from "react-router"

import { useParseResume } from "@/api/hooks"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import { Label } from "@/components/ui/label"
import { setProfileDraft } from "@/features/profile/draft"
import { cn } from "@/lib/utils"

import { acceptedResumeTypes, checkResumeFile, uploadErrorMessage } from "./resumeFile"

const loadingSteps = ["Reading your resume…", "Finding your experience…", "Matching your occupation…"]
const stepIntervalMs = 3500

export function UploadPage() {
  const navigate = useNavigate()
  const parse = useParseResume()
  const inputRef = useRef<HTMLInputElement>(null)
  const abortRef = useRef<AbortController | null>(null)
  const consentId = useId()

  const [file, setFile] = useState<File | null>(null)
  const [fileError, setFileError] = useState<string | null>(null)
  const [consent, setConsent] = useState(false)
  const [dragging, setDragging] = useState(false)

  // Abort an upload still running when the user leaves the page.
  useEffect(() => () => abortRef.current?.abort(), [])

  function choose(next: File | undefined) {
    if (!next) return
    parse.reset()
    setFile(next)
    setFileError(checkResumeFile(next))
  }

  function onDrop(event: DragEvent) {
    event.preventDefault()
    setDragging(false)
    choose(event.dataTransfer.files[0])
  }

  function analyse() {
    if (!file || fileError || !consent) return
    const controller = new AbortController()
    abortRef.current = controller
    parse.mutate(
      { file, signal: controller.signal },
      {
        onSuccess: (extracted) => {
          setProfileDraft({ source: "resume", extracted })
          navigate("/profile")
        },
      },
    )
  }

  function cancel() {
    abortRef.current?.abort()
    parse.reset()
  }

  function skip() {
    setProfileDraft({ source: "manual", extracted: null })
    navigate("/profile")
  }

  if (parse.isPending) {
    return <Analysing onCancel={cancel} />
  }

  const cancelled = parse.error instanceof DOMException && parse.error.name === "AbortError"
  const error = fileError ?? (parse.isError && !cancelled ? uploadErrorMessage(parse.error) : null)

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-8">
      <div className="flex flex-col gap-2">
        <h1 className="text-3xl">Upload your resume</h1>
        <p>
          We'll pull out your education, work experience and languages so you don't have to type them. You can check
          and change everything on the next step.
        </p>
      </div>

      <div
        onDragOver={(event) => {
          event.preventDefault()
          setDragging(true)
        }}
        onDragLeave={() => setDragging(false)}
        onDrop={onDrop}
        className={cn(
          "flex flex-col items-center gap-4 rounded-card border-2 border-dashed bg-card px-6 py-10 text-center transition-colors",
          dragging ? "border-teal bg-mist" : "border-line",
        )}
      >
        <input
          ref={inputRef}
          type="file"
          accept={Object.entries(acceptedResumeTypes).flat().join(",")}
          className="sr-only"
          tabIndex={-1}
          aria-hidden="true"
          onChange={(event) => {
            choose(event.target.files?.[0])
            event.target.value = ""
          }}
        />
        {file ? (
          <div className="flex w-full max-w-md items-center gap-3 rounded-lg border bg-sand px-4 py-3 text-left">
            <FileText className="size-6 shrink-0 text-teal" aria-hidden="true" />
            <div className="min-w-0 flex-1">
              <p className="truncate font-medium text-navy">{file.name}</p>
              <p className="text-sm">{formatSize(file.size)}</p>
            </div>
            <Button
              variant="ghost"
              size="icon-sm"
              aria-label="Remove file"
              onClick={() => {
                setFile(null)
                setFileError(null)
                parse.reset()
              }}
            >
              <X />
            </Button>
          </div>
        ) : (
          <>
            <span className="flex size-12 items-center justify-center rounded-full bg-mist text-teal">
              <FileUp className="size-6" aria-hidden="true" />
            </span>
            <div className="flex flex-col gap-1">
              <p className="font-medium text-navy">Drag your resume here</p>
              <p className="text-sm">PDF or Word (.docx), up to 5 MB</p>
            </div>
          </>
        )}
        <Button variant="outline" onClick={() => inputRef.current?.click()}>
          {file ? "Choose another file" : "Choose a file"}
        </Button>
      </div>

      {error && (
        <Alert variant="destructive">
          <AlertCircle />
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      <div className="flex items-start gap-3">
        <Checkbox
          id={consentId}
          checked={consent}
          onCheckedChange={(checked) => setConsent(checked === true)}
          className="mt-0.5 bg-card"
        />
        <Label htmlFor={consentId} className="block text-sm leading-relaxed font-normal">
          I agree that NextShore sends my resume to an AI service (Anthropic's Claude) to extract my profile. The file
          and its text are used once and never stored.
        </Label>
      </div>

      <div className="flex flex-col-reverse gap-3 sm:flex-row sm:items-center sm:justify-between">
        <Button variant="link" className="self-start px-0" onClick={skip}>
          Skip — fill in manually
        </Button>
        <Button size="lg" className="h-11 px-5 text-base" disabled={!file || !!fileError || !consent} onClick={analyse}>
          Analyse resume
        </Button>
      </div>

      <p className="text-sm">
        Don't have your resume handy?{" "}
        <Link to="/" className="text-teal underline-offset-4 hover:underline">
          Read how NextShore works
        </Link>
        .
      </p>
    </div>
  )
}

function Analysing({ onCancel }: { onCancel: () => void }) {
  const [step, setStep] = useState(0)

  // The API gives no progress events; advance the copy on a timer and hold on the last step until it answers.
  useEffect(() => {
    const timer = setInterval(() => setStep((s) => Math.min(s + 1, loadingSteps.length - 1)), stepIntervalMs)
    return () => clearInterval(timer)
  }, [])

  return (
    <div className="mx-auto flex max-w-md flex-col items-center gap-8 py-12 text-center" aria-live="polite">
      <span className="flex size-14 items-center justify-center rounded-full bg-mist text-teal">
        <LoaderCircle className="size-7 animate-spin" aria-hidden="true" />
      </span>
      <h1 className="text-2xl">Analysing your resume</h1>
      <ol className="flex w-full flex-col gap-3 text-left">
        {loadingSteps.map((label, index) => (
          <li
            key={label}
            className={cn("flex items-center gap-3", index > step && "opacity-50")}
            aria-current={index === step ? "step" : undefined}
          >
            {index < step ? (
              <Check className="size-4 text-teal" aria-hidden="true" />
            ) : index === step ? (
              <LoaderCircle className="size-4 animate-spin text-teal" aria-hidden="true" />
            ) : (
              <span className="size-4" aria-hidden="true" />
            )}
            <span className={cn(index === step && "font-medium text-navy")}>{label}</span>
          </li>
        ))}
      </ol>
      <p className="text-sm">This usually takes 10–20 seconds.</p>
      <Button variant="outline" onClick={onCancel}>
        Cancel
      </Button>
    </div>
  )
}

function formatSize(bytes: number) {
  return bytes < 1024 * 1024 ? `${Math.max(1, Math.round(bytes / 1024))} KB` : `${(bytes / 1024 / 1024).toFixed(1)} MB`
}
