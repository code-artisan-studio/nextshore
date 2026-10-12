import { createContext } from "react"

/**
 * Which fields came from the resume, which of those the user should check (confidence < 0.7), and which they have
 * since edited (no tag: the value is theirs now).
 */
export const ResumeMarksContext = createContext<{ fromResume: string[]; checkThis: string[]; edited: string[] }>({
  fromResume: [],
  checkThis: [],
  edited: [],
})
