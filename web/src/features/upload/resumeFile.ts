import { ApiError } from "@/api/client"

/** Same limit as the API (`ResumeFile.MaxBytes`). */
export const maxResumeBytes = 5 * 1024 * 1024

export const acceptedResumeTypes = {
  "application/pdf": ".pdf",
  "application/vnd.openxmlformats-officedocument.wordprocessingml.document": ".docx",
} as const

/** Quick checks before upload; the API re-checks the file's actual content. Returns an error message or null. */
export function checkResumeFile(file: File): string | null {
  const name = file.name.toLowerCase()
  if (!name.endsWith(".pdf") && !name.endsWith(".docx")) {
    return "Choose a PDF or Word (.docx) file."
  }
  if (file.size === 0) {
    return "This file is empty."
  }
  if (file.size > maxResumeBytes) {
    return "This file is over 5 MB. Try a smaller export of your resume."
  }
  return null
}

/** Turns a failed `/resumes/parse` call into a message the user can act on. */
export function uploadErrorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return "Something went wrong. Try again, or fill in your details manually."
  }
  switch (error.status) {
    case 0:
      return "We can't reach NextShore right now. Check your connection and try again."
    case 400:
      return error.errors.file?.[0] ?? error.errors.consent?.[0] ?? error.detail ?? "This file can't be used."
    case 413:
      return "This file is over 5 MB. Try a smaller export of your resume."
    case 422:
      return "We couldn't read this resume. Try a PDF with selectable text, or fill in your details manually."
    case 429:
      return "You've reached today's limit of 10 resume uploads. You can still fill in your details manually."
    default:
      return "Our resume reader isn't available right now. Try again in a minute, or fill in your details manually."
  }
}
